using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using RealPlayTester.Core;

namespace RealPlayTester.Input
{
    /// <summary>
    /// Reflection-based shim for Unity's new Input System. Maps KeyCode to InputSystem.Key values.
    /// </summary>
    internal static class InputSystemShim
    {
        private static readonly Type KeyboardType = Type.GetType("UnityEngine.InputSystem.Keyboard, Unity.InputSystem");
        private static readonly Type KeyboardStateType = Type.GetType("UnityEngine.InputSystem.LowLevel.KeyboardState, Unity.InputSystem");
        private static readonly Type KeyEnumType = Type.GetType("UnityEngine.InputSystem.Key, Unity.InputSystem");
        private static readonly Type InputSystemType = Type.GetType("UnityEngine.InputSystem.InputSystem, Unity.InputSystem");
        private static readonly Type KeyControlType = Type.GetType("UnityEngine.InputSystem.Controls.KeyControl, Unity.InputSystem");

        private static readonly MethodInfo QueueStateEventGeneric = GetQueueStateEventGeneric();
        private static readonly MethodInfo KeyboardStateSetMethod = KeyboardStateType != null && KeyEnumType != null
            ? KeyboardStateType.GetMethod("Set", new[] { KeyEnumType, typeof(bool) })
            : null;

        private static readonly PropertyInfo KeyboardCurrentProperty = KeyboardType != null
            ? KeyboardType.GetProperty("current", BindingFlags.Public | BindingFlags.Static)
            : null;

        private static readonly PropertyInfo KeyboardItemProperty = KeyboardType != null && KeyEnumType != null
            ? KeyboardType.GetProperty("Item", new[] { KeyEnumType })
            : null;

        private static readonly PropertyInfo WasPressedThisFrameProperty = KeyControlType != null
            ? KeyControlType.GetProperty("wasPressedThisFrame", BindingFlags.Public | BindingFlags.Instance)
            : null;

        private static readonly HashSet<KeyCode> WarnedUnmappedKeys = new HashSet<KeyCode>();

        private static readonly Dictionary<KeyCode, int> KeyCodeToInputSystemKey = new Dictionary<KeyCode, int>
        {
            { KeyCode.A, 15 }, { KeyCode.B, 16 }, { KeyCode.C, 17 }, { KeyCode.D, 18 },
            { KeyCode.E, 19 }, { KeyCode.F, 20 }, { KeyCode.G, 21 }, { KeyCode.H, 22 },
            { KeyCode.I, 23 }, { KeyCode.J, 24 }, { KeyCode.K, 25 }, { KeyCode.L, 26 },
            { KeyCode.M, 27 }, { KeyCode.N, 28 }, { KeyCode.O, 29 }, { KeyCode.P, 30 },
            { KeyCode.Q, 31 }, { KeyCode.R, 32 }, { KeyCode.S, 33 }, { KeyCode.T, 34 },
            { KeyCode.U, 35 }, { KeyCode.V, 36 }, { KeyCode.W, 37 }, { KeyCode.X, 38 },
            { KeyCode.Y, 39 }, { KeyCode.Z, 40 },
            { KeyCode.Alpha0, 50 }, { KeyCode.Alpha1, 41 }, { KeyCode.Alpha2, 42 },
            { KeyCode.Alpha3, 43 }, { KeyCode.Alpha4, 44 }, { KeyCode.Alpha5, 45 },
            { KeyCode.Alpha6, 46 }, { KeyCode.Alpha7, 47 }, { KeyCode.Alpha8, 48 },
            { KeyCode.Alpha9, 49 },
            { KeyCode.F1, 92 }, { KeyCode.F2, 93 }, { KeyCode.F3, 94 }, { KeyCode.F4, 95 },
            { KeyCode.F5, 96 }, { KeyCode.F6, 97 }, { KeyCode.F7, 98 }, { KeyCode.F8, 99 },
            { KeyCode.F9, 100 }, { KeyCode.F10, 101 }, { KeyCode.F11, 102 }, { KeyCode.F12, 103 },
            { KeyCode.Space, 1 },
            { KeyCode.Return, 2 }, { KeyCode.KeypadEnter, 2 },
            { KeyCode.Tab, 3 },
            { KeyCode.Backspace, 5 },
            { KeyCode.Escape, 6 },
            { KeyCode.LeftShift, 7 }, { KeyCode.RightShift, 8 },
            { KeyCode.LeftAlt, 9 }, { KeyCode.RightAlt, 10 },
            { KeyCode.LeftControl, 11 }, { KeyCode.RightControl, 12 },
            { KeyCode.LeftCommand, 13 }, { KeyCode.RightCommand, 14 },
            { KeyCode.UpArrow, 63 }, { KeyCode.DownArrow, 64 },
            { KeyCode.LeftArrow, 65 }, { KeyCode.RightArrow, 66 },
            { KeyCode.Delete, 76 },
            { KeyCode.Home, 78 }, { KeyCode.End, 79 },
            { KeyCode.PageUp, 80 }, { KeyCode.PageDown, 81 },
            { KeyCode.Insert, 77 },
            { KeyCode.CapsLock, 4 },
            { KeyCode.Numlock, 83 }, { KeyCode.Print, 75 }, { KeyCode.ScrollLock, 84 }, { KeyCode.Pause, 85 },
            { KeyCode.Keypad0, 86 }, { KeyCode.Keypad1, 87 }, { KeyCode.Keypad2, 88 },
            { KeyCode.Keypad3, 89 }, { KeyCode.Keypad4, 90 }, { KeyCode.Keypad5, 91 },
            { KeyCode.Keypad6, 92 }, { KeyCode.Keypad7, 93 }, { KeyCode.Keypad8, 94 },
            { KeyCode.Keypad9, 95 }, { KeyCode.KeypadDivide, 96 }, { KeyCode.KeypadMultiply, 97 },
            { KeyCode.KeypadMinus, 98 }, { KeyCode.KeypadPlus, 99 }, { KeyCode.KeypadPeriod, 100 },
            { KeyCode.KeypadEquals, 101 },
            { KeyCode.Quote, 52 }, { KeyCode.BackQuote, 53 }, { KeyCode.Comma, 54 },
            { KeyCode.Period, 55 }, { KeyCode.Slash, 56 }, { KeyCode.Backslash, 57 },
            { KeyCode.Semicolon, 58 }, { KeyCode.LeftBracket, 59 }, { KeyCode.RightBracket, 60 },
            { KeyCode.Minus, 61 }, { KeyCode.Equals, 62 }
        };

        private static readonly MethodInfo AddDeviceMethod = InputSystemType != null
            ? InputSystemType.GetMethod("AddDevice", new[] { typeof(Type) })
            : null;

        internal static object _virtualKeyboard;
        internal static object _virtualMouse;

        public static bool IsAvailable
        {
            get { return KeyboardType != null && KeyboardStateType != null && KeyEnumType != null && InputSystemType != null && KeyboardCurrentProperty != null && QueueStateEventGeneric != null; }
        }

        public static void InitializeDevices()
        {
            if (InputSystemType == null) return;

            RealPlayLog.Info("InputSystemShim: Initializing virtual devices...");
            
            object keyboard = EnsureKeyboard();
            object mouse = EnsureMouse();

            if (keyboard != null) MakeCurrent(keyboard);
            if (mouse != null) MakeCurrent(mouse);
            
            RealPlayLog.Info($"InputSystemShim: Initialized devices - Keyboard: {(keyboard != null ? "Ready" : "Missing")}, Mouse: {(mouse != null ? "Ready" : "Missing")}");
        }

        private static void MakeCurrent(object device)
        {
            if (device == null) return;
            var method = device.GetType().GetMethod("MakeCurrent", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            if (method != null)
            {
                method.Invoke(device, null);
            }
            else
            {
                // Fallback for some versions where it might be on a base type
                var deviceType = Type.GetType("UnityEngine.InputSystem.InputDevice, Unity.InputSystem");
                if (deviceType != null)
                {
                    method = deviceType.GetMethod("MakeCurrent", BindingFlags.Public | BindingFlags.Instance);
                    method?.Invoke(device, null);
                }
            }
        }

        public static void UpdateInput()
        {
            if (InputSystemType == null) return;
            var method = InputSystemType.GetMethod("Update", BindingFlags.Public | BindingFlags.Static);
            method?.Invoke(null, null);
        }

        public static void KeyDown(KeyCode key)
        {
            QueueKeyState(key, true);
        }

        public static void KeyUp(KeyCode key)
        {
            QueueKeyState(key, false);
        }

        public static bool GetKeyDown(KeyCode key)
        {
            if (!IsAvailable)
            {
                return false;
            }

            object keyboard = GetKeyboard();
            if (keyboard == null || KeyboardItemProperty == null || WasPressedThisFrameProperty == null)
            {
                return false;
            }

            if (!TryMapKeyCode(key, out int inputSystemKeyValue))
            {
                WarnUnmapped(key);
                return false;
            }

            object keyEnum = Enum.ToObject(KeyEnumType, inputSystemKeyValue);
            object keyControl = KeyboardItemProperty.GetValue(keyboard, new[] { keyEnum });
            if (keyControl == null)
            {
                return false;
            }

            object wasPressed = WasPressedThisFrameProperty.GetValue(keyControl);
            return wasPressed is bool pressed && pressed;
        }

        private static object GetKeyboard()
        {
            // First check if already cached and still valid
            if (_virtualKeyboard != null && IsDeviceInSystem(_virtualKeyboard))
            {
                return _virtualKeyboard;
            }

            // Check if InputSystem.Keyboard.current is set
            if (KeyboardCurrentProperty != null)
            {
                object keyboard = KeyboardCurrentProperty.GetValue(null);
                if (keyboard != null) return keyboard;
            }

            // Aggressive discovery in all devices
            if (InputSystemType != null)
            {
                var devicesProp = InputSystemType.GetProperty("devices", BindingFlags.Public | BindingFlags.Static);
                if (devicesProp != null)
                {
                    var devices = devicesProp.GetValue(null) as IEnumerable;
                    if (devices != null)
                    {
                        foreach (var d in devices)
                        {
                            if (d != null && d.GetType().Name.Contains("Keyboard"))
                            {
                                return d;
                            }
                        }
                    }
                }
            }

            return null;
        }

        private static bool IsDeviceInSystem(object device)
        {
             if (device == null || InputSystemType == null) return false;
             var devicesProperty = InputSystemType.GetProperty("devices", BindingFlags.Public | BindingFlags.Static);
             if (devicesProperty == null) return false;
             var devices = devicesProperty.GetValue(null) as IEnumerable;
             if (devices == null) return false;
             foreach (var d in devices) if (d == device) return true;
             return false;
        }

        private static object EnsureKeyboard()
        {
            object keyboard = GetKeyboard();
            if (keyboard != null) 
            {
                _virtualKeyboard = keyboard;
                return keyboard;
            }

            if (InputSystemType != null)
            {
                try
                {
                    // Specifically look for the non-generic AddDevice(string) overload
                    MethodInfo addDeviceMethod = null;
                    foreach (var m in InputSystemType.GetMethods(BindingFlags.Public | BindingFlags.Static))
                    {
                        if (m.Name == "AddDevice" && !m.IsGenericMethodDefinition)
                        {
                            var parameters = m.GetParameters();
                            if (parameters.Length >= 1 && parameters[0].ParameterType == typeof(string))
                            {
                                addDeviceMethod = m;
                                break;
                            }
                        }
                    }

                    if (addDeviceMethod != null)
                    {
                        _virtualKeyboard = addDeviceMethod.Invoke(null, new object[] { "Keyboard", null, null });
                    }
                    else if (AddDeviceMethod != null && KeyboardType != null)
                    {
                        _virtualKeyboard = AddDeviceMethod.Invoke(null, new[] { KeyboardType });
                    }
                    
                    if (_virtualKeyboard != null)
                    {
                        RealPlayLog.Info("InputSystemShim: Created virtual Keyboard device.");
                        return _virtualKeyboard;
                    }
                }
                catch (Exception ex)
                {
                    RealPlayLog.Warn($"InputSystemShim: Failed to create virtual Keyboard: {ex.Message}");
                }
            }

            return null;
        }

        private static void QueueKeyState(KeyCode key, bool pressed)
        {
            if (!IsAvailable)
            {
                return;
            }

            if (!TryMapKeyCode(key, out int inputSystemKeyValue))
            {
                WarnUnmapped(key);
                return;
            }

            object keyboard = EnsureKeyboard();
            if (keyboard == null)
            {
                RealPlayLog.Warn("InputSystemShim: No keyboard available and failed to create virtual one. Key press ignored.");
                return;
            }

            object state = Activator.CreateInstance(KeyboardStateType);
            object keyValue = Enum.ToObject(KeyEnumType, inputSystemKeyValue);
            KeyboardStateSetMethod.Invoke(state, new[] { keyValue, pressed });
            QueueStateEventGeneric.Invoke(null, new[] { keyboard, state, -1d });
        }

        // ===== MOUSE SIMULATION SUPPORT =====
        private static readonly Type MouseType = Type.GetType("UnityEngine.InputSystem.Mouse, Unity.InputSystem");
        private static readonly Type MouseStateType = Type.GetType("UnityEngine.InputSystem.LowLevel.MouseState, Unity.InputSystem");
        private static readonly PropertyInfo MouseCurrentProperty = MouseType != null
            ? MouseType.GetProperty("current", BindingFlags.Public | BindingFlags.Static)
            : null;

        private static object _lastMouseState;
        private static MethodInfo _queueStateEventMouse;

        public static void MouseMove(Vector2 position)
        {
            if (!IsAvailable || MouseType == null) return;

            object mouse = EnsureMouse();
            if (mouse == null) return;

            // Use cached state or create fresh if none
            object state = _lastMouseState ?? Activator.CreateInstance(MouseStateType);
            
            // Set position (MouseState.position is Vector2)
            var posField = MouseStateType.GetField("position");
            if (posField != null) posField.SetValue(state, position);
            
            // Cache it
            _lastMouseState = state;
            
            // Queue Event
            if (_queueStateEventMouse == null) _queueStateEventMouse = GetQueueStateEventGeneric(MouseStateType);
            if (_queueStateEventMouse != null)
                _queueStateEventMouse.Invoke(null, new[] { mouse, state, -1d });
        }

        public static void MouseButton(int buttonId, bool pressed)
        {
             if (!IsAvailable || MouseType == null) return;
             
             object mouse = EnsureMouse();
             if (mouse == null) return;
             
             // Use cached state or create fresh if none
             object state = _lastMouseState ?? Activator.CreateInstance(MouseStateType);
             
             var buttonsField = MouseStateType.GetField("buttons");
             if (buttonsField != null)
             {
                 // Get current buttons from cached state to preserve others
                 // Assuming 'buttons' is a ushort bitmask
                 ushort currentButtons = (ushort)buttonsField.GetValue(state);
                 ushort mask = (ushort)(1 << buttonId);
                 
                 if (pressed)
                     currentButtons |= mask;
                 else
                     currentButtons &= (ushort)~mask;
                 
                 buttonsField.SetValue(state, currentButtons); 
             }
             
             // Cache it
             _lastMouseState = state;
             
             if (_queueStateEventMouse == null) _queueStateEventMouse = GetQueueStateEventGeneric(MouseStateType);
             if (_queueStateEventMouse != null)
                _queueStateEventMouse.Invoke(null, new[] { mouse, state, -1d });
        }
        
        private static object GetMouse()
        {
            // First check if already cached and still valid
            if (_virtualMouse != null && IsDeviceInSystem(_virtualMouse))
            {
                return _virtualMouse;
            }

            // Check if InputSystem.Mouse.current is set
            if (MouseCurrentProperty != null)
            {
                object mouse = MouseCurrentProperty.GetValue(null);
                if (mouse != null) return mouse;
            }

            // Aggressive discovery in all devices
            if (InputSystemType != null)
            {
                var devicesProp = InputSystemType.GetProperty("devices", BindingFlags.Public | BindingFlags.Static);
                if (devicesProp != null)
                {
                    var devices = devicesProp.GetValue(null) as IEnumerable;
                    if (devices != null)
                    {
                        foreach (var d in devices)
                        {
                            if (d != null && d.GetType().Name.Contains("Mouse"))
                            {
                                return d;
                            }
                        }
                    }
                }
            }

            return null;
        }

        private static object EnsureMouse()
        {
            object mouse = GetMouse();
            if (mouse != null) 
            {
                _virtualMouse = mouse;
                return mouse;
            }

            if (InputSystemType != null)
            {
                try
                {
                    // Specifically look for the non-generic AddDevice(string) overload
                    MethodInfo addDeviceMethod = null;
                    foreach (var m in InputSystemType.GetMethods(BindingFlags.Public | BindingFlags.Static))
                    {
                        if (m.Name == "AddDevice" && !m.IsGenericMethodDefinition)
                        {
                            var parameters = m.GetParameters();
                            if (parameters.Length >= 1 && parameters[0].ParameterType == typeof(string))
                            {
                                addDeviceMethod = m;
                                break;
                            }
                        }
                    }

                    if (addDeviceMethod != null)
                    {
                        _virtualMouse = addDeviceMethod.Invoke(null, new object[] { "Mouse", null, null });
                    }
                    else if (AddDeviceMethod != null && MouseType != null)
                    {
                        _virtualMouse = AddDeviceMethod.Invoke(null, new[] { MouseType });
                    }

                    if (_virtualMouse != null)
                    {
                        RealPlayLog.Info("InputSystemShim: Created virtual Mouse device.");
                        return _virtualMouse;
                    }
                }
                catch (Exception ex)
                {
                    RealPlayLog.Warn($"InputSystemShim: Failed to create virtual Mouse: {ex.Message}");
                }
            }
            return null;
        }

        private static MethodInfo GetQueueStateEventGeneric(Type stateType)
        {
            if (InputSystemType == null || stateType == null)
            {
                return null;
            }

            foreach (var method in InputSystemType.GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if (!method.IsGenericMethodDefinition) continue;
                if (method.Name != "QueueStateEvent") continue;

                var parameters = method.GetParameters();
                if (parameters.Length >= 2)
                {
                    return method.MakeGenericMethod(stateType);
                }
            }
            return null;
        }

        private static MethodInfo GetQueueStateEventGeneric() => GetQueueStateEventGeneric(KeyboardStateType);

        private static bool TryMapKeyCode(KeyCode key, out int inputSystemKeyValue)
        {
            return KeyCodeToInputSystemKey.TryGetValue(key, out inputSystemKeyValue);
        }

        private static void WarnUnmapped(KeyCode key)
        {
            if (WarnedUnmappedKeys.Add(key))
            {
                RealPlayLog.Warn($"KeyCode.{key} has no mapping to Input System Key enum.");
            }
        }
    }
}
