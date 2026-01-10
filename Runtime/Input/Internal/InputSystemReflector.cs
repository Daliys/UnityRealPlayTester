using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using RealPlayTester.Core;

namespace RealPlayTester.Input.Internal
{
    /// <summary>
    /// Internal helper that handles all reflection lookups for the Unity Input System.
    /// </summary>
    internal static class InputSystemReflector
    {
        internal static class Types
        {
            public static readonly Type Keyboard = Type.GetType("UnityEngine.InputSystem.Keyboard, Unity.InputSystem");
            public static readonly Type KeyboardState = Type.GetType("UnityEngine.InputSystem.LowLevel.KeyboardState, Unity.InputSystem");
            public static readonly Type KeyEnum = Type.GetType("UnityEngine.InputSystem.Key, Unity.InputSystem");
            public static readonly Type InputSystem = Type.GetType("UnityEngine.InputSystem.InputSystem, Unity.InputSystem");
            public static readonly Type KeyControl = Type.GetType("UnityEngine.InputSystem.Controls.KeyControl, Unity.InputSystem");
            public static readonly Type Mouse = Type.GetType("UnityEngine.InputSystem.Mouse, Unity.InputSystem");
            public static readonly Type MouseState = Type.GetType("UnityEngine.InputSystem.LowLevel.MouseState, Unity.InputSystem");
            public static readonly Type InputDevice = Type.GetType("UnityEngine.InputSystem.InputDevice, Unity.InputSystem");
            public static readonly Type InputUpdate = Type.GetType("UnityEngine.InputSystem.LowLevel.InputUpdateType, Unity.InputSystem");
            public static readonly Type InputSettings = Type.GetType("UnityEngine.InputSystem.InputSettings, Unity.InputSystem");
            
            // Gamepad Support
            public static readonly Type Gamepad = Type.GetType("UnityEngine.InputSystem.Gamepad, Unity.InputSystem");
            public static readonly Type GamepadState = Type.GetType("UnityEngine.InputSystem.LowLevel.GamepadState, Unity.InputSystem");
            public static readonly Type GamepadButtonEnum = Type.GetType("UnityEngine.InputSystem.LowLevel.GamepadButton, Unity.InputSystem");
        }

        public static readonly MethodInfo QueueStateEventKeyboard = GetQueueStateEventGeneric(Types.KeyboardState);
        public static readonly MethodInfo QueueStateEventMouse = GetQueueStateEventGeneric(Types.MouseState);
        public static readonly MethodInfo QueueStateEventGamepad = GetQueueStateEventGeneric(Types.GamepadState);
        
        public static readonly MethodInfo KeyboardStateSetMethod = Types.KeyboardState != null && Types.KeyEnum != null
            ? Types.KeyboardState.GetMethod("Set", new[] { Types.KeyEnum, typeof(bool) })
            : null;

        public static readonly PropertyInfo KeyboardCurrentProperty = Types.Keyboard != null
            ? Types.Keyboard.GetProperty("current", BindingFlags.Public | BindingFlags.Static)
            : null;

        public static readonly PropertyInfo KeyboardItemProperty = Types.Keyboard != null && Types.KeyEnum != null
            ? Types.Keyboard.GetProperty("Item", new[] { Types.KeyEnum })
            : null;

        public static readonly PropertyInfo WasPressedThisFrameProperty = Types.KeyControl != null
            ? Types.KeyControl.GetProperty("wasPressedThisFrame", BindingFlags.Public | BindingFlags.Instance)
            : null;
            
        public static readonly PropertyInfo MouseCurrentProperty = Types.Mouse != null
            ? Types.Mouse.GetProperty("current", BindingFlags.Public | BindingFlags.Static)
            : null;

        public static readonly MethodInfo AddDeviceMethod = Types.InputSystem != null
            ? Types.InputSystem.GetMethod("AddDevice", new[] { typeof(Type) })
            : null;

        public static readonly MethodInfo UpdateMethod = Types.InputSystem != null && Types.InputUpdate != null
            ? Types.InputSystem.GetMethod("Update", BindingFlags.Public | BindingFlags.Static, null, new[] { Types.InputUpdate }, null)
            : null;

        public static readonly MethodInfo UpdateMethodNoArgs = Types.InputSystem != null
            ? Types.InputSystem.GetMethod("Update", BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null)
            : null;

        internal static MethodInfo GetQueueStateEventGeneric(Type stateType)
        {
            if (Types.InputSystem == null || stateType == null) return null;
            MethodInfo bestMatch = null;
            int bestParamCount = 99;

            foreach (var m in Types.InputSystem.GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if (m.Name == "QueueStateEvent")
                {
                    var p = m.GetParameters();
                    if (m.IsGenericMethodDefinition && p.Length >= 2 && p[0].ParameterType.Name.Contains("InputDevice"))
                    {
                        if (p.Length == 3) return m.MakeGenericMethod(stateType);
                        if (p.Length < bestParamCount)
                        {
                            bestMatch = m;
                            bestParamCount = p.Length;
                        }
                    }
                }
            }
            return bestMatch?.MakeGenericMethod(stateType);
        }
    }
}