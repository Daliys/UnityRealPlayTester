using System;
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

        public static bool IsAvailable
        {
            get { return KeyboardType != null && KeyboardStateType != null && KeyEnumType != null && InputSystemType != null && KeyboardCurrentProperty != null && QueueStateEventGeneric != null; }
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

            object keyboard = KeyboardCurrentProperty.GetValue(null);
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

            object keyboard = KeyboardCurrentProperty.GetValue(null);
            if (keyboard == null)
            {
                return;
            }

            object state = Activator.CreateInstance(KeyboardStateType);
            object keyValue = Enum.ToObject(KeyEnumType, inputSystemKeyValue);
            KeyboardStateSetMethod.Invoke(state, new[] { keyValue, pressed });
            QueueStateEventGeneric.Invoke(null, new[] { keyboard, state, -1d });
        }

        private static MethodInfo GetQueueStateEventGeneric()
        {
            if (InputSystemType == null || KeyboardStateType == null)
            {
                return null;
            }

            foreach (var method in InputSystemType.GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if (!method.IsGenericMethodDefinition)
                {
                    continue;
                }

                if (method.Name != "QueueStateEvent")
                {
                    continue;
                }

                var parameters = method.GetParameters();
                if (parameters.Length >= 2)
                {
                    return method.MakeGenericMethod(KeyboardStateType);
                }
            }

            return null;
        }

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
