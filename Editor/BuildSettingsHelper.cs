#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Linq;

namespace RealPlayTester.Editor
{
    [InitializeOnLoad]
    public static class BuildSettingsHelper
    {
        static BuildSettingsHelper()
        {
            EnsureSceneInBuildSettings("Assets/Scenes/Tests.unity");
        }

        public static void EnsureSceneInBuildSettings(string scenePath)
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.Any(s => s.path == scenePath)) return;

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
            {
                UnityEngine.Debug.LogWarning($"[RealPlayTester] Scene not found at {scenePath}");
                return;
            }

            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            UnityEngine.Debug.Log($"[RealPlayTester] Added {scenePath} to Build Settings");
        }
    }
}
#endif
