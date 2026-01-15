using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace RealPlayTester.Utilities
{
    /// <summary>
    /// Probes a GameObject to determine its "Semantic Affordances" (valid AI actions).
    /// Helps AI agents avoid trial-and-error by explicitly stating what is possible.
    /// </summary>
    public static class AffordanceProbe
    {
        public static string[] GetAffordances(GameObject go)
        {
            if (go == null) return Array.Empty<string>();

            var affordances = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 1. Click / Select Affordances
            if (go.GetComponent<Button>() != null || go.GetComponent<IPointerClickHandler>() != null)
            {
                affordances.Add("Click");
                affordances.Add("Select");
            }

            // 2. Text / Input Affordances
            if (go.GetComponent<InputField>() != null || go.GetComponent("TMPro.TMP_InputField") != null)
            {
                affordances.Add("Type");
                affordances.Add("Submit");
            }

            // 3. Scroll Affordances
            if (go.GetComponent<ScrollRect>() != null || go.GetComponentInParent<ScrollRect>() != null)
            {
                affordances.Add("Scroll");
            }

            // 4. Drag Affordances
            if (go.GetComponent<IDragHandler>() != null || go.GetComponent<Slider>() != null)
            {
                affordances.Add("Drag");
            }

            // 5. General Interaction
            if (go.GetComponent<Selectable>() != null)
            {
                affordances.Add("Hover");
                if (go.GetComponent<Selectable>().navigation.mode != Navigation.Mode.None)
                {
                    affordances.Add("Navigate");
                }
            }

            // 6. 3D Physics Affordances
            if (go.GetComponent<Collider>() != null || go.GetComponent<Collider2D>() != null)
            {
                affordances.Add("PointerDown");
            }

            return new List<string>(affordances).ToArray();
        }
    }
}
