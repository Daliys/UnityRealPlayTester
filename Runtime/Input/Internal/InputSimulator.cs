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
                    // Using Dynamic update (1)
                    InputSystemReflector.UpdateMethod.Invoke(null, new object[] { 1 }); 
                }
                else
                {
                    InputSystemReflector.UpdateMethodNoArgs?.Invoke(null, null);
                }

                // Force physics simulation in batchmode to ensure movement happens during Waits
                if (Application.isBatchMode || !Application.isPlaying)
                {
                    if (RealPlaySettings.AutoSimulatePhysics3D)
                    {
                        try { if (Physics.simulationMode != SimulationMode.Script) Physics.simulationMode = SimulationMode.Script; } catch { }
                        Physics.Simulate(Time.fixedDeltaTime);
                    }

                    if (RealPlaySettings.AutoSimulatePhysics2D)
                    {
                        try { if (Physics2D.simulationMode != SimulationMode2D.Script) Physics2D.simulationMode = SimulationMode2D.Script; } catch { }
                        Physics2D.Simulate(Time.fixedDeltaTime);
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
                
                // Update persistent state
                InputSystemReflector.KeyboardStateSetMethod.Invoke(_keyboardState, new[] { keyEnum, (object)down });
                
                double time = -1.0;
                try 
                {
                    var timeProp = InputSystemReflector.Types.InputSystem.GetProperty("currentTime", BindingFlags.Public | BindingFlags.Static);
                    if (timeProp != null) time = (double)timeProp.GetValue(null);
                } 
                catch { }

                var parameters = InputSystemReflector.QueueStateEventKeyboard.GetParameters();
                if (parameters.Length == 3)
                    InputSystemReflector.QueueStateEventKeyboard.Invoke(null, new[] { keyboard, _keyboardState, time });
                else if (parameters.Length == 2)
                    InputSystemReflector.QueueStateEventKeyboard.Invoke(null, new[] { keyboard, _keyboardState });
                else
                    InputSystemReflector.QueueStateEventKeyboard.Invoke(null, new[] { keyboard, _keyboardState, time, (object)null });
                
                UpdateInput();
            }
            catch (Exception ex) 
            { 
                RealPlayLog.Warn($"InputSimulator: QueueKeyState failed: {ex}"); 
            }
            return Task.CompletedTask;
        }

        public static Task QueueMouseState(Vector2 position, int button, bool down, bool isMove)
        {
            if (InputSystemReflector.Types.InputSystem == null) return Task.CompletedTask;
            object mouse = VirtualDeviceManager.EnsureMouse();
            if (mouse == null) return Task.CompletedTask;
            
            try
            {
                var queueMethod = InputSystemReflector.GetQueueStateEventGeneric(InputSystemReflector.Types.MouseState);
                if (queueMethod == null) return Task.CompletedTask;

                object state = _lastMouseState ?? Activator.CreateInstance(InputSystemReflector.Types.MouseState);
                ApplyMouseStateChanges(state, position, button, down, isMove);
                _lastMouseState = state;
                
                InvokeQueueMethod(queueMethod, mouse, state);
                if (Tester.Settings.EnableInputHeartbeat) UpdateInput();
            }
            catch (Exception ex) { RealPlayLog.Warn($"InputSimulator: QueueMouseState failed: {ex.Message}"); }
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

        private static void InvokeQueueMethod(MethodInfo queueMethod, object device, object state)
        {
            var parameters = queueMethod.GetParameters();
            if (parameters.Length == 3)
                queueMethod.Invoke(null, new[] { device, state, -1.0 });
            else if (parameters.Length == 2)
                queueMethod.Invoke(null, new[] { device, state });
            else
                queueMethod.Invoke(null, new[] { device, state, -1.0, (object)null });
        }

        public static void ClearMouseState() => _lastMouseState = null;
    }
}
