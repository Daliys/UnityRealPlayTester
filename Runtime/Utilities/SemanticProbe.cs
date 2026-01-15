using System;
using UnityEngine;
using UnityEngine.UI;

namespace RealPlayTester.Utilities
{
    public enum SemanticRole
    {
        None,
        Button,
        InputField,
        ValueSlider,
        ScrollArea,
        Heading,
        Label,
        Dialog,
        Navigation,
        Toggle,
        Dropdown,
        View,
        WorldObject
    }

    /// <summary>
    /// Categorizes GameObjects into high-level roles based on components and naming.
    /// This helps AI agents understand the purpose of objects beyond just their Unity type.
    /// </summary>
    public static class SemanticProbe
    {
        public static string GetRoleName(GameObject go)
        {
            if (go == null) return string.Empty;

            // 0. Explicit Attribute Hint
            var components = go.GetComponents<MonoBehaviour>();
            foreach (var comp in components)
            {
                if (comp == null) continue;
                var attr = System.Attribute.GetCustomAttribute(comp.GetType(), typeof(RealPlayRoleAttribute)) as RealPlayRoleAttribute;
                if (attr != null) return attr.RoleName;
            }

            var role = GetRole(go);
            return role != SemanticRole.None ? role.ToString() : string.Empty;
        }

        public static SemanticRole GetRole(GameObject go)
        {
            if (go == null) return SemanticRole.None;

            var role = TryGetUIComponentRole(go);
            if (role != SemanticRole.None) return role;

            role = TryGetStructuralRole(go);
            if (role != SemanticRole.None) return role;

            role = TryGetContentRole(go);
            if (role != SemanticRole.None) return role;

            return GetFallbackRole(go);
        }

        private static SemanticRole TryGetUIComponentRole(GameObject go)
        {
            if (go.GetComponent<Button>()) return SemanticRole.Button;
            if (go.GetComponent<Toggle>()) return SemanticRole.Toggle;
            if (go.GetComponent<Slider>()) return SemanticRole.ValueSlider;
            if (go.GetComponent<ScrollRect>()) return SemanticRole.ScrollArea;
            if (go.GetComponent<InputField>() || go.GetComponent("TMPro.TMP_InputField")) return SemanticRole.InputField;
            if (go.GetComponent<Dropdown>() || go.GetComponent("TMPro.TMP_Dropdown")) return SemanticRole.Dropdown;
            return SemanticRole.None;
        }

        private static SemanticRole TryGetStructuralRole(GameObject go)
        {
            string name = go.name.ToLower();
            if (name.Contains("panel") || name.Contains("dialog") || name.Contains("modal") || name.Contains("window"))
                return SemanticRole.Dialog;
            
            if (name.Contains("menu") || name.Contains("navigation") || name.Contains("navbar") || name.Contains("sidebar"))
                return SemanticRole.Navigation;

            return SemanticRole.None;
        }

        private static SemanticRole TryGetContentRole(GameObject go)
        {
            var text = GetText(go);
            if (string.IsNullOrEmpty(text)) return SemanticRole.None;

            string name = go.name.ToLower();
            if (name.Contains("header") || name.Contains("heading") || name.Contains("title"))
                return SemanticRole.Heading;
                
            return SemanticRole.Label;
        }

        private static SemanticRole GetFallbackRole(GameObject go)
        {
            if (go.GetComponent<CanvasRenderer>() != null || go.GetComponentInParent<Canvas>() != null)
                return SemanticRole.View;

            if (go.GetComponent<Collider>() != null || go.GetComponent<Renderer>() != null)
                return SemanticRole.WorldObject;

            return SemanticRole.None;
        }

        private static string GetText(GameObject go)
        {
            var txt = go.GetComponent<UnityEngine.UI.Text>();
            if (txt != null) return txt.text;

            var tmp = go.GetComponent("TMPro.TMP_Text");
            if (tmp != null)
            {
                var prop = tmp.GetType().GetProperty("text");
                return prop?.GetValue(tmp) as string;
            }
            return null;
        }
    }
}
