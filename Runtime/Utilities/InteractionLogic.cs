using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace RealPlayTester.Utilities
{
    /// <summary>
    /// Interface for custom scripts to report their logical interactability to the tester.
    /// Use this to prevent 'Ghost Interactions' (e.g., when a button is active but logic returns early).
    /// </summary>
    public interface IInteractableLogic
    {
        /// <summary>Returns true if the object can be logically interacted with right now.</summary>
        bool CanInteract();
        
        /// <summary>Optional reason why interaction is blocked (e.g. 'Not enough gold').</summary>
        string GetBlockedReason();
    }

    /// <summary>
    /// Logic probe that uses both explicit interfaces and reflection heuristics to find blocked interactions.
    /// </summary>
    public static class InteractionLogic
    {
        private static readonly Dictionary<string, MemberInfo> _memberCache = new Dictionary<string, MemberInfo>();

        public struct LogicResult
        {
            public bool IsBlocked;
            public string Reason;
        }

        public static LogicResult CheckLogicalInteractivity(GameObject go)
        {
            if (go == null) return new LogicResult { IsBlocked = true, Reason = "Null object" };

            // 1. Explicit Interface Check (Preferred)
            var components = go.GetComponents<Component>();
            foreach (var comp in components)
            {
                if (comp is IInteractableLogic logic)
                {
                    if (!logic.CanInteract())
                    {
                        return new LogicResult { IsBlocked = true, Reason = logic.GetBlockedReason() ?? "Blocked by IInteractableLogic" };
                    }
                }
            }

            // 2. Reflection Heuristics (Common Patterns)
            foreach (var comp in components)
            {
                if (comp == null || comp is Transform || comp is CanvasRenderer) continue;

                var type = comp.GetType();
                
                // Check for 'IsLocked' property or field
                if (CheckBoolMember(comp, type, "IsLocked", true)) return new LogicResult { IsBlocked = true, Reason = "Locked (heuristic)" };
                if (CheckBoolMember(comp, type, "m_IsLocked", true)) return new LogicResult { IsBlocked = true, Reason = "Locked (heuristic)" };
                
                // Check for 'CanInteract' property or method
                if (HasFalseMember(comp, type, "CanInteract")) return new LogicResult { IsBlocked = true, Reason = "CanInteract is false (heuristic)" };
            }

            return new LogicResult { IsBlocked = false, Reason = string.Empty };
        }

        private static bool CheckBoolMember(object instance, Type type, string name, bool expectedValue)
        {
            string key = $"{type.FullName}.{name}";
            
            // Check Property
            PropertyInfo prop;
            if (!_memberCache.TryGetValue(key + "_p", out MemberInfo m) || !(m is PropertyInfo))
            {
                prop = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                _memberCache[key + "_p"] = prop;
            }
            else prop = (PropertyInfo)m;

            if (prop != null && prop.PropertyType == typeof(bool))
            {
                if ((bool)prop.GetValue(instance) == expectedValue) return true;
            }

            // Check Field
            FieldInfo field;
            if (!_memberCache.TryGetValue(key + "_f", out m) || !(m is FieldInfo))
            {
                field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                _memberCache[key + "_f"] = field;
            }
            else field = (FieldInfo)m;

            if (field != null && field.FieldType == typeof(bool))
            {
                if ((bool)field.GetValue(instance) == expectedValue) return true;
            }

            return false;
        }

        private static bool HasFalseMember(object instance, Type type, string name)
        {
            string key = $"{type.FullName}.{name}";

            // Check Property
            PropertyInfo prop;
            if (!_memberCache.TryGetValue(key + "_p", out MemberInfo m) || !(m is PropertyInfo))
            {
                prop = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                _memberCache[key + "_p"] = prop;
            }
            else prop = (PropertyInfo)m;

            if (prop != null && prop.PropertyType == typeof(bool))
            {
                if (!(bool)prop.GetValue(instance)) return true;
            }

            // Check Method (No args)
            MethodInfo method;
            if (!_memberCache.TryGetValue(key + "_m", out m) || !(m is MethodInfo))
            {
                method = type.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null);
                _memberCache[key + "_m"] = method;
            }
            else method = (MethodInfo)m;

            if (method != null && method.ReturnType == typeof(bool))
            {
                if (!(bool)method.Invoke(instance, null)) return true;
            }

            return false;
        }
    }
}
