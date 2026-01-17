using UnityEditor;
using UnityEngine;

namespace RealPlayTester.Editor
{
    /// <summary>
    /// Weaponized tool to test the library's resilience against Domain Reload persistence.
    /// This script simulates a 'Bad Developer' environment by disabling Domain Reload.
    /// </summary>
    public static class PlayModeChaosToggle
    {
        [MenuItem("RealPlayTester/Chaos/Disable Domain Reload")]
        public static void DisableDomainReload()
        {
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.None; // This means NO reload
            Debug.Log("[CHAOS] Domain Reload DISABLED. Static state will persist between runs.");
        }

        [MenuItem("RealPlayTester/Chaos/Enable Domain Reload (Standard)")]
        public static void EnableDomainReload()
        {
            EditorSettings.enterPlayModeOptionsEnabled = false;
            Debug.Log("[CHAOS] Domain Reload ENABLED (Standard Unity behavior).");
        }

        [MenuItem("RealPlayTester/Chaos/Toggle Chaos Mode")]
        public static void ToggleChaos()
        {
            EditorSettings.enterPlayModeOptionsEnabled = !EditorSettings.enterPlayModeOptionsEnabled;
            Debug.Log($"[CHAOS] Enter Play Mode Options: {(EditorSettings.enterPlayModeOptionsEnabled ? "ENABLED (Potentially No Reload)" : "DISABLED (Standard Reload)")}");
        }
    }
}
