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
            // Check if InputSystem package is available
            var inputSystemType = System.Type.GetType("UnityEngine.InputSystem.InputSystem, Unity.InputSystem");
            s_isInputSystemActive = inputSystemType != null;
            
            if (!s_isInputSystemActive.Value)
            {
                Debug.LogWarning("[SimulatedInputGuard] Input System is enabled in project settings but package not found. Using legacy input fallback.");
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

#if ENABLE_INPUT_SYSTEM
            // New Input System
            var inputSystemType = System.Type.GetType("UnityEngine.InputSystem.InputSystem, Unity.InputSystem");
            if (inputSystemType != null)
            {
                var mouseType = System.Type.GetType("UnityEngine.InputSystem.Mouse, Unity.InputSystem");
                if (mouseType != null)
                {
                    var currentProp = mouseType.GetProperty("current", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    var currentMouse = currentProp?.GetValue(null);
                    
                    if (currentMouse != null)
                    {
                        var positionProp = currentMouse.GetType().GetProperty("position");
                        if (positionProp != null)
                        {
                            var pointerPosition = (UnityEngine.InputSystem.InputControl<Vector2>)positionProp.GetValue(currentMouse);
                            return EventSystem.current.IsPointerOverGameObject();
                        }
                    }
                }
            }
#endif

            // Legacy Input Manager fallback
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
