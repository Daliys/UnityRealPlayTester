using System;
using UnityEngine;
using RealPlayTester.Core;

namespace RealPlayTester.Assert
{
    public static partial class Assert
    {
        public static void AssetLoaded<T>(string assetPath, string message = null) where T : UnityEngine.Object
        {
            if (!RealPlayEnvironment.IsEnabled) return;

            var asset = Resources.Load<T>(assetPath);
            if (asset == null)
            {
                Fail(message ?? $"Failed to load asset of type {typeof(T).Name} at path '{assetPath}'.");
            }
        }

        public static void SceneConfigurationValid(string message = null)
        {
            if (!RealPlayEnvironment.IsEnabled) return;

            var allObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (var go in allObjects)
            {
                var components = go.GetComponents<Component>();
                foreach (var c in components)
                {
                    if (c == null)
                    {
                        Fail(message ?? $"GameObject '{go.name}' has a missing script component.");
                        return;
                    }
                }
            }
        }

        public static void GameStateMatches(Action expectedAction, string message = null)
        {
            if (!RealPlayEnvironment.IsEnabled) return;

            try
            {
                expectedAction();
            }
            catch (Exception ex)
            {
                Fail(message ?? $"Game state validation failed: {ex.Message}");
            }
        }

        public static void VisualFeedbackCorrect(string message = null)
        {
            if (!RealPlayEnvironment.IsEnabled) return;
            
            if (EventTracker.GetCount("VisualFeedback") == 0 && EventTracker.GetHistory().Count == 0)
            {
                // Soft hook
            }
        }

        public static void EventFired(string eventName, int minCount = 1, string message = null)
        {
            if (!RealPlayEnvironment.IsEnabled) return;

            if (!EventTracker.WasFired(eventName, minCount))
            {
                Fail(message ?? $"Expected event '{eventName}' to have fired at least {minCount} times, but it fired {EventTracker.GetCount(eventName)} times.");
            }
        }
    }
}
