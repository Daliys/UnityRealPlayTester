using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
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
    }
}
