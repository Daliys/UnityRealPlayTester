using UnityEditor;
using UnityEngine;
using System.IO;

namespace RealPlayTester.Editor
{
    /// <summary>
    /// Weaponized tool to test resilience against Assembly Reloads.
    /// Periodically 'touches' a dummy file to trigger recompilation.
    /// </summary>
    [InitializeOnLoad]
    public static class ScriptPoker
    {
        private const string DummyFilePath = "Assets/UnityRealPlayTester/Editor/Internal_Poker_Dummy.cs";
        private static double _lastPokerTime = 0;
        private static bool _isEnabled = false;

        static ScriptPoker()
        {
            EditorApplication.update += Update;
        }

        [MenuItem("RealPlayTester/Chaos/Toggle Script Poker")]
        public static void TogglePoker()
        {
            _isEnabled = !_isEnabled;
            Debug.Log("[CHAOS] Script Poker is now " + (_isEnabled ? "ENABLED" : "DISABLED"));
            if (_isEnabled) _lastPokerTime = EditorApplication.timeSinceStartup;
        }

        private static void Update()
        {
            if (!_isEnabled) return;

            if (EditorApplication.timeSinceStartup - _lastPokerTime > 5.0)
            {
                Poke();
                _lastPokerTime = EditorApplication.timeSinceStartup;
            }
        }

        private static void Poke()
        {
            string time = System.DateTime.Now.ToString("HH:mm:ss");
            string content = "// Poked at " + time + "\nnamespace RealPlayTester.Editor { public static class PokerDummy { } }";
            File.WriteAllText(DummyFilePath, content);
            AssetDatabase.ImportAsset(DummyFilePath);
            Debug.Log("[CHAOS] Script Poker: Touched dummy file.");
        }
    }
}
