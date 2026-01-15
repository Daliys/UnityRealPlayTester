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
                    if (RealPlaySettings.AutoSimulatePhysics3D)
                    {
                        try { if (Physics.simulationMode != SimulationMode.Script) Physics.simulationMode = SimulationMode.Script; } catch { }
                        Physics.Simulate(Time.fixedDeltaTime * Time.timeScale);
                    }

                    if (RealPlaySettings.AutoSimulatePhysics2D)
                    {
                        try { if (Physics2D.simulationMode != SimulationMode2D.Script) Physics2D.simulationMode = SimulationMode2D.Script; } catch { }
                        Physics2D.Simulate(Time.fixedDeltaTime * Time.timeScale);
                    }
                }
            }
            catch (Exception) { }
        }

        public static Task QueueKeyState(int inputSystemKeyValue, bool down)
        {
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
                var posField = InputSystemReflector.Types.MouseState.GetField("position");
                posField?.SetValue(state, position);
            }

            if (button >= 0)
            {
                var buttonsField = InputSystemReflector.Types.MouseState.GetField("buttons");
                if (buttonsField != null)
                {
                    ushort buttons = (ushort)buttonsField.GetValue(state);
                    if (down) buttons |= (ushort)(1 << button);
                    else buttons &= (ushort)~(1 << button);
                    buttonsField.SetValue(state, buttons);
                }
            }
        }

        private static void ApplyGamepadStateChanges(object state, uint buttons, Vector2 leftStick, Vector2 rightStick)
        {
            var type = InputSystemReflector.Types.GamepadState;
            type.GetField("buttons")?.SetValue(state, buttons);
            type.GetField("leftStick")?.SetValue(state, leftStick);
            type.GetField("rightStick")?.SetValue(state, rightStick);
        }

        private static void InvokeQueueMethod(MethodInfo queueMethod, object device, object state)
        {
            if (queueMethod == null) return;
            var parameters = queueMethod.GetParameters();
            if (parameters.Length == 3)
                queueMethod.Invoke(null, new[] { device, state, -1.0 });
            else if (parameters.Length == 2)
                queueMethod.Invoke(null, new[] { device, state });
            else
                queueMethod.Invoke(null, new[] { device, state, -1.0, (object)null });
        }

        public static void ClearMouseState() => _lastMouseState = null;
        public static void ClearGamepadState() => _lastGamepadState = null;
    }
}