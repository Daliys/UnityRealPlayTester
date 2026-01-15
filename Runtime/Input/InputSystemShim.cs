using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using RealPlayTester.Core;
using RealPlayTester.Input.Internal;

namespace RealPlayTester.Input
{
    public enum GamepadButton
    {
        DpadUp = 0, DpadDown = 1, DpadLeft = 2, DpadRight = 3,
        North = 4, South = 5, West = 6, East = 7,
        LeftStick = 8, RightStick = 9, LeftShoulder = 10, RightShoulder = 11,
        Start = 12, Select = 13, LeftTrigger = 32, RightTrigger = 33 
    }

    /// <summary>
    /// Reflection-based facade for Unity's new Input System.
    /// Provides a high-level API for simulating input.
    /// </summary>
    public static class InputSystemShim
    {
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

        private static readonly HashSet<KeyCode> WarnedUnmappedKeys = new HashSet<KeyCode>();
        private static uint _currentGamepadButtons = 0;
        private static Vector2 _currentLeftStick = Vector2.zero;
        private static Vector2 _currentRightStick = Vector2.zero;

        private static void InitializeKeyCodeMap()
        {
            if (KeyCodeToInputSystemKey.Count > 0) return;
            
            // DYNAMIC MAPPING FIX: Use reflection to map KeyCode to InputSystem.Key enum
            var keyEnumType = InputSystemReflector.Types.KeyEnum;
            if (keyEnumType == null) return;

            foreach (KeyCode kc in Enum.GetValues(typeof(KeyCode)))
            {
                string name = kc.ToString();
                // Special cases
                if (name.StartsWith("Alpha")) name = name.Substring(5);
                else if (name.Length == 1 && char.IsLetter(name[0])) { /* OK */ }
                
                try {
                    object keyVal = Enum.Parse(keyEnumType, name, true);
                    KeyCodeToInputSystemKey[kc] = (int)keyVal;
                } catch {
                    // Fallback to manual map for complex keys if reflection fails
                }
            }
        }

        public static bool IsAvailable => InputSystemReflector.Types.InputSystem != null;

        public static void InitializeDevices()
        {
            if (!IsAvailable) return;
            InitializeKeyCodeMap();
            VirtualDeviceManager.EnsureKeyboard();
            VirtualDeviceManager.EnsureMouse();
            VirtualDeviceManager.EnsureGamepad();
        }

        public static void UpdateInput() => InputSimulator.UpdateInput();

        public static Task QueueKeyState(KeyCode key, bool down)
        {
            if (!TryMapKeyCode(key, out int kv))
            {
                WarnUnmapped(key);
                return Task.CompletedTask;
            }
            return InputSimulator.QueueKeyState(kv, down);
        }

        public static async Task PressKey(KeyCode key, float duration = 0.05f)
        {
            await QueueKeyState(key, true);
            await RealPlayTester.Await.Wait.Seconds(duration);
            await QueueKeyState(key, false);
        }

        public static void KeyDown(KeyCode key) => _ = QueueKeyState(key, true);
        public static void KeyUp(KeyCode key) => _ = QueueKeyState(key, false);

        public static Task MouseDown(int button = 0) => InputSimulator.QueueMouseState(RealInputUtility.LastSimulatedPosition, button, true, false);
        public static Task MouseUp(int button = 0) => InputSimulator.QueueMouseState(RealInputUtility.LastSimulatedPosition, button, false, false);
        public static Task MouseMove(Vector2 position) => InputSimulator.QueueMouseState(position, -1, false, true);
        public static void MouseButton(int button = 0, bool down = true) => _ = InputSimulator.QueueMouseState(RealInputUtility.LastSimulatedPosition, button, down, false);

        public static Task GamepadButton(GamepadButton button, bool down)
        {
            if (down) _currentGamepadButtons |= (uint)(1 << (int)button);
            else _currentGamepadButtons &= ~(uint)(1 << (int)button);
            return InputSimulator.QueueGamepadState(_currentGamepadButtons, _currentLeftStick, _currentRightStick);
        }

        public static Task GamepadStick(bool left, Vector2 value)
        {
            if (left) _currentLeftStick = value;
            else _currentRightStick = value;
            return InputSimulator.QueueGamepadState(_currentGamepadButtons, _currentLeftStick, _currentRightStick);
        }

        public static bool GetKeyHeld(KeyCode key)
        {
            if (!IsAvailable) return false;
            object keyboard = VirtualDeviceManager.GetKeyboard();
            if (keyboard == null || InputSystemReflector.KeyboardItemProperty == null) return false;
            if (!TryMapKeyCode(key, out int kv)) return false;
            
            object keyEnum = Enum.ToObject(InputSystemReflector.Types.KeyEnum, kv);
            object control = InputSystemReflector.KeyboardItemProperty.GetValue(keyboard, new[] { keyEnum });
            if (control == null) return false;
            
            var isPressedProp = control.GetType().GetProperty("isPressed", BindingFlags.Public | BindingFlags.Instance);
            return isPressedProp != null && (bool)isPressedProp.GetValue(control);
        }

        public static bool GetKeyDown(KeyCode key)
        {
            if (!IsAvailable) return false;
            object keyboard = VirtualDeviceManager.GetKeyboard();
            if (keyboard == null || InputSystemReflector.KeyboardItemProperty == null || InputSystemReflector.WasPressedThisFrameProperty == null) return false;
            if (!TryMapKeyCode(key, out int kv)) return false;
            
            object kEnum = Enum.ToObject(InputSystemReflector.Types.KeyEnum, kv);
            object ctrl = InputSystemReflector.KeyboardItemProperty.GetValue(keyboard, new[] { kEnum });
            return ctrl != null && (bool)InputSystemReflector.WasPressedThisFrameProperty.GetValue(ctrl);
        }

        public static void ClearEvents()
        {
            InputSimulator.ClearMouseState();
            InputSimulator.ClearGamepadState();
            InitializeDevices();
        }

        private static bool TryMapKeyCode(KeyCode key, out int kv) => KeyCodeToInputSystemKey.TryGetValue(key, out kv);

        private static void WarnUnmapped(KeyCode key)
        {
            if (WarnedUnmappedKeys.Add(key)) RealPlayLog.Warn($"KeyCode.{key} has no mapping to Input System Key enum.");
        }
    }
}