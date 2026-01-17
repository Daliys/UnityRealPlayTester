using UnityEngine;
using UnityEngine.UI;
using RealPlayTester.Core;

namespace RealPlayTester.UI
{
    /// <summary>
    /// Monitors UI panel state for test validation.
    /// Provides structured information about panel visibility and interactability.
    /// </summary>
    public class PanelStateMonitor
    {
        public class PanelState
        {
            public bool IsVisible { get; set; }
            public float Alpha { get; set; }
            public bool Interactable { get; set; }
            public bool ActiveInHierarchy { get; set; }
            public bool HasCanvasGroup { get; set; }
            
            public override string ToString()
            {
                return $"Visible:{IsVisible}, Alpha:{Alpha:F2}, Interactable:{Interactable}, Active:{ActiveInHierarchy}";
            }
        }

        /// <summary>
        /// Check the state of a panel by GameObject reference.
        /// </summary>
        public static PanelState CheckPanelState(GameObject panel)
        {
            if (panel == null) return GetEmptyState();

            var state = new PanelState
            {
                ActiveInHierarchy = panel.activeInHierarchy,
                Alpha = 1f,
                Interactable = true,
                HasCanvasGroup = false
            };

            ApplyCanvasGroupState(panel, state);
            ApplyParentCanvasGroups(panel, state);

            state.IsVisible = state.ActiveInHierarchy && state.Alpha > 0.01f;
            return state;
        }

        private static PanelState GetEmptyState()
        {
            return new PanelState
            {
                IsVisible = false,
                Alpha = 0f,
                Interactable = false,
                ActiveInHierarchy = false,
                HasCanvasGroup = false
            };
        }

        private static void ApplyCanvasGroupState(GameObject panel, PanelState state)
        {
            var canvasGroup = panel.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                state.HasCanvasGroup = true;
                state.Alpha = canvasGroup.alpha;
                state.Interactable = canvasGroup.interactable;
            }
        }

        private static void ApplyParentCanvasGroups(GameObject panel, PanelState state)
        {
            if (panel == null) return;
            try
            {
                var parentGroups = panel.GetComponentsInParent<CanvasGroup>();
                foreach (var group in parentGroups)
                {
                    if (group == null || group.gameObject == panel) continue;
                    // O025: Correct composition is multiplicative
                    state.Alpha *= group.alpha;
                    if (!group.interactable) state.Interactable = false;
                    if (group.ignoreParentGroups) break;
                }
            }
            catch (UnityEngine.MissingReferenceException) { state.IsVisible = false; state.Interactable = false; }
        }

        /// <summary>
        /// Check the state of a panel by name.
        /// </summary>
        public static PanelState CheckPanelState(string panelName)
        {
            if (string.IsNullOrEmpty(panelName)) return GetEmptyState();
            GameObject panel = GameObject.Find(panelName);
            return CheckPanelState(panel);
        }

        /// <summary>
        /// Check if a panel is fully visible and interactable.
        /// </summary>
        public static bool IsPanelReady(GameObject panel)
        {
            if (panel == null) return false;
            var state = CheckPanelState(panel);
            return state.IsVisible && state.Interactable;
        }

        /// <summary>
        /// Heuristic to determine current high-level game state name based on scene and top-most panel.
        /// </summary>
        public static string GetActiveStateName()
        {
            try
            {
                string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                
                // 1. Check for explicit state components (high priority)
                var explicitStates = UnityEngine.Object.FindObjectsByType<RealPlayState>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                RealPlayState bestState = null;
                int maxStateDepth = -1;
                
                foreach (var s in explicitStates)
                {
                    if (s != null && s.gameObject.activeInHierarchy)
                    {
                        int depth = GetHierarchyDepth(s.transform);
                        if (depth > maxStateDepth) { maxStateDepth = depth; bestState = s; }
                    }
                }
                if (bestState != null) return $"{scene}.{bestState.GetFullName()}";

                // 2. Fallback to top-most CanvasGroup heuristic
                var panels = UnityEngine.Object.FindObjectsByType<CanvasGroup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                CanvasGroup bestPanel = null;
                int maxDepth = -1;

                foreach (var p in panels)
                {
                    if (p != null && p.isActiveAndEnabled && p.gameObject.activeInHierarchy && p.alpha > 0.5f)
                    {
                        int depth = GetHierarchyDepth(p.transform);
                        if (depth > maxDepth) { maxDepth = depth; bestPanel = p; }
                    }
                }
                
                string topPanel = bestPanel != null ? bestPanel.gameObject.name : "Base";
                return $"{scene}.{topPanel}";
            }
            catch (UnityEngine.MissingReferenceException) { return "Transitioning"; }
        }

        private static int GetHierarchyDepth(Transform t)
        {
            if (t == null) return 0;
            int d = 0;
            try
            {
                while (t.parent != null) { d++; t = t.parent; }
            }
            catch (UnityEngine.MissingReferenceException) { }
            return d;
        }
    }
}
