using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using RealPlayTester.Input;
using RealPlayTester.Utilities;
using UnityEngine.EventSystems;

namespace RealPlayTester.Core.Perception
{
    public static partial class RealPlaySemanticDOMDumper
    {
        private static List<string> GetAffordances(GameObject go)
        {
            var affordances = new HashSet<string>();

            CheckStandardComponents(go, affordances);
            CheckTMPComponents(go, affordances);
            CheckEventTriggers(go, affordances);
            CheckCustomHandlers(go, affordances);

            return new List<string>(affordances);
        }

        private static void CheckStandardComponents(GameObject go, HashSet<string> affordances)
        {
            if (!IsInteractable(go)) return;

            if (go.GetComponent<Button>() != null)
            {
                affordances.Add("Click");
                affordances.Add("Submit");
            }

            if (go.GetComponent<Toggle>() != null) affordances.Add("Click");
            if (go.GetComponent<Slider>() != null) affordances.Add("Drag");
            
            if (go.GetComponent<Scrollbar>() != null)
            {
                affordances.Add("Drag");
                affordances.Add("Scroll");
            }

            if (go.GetComponent<ScrollRect>() != null)
            {
                affordances.Add("Scroll");
                affordances.Add("Drag");
            }

            if (go.GetComponent<InputField>() != null)
            {
                affordances.Add("Type");
                affordances.Add("Click");
                affordances.Add("Submit");
            }
        }

        private static void CheckTMPComponents(GameObject go, HashSet<string> affordances)
        {
            var tmpInputType = Type.GetType("TMPro.TMP_InputField, Unity.TextMeshPro");
            if (tmpInputType == null) return;

            var tmpInput = go.GetComponent(tmpInputType);
            if (tmpInput != null)
            {
                var interactableProp = tmpInputType.GetProperty("interactable");
                bool isInteractable = interactableProp == null || (bool)interactableProp.GetValue(tmpInput);
                if (isInteractable)
                {
                    affordances.Add("Type");
                    affordances.Add("Click");
                    affordances.Add("Submit");
                }
            }
        }

        private static void CheckEventTriggers(GameObject go, HashSet<string> affordances)
        {
            var et = go.GetComponent<EventTrigger>();
            if (et == null) return;

            foreach (var entry in et.triggers)
            {
                switch (entry.eventID)
                {
                    case EventTriggerType.PointerClick: affordances.Add("Click"); break;
                    case EventTriggerType.PointerEnter: affordances.Add("Hover"); break;
                    case EventTriggerType.Submit: affordances.Add("Submit"); break;
                    case EventTriggerType.Drag:
                    case EventTriggerType.BeginDrag: affordances.Add("Drag"); break;
                    case EventTriggerType.Scroll: affordances.Add("Scroll"); break;
                    case EventTriggerType.PointerDown: affordances.Add("LongPress"); break;
                }
            }
        }

        private static void CheckCustomHandlers(GameObject go, HashSet<string> affordances)
        {
            var components = go.GetComponents<MonoBehaviour>();
            foreach (var comp in components)
            {
                if (comp == null || !comp.isActiveAndEnabled) continue;
                if (comp is Selectable sel && !sel.interactable) continue;

                if (comp is IPointerClickHandler) affordances.Add("Click");
                if (comp is IDragHandler) affordances.Add("Drag");
                if (comp is IScrollHandler) affordances.Add("Scroll");
                if (comp is ISubmitHandler) affordances.Add("Submit");
                if (comp is IPointerEnterHandler) affordances.Add("Hover");

                // Heuristic for custom frameworks: look for public methods with specific names
                CheckMethodHeuristics(comp, affordances);
            }
        }

        private static void CheckMethodHeuristics(MonoBehaviour comp, HashSet<string> affordances)
        {
            var type = comp.GetType();
            var methods = type.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly);
            
            foreach (var m in methods)
            {
                string name = m.Name.ToLower();
                if (name.Contains("onclick")) affordances.Add("Click");
                if (name.Contains("onsubmit")) affordances.Add("Submit");
                if (name.Contains("onselect")) affordances.Add("Select");
                if (name.Contains("onvaluechanged") || name.Contains("onscroll")) affordances.Add("Scroll");
            }
        }
    }
}
