using UnityEngine;
using UnityEditor;

namespace RealPlayTester.Editor.Validation
{
    public static class ProjectValidator
    {
        public static void Validate()
        {
            // If we reached here, domain reload passed, so compilation is good.
            Debug.Log("[Diagnostics] Compilation successful.");
            EditorApplication.Exit(0);
        }
    }
}
