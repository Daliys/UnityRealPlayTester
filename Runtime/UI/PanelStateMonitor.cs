using UnityEngine;
using UnityEngine.UI;

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
            if (panel == null)
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

            var state = new PanelState
            {
                ActiveInHierarchy = panel.activeInHierarchy,
                Alpha = 1f,
                Interactable = true,
                HasCanvasGroup = false
            };

            // Check CanvasGroup if present
            var canvasGroup = panel.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                state.HasCanvasGroup = true;
                state.Alpha = canvasGroup.alpha;
                state.Interactable = canvasGroup.interactable;
            }

            // Also check parent CanvasGroups that might affect this panel
            var parentGroups = panel.GetComponentsInParent<CanvasGroup>();
            foreach (var group in parentGroups)
            {
                if (group.gameObject == panel)
                    continue;

                if (group.alpha < state.Alpha)
                    state.Alpha = group.alpha;

                if (!group.interactable)
                    state.Interactable = false;

                if (group.ignoreParentGroups)
                    break;
            }

            // Panel is visible if active and alpha > threshold
            state.IsVisible = state.ActiveInHierarchy && state.Alpha > 0.01f;

            return state;
        }

        /// <summary>
        /// Check the state of a panel by name.
        /// </summary>
        public static PanelState CheckPanelState(string panelName)
        {
            GameObject panel = GameObject.Find(panelName);
            return CheckPanelState(panel);
        }

        /// <summary>
        /// Check if a panel is fully visible and interactable.
        /// </summary>
        public static bool IsPanelReady(GameObject panel)
        {
            var state = CheckPanelState(panel);
            return state.IsVisible && state.Interactable;
        }

        /// <summary>
        /// Check if a panel is fully visible and interactable by name.
        /// </summary>
        public static bool IsPanelReady(string panelName)
        {
            var state = CheckPanelState(panelName);
            return state.IsVisible && state.Interactable;
        }
    }
}
