using System;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using RealPlayTester.Core;

namespace RealPlayTester.Input.Internal
{
    /// <summary>
    /// Internal simulator that performs the actual queuing of events into the Input System.
    /// </summary>
    internal static class InputSimulator
    {
        private static object _keyboardState;
        private static object _lastMouseState;
        private static object _lastGamepadState;

        private static FieldInfo _mousePositionField;
        private static FieldInfo _mouseButtonsField;
        private static FieldInfo _gamepadButtonsField;
        private static FieldInfo _gamepadLeftStickField;
        private static FieldInfo _gamepadRightStickField;

        private static float _physicsAccumulator = 0f;
        private static int _lastUpdateFrame = -1;
        private static int _eventsThisFrame = 0;

        private static bool ShouldThrottle()
        {
            if (Time.frameCount != _lastUpdateFrame)
            {
                _lastUpdateFrame = Time.frameCount;
                _eventsThisFrame = 0;
            }

            int limit = RealPlaySettings.Input.MaxEventsPerFrame;
            if (limit <= 0) return false;

            if (_eventsThisFrame >= limit)
            {
                if (_eventsThisFrame == limit)
                {
                    RealPlayLog.Warn($"[INPUT] Throttling triggered: Exceeded {limit} events this frame. Dropping excessive inputs to prevent overflow.");
                }
                _eventsThisFrame++;
                return true;
            }

            _eventsThisFrame++;
            return false;
        }

        private static void EnsureKeyboardState()
        {
            if (_keyboardState == null && InputSystemReflector.Types.KeyboardState != null)
                _keyboardState = Activator.CreateInstance(InputSystemReflector.Types.KeyboardState);
        }

        public static void UpdateInput()
        {
            if (InputSystemReflector.Types.InputSystem == null) return;
            try
            {
                if (InputSystemReflector.UpdateMethod != null)
                {
                    InputSystemReflector.UpdateMethod.Invoke(null, new object[] { 1 }); 
                }
                else
                {
                    InputSystemReflector.UpdateMethodNoArgs?.Invoke(null, null);
                }

                if ((Application.isBatchMode || !Application.isPlaying) && Time.timeScale > 0)
                {
                    // M015: Use accumulator for deterministic physics in batchmode
                    _physicsAccumulator += Time.unscaledDeltaTime * Time.timeScale;
                    float step = Time.fixedDeltaTime;

                    while (_physicsAccumulator >= step)
                    {
                        if (RealPlaySettings.Physics.AutoSimulate3D)
                        {
                            try { if (Physics.simulationMode != SimulationMode.Script) Physics.simulationMode = SimulationMode.Script; } catch { }
                            Physics.Simulate(step);
                        }

                        if (RealPlaySettings.Physics.AutoSimulate2D)
                        {
                            try { if (Physics2D.simulationMode != SimulationMode2D.Script) Physics2D.simulationMode = SimulationMode2D.Script; } catch { }
                            Physics2D.Simulate(step);
                        }
                        _physicsAccumulator -= step;
                    }
                }
            }
            catch (Exception) { }
        }

        public static Task QueueKeyState(int inputSystemKeyValue, bool down)
        {
            if (ShouldThrottle()) return Task.CompletedTask;
            if (InputSystemReflector.Types.InputSystem == null || InputSystemReflector.QueueStateEventKeyboard == null) return Task.CompletedTask;
            object keyboard = VirtualDeviceManager.EnsureKeyboard();
            if (keyboard == null) return Task.CompletedTask;

            try
            {
                object keyEnum = Enum.ToObject(InputSystemReflector.Types.KeyEnum, inputSystemKeyValue);
                EnsureKeyboardState();
                InputSystemReflector.KeyboardStateSetMethod.Invoke(_keyboardState, new[] { keyEnum, (object)down });
                
                InvokeQueueMethod(InputSystemReflector.QueueStateEventKeyboard, keyboard, _keyboardState);
                UpdateInput();
            }
            catch (Exception ex) { RealPlayLog.Warn($"InputSimulator: QueueKeyState failed: {ex}"); }
            return Task.CompletedTask;
        }

        public static Task QueueMouseState(Vector2 position, int button, bool down, bool isMove)
        {
            if (ShouldThrottle()) return Task.CompletedTask;
            if (InputSystemReflector.Types.InputSystem == null || InputSystemReflector.QueueStateEventMouse == null) return Task.CompletedTask;
            object mouse = VirtualDeviceManager.EnsureMouse();
            if (mouse == null) return Task.CompletedTask;
            
            try
            {
                object state = _lastMouseState ?? Activator.CreateInstance(InputSystemReflector.Types.MouseState);
                ApplyMouseStateChanges(state, position, button, down, isMove);
                _lastMouseState = state;
                
                InvokeQueueMethod(InputSystemReflector.QueueStateEventMouse, mouse, state);
                if (Tester.Settings.EnableInputHeartbeat) UpdateInput();
            }
            catch (Exception ex) { RealPlayLog.Warn($"InputSimulator: QueueMouseState failed: {ex.Message}"); }
            return Task.CompletedTask;
        }

        public static Task QueueGamepadState(uint buttons, Vector2 leftStick, Vector2 rightStick)
        {
            if (ShouldThrottle()) return Task.CompletedTask;
            if (InputSystemReflector.Types.InputSystem == null || InputSystemReflector.QueueStateEventGamepad == null) return Task.CompletedTask;
            object gamepad = VirtualDeviceManager.EnsureGamepad();
            if (gamepad == null) return Task.CompletedTask;

            try
            {
                object state = _lastGamepadState ?? Activator.CreateInstance(InputSystemReflector.Types.GamepadState);
                ApplyGamepadStateChanges(state, buttons, leftStick, rightStick);
                _lastGamepadState = state;

                InvokeQueueMethod(InputSystemReflector.QueueStateEventGamepad, gamepad, state);
                if (Tester.Settings.EnableInputHeartbeat) UpdateInput();
            }
            catch (Exception ex) { RealPlayLog.Warn($"InputSimulator: QueueGamepadState failed: {ex.Message}"); }
            return Task.CompletedTask;
        }

        private static void ApplyMouseStateChanges(object state, Vector2 position, int button, bool down, bool isMove)
        {
            if (isMove)
            {
                if (_mousePositionField == null) _mousePositionField = InputSystemReflector.Types.MouseState.GetField("position");
                _mousePositionField?.SetValue(state, position);
            }

            if (button >= 0)
            {
                if (_mouseButtonsField == null) _mouseButtonsField = InputSystemReflector.Types.MouseState.GetField("buttons");
                if (_mouseButtonsField != null)
                {
                    ushort buttons = (ushort)_mouseButtonsField.GetValue(state);
                    if (down) buttons |= (ushort)(1 << button);
                    else buttons &= (ushort)~(1 << button);
                    _mouseButtonsField.SetValue(state, buttons);
                }
            }
        }

        private static void ApplyGamepadStateChanges(object state, uint buttons, Vector2 leftStick, Vector2 rightStick)
        {
            var type = InputSystemReflector.Types.GamepadState;
            if (_gamepadButtonsField == null) _gamepadButtonsField = type.GetField("buttons");
            if (_gamepadLeftStickField == null) _gamepadLeftStickField = type.GetField("leftStick");
            if (_gamepadRightStickField == null) _gamepadRightStickField = type.GetField("rightStick");

            _gamepadButtonsField?.SetValue(state, buttons);
            _gamepadLeftStickField?.SetValue(state, leftStick);
            _gamepadRightStickField?.SetValue(state, rightStick);
        }

        private static void InvokeQueueMethod(MethodInfo queueMethod, object device, object state)
        {
            if (queueMethod == null || device == null || state == null) return;

            // H052: Validate that the device layout matches the state type to prevent native corruption
            // The queueMethod is already specialized (Generic) for the state type.
            // We check if the device is compatible with that state type.
            var stateType = state.GetType();
            var deviceType = device.GetType();
            
            if (!IsDeviceCompatibleWithState(deviceType, stateType))
            {
                RealPlayLog.Error($"[INPUT] Layout Mismatch: Cannot queue {stateType.Name} for device {deviceType.Name}. Dropping event to prevent crash.");
                return;
            }

            var parameters = queueMethod.GetParameters();
            if (parameters.Length == 3)
                queueMethod.Invoke(null, new[] { device, state, -1.0 });
            else if (parameters.Length == 2)
                queueMethod.Invoke(null, new[] { device, state });
            else
                queueMethod.Invoke(null, new[] { device, state, -1.0, (object)null });
        }

        private static bool IsDeviceCompatibleWithState(Type deviceType, Type stateType)
        {
            if (stateType == InputSystemReflector.Types.KeyboardState)
                return InputSystemReflector.Types.Keyboard.IsAssignableFrom(deviceType);
            if (stateType == InputSystemReflector.Types.MouseState)
                return InputSystemReflector.Types.Mouse.IsAssignableFrom(deviceType);
            if (stateType == InputSystemReflector.Types.GamepadState)
                return InputSystemReflector.Types.Gamepad.IsAssignableFrom(deviceType);
            return true; // Unknown state types pass for now
        }

        public static void ClearMouseState() => _lastMouseState = null;
        public static void ClearGamepadState() => _lastGamepadState = null;
    }
}
