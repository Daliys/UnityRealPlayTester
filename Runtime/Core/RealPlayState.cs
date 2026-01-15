using UnityEngine;

namespace RealPlayTester.Core
{
    /// <summary>
    /// Attach this to a GameObject to explicitly define a Game State name for navigation auto-discovery.
    /// This overrides the default 'TopPanel' heuristic.
    /// </summary>
    [AddComponentMenu("RealPlayTester/RealPlay State")]
    public class RealPlayState : MonoBehaviour
    {
        [Tooltip("The name of the state (e.g. 'MainHUD', 'Inventory_Open').")]
        public string StateName;

        /// <summary>
        /// Optional: Dynamic sub-state (e.g. 'Level_1').
        /// If provided, the state will be 'StateName.SubState'.
        /// </summary>
        public string SubState;

        public string GetFullName()
        {
            string baseName = string.IsNullOrEmpty(StateName) ? gameObject.name : StateName;
            if (string.IsNullOrEmpty(SubState)) return baseName;
            return $"{baseName}.{SubState}";
        }
    }
}
