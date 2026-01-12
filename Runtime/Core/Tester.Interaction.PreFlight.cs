using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using RealPlayTester.Core.Perception;
using RealPlayTester.Input;

namespace RealPlayTester.Core
{
    public static partial class Tester
    {
        public static partial class Interaction
        {
            public struct PreFlightResult
            {
                public bool Success;
                public string BlockReason;
            }

            /// <summary>
            /// Checks if the intended interaction is possible on the target named object.
            /// </summary>
            public static async Task<PreFlightResult> PreFlight(string intent, string targetName)
            {
                if (!RealPlayEnvironment.IsEnabled)
                    return new PreFlightResult { Success = false, BlockReason = "Environment Disabled" };

                var target = Perception.Find(targetName);
                if (target == null)
                {
                    return new PreFlightResult { Success = false, BlockReason = $"Target '{targetName}' not found in scene." };
                }

                return await PreFlight(intent, target);
            }

            /// <summary>
            /// Checks if the intended interaction is possible on the target GameObject.
            /// Validates hierarchy activation, UI interactability, logic blockers, and visual occlusion.
            /// </summary>
            public static Task<PreFlightResult> PreFlight(string intent, GameObject target)
            {
                if (!RealPlayEnvironment.IsEnabled)
                    return Task.FromResult(new PreFlightResult { Success = false, BlockReason = "Environment Disabled" });

                if (target == null)
                    return Task.FromResult(new PreFlightResult { Success = false, BlockReason = "Target is null" });

                // 1. Hierarchy Check
                if (!target.activeInHierarchy)
                    return Task.FromResult(new PreFlightResult { Success = false, BlockReason = "Target is not active in hierarchy" });

                // 2. Component Enable Check & Interactable Check
                if (!IsComponentEnabled(target))
                     return Task.FromResult(new PreFlightResult { Success = false, BlockReason = "Target component is disabled" });

                if (!IsInteractable(target))
                     return Task.FromResult(new PreFlightResult { Success = false, BlockReason = "Target UI is not interactable" });

                // 3. Logic Check (IAffordanceValidator)
                var validator = target.GetComponent<IAffordanceValidator>();
                if (validator != null)
                {
                    if (!validator.CanInteract(intent))
                    {
                        return Task.FromResult(new PreFlightResult { Success = false, BlockReason = $"Logic Block: {validator.GetBlockReason()}" });
                    }
                }

                // 4. Raycast Target Check (for UI)
                if (IsUI(target) && !IsRaycastTarget(target))
                {
                     // Not necessarily a block if we are clicking a child, but often a reason for failure
                     // Relaxed check: look for ANY raycast target in children? No, usually specific target.
                }

                // 5. Occlusion Check
                var occlusion = RealPlayOcclusionRaycaster.CheckOcclusion(target);
                if (occlusion.IsOccluded)
                {
                    return Task.FromResult(new PreFlightResult { Success = false, BlockReason = $"Occluded by '{occlusion.BlockingObjectName}'" });
                }

                return Task.FromResult(new PreFlightResult { Success = true });
            }

            private static bool IsComponentEnabled(GameObject go)
            {
                var behaviour = go.GetComponent<Behaviour>();
                return behaviour == null || behaviour.enabled;
            }

            private static bool IsInteractable(GameObject go)
            {
                var btn = go.GetComponent<Button>();
                if (btn != null) return btn.interactable;

                var toggle = go.GetComponent<Toggle>();
                if (toggle != null) return toggle.interactable;

                var slider = go.GetComponent<Slider>();
                if (slider != null) return slider.interactable;

                var input = go.GetComponent<InputField>();
                if (input != null) return input.interactable;

                var scroll = go.GetComponent<Scrollbar>();
                if (scroll != null) return scroll.interactable;

                var group = go.GetComponent<CanvasGroup>();
                if (group != null && !group.interactable) return false;

                return true;
            }

            private static bool IsUI(GameObject go) => go.GetComponent<RectTransform>() != null;

            private static bool IsRaycastTarget(GameObject go)
            {
                var graphic = go.GetComponent<Graphic>();
                return graphic != null && graphic.raycastTarget;
            }
        }
    }
}