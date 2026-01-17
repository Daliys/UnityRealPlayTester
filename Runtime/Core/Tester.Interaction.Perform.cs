using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using RealPlayTester.Input;
using RealPlayTester.Utilities;
using RealPlayTester.Core.Perception;
using RealPlayTester.Diagnostics;
using RealPlayTester.Await;

namespace RealPlayTester.Core
{
    public static partial class Tester
    {
        public static partial class Interaction
        {
            /// <summary>
            /// Performs a high-level semantic action on a target object.
            /// Intelligently chooses between Mouse, Touch, and Gamepad simulation based on RealPlaySettings.PreferredMode.
            /// Returns a Natural Language Summary of the causal reaction.
            /// </summary>
            public static async Task<string> Perform(string intent, string targetName)
            {
                if (!RealPlayEnvironment.IsEnabled) return "Environment Disabled";
                var target = Perception.Find(targetName);
                if (target == null)
                {
                    string msg = $"[Interaction] Perform('{intent}') failed: Target '{targetName}' not found.";
                    RealPlayLog.Warn(msg);
                    return msg;
                }
                RealPlayLog.Info($"[Interaction] Found target '{target.name}' for intent '{intent}'");
                return await Perform(intent, target);
            }

            /// <summary>
            /// Performs a high-level semantic action on a target object.
            /// Returns a Natural Language Summary of the causal reaction.
            /// </summary>
            public static async Task<string> Perform(string intent, GameObject target)
            {
                if (!RealPlayEnvironment.IsEnabled) return "Environment Disabled";
                if (IsInteractionBlocked(intent, target)) return "Action Blocked";

                string envError = ValidateEnvironmentForTarget(target);
                if (envError != null)
                {
                    RealPlayLog.Error($"[Interaction] Perform '{intent}' failed: {envError}");
                    return $"Error: {envError}";
                }

                string fromScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                string fromState = RealPlayTester.UI.PanelStateMonitor.GetActiveStateName();
                var stateBefore = StateTracker.Capture();
                RealPlayTester.Diagnostics.TestRunContextTracker.RecordBreadcrumb("Intent", $"Perform '{intent}' on '{(target != null ? target.name : "null")}'");

                if (target != null) CheckLogicalBlocking(target);

                await DispatchIntent(intent, target);
                
                // M014/C003: Check if scene changed during interaction
                string toScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                if (fromScene != toScene)
                {
                    RealPlayLog.Info($"[Interaction] Scene changed from '{fromScene}' to '{toScene}' during '{intent}'");
                    return $"Scene changed to {toScene}";
                }

                await Wait.Seconds(0.05f, unscaled: true);

                return AnalyzeCausalReaction(stateBefore, fromState, intent, target);
            }

            private static string AnalyzeCausalReaction(StateTracker.StateSnapshot stateBefore, string fromState, string intent, GameObject target)
            {
                bool targetDestroyed = target == null;
                string targetName = targetDestroyed ? "DestroyedObject" : target.name;

                // STABILITY FIX: Ensure we are still in a valid state
                if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == null) return "Scene Unloading";

                var stateAfter = StateTracker.Capture();
                string toState = RealPlayTester.UI.PanelStateMonitor.GetActiveStateName();
                
                string summary = StateTracker.GetDiff(stateBefore, stateAfter, $"{intent} '{targetName}'");
                RealPlayTester.Diagnostics.TestRunContextTracker.RecordBreadcrumb("Reaction", summary);

                if (!targetDestroyed) Navigation.RecordStateTransition(fromState, toState, intent, targetName);

                return summary;
            }

            private static bool IsInteractionBlocked(string intent, GameObject target)
            {
                if (target == null) return false;

                // 1. Explicit Affordance Validator (Highest priority)
                var validator = target.GetComponent<IAffordanceValidator>();
                if (validator != null && !validator.CanInteract(intent))
                {
                    RealPlayLog.Warn($"[Interaction] Perform '{intent}' on '{target.name}' blocked: {validator.GetBlockReason()}");
                    return true;
                }

                // 2. Standard UI Interactability (CanvasGroup, etc)
                if (target.GetComponent<RectTransform>() != null)
                {
                    if (!RealPlayTester.UI.PanelStateMonitor.IsPanelReady(target))
                    {
                        var state = RealPlayTester.UI.PanelStateMonitor.CheckPanelState(target);
                        RealPlayLog.Warn($"[Interaction] Perform '{intent}' on '{target.name}' blocked by UI state: {state}");
                        return true;
                    }
                }

                return false;
            }

            private static string ValidateEnvironmentForTarget(GameObject target)
            {
                if (target == null) return null;

                bool isUi = target.GetComponent<RectTransform>() != null;
                if (isUi)
                {
                    if (EventSystem.current == null && !RealPlaySettings.AutoCreateEventSystem)
                        return "No EventSystem found. UI is unclickable.";

                    var canvas = target.GetComponentInParent<Canvas>();
                    if (canvas != null && canvas.GetComponent<GraphicRaycaster>() == null)
                        return $"Canvas '{canvas.name}' is missing GraphicRaycaster. UI is unclickable.";
                }
                else
                {
                    // For world objects, check if there is a PhysicsRaycaster or if the library will add one
                    // Actually, Click.WorldObject adds one if missing, so we're good there.
                }

                return null;
            }

            private static async Task DispatchIntent(string intent, GameObject target)
            {
                switch (intent.ToLowerInvariant())
                {
                    case "select":
                    case "click":
                    case "tap":
                        await ExecuteSelectIntent(target);
                        break;
                    case "submit":
                        await ExecuteSubmitIntent(target);
                        break;
                    case "cancel":
                    case "back":
                        await ExecuteCancelIntent();
                        break;
                    case "drag":
                    case "slide":
                        if (target != null) 
                        {
                            var center = RealInputUtility.GetScreenCenter(target);
                            await Mouse.Drag(center, center + Vector2.right * 50f); // Relative drag in screen space
                        }
                        break;
                    case "move":
                    case "hover":
                        if (target != null) await Mouse.MoveTo(RealInputUtility.GetScreenCenter(target), 0.2f);
                        break;
                    default:
                        RealPlayLog.Warn($"[Interaction] Unknown intent: '{intent}'");
                        break;
                }
            }

            private static void CheckLogicalBlocking(GameObject target)
            {
                var logic = InteractionLogic.CheckLogicalInteractivity(target);
                if (logic.IsBlocked)
                {
                    RealPlayTester.Diagnostics.TestRunContextTracker.RecordBreadcrumb("Input", $"[BLOCKER] Target '{target.name}' is logically blocked: {logic.Reason}");
                    RealPlayLog.Warn($"[Interaction] Target '{target.name}' is logically blocked: {logic.Reason}. Proceeding anyway.");
                }
            }

            private static async Task ExecuteSelectIntent(GameObject target)
            {
                if (target == null) return;

                var mode = ResolveEffectiveMode();
                switch (mode)
                {
                    case PreferredInputMode.Touch:
                        await Touch.Tap(RealInputUtility.GetScreenCenter(target));
                        break;
                    case PreferredInputMode.Gamepad:
                        var selectable = target.GetComponent<Selectable>();
                        if (selectable != null) selectable.Select();
                        await Gamepad.Click(GamepadButton.South);
                        break;
                    case PreferredInputMode.Mouse:
                    default:
                        await Mouse.ClickObject(target);
                        break;
                }

                // HEURISTIC FALLBACK: If the object has non-standard click methods, invoke them
                InvokeHeuristicMethod(target, "OnClick");
            }

            private static void InvokeHeuristicMethod(GameObject target, string methodName)
            {
                var comps = target.GetComponents<MonoBehaviour>();
                foreach (var c in comps)
                {
                    if (c == null) continue;
                    var type = c.GetType();
                    
                    // Try Method
                    var method = type.GetMethod(methodName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
                    if (method != null && method.GetParameters().Length == 0)
                    {
                        method.Invoke(c, null);
                        continue;
                    }

                    // Try UnityEvent property (e.g. onClick)
                    var prop = type.GetProperty(methodName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
                    if (prop != null && typeof(UnityEngine.Events.UnityEvent).IsAssignableFrom(prop.PropertyType))
                    {
                        var unityEvent = prop.GetValue(c) as UnityEngine.Events.UnityEvent;
                        unityEvent?.Invoke();
                    }
                }
            }

            private static async Task ExecuteSubmitIntent(GameObject target)
            {
                if (target != null)
                {
                    var inputField = target.GetComponent<InputField>();
                    if (inputField != null)
                    {
                        inputField.OnPointerClick(new PointerEventData(EventSystem.current));
                        await Press.Key(KeyCode.Return, 0.1f);
                        return;
                    }
                    
                    var tmpType = System.Type.GetType("TMPro.TMP_InputField, Unity.TextMeshPro");
                    var tmp = tmpType != null ? target.GetComponent(tmpType) : null;
                    if (tmp != null)
                    {
                        var pointerData = new PointerEventData(EventSystem.current);
                        var method = tmpType.GetMethod("OnPointerClick", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                        method?.Invoke(tmp, new object[] { pointerData });
                        await Press.Key(KeyCode.Return, 0.1f);
                        return;
                    }
                }
                
                await Press.Key(KeyCode.Return, 0.1f);
            }

            private static async Task ExecuteCancelIntent()
            {
                await Press.Key(KeyCode.Escape, 0.1f);
            }

            private static PreferredInputMode ResolveEffectiveMode()
            {
                if (Tester.Settings.PreferredMode != PreferredInputMode.Auto)
                    return Tester.Settings.PreferredMode;

                if (Input.Touch.IsAvailable) return PreferredInputMode.Touch;
                return PreferredInputMode.Mouse;
            }
        }
    }
}
