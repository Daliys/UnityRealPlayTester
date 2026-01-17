using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using RealPlayTester.Core;
using RealPlayTester.Utilities;
using NUnitAssert = NUnit.Framework.Assert;

namespace RealPlayTester.Tests.Verification
{
    public class InteractionBreakers
    {
        private GameObject _root;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            _root = new GameObject("InteractionBreakersRoot");
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            if (_root != null) Object.DestroyImmediate(_root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ISSUE_027_Input_Blocked_TimeScale_Zero() => ToCoroutine(async () =>
        {
            Time.timeScale = 0f;
            try
            {
                var go = new GameObject("Target", typeof(Image), typeof(Button));
                go.transform.SetParent(_root.transform);
                
                // This might hang if it uses WaitForSeconds or depends on physics/input updates that stop at timescale 0
                var sw = System.Diagnostics.Stopwatch.StartNew();
                await Tester.Interaction.Perform("Click", go);
                UnityEngine.Debug.Log($"[ISSUE-027] Perform finished at TimeScale 0 in {sw.ElapsedMilliseconds}ms");
            }
            finally
            {
                Time.timeScale = 1f;
            }
        });

        [UnityTest]
        public IEnumerator ISSUE_034_DoubleClick_Timing() => ToCoroutine(async () =>
        {
            var go = new GameObject("Target", typeof(Image));
            go.transform.SetParent(_root.transform);
            int clicks = 0;
            // Mock a double click handler
            // RealPlayTester doesn't have a "DoubleClick" intent, so we try two "Click" intents rapidly.
            
            await Tester.Interaction.Perform("Click", "Target");
            await Tester.Interaction.Perform("Click", "Target");
            
            // We want to see if the second click is treated as part of a double click or if it resets.
            // But since we simulate events manually, we check if events are dispatched correctly.
        });

        [UnityTest]
        public IEnumerator ISSUE_050_Selectable_Interactable_ParentGroup() => ToCoroutine(async () =>
        {
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(GraphicRaycaster));
            var group = canvasGo.AddComponent<CanvasGroup>();
            group.interactable = false; // Disable everything in canvas

            var buttonGo = new GameObject("Button", typeof(Image), typeof(Button));
            buttonGo.transform.SetParent(canvasGo.transform);
            var button = buttonGo.GetComponent<Button>();
            bool clicked = false;
            button.onClick.AddListener(() => clicked = true);

            // Perceptually, is it interactable?
            var affordances = Tester.Perception.GetActionableElements();
            UnityEngine.Debug.Log($"[ISSUE-050] Affordances: {affordances}");
            
            // Actionably, does it click?
            await Tester.Interaction.Perform("Click", "Button");
            
            NUnitAssert.IsFalse(clicked, "Clicked a button that should be disabled by parent CanvasGroup!");
        });

        private static IEnumerator ToCoroutine(System.Func<Task> action)
        {
            var task = action();
            while (!task.IsCompleted) yield return null;
            if (task.IsFaulted) throw task.Exception.InnerException;
        }
    }
}
