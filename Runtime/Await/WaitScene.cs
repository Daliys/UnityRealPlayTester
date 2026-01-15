using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using RealPlayTester.Core;

namespace RealPlayTester.Await
{
    public static partial class Wait
    {
        public static Task ForObject(string name, float? timeoutSeconds = null)
        {
            if (!RealPlayEnvironment.IsEnabled) return Task.CompletedTask;
            
            // STABILITY FIX: Also look for inactive objects as they might be about to activate
            return Until(() => 
            {
                var all = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach(var go in all) if (go.name == name) return true;
                return false;
            }, timeoutSeconds, $"Object '{name}' to exist");
        }

        public static Task ForComponent<T>(float? timeoutSeconds = null) where T : Component
        {
            if (!RealPlayEnvironment.IsEnabled) return Task.CompletedTask;
            return Until(() => UnityEngine.Object.FindFirstObjectByType<T>(FindObjectsInactive.Include) != null, timeoutSeconds, $"Component<{typeof(T).Name}> to exist");
        }

        public static Task ForLoadingComplete(string loadingObjectName = "LoadingScreen", float? timeoutSeconds = 30f)
        {
            if (!RealPlayEnvironment.IsEnabled) return Task.CompletedTask;

            return Until(() =>
            {
                var all = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach(var go in all) if (go.name == loadingObjectName && go.activeInHierarchy) return false;
                return true;
            }, timeoutSeconds, $"Loading screen '{loadingObjectName}' to disappear");
        }
        public static async Task SceneLoaded(string sceneName, float? timeoutSeconds = null)
        {
            if (!RealPlayEnvironment.IsEnabled) return;

            await Until(() => IsSceneLoaded(sceneName), timeoutSeconds, $"Scene '{sceneName}' to be loaded");
        }

        private static bool IsSceneLoaded(string name)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var s = SceneManager.GetSceneAt(i);
                if (s.name == name && s.isLoaded) return true;
            }
            return false;
        }
    }
}
