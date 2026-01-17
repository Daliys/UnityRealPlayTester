using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using RealPlayTester.Core;
using RealPlayTester.Input;
using RealPlayTester.Diagnostics;
using System.IO;
using NUnitAssert = NUnit.Framework.Assert;

namespace RealPlayTester.Tests.Verification
{
    public class HandsExtendedTests
    {
        private VerificationSceneSetup _scene;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            _scene = VerificationSceneSetup.Create();
            TestRunContextTracker.BeginTest("HandsExtendedTest", "VirtualScene");
            yield return null;
            
            Screen.SetResolution(1024, 768, false);
            yield return null;

            Tester.Settings.ForceBatchmodeVisibility = true;
            Tester.Settings.EnableInputHeartbeat = true;
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            if (_scene != null) _scene.Cleanup();
            TestRunContextTracker.EndTest();
            yield return null;
        }

        // Test 1: Drag UI Panel
        [UnityTest]
        public IEnumerator Mouse_Drag_UIPanel_MovesIt() => ToCoroutine(async () =>
        {
            RectTransform rt = _scene.DraggableObj.GetComponent<RectTransform>();
            Vector2 initialPos = rt.anchoredPosition;
            Vector2 screenStart = RealInputUtility.GetScreenCenter(_scene.DraggableObj);
            Vector2 screenEnd = screenStart + new Vector2(200, 0);

            // Add a simple drag handler to the object so it actually moves
            var handler = _scene.DraggableObj.AddComponent<DragHandler>();
            handler.Target = rt;

            await Tester.Advanced.Drag(screenStart, screenEnd, 0.5f);
            
            await Tester.Await.Frames(10);
            NUnitAssert.AreNotEqual(initialPos.x, rt.anchoredPosition.x, "Panel should have moved on X axis after drag");
        });

        // Simple helper for the test
        private class DragHandler : MonoBehaviour, IDragHandler
        {
            public RectTransform Target;
            public void OnDrag(PointerEventData eventData)
            {
                Target.anchoredPosition += eventData.delta;
            }
        }

        // Test 2: Keyboard Modifier Combo (Shift + Key)
        [UnityTest]
        public IEnumerator Keyboard_ModifierCombo_RecordsBreadcrumb() => ToCoroutine(async () =>
        {
            await Tester.Interaction.Keyboard.Down(KeyCode.LeftShift);
            await Tester.Interaction.Keyboard.Press(KeyCode.W);
            await Tester.Interaction.Keyboard.Up(KeyCode.LeftShift);

            string md = GetContextMarkdown();
            NUnitAssert.IsTrue(md.Contains("Pressing key: W"), "Timeline missing key press");
        });

        // Test 3: Touch Swipe Scroll
        [UnityTest]
        public IEnumerator Touch_Swipe_ScrollsView() => ToCoroutine(async () =>
        {
            float initialScroll = _scene.ScrollView.verticalNormalizedPosition;
            Vector2 center = RealInputUtility.GetScreenCenter(_scene.ScrollView.gameObject);
            Vector2 end = center + new Vector2(0, 300); // Swipe UP to scroll DOWN

            await Tester.Interaction.Touch.Swipe(center, end, 0.5f);
            
            await Tester.Await.Frames(10);
            NUnitAssert.Less(_scene.ScrollView.verticalNormalizedPosition, initialScroll - 0.05f, "ScrollView should have scrolled down");
        });

        // Test 4: Gesture Spread (Reverse Pinch)
        [UnityTest]
        public IEnumerator Gestures_Spread_SimulatesZoom() => ToCoroutine(async () =>
        {
            await Tester.Interaction.Gestures.Pinch(new Vector2(512, 384), 10, 500, 0, 0.5f);
            
            string md = GetContextMarkdown();
            NUnitAssert.IsTrue(md.Contains("dist 10->500"), "Timeline missing spread distance info");
        });

        // Test 5: Gesture Multi-Angle Pinch
        [UnityTest]
        public IEnumerator Gestures_AngledPinch_Works() => ToCoroutine(async () =>
        {
            await Tester.Interaction.Gestures.Pinch(new Vector2(512, 384), 100, 300, 90, 0.5f);
            NUnitAssert.Pass("Angled pinch executed without error");
        });

        // Test 6: Gamepad Simultaneous Input (Stick + Button)
        [UnityTest]
        public IEnumerator Gamepad_SimultaneousInput_RecordsBoth() => ToCoroutine(async () =>
        {
            var task1 = Tester.Interaction.Gamepad.MoveStick(true, Vector2.one);
            var task2 = Tester.Interaction.Gamepad.Press(GamepadButton.South);
            await Task.WhenAll(task1, task2);
            
            string md = GetContextMarkdown();
            NUnitAssert.IsTrue(md.Contains("Left Stick to (1.00, 1.00)"), "Timeline missing stick move");
            NUnitAssert.IsTrue(md.Contains("Press South"), "Timeline missing button press");
        });

        // Test 7: Advanced: Click Button with specific Text
        [UnityTest]
        public IEnumerator Advanced_ClickButtonWithText_Works() => ToCoroutine(async () =>
        {
            bool clicked = false;
            _scene.TestButton.onClick.AddListener(() => clicked = true);

            await Tester.Advanced.ClickButtonWithText("Click Me");
            
            await Tester.Await.Seconds(0.2f);
            NUnitAssert.IsTrue(clicked, "Button with text 'Click Me' should have been clicked");
        });

        // Test 8: Advanced: Scroll Until Visible
        [UnityTest]
        public IEnumerator Advanced_ScrollUntilVisible_FindsDeepChild() => ToCoroutine(async () =>
        {
            _scene.ScrollView.verticalNormalizedPosition = 1.0f; // Reset to top
            await Tester.Await.Frames(2);

            await Tester.Advanced.ScrollUntilVisible(_scene.ScrollView, _scene.ScrollTarget);
            
            NUnitAssert.Less(_scene.ScrollView.verticalNormalizedPosition, 0.2f, "Should have scrolled near bottom to find target");
        });

        // Test 9: Gamepad Rapid Clicking
        [UnityTest]
        public IEnumerator Gamepad_RapidClicking_StaysStable() => ToCoroutine(async () =>
        {
            for(int i = 0; i < 5; i++)
            {
                await Tester.Interaction.Gamepad.Click(GamepadButton.West, 0.05f);
            }
            NUnitAssert.Pass("Rapid gamepad clicking handled");
        });

        // Test 10: Touch Long Press
        [UnityTest]
        public IEnumerator Touch_LongPress_Completes() => ToCoroutine(async () =>
        {
            await Tester.Interaction.Touch.LongPress(new Vector2(100, 100), 0.5f);
            NUnitAssert.Pass("Long press handled");
        });

        private string GetContextMarkdown()
        {
            string mdPath = Path.Combine(RealPlayEnvironment.TestReportsPath, "current-test-context.md");
            return File.ReadAllText(mdPath);
        }

        private static IEnumerator ToCoroutine(System.Func<Task> action)
        {
            var task = action();
            while (!task.IsCompleted) yield return null;
            if (task.IsFaulted) throw task.Exception.InnerException;
        }
    }
}