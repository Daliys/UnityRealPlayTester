using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using RealPlayTester.Core;

namespace RealPlayTester.Input.Internal
{
    /// <summary>
    /// Internal manager for virtual simulation devices (Keyboard, Mouse, Gamepad).
    /// </summary>
    public static class VirtualDeviceManager
    {
        [ThreadStatic] private static object _threadKeyboard;
        [ThreadStatic] private static object _threadMouse;
        [ThreadStatic] private static object _threadGamepad;

        public static object VirtualKeyboard { get => _threadKeyboard; set => _threadKeyboard = value; }
        public static object VirtualMouse { get => _threadMouse; set => _threadMouse = value; }
        public static object VirtualGamepad { get => _threadGamepad; set => _threadGamepad = value; }

        public static object EnsureKeyboard()
        {
            object keyboard = GetKeyboard();
            if (keyboard != null) { _threadKeyboard = keyboard; return keyboard; }
            return CreateDevice("Keyboard", ref _threadKeyboard, InputSystemReflector.Types.Keyboard);
        }

        public static object EnsureMouse()
        {
            object mouse = GetMouse();
            if (mouse != null) { _threadMouse = mouse; return mouse; }
            return CreateDevice("Mouse", ref _threadMouse, InputSystemReflector.Types.Mouse);
        }

        public static object EnsureGamepad()
        {
            object gamepad = GetGamepad();
            if (gamepad != null) { _threadGamepad = gamepad; return gamepad; }
            return CreateDevice("Gamepad", ref _threadGamepad, InputSystemReflector.Types.Gamepad);
        }

        public static object GetKeyboard()
        {
            if (CheckCachedDevice(_threadKeyboard, "FastKeyboard", out var cached)) return cached;
            _threadKeyboard = FindDevice("Keyboard", "FastKeyboard") ?? _threadKeyboard;
            if (_threadKeyboard != null && IsDeviceInSystem(_threadKeyboard))
            {
                EnableDevice(_threadKeyboard);
                return _threadKeyboard;
            }
            return null;
        }

        public static object GetMouse()
        {
            if (CheckCachedDevice(_threadMouse, "FastMouse", out var cached)) return cached;
            _threadMouse = FindDevice("Mouse", "FastMouse") ?? _threadMouse;
            if (_threadMouse != null && IsDeviceInSystem(_threadMouse))
            {
                EnableDevice(_threadMouse);
                return _threadMouse;
            }
            return null;
        }

        public static object GetGamepad()
        {
            if (CheckCachedDevice(_threadGamepad, "Gamepad", out var cached)) return cached;
            _threadGamepad = FindDevice("Gamepad", "Gamepad") ?? _threadGamepad;
            if (_threadGamepad != null && IsDeviceInSystem(_threadGamepad))
            {
                EnableDevice(_threadGamepad);
                return _threadGamepad;
            }
            return null;
        }

        public static void CleanupDevices()
        {
            RemoveDevice(ref _threadKeyboard);
            RemoveDevice(ref _threadMouse);
            RemoveDevice(ref _threadGamepad);
        }

        private static void RemoveDevice(ref object device)
        {
            if (device == null || InputSystemReflector.Types.InputSystem == null) return;
            try
            {
                var method = InputSystemReflector.Types.InputSystem.GetMethod("RemoveDevice", BindingFlags.Public | BindingFlags.Static);
                method?.Invoke(null, new[] { device });
                device = null;
            }
            catch { }
        }

        private static bool CheckCachedDevice(object device, string typeName, out object result)
        {
            result = null;
            if (device != null && device.GetType().Name.Contains(typeName) && IsDeviceInSystem(device))
            {
                EnableDevice(device);
                result = device;
                return true;
            }
            return false;
        }

        private static object FindDevice(string typePart, string fastTypeName)
        {
            if (InputSystemReflector.Types.InputSystem == null) return null;
            var devicesProp = InputSystemReflector.Types.InputSystem.GetProperty("devices", BindingFlags.Public | BindingFlags.Static);
            var devices = devicesProp?.GetValue(null) as IEnumerable;
            if (devices == null) return null;

            object bestFound = null;
            foreach (var d in devices)
            {
                if (d == null) continue;
                string t = d.GetType().Name;
                if (t == fastTypeName) { EnableDevice(d); return d; }
                if (t.Contains(typePart)) bestFound = d;
            }
            if (bestFound != null) EnableDevice(bestFound);
            return bestFound;
        }

        public static void EnableDevice(object device)
        {
            if (device == null || InputSystemReflector.Types.InputSystem == null) return;
            try
            {
                var method = InputSystemReflector.Types.InputSystem.GetMethod("EnableDevice", BindingFlags.Public | BindingFlags.Static);
                method?.Invoke(null, new[] { device });
            }
            catch { }
        }

        public static bool IsDeviceEnabled(object device)
        {
            if (device == null) return false;
            var prop = device.GetType().GetProperty("enabled", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            return prop != null && (bool)prop.GetValue(device);
        }

        public static bool IsCurrent(object device)
        {
            if (device == null) return false;
            string name = device.GetType().Name;
            if (name.Contains("Keyboard")) return InputSystemReflector.KeyboardCurrentProperty?.GetValue(null) == device;
            if (name.Contains("Mouse")) return InputSystemReflector.MouseCurrentProperty?.GetValue(null) == device;
            return false;
        }

        public static void MakeCurrent(object device)
        {
            if (device == null) return;
            var method = device.GetType().GetMethod("MakeCurrent", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            if (method != null) method.Invoke(device, null);
            else
            {
                InputSystemReflector.Types.InputDevice?.GetMethod("MakeCurrent", BindingFlags.Public | BindingFlags.Instance)?.Invoke(device, null);
            }
        }

        public static bool IsDeviceInSystem(object device)
        {
             if (device == null || InputSystemReflector.Types.InputSystem == null) return false;
             var devices = InputSystemReflector.Types.InputSystem.GetProperty("devices", BindingFlags.Public | BindingFlags.Static)?.GetValue(null) as IEnumerable;
             if (devices == null) return false;
             foreach (var d in devices) if (d == device) return true;
             return false;
        }

        private static object CreateDevice(string name, ref object cache, Type fallbackType)
        {
            if (InputSystemReflector.Types.InputSystem == null) return null;
            try
            {
                var addDeviceWithType = InputSystemReflector.Types.InputSystem.GetMethod("AddDevice", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(Type), typeof(string) }, null);
                if (addDeviceWithType != null && fallbackType != null)
                {
                    cache = addDeviceWithType.Invoke(null, new object[] { fallbackType, name });
                }
                else
                {
                    MethodInfo addMethod = null;
                    foreach (var m in InputSystemReflector.Types.InputSystem.GetMethods(BindingFlags.Public | BindingFlags.Static))
                    {
                        if (m.Name == "AddDevice" && !m.IsGenericMethodDefinition)
                        {
                            var p = m.GetParameters();
                            if (p.Length >= 1 && p[0].ParameterType == typeof(string)) { addMethod = m; break; }
                        }
                    }
                    if (addMethod != null) cache = addMethod.Invoke(null, new object[] { name, null, null });
                    else if (InputSystemReflector.AddDeviceMethod != null && fallbackType != null) cache = InputSystemReflector.AddDeviceMethod.Invoke(null, new[] { fallbackType });
                }
                
                if (cache != null)
                {
                    RealPlayLog.Info($"VirtualDeviceManager: Created virtual {name} of type {cache.GetType().FullName}.");
                    EnableDevice(cache);
                    if (RealPlaySettings.VirtualDevicesMakeCurrent) MakeCurrent(cache);
                }
                return cache;
            }
            catch (Exception ex) { RealPlayLog.Warn($"VirtualDeviceManager: Failed to create virtual {name}: {ex.Message}"); }
            return null;
        }

        public static string GetDeviceName(object device)
        {
            if (device == null) return "null";
            return device.GetType().GetProperty("name")?.GetValue(device) as string ?? "unknown";
        }
    }
}
