using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using RealPlayTester.Await;
using RealPlayTester.Input;
using AssertLib = RealPlayTester.Assert.Assert;

namespace RealPlayTester.Core
{
    /// <summary>
    /// Single entry surface exposing all helper methods for AI-friendly usage.
    /// Provides complete facade over Click, Press, Drag, Wait, and Assert APIs.
    /// </summary>
    public static partial class Tester
    {
        public static class Settings
        {
            public static PreferredInputMode PreferredMode { get => RealPlaySettings.PreferredMode; set => RealPlaySettings.PreferredMode = value; }
            public static bool ShowVisualPointer { get => RealPlaySettings.ShowVisualPointer; set => RealPlaySettings.ShowVisualPointer = value; }
            public static bool ShowVisualAnchors { get => RealPlaySettings.ShowVisualAnchors; set => RealPlaySettings.ShowVisualAnchors = value; }
            public static bool ShowInteractionHeatmap { get => RealPlaySettings.ShowInteractionHeatmap; set => RealPlaySettings.ShowInteractionHeatmap = value; }
            public static bool ForceBatchmodeVisibility { get => RealPlaySettings.ForceBatchmodeVisibility; set => RealPlaySettings.ForceBatchmodeVisibility = value; }
            public static bool EnableInputHeartbeat { get => RealPlaySettings.EnableInputHeartbeat; set => RealPlaySettings.EnableInputHeartbeat = value; }
            public static bool MirrorLogsToStdout { get => RealPlaySettings.MirrorLogsToStdout; set => RealPlaySettings.MirrorLogsToStdout = value; }
            public static bool EnableNavigationLearning { get => RealPlaySettings.EnableNavigationLearning; set => RealPlaySettings.EnableNavigationLearning = value; }
            public static bool FilterDOMToViewport { get => RealPlaySettings.FilterDOMToViewport; set => RealPlaySettings.FilterDOMToViewport = value; }
            public static bool CollapseRepetitiveSiblings { get => RealPlaySettings.CollapseRepetitiveSiblings; set => RealPlaySettings.CollapseRepetitiveSiblings = value; }
            
            public static bool AutoSimulatePhysics 
            { 
                get => RealPlaySettings.AutoSimulatePhysics2D && RealPlaySettings.AutoSimulatePhysics3D; 
                set { RealPlaySettings.AutoSimulatePhysics2D = value; RealPlaySettings.AutoSimulatePhysics3D = value; } 
            }
            public static bool AutoSimulatePhysics2D { get => RealPlaySettings.AutoSimulatePhysics2D; set => RealPlaySettings.AutoSimulatePhysics2D = value; }
            public static bool AutoSimulatePhysics3D { get => RealPlaySettings.AutoSimulatePhysics3D; set => RealPlaySettings.AutoSimulatePhysics3D = value; }
        }

        // ===== NESTED API GROUPS =====

        public static class Wait
        {
            public static Task Seconds(float seconds, bool unscaled = false) => RealPlayEnvironment.IsEnabled ? RealPlayTester.Await.Wait.Seconds(seconds, unscaled) : Task.CompletedTask;
            public static Task Frames(int frames) => RealPlayEnvironment.IsEnabled ? RealPlayTester.Await.Wait.Frames(frames) : Task.CompletedTask;
            public static Task Until(Func<bool> predicate, float? timeoutSeconds = null) => RealPlayEnvironment.IsEnabled ? RealPlayTester.Await.Wait.Until(predicate, timeoutSeconds) : Task.CompletedTask;
            public static Task SceneLoaded(string sceneName, float? timeoutSeconds = null) => RealPlayEnvironment.IsEnabled ? RealPlayTester.Await.Wait.SceneLoaded(sceneName, timeoutSeconds) : Task.CompletedTask;
            public static Task ForObject(string name, float? timeoutSeconds = null) => RealPlayEnvironment.IsEnabled ? RealPlayTester.Await.Wait.ForObject(name, timeoutSeconds) : Task.CompletedTask;
            public static Task ForComponent<T>(float? timeoutSeconds = null) where T : Component => RealPlayEnvironment.IsEnabled ? RealPlayTester.Await.Wait.ForComponent<T>(timeoutSeconds) : Task.CompletedTask;
            public static Task ForUIVisible(string name, float? timeoutSeconds = null) => RealPlayEnvironment.IsEnabled ? RealPlayTester.Await.Wait.ForUIVisible(name, timeoutSeconds) : Task.CompletedTask;
            public static Task ForInteractable<T>(string textFilter = null, float? timeoutSeconds = null) where T : Component => RealPlayEnvironment.IsEnabled ? RealPlayTester.Await.Wait.ForInteractable<T>(textFilter, timeoutSeconds) : Task.CompletedTask;
            public static Task ForAnimationState(Animator animator, string stateName, float? timeoutSeconds = null) => RealPlayEnvironment.IsEnabled ? RealPlayTester.Await.Wait.ForAnimationState(animator, stateName, timeoutSeconds) : Task.CompletedTask;
            public static Task ForAudioComplete(AudioSource source, float? timeoutSeconds = null) => RealPlayEnvironment.IsEnabled ? RealPlayTester.Await.Wait.ForAudioComplete(source, timeoutSeconds) : Task.CompletedTask;
            public static Task While(Func<bool> predicate, float? timeoutSeconds = null) => RealPlayEnvironment.IsEnabled ? RealPlayTester.Await.Wait.While(predicate, timeoutSeconds) : Task.CompletedTask;
            public static Task ForLoadingComplete(string loadingObjectName = "LoadingScreen", float? timeoutSeconds = 30f) => RealPlayEnvironment.IsEnabled ? RealPlayTester.Await.Wait.ForLoadingComplete(loadingObjectName, timeoutSeconds) : Task.CompletedTask;
            public static Task ForStablePhysics(float velocityThreshold = 0.01f, float? timeoutSeconds = null) => RealPlayEnvironment.IsEnabled ? RealPlayTester.Await.Wait.ForStablePhysics(velocityThreshold, timeoutSeconds) : Task.CompletedTask;
            public static void Step(string label) { if (RealPlayEnvironment.IsEnabled) RealPlayTester.Await.Wait.Step(label); }
        }

        public static class Assert
        {
            public static void IsTrue(bool condition, string message = null) => AssertLib.IsTrue(condition, message);
            public static void IsFalse(bool condition, string message = null) => AssertLib.IsFalse(condition, message);
            public static void AreEqual<T>(T expected, T actual, string message = null) => AssertLib.AreEqual(expected, actual, message);
            public static void IsNull(object value, string message = null) => AssertLib.IsNull(value, message);
            public static void IsNotNull(object value, string message = null) => AssertLib.IsNotNull(value, message);
            public static void Fail(string message = null) => AssertLib.Fail(message);
            public static void Greater<T>(T value, T threshold, string message = null) where T : IComparable<T> => AssertLib.Greater(value, threshold, message);
            public static void Less<T>(T value, T threshold, string message = null) where T : IComparable<T> => AssertLib.Less(value, threshold, message);
            public static void Contains(string haystack, string needle, string message = null) => AssertLib.Contains(haystack, needle, message);
            public static void InRange<T>(T value, T min, T max, string message = null) where T : IComparable<T> => AssertLib.InRange(value, min, max, message);
            public static void Throws<TException>(Action action, string message = null) where TException : Exception => AssertLib.Throws<TException>(action, message);
            public static void StateMatches<T>(T stateObject, string baselineJson, string message = null)
            {
                if (!RealPlayEnvironment.IsEnabled) return;
                string currentState = Utilities.StateTracker.CaptureObjectState(stateObject);
                if (currentState != baselineJson)
                {
                    string diff = Utilities.StateTracker.GetJsonDiff(baselineJson, currentState);
                    AssertLib.Fail($"{message ?? "State mismatch detected."}\n\n[JSON DIFF]\n{diff}");
                }
            }
            public static void IsVisible(GameObject go, string message = null) => AssertLib.IsVisible(go, message);
            public static void HasSprite(Component target, Sprite expected, string message = null) => AssertLib.HasSprite(target, expected, message);
            public static void ScreenElementVisible(string name, string msg = null) => AssertLib.ScreenElementVisible(name, msg);
            public static void VisualStateMatches(string name, string msg = null) => AssertLib.VisualStateMatches(name, msg);
            public static void NoMissingMaterials(string msg = null) => AssertLib.NoMissingMaterials(msg);
            public static void AssetLoaded<T>(string path, string msg = null) where T : UnityEngine.Object => AssertLib.AssetLoaded<T>(path, msg);
            public static void NoUnexpectedErrors() => RealPlayTester.Assert.LogAssert.NoUnexpectedErrors();
            public static void ExpectLog(string regex) => RealPlayTester.Assert.LogAssert.Expect(regex);
        }

        public static class Screenshot
        {
            public static string Capture(string name = null) => RealPlayEnvironment.IsEnabled ? RealPlayTester.Assert.Capture.Screenshot(name) : string.Empty;
            public static void Compare(string path, float threshold = 0.95f) { if (RealPlayEnvironment.IsEnabled) RealPlayTester.Assert.Capture.CompareToBaseline(path, threshold); }
            public static void CaptureAndCompare(string testName) { if (RealPlayEnvironment.IsEnabled) RealPlayTester.Core.Screenshot.CaptureAndCompare(testName); }
            public static void CaptureAndCompareRegion(string testName, Rect region) { if (RealPlayEnvironment.IsEnabled) RealPlayTester.Core.Screenshot.CaptureAndCompareRegion(testName, region); }
            public static void SaveHeatmap(string testName) { if (RealPlayEnvironment.IsEnabled) RealPlayTester.Core.Screenshot.SaveHeatmapAsPNG(testName); }
            public static Texture2D CaptureWithAnnotations(List<GameObject> targets, out Dictionary<string, string> labelMap)
            {
                if (RealPlayEnvironment.IsEnabled) return RealPlayTester.Core.Screenshot.CaptureWithAnnotations(targets, out labelMap);
                labelMap = new Dictionary<string, string>();
                return null;
            }
        }

        public static class Monitoring
        {
            public static void StartVisualHealthCheck(float interval = 2.0f) => RealPlayTester.Utilities.VisualHealthMonitor.StartMonitoring(interval);
            public static void StopVisualHealthCheck() => RealPlayTester.Utilities.VisualHealthMonitor.StopMonitoring();
        }

        public static class Events
        {
            public static void Record(string eventName) => RealPlayTester.Core.EventTracker.Record(eventName);
            public static void Clear() => RealPlayTester.Core.EventTracker.Clear();
            public static bool WasFired(string eventName, int min = 1) => RealPlayTester.Core.EventTracker.WasFired(eventName, min);
        }

        public static class Navigation
        {
             private static readonly RealPlayTester.Core.Navigation.NavigationGraph _graph = new RealPlayTester.Core.Navigation.NavigationGraph();

             public static void RegisterPath(string from, string to, Func<Task> navigationAction)
                 => _graph.RegisterPath(from, to, navigationAction);

             public static void RecordStateTransition(string from, string to, string intent, string targetName)
                 => _graph.RecordStateTransition(from, to, intent, targetName);

             public static void Clear() => _graph.Clear();

             public static Task<bool> Navigate(string from, string to)
                 => RealPlayEnvironment.IsEnabled ? _graph.Navigate(from, to) : Task.FromResult(false);

             public static string ExportToJson() => _graph.ExportToJson();
             public static void ImportFromJson(string json) => _graph.ImportFromJson(json);
             public static void SaveToFile(string path) => _graph.SaveToFile(path);
             public static void LoadFromFile(string path) => _graph.LoadFromFile(path);
        }
    }
}