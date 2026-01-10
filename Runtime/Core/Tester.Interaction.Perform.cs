using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using RealPlayTester.Input;
using RealPlayTester.Utilities;

namespace RealPlayTester.Core
{
    public static partial class Tester
    {
        public static partial class Interaction
        {
            /// <summary>
            /// Performs a high-level semantic action on a target object.
            /// Intelligently chooses between Mouse, Touch, and Gamepad simulation based on RealPlaySettings.PreferredMode.
            /// </summary>
            public static async Task Perform(string intent, string targetName)
            {
                if (!RealPlayEnvironment.IsEnabled) return;
                var target = Perception.Find(targetName);
                if (target == null)
                {
                    RealPlayLog.Warn($"[Interaction] Perform('{intent}') failed: Target '{targetName}' not found.");
                    return;
                }
                await Perform(intent, target);
            }

            /// <summary>
            /// Performs a high-level semantic action on a target object.
            /// </summary>
            public static async Task Perform(string intent, GameObject target)
            {
                if (!RealPlayEnvironment.IsEnabled) return;

                RealPlayTester.Diagnostics.TestRunContextTracker.RecordBreadcrumb("Intent", $"Perform '{intent}' on '{(target != null ? target.name : "null")}' (Mode: {RealPlaySettings.PreferredMode})");

                if (target != null) CheckLogicalBlocking(target);

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
