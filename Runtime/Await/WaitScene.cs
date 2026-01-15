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
            return Until(() => GameObject.Find(name) != null, timeoutSeconds);
        }

        public static Task ForComponent<T>(float? timeoutSeconds = null) where T : Component
        {
            if (!RealPlayEnvironment.IsEnabled) return Task.CompletedTask;
            return Until(() => GameObject.FindFirstObjectByType<T>() != null, timeoutSeconds);
        }

        public static Task ForLoadingComplete(string loadingObjectName = "LoadingScreen", float? timeoutSeconds = 30f)
        {
            if (!RealPlayEnvironment.IsEnabled) return Task.CompletedTask;

            return Until(() =>
            {
                var obj = GameObject.Find(loadingObjectName);
                return obj == null || obj.activeInHierarchy == false;
            }, timeoutSeconds);
        }
        public static async Task SceneLoaded(string sceneName, float? timeoutSeconds = null)
        {
            if (!RealPlayEnvironment.IsEnabled) return;

            var scene = SceneManager.GetSceneByName(sceneName);
            if (scene.isLoaded) return;

            await WaitForSceneLoop(sceneName, timeoutSeconds);
        }

        private static async Task WaitForSceneLoop(string sceneName, float? timeoutSeconds)
        {
            float startTime = Time.realtimeSinceStartup;
            float timeout = timeoutSeconds ?? float.MaxValue;
            var token = RealPlayExecutionContext.Token;

            while (!SceneManager.GetSceneByName(sceneName).isLoaded)
            {
                token.ThrowIfCancellationRequested();
                if (Time.realtimeSinceStartup - startTime >= timeout) throw new TimeoutException($"Scene '{sceneName}' failed to load.");
                await Task.Yield();
            }
        }
    }
}
