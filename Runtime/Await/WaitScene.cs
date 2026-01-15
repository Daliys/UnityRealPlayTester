using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using RealPlayTester.Core;
using RealPlayTester.Utilities;

namespace RealPlayTester.Await
{
    public static partial class Wait
    {
        public static Task ForObject(string name, float? timeoutSeconds = null)
        {
            if (!RealPlayEnvironment.IsEnabled) return Task.CompletedTask;
            
            return Until(() => 
            {
                var all = SceneCache.Instance.AllObjects;
                foreach(var go in all) if (go != null && go.name == name) return true;
                return false;
            }, timeoutSeconds, $"Object '{name}' to exist");
        }

        public static Task ForComponent<T>(float? timeoutSeconds = null) where T : Component
        {
            if (!RealPlayEnvironment.IsEnabled) return Task.CompletedTask;
            return Until(() => 
            {
                // Optimization: use specialized lists if available
                if (typeof(T) == typeof(Rigidbody)) return SceneCache.Instance.Rigidbodies3D.Count > 0;
                if (typeof(T) == typeof(Rigidbody2D)) return SceneCache.Instance.Rigidbodies2D.Count > 0;
                if (typeof(T) == typeof(Animator)) return SceneCache.Instance.Animators.Count > 0;
                if (typeof(T) == typeof(ParticleSystem)) return SceneCache.Instance.ParticleSystems.Count > 0;

                var all = SceneCache.Instance.AllObjects;
                foreach(var go in all) if (go != null && go.TryGetComponent<T>(out _)) return true;
                return false;
            }, timeoutSeconds, $"Component<{typeof(T).Name}> to exist");
        }

        public static Task ForLoadingComplete(string loadingObjectName = "LoadingScreen", float? timeoutSeconds = 30f)
        {
            if (!RealPlayEnvironment.IsEnabled) return Task.CompletedTask;

            return Until(() =>
            {
                var all = SceneCache.Instance.AllObjects;
                foreach(var go in all) if (go != null && go.name == loadingObjectName && go.activeInHierarchy) return false;
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
