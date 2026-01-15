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

                // Capture State Before
                string fromState = RealPlayTester.UI.PanelStateMonitor.GetActiveStateName();
                var stateBefore = StateTracker.Capture();
                RealPlayTester.Diagnostics.TestRunContextTracker.RecordBreadcrumb("Intent", $"Perform '{intent}' on '{(target != null ? target.name : "null")}'");

                if (target != null) CheckLogicalBlocking(target);

                await DispatchIntent(intent, target);

                // STABILITY FIX: Use unscaled wait for causality feedback
                await Wait.Seconds(0.05f, unscaled: true);

                // RELIABILITY FIX: Target might be destroyed during wait (C002)
                bool targetDestroyed = target == null;
                string targetName = targetDestroyed ? "DestroyedObject" : target.name;

                // Capture State After
                var stateAfter = StateTracker.Capture();
                string toState = RealPlayTester.UI.PanelStateMonitor.GetActiveStateName();
                
                string summary = StateTracker.GetDiff(stateBefore, stateAfter, $"{intent} '{targetName}'");
                RealPlayTester.Diagnostics.TestRunContextTracker.RecordBreadcrumb("Reaction", summary);

                // Auto-Discovery: Learn navigation path
                if (!targetDestroyed) Navigation.RecordStateTransition(fromState, toState, intent, targetName);

                return summary;
            }

            private static bool IsInteractionBlocked(string intent, GameObject target)
            {
                if (target == null) return false;
                var validator = target.GetComponent<IAffordanceValidator>();
                if (validator != null && !validator.CanInteract(intent))
                {
                    RealPlayLog.Warn($"[Interaction] Perform '{intent}' on '{target.name}' blocked: {validator.GetBlockReason()}");
                    return true;
                }
                return false;
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
                        if (target != null) await Mouse.Drag(target, target.transform.position + Vector3.right * 100f); // Default drag
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

                // NEW: MEGA_010 heuristic invocation if standard events didn't trigger everything
                // Or rather, always try to invoke OnClick if it exists, to support non-event-system components.
                InvokeHeuristicMethod(target, "OnClick");
            }

            private static void InvokeHeuristicMethod(GameObject target, string methodName)
            {
                var comps = target.GetComponents<MonoBehaviour>();
                foreach (var c in comps)
                {
                    if (c == null) continue;
                    var method = c.GetType().GetMethod(methodName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
                    if (method != null && method.GetParameters().Length == 0)
                    {
                        method.Invoke(c, null);
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
