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
                var stateBefore = StateDiffer.CaptureActiveState(StateDiffer.CaptureState());

                RealPlayTester.Diagnostics.TestRunContextTracker.RecordBreadcrumb("Intent", $"Perform '{intent}' on '{(target != null ? target.name : "null")}' (Mode: {RealPlaySettings.PreferredMode})");

                if (target != null) CheckLogicalBlocking(target);

                // Perform Action
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

                // Wait for reaction (heuristic: wait for 1-2 frames or a small time for UI to update)
                // RealPlayTester actions usually encompass some wait, but let's add a small steady state wait if needed?
                // The underlying actions (Mouse.ClickObject) usually have some delay.
                // Let's add a short wait to ensure frame update.
                await Task.Yield();
                await Task.Yield();

                // Capture State After
                var stateAfter = StateDiffer.CaptureActiveState(StateDiffer.CaptureState());

                // Diff
                string summary = StateDiffer.GetDiff(stateBefore, stateAfter, $"{intent} '{target?.name}'");

                // Log to Unified Stream
                RealPlayTester.Diagnostics.TestRunContextTracker.RecordBreadcrumb("Reaction", summary);

                return summary;
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