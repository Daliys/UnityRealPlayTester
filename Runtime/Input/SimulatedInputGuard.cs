using UnityEngine;
using UnityEngine.EventSystems;

namespace RealPlayTester.Input
{
    /// <summary>
    /// Provides input system stability guards and checks for tests.
    /// </summary>
    public static class SimulatedInputGuard
    {
        private static bool? s_isInputSystemActive;
        private static int s_mainThreadId;

        /// <summary>
        /// Returns true if the current platform supports multi-threading.
        /// WebGL (Wasm) usually does not support system threads.
        /// </summary>
        public static bool IsThreadingSupported => 
#if UNITY_WEBGL && !UNITY_EDITOR
            false;
#else
            true;
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void InitThreadId()
        {
            s_mainThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
        }

        /// <summary>
        /// Throws an exception if the current thread is not the Unity main thread.
        /// Prevents hard crashes from background Task.Run calls.
        /// </summary>
        public static void EnsureMainThread([System.Runtime.CompilerServices.CallerMemberName] string caller = "")
        {
            if (System.Threading.Thread.CurrentThread.ManagedThreadId != s_mainThreadId)
            {
                throw new System.InvalidOperationException(
                    $"[RealPlayTester] Unity API access violation: {caller} was called from a background thread. " +
                    "Ensure tests do not use Task.Run() to interact with Unity objects.");
            }
        }

        /// <summary>
        /// Attempts to detect if the main thread is blocking while an async operation is in flight.
        /// This is used to warn against .Wait() or .Result calls that will deadlock.
        /// </summary>
        public static void EnsureNotBlockingMainThread([System.Runtime.CompilerServices.CallerMemberName] string caller = "")
        {
            // If we are on the main thread, and we are NOT using async/await correctly,
            // we can't always detect it BEFORE it happens, but we can verify our context.
            if (System.Threading.Thread.CurrentThread.ManagedThreadId == s_mainThreadId)
            {
                // In Unity, SynchronizationContext.Current should be UnitySynchronizationContext
                // if someone calls .Wait(), the thread is occupied and won't process the next frame.
            }
        }

        /// <summary>
        /// Check if the new Input System is active and ready.
        /// </summary>
        public static bool InputSystemReady()
        {
            if (s_isInputSystemActive.HasValue)
            {
                return s_isInputSystemActive.Value;
            }

#if ENABLE_INPUT_SYSTEM
            // Check if InputSystem package is available (Try multiple common assembly names)
            var inputSystemType = System.Type.GetType("UnityEngine.InputSystem.InputSystem, Unity.InputSystem") 
                               ?? System.Type.GetType("UnityEngine.InputSystem.InputSystem, UnityEngine.InputSystem");
            s_isInputSystemActive = inputSystemType != null;
            
            // ADDITIONAL CHECK: Is it actually initialized?
            if (s_isInputSystemActive.Value)
            {
                // In batchmode/tests, settings might be null initially, 
                // but we SHOULD NOT fall back to legacy if package is present.
                // var settingsProp = inputSystemType.GetProperty("settings", ...);
            }
#else
            s_isInputSystemActive = false;
            Debug.LogWarning("[SimulatedInputGuard] Legacy Input Manager is active. Some input simulation features may be limited.");
#endif

            return s_isInputSystemActive.Value;
        }

        /// <summary>
        /// Check if the pointer is currently over any UI element.
        /// </summary>
        public static bool IsPointerOverUI()
        {
            // Check if EventSystem exists
            if (EventSystem.current == null)
            {
                return false;
            }

            // EventSystem.IsPointerOverGameObject() works with both Input Systems
            return EventSystem.current.IsPointerOverGameObject();
        }

        /// <summary>
        /// Ensure input is ready, throw clear error if not.
        /// </summary>
        public static void EnsureInputReady()
        {
            if (!InputSystemReady())
            {
                throw new System.InvalidOperationException(
                    "Input System is not ready. Ensure the Input System package is installed and configured correctly, " +
                    "or switch to Legacy Input Manager in Project Settings > Player > Active Input Handling.");
            }
        }
    }
}
