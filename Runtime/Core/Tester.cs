using System;
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
    public static class Tester
    {
        // ===== CLICK HELPERS =====

        /// <summary>Click at screen position as percentage (0-1 range).</summary>
        public static Task ClickScreenPercent(float x, float y) => 
            RealPlayEnvironment.IsEnabled ? Click.ScreenPercent(x, y) : Task.CompletedTask;

        /// <summary>Click at exact screen pixel position.</summary>
        public static Task ClickScreenPixels(float x, float y) => 
            RealPlayEnvironment.IsEnabled ? Click.ScreenPixels(x, y) : Task.CompletedTask;

        /// <summary>Click at world position.</summary>
        public static Task ClickWorldPosition(Vector3 worldPosition, Camera camera = null) => 
            RealPlayEnvironment.IsEnabled ? Click.WorldPosition(worldPosition, camera) : Task.CompletedTask;

        /// <summary>Click on a specific GameObject in world space.</summary>
        public static Task ClickWorldObject(GameObject target, Camera camera = null) => 
            RealPlayEnvironment.IsEnabled ? Click.WorldObject(target, camera) : Task.CompletedTask;

        /// <summary>Raycast from camera and return hit result.</summary>
        public static RaycastResult RaycastFromCamera(Camera camera, Vector2 screenPosition) =>
            RealPlayEnvironment.IsEnabled ? Click.RaycastFromCamera(camera, screenPosition) : default;

        // ===== PRESS HELPERS =====

        /// <summary>Press and hold a key for the specified duration.</summary>
        public static Task PressKey(KeyCode key, float durationSeconds) =>
            RealPlayEnvironment.IsEnabled ? Press.Key(key, durationSeconds) : Task.CompletedTask;

        /// <summary>Press a key down (without releasing).</summary>
        public static Task PressKeyDown(KeyCode key) =>
            RealPlayEnvironment.IsEnabled ? Press.KeyDown(key) : Task.CompletedTask;

        /// <summary>Release a previously pressed key.</summary>
        public static Task PressKeyUp(KeyCode key) =>
            RealPlayEnvironment.IsEnabled ? Press.KeyUp(key) : Task.CompletedTask;

        // ===== DRAG HELPERS =====

        /// <summary>Drag from start to end screen position over duration.</summary>
        public static Task DragFromTo(Vector2 startScreenPos, Vector2 endScreenPos, float durationSeconds) =>
            RealPlayEnvironment.IsEnabled ? Drag.FromTo(startScreenPos, endScreenPos, durationSeconds) : Task.CompletedTask;

        // ===== WAIT HELPERS =====

        /// <summary>Wait for specified seconds (scaled time by default).</summary>
        public static Task WaitSeconds(float seconds, bool unscaled = false) =>
            RealPlayEnvironment.IsEnabled ? Wait.Seconds(seconds, unscaled) : Task.CompletedTask;

        /// <summary>Wait for specified number of frames.</summary>
        public static Task WaitFrames(int frames) =>
            RealPlayEnvironment.IsEnabled ? Wait.Frames(frames) : Task.CompletedTask;

        /// <summary>Wait until predicate returns true.</summary>
        public static Task WaitUntil(Func<bool> predicate, float? timeoutSeconds = null) =>
            RealPlayEnvironment.IsEnabled ? Wait.Until(predicate, timeoutSeconds) : Task.CompletedTask;

        /// <summary>Wait until a scene is loaded.</summary>
        public static Task WaitSceneLoaded(string sceneName, float? timeoutSeconds = null) =>
            RealPlayEnvironment.IsEnabled ? Wait.SceneLoaded(sceneName, timeoutSeconds) : Task.CompletedTask;

        /// <summary>Wait for a GameObject by name to exist.</summary>
        public static Task WaitForObject(string name, float? timeoutSeconds = null) =>
            RealPlayEnvironment.IsEnabled ? Wait.ForObject(name, timeoutSeconds) : Task.CompletedTask;

        /// <summary>Wait for a component of type T to exist.</summary>
        public static Task WaitForComponent<T>(float? timeoutSeconds = null) where T : Component =>
            RealPlayEnvironment.IsEnabled ? Wait.ForComponent<T>(timeoutSeconds) : Task.CompletedTask;

        /// <summary>Wait for a UI element (by name) to be visible and interactable.</summary>
        public static Task WaitForUIVisible(string name, float? timeoutSeconds = null) =>
            RealPlayEnvironment.IsEnabled ? Wait.ForUIVisible(name, timeoutSeconds) : Task.CompletedTask;

        /// <summary>Wait for an Animator to reach a specific state.</summary>
        public static Task WaitForAnimationState(Animator animator, string stateName, float? timeoutSeconds = null) =>
            RealPlayEnvironment.IsEnabled ? Wait.ForAnimationState(animator, stateName, timeoutSeconds) : Task.CompletedTask;

        /// <summary>Wait for an AudioSource to finish playing.</summary>
        public static Task WaitForAudioComplete(AudioSource source, float? timeoutSeconds = null) =>
            RealPlayEnvironment.IsEnabled ? Wait.ForAudioComplete(source, timeoutSeconds) : Task.CompletedTask;

        /// <summary>Wait while predicate remains true (opposite of Until).</summary>
        public static Task WaitWhile(Func<bool> predicate, float? timeoutSeconds = null) =>
            RealPlayEnvironment.IsEnabled ? Wait.While(predicate, timeoutSeconds) : Task.CompletedTask;

        /// <summary>Wait for loading screen (by name) to disappear.</summary>
        public static Task WaitForLoadingComplete(string loadingObjectName = "LoadingScreen", float? timeoutSeconds = 30f) =>
            RealPlayEnvironment.IsEnabled ? Wait.ForLoadingComplete(loadingObjectName, timeoutSeconds) : Task.CompletedTask;

        // ===== ASSERT HELPERS =====

        /// <summary>Assert condition is true. On failure: screenshot, pause, overlay, throw.</summary>
        public static void AssertTrue(bool condition, string message = null)
        {
            if (RealPlayEnvironment.IsEnabled) AssertLib.IsTrue(condition, message);
        }

        /// <summary>Assert two values are equal. On failure: screenshot, pause, overlay, throw.</summary>
        public static void AssertAreEqual<T>(T expected, T actual, string message = null)
        {
            if (RealPlayEnvironment.IsEnabled) AssertLib.AreEqual(expected, actual, message);
        }

        /// <summary>Immediately fail with message. Takes screenshot, pauses, overlays, throws.</summary>
        public static void AssertFail(string message = null)
        {
            if (RealPlayEnvironment.IsEnabled) AssertLib.Fail(message);
        }

        public static void AssertFalse(bool condition, string message = null)
        {
            if (RealPlayEnvironment.IsEnabled) AssertLib.IsFalse(condition, message);
        }

        public static void AssertNull(object value, string message = null)
        {
            if (RealPlayEnvironment.IsEnabled) AssertLib.IsNull(value, message);
        }

        public static void AssertNotNull(object value, string message = null)
        {
            if (RealPlayEnvironment.IsEnabled) AssertLib.IsNotNull(value, message);
        }

        public static void AssertGreater<T>(T value, T threshold, string message = null) where T : IComparable<T>
        {
            if (RealPlayEnvironment.IsEnabled) AssertLib.Greater(value, threshold, message);
        }

        public static void AssertLess<T>(T value, T threshold, string message = null) where T : IComparable<T>
        {
            if (RealPlayEnvironment.IsEnabled) AssertLib.Less(value, threshold, message);
        }

        public static void AssertContains(string haystack, string needle, string message = null)
        {
            if (RealPlayEnvironment.IsEnabled) AssertLib.Contains(haystack, needle, message);
        }

        public static void AssertInRange<T>(T value, T min, T max, string message = null) where T : IComparable<T>
        {
            if (RealPlayEnvironment.IsEnabled) AssertLib.InRange(value, min, max, message);
        }

        public static void AssertThrows<TException>(Action action, string message = null) where TException : Exception
        {
            if (RealPlayEnvironment.IsEnabled) AssertLib.Throws<TException>(action, message);
        }

        // ===== CAPTURE HELPERS =====

        public static string CaptureScreenshot(string name = null) =>
            RealPlayEnvironment.IsEnabled ? RealPlayTester.Assert.Capture.Screenshot(name) : string.Empty;

        public static void CaptureCompareToBaseline(string baselinePath, float threshold = 0.95f)
        {
            if (RealPlayEnvironment.IsEnabled)
            {
                RealPlayTester.Assert.Capture.CompareToBaseline(baselinePath, threshold);
            }
        }

        public static void CaptureStartRecording()
        {
            if (RealPlayEnvironment.IsEnabled)
            {
                RealPlayTester.Assert.Capture.StartRecording();
            }
        }

        public static void CaptureStopRecording(string outputPath = null)
        {
            if (RealPlayEnvironment.IsEnabled)
            {
                RealPlayTester.Assert.Capture.StopRecording(outputPath);
            }
        }

        // ===== DEBUG HELPERS =====

        public static Task DebugBreakpoint(KeyCode resumeKey = KeyCode.Space) =>
            RealPlayEnvironment.IsEnabled ? RealPlayTester.Utilities.DevTools.Breakpoint(resumeKey) : Task.CompletedTask;

        public static void DebugShowClickMarker(Vector2 screenPos, float duration = 0.5f)
        {
            if (RealPlayEnvironment.IsEnabled) RealPlayTester.Utilities.DevTools.ShowClickMarker(screenPos, duration);
        }

        public static void DebugSetSlowMotion(float timeScale = 0.25f)
        {
            if (RealPlayEnvironment.IsEnabled) RealPlayTester.Utilities.DevTools.SetSlowMotion(timeScale);
        }

        public static void DebugInspect<T>(string name, T value)
        {
            if (RealPlayEnvironment.IsEnabled) RealPlayTester.Utilities.DevTools.Inspect(name, value);
        }

        // ===== TEXT HELPERS =====

        /// <summary>Type text into the currently focused input field (InputField or TMP_InputField).</summary>
        public static Task TextType(string text, float delayBetweenChars = 0.05f) =>
            RealPlayEnvironment.IsEnabled ? RealPlayTester.Input.Text.Type(text, delayBetweenChars) : Task.CompletedTask;

        /// <summary>Type text into a specific input field by GameObject name.</summary>
        public static Task TextTypeIntoField(string fieldName, string text, float delayBetweenChars = 0.05f) =>
            RealPlayEnvironment.IsEnabled ? RealPlayTester.Input.Text.TypeIntoField(fieldName, text, delayBetweenChars) : Task.CompletedTask;

        // ===== UI FIND HELPERS =====

        public static Task ClickButtonWithText(string buttonText) =>
            RealPlayEnvironment.IsEnabled ? RealPlayTester.Input.Click.ButtonWithText(buttonText) : Task.CompletedTask;

        public static Task ClickObjectNamed(string name) =>
            RealPlayEnvironment.IsEnabled ? RealPlayTester.Input.Click.ObjectNamed(name) : Task.CompletedTask;

        public static Task ClickComponent<T>() where T : Component =>
            RealPlayEnvironment.IsEnabled ? RealPlayTester.Input.Click.Component<T>() : Task.CompletedTask;

        // ===== TOUCH HELPERS =====

        public static Task TouchTap(Vector2 screenPos, float duration = 0.1f) =>
            RealPlayEnvironment.IsEnabled ? RealPlayTester.Input.Touch.Tap(screenPos, duration) : Task.CompletedTask;

        public static Task TouchSwipe(Vector2 from, Vector2 to, float duration = 0.3f) =>
            RealPlayEnvironment.IsEnabled ? RealPlayTester.Input.Touch.Swipe(from, to, duration) : Task.CompletedTask;

        public static Task TouchPinch(Vector2 center, float startDistance, float endDistance, float duration = 0.5f) =>
            RealPlayEnvironment.IsEnabled ? RealPlayTester.Input.Touch.Pinch(center, startDistance, endDistance, duration) : Task.CompletedTask;

        public static Task TouchLongPress(Vector2 screenPos, float duration = 1.0f) =>
            RealPlayEnvironment.IsEnabled ? RealPlayTester.Input.Touch.LongPress(screenPos, duration) : Task.CompletedTask;

        // ===== SCROLL HELPERS =====

        public static Task ScrollToBottom(ScrollRect scrollRect, float duration = 0.5f) =>
            RealPlayEnvironment.IsEnabled ? RealPlayTester.Input.Scroll.ToBottom(scrollRect, duration) : Task.CompletedTask;

        public static Task ScrollUntilVisible(ScrollRect scrollRect, RectTransform target, float timeoutSeconds = 5f) =>
            RealPlayEnvironment.IsEnabled ? RealPlayTester.Input.Scroll.UntilVisible(scrollRect, target, timeoutSeconds) : Task.CompletedTask;
        // ===== DEBUGGING & LOGGING =====

        /// <summary>Returns a string dump of the active scene hierarchy for debugging.</summary>
        public static string DumpHierarchy() => 
            RealPlayEnvironment.IsEnabled ? RealPlayTester.Utilities.VisualTreeLogger.DumpHierarchy() : string.Empty;

        /// <summary>Probes the screen at position to see what UI/World elements are hit.</summary>
        public static string ProbeScreen(Vector2 screenPos) =>
            RealPlayEnvironment.IsEnabled ? RealPlayTester.Utilities.InteractionProbe.ProbeScreen(screenPos) : string.Empty;

        /// <summary>Assert that no unexpected error logs occurred during the test.</summary>
        public static void AssertNoLogErrors() =>
            RealPlayTester.Assert.LogAssert.NoUnexpectedErrors();

        /// <summary>Expect a specific error log pattern (Regex) to appear (and ignore it).</summary>
        public static void ExpectLog(string regexPattern) =>
            RealPlayTester.Assert.LogAssert.Expect(regexPattern);

        /// <summary>Find a GameObject using fuzzy matching (Exact -> CaseInsensitive -> Contains).</summary>
        public static GameObject FindObject(string fuzzyName) =>
            RealPlayEnvironment.IsEnabled ? RealPlayTester.Input.SmartFind.Object(fuzzyName) : null;
    }
}
