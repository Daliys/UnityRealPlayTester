using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using RealPlayTester.Input;
using RealPlayTester.Utilities;
using RealPlayTester.Core.Perception;

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

                // Capture State Before
                string fromState = RealPlayTester.UI.PanelStateMonitor.GetActiveStateName();
                var stateBefore = StateTracker.Capture();
                RealPlayTester.Diagnostics.TestRunContextTracker.RecordBreadcrumb("Intent", $"Perform '{intent}' on '{(target != null ? target.name : "null")}'");

                if (target != null) CheckLogicalBlocking(target);

                await DispatchIntent(intent, target);

                await Task.Yield();
                await Task.Yield();

                // Capture State After
                var stateAfter = StateTracker.Capture();
                string toState = RealPlayTester.UI.PanelStateMonitor.GetActiveStateName();
                
                string summary = StateTracker.GetDiff(stateBefore, stateAfter, $"{intent} '{target?.name}'");
                RealPlayTester.Diagnostics.TestRunContextTracker.RecordBreadcrumb("Reaction", summary);

                // Auto-Discovery: Learn navigation path
                if (target != null) Navigation.RecordStateTransition(fromState, toState, intent, target.name);

                return summary;
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
            }

            private static async Task ExecuteSubmitIntent(GameObject target)
            {
                if (target != null)
                {
                    var inputField = target.GetComponent<InputField>();
                    var tmpType = Type.GetType("TMPro.TMP_InputField, Unity.TextMeshPro");
                    var isTMP = tmpType != null && target.GetComponent(tmpType) != null;

                    if (inputField != null || isTMP)
                    {
                        await Keyboard.Press(KeyCode.Return);
                        return;
                    }
                }
                
                var mode = ResolveEffectiveMode();
                if (mode == PreferredInputMode.Gamepad) await Gamepad.Click(GamepadButton.Start);
                else await Keyboard.Press(KeyCode.Return);
            }

            private static async Task ExecuteCancelIntent()
            {
                var mode = ResolveEffectiveMode();
                if (mode == PreferredInputMode.Gamepad) await Gamepad.Click(GamepadButton.East);
                else await Keyboard.Press(KeyCode.Escape);
            }

            private static PreferredInputMode ResolveEffectiveMode()
            {
                if (RealPlaySettings.PreferredMode != PreferredInputMode.Auto)
                    return RealPlaySettings.PreferredMode;

                if (Application.isMobilePlatform) return PreferredInputMode.Touch;
                if (RealPlayTester.Input.Internal.VirtualDeviceManager.VirtualGamepad != null)
                    return PreferredInputMode.Gamepad;

                return PreferredInputMode.Mouse;
            }
        }
    }
}