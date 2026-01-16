using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using RealPlayTester.Core;
using RealPlayTester.Utilities;
using RealPlayTester.Diagnostics;
using RealPlayTester.Await;

namespace RealPlayTester.Tests.Verification
{
    public partial class MegaStressTests
    {
        private GameObject _root;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            _root = new GameObject("MegaStressRoot");
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            if (_root != null) UnityEngine.Object.DestroyImmediate(_root);
            yield return null;
        }

        /// <summary>
        /// A massive 100-step test that simulates a long-running session.
        /// </summary>
        [UnityTest]
        public IEnumerator MEGA_001_Long_Session_Endurance() => ToCoroutine(async () =>
        {
            TestRunContextTracker.BeginTest("EnduranceTest", "None");
            var buttons = CreateEnduranceUI();

            for (int step = 0; step < 100; step++)
            {
                await PerformEnduranceStep(step, buttons);
                await Task.Yield();
            }

            TestRunContextTracker.EndTest();
        });

        private List<Button> CreateEnduranceUI()
        {
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(GraphicRaycaster), typeof(CanvasScaler));
            canvasGo.transform.SetParent(_root.transform);
            
            var buttons = new List<Button>();
            for (int i = 0; i < 20; i++)
            {
                var btnGo = new GameObject($"Button_{i}", typeof(Image), typeof(Button));
                btnGo.transform.SetParent(canvasGo.transform);
                var btn = btnGo.GetComponent<Button>();
                int index = i;
                btn.onClick.AddListener(() => UnityEngine.Debug.Log($"[MEGA] Button {index} clicked"));
                buttons.Add(btn);
            }
            return buttons;
        }

        private async Task PerformEnduranceStep(int step, List<Button> buttons)
        {
            int targetIdx = UnityEngine.Random.Range(0, buttons.Count);
            await Tester.Interaction.Perform("Click", buttons[targetIdx].gameObject);
            
            if (step % 10 == 0) Tester.Perception.DumpHierarchyJson();
            if (step % 5 == 0) for (int j = 0; j < 50; j++) UnityEngine.Debug.Log($"Noise Log {j} at step {step}");
        }

        /// <summary>
        /// Rapidly starts and cancels async tasks to find race conditions.
        /// </summary>
        [UnityTest]
        public IEnumerator MEGA_002_Async_Cancellation_Chaos() => ToCoroutine(async () =>
        {
            var btn = new GameObject("Button", typeof(Image), typeof(Button));
            btn.transform.SetParent(_root.transform);

            for (int i = 0; i < 50; i++)
            {
                using (var cts = new CancellationTokenSource())
                {
                    RealPlayExecutionContext.SetToken(cts.Token);
                    var task = Tester.Interaction.Perform("Click", btn);
                    if (UnityEngine.Random.value > 0.5f) cts.Cancel();

                    try { await task; }
                    catch (OperationCanceledException) { }
                    catch (Exception ex) { UnityEngine.Debug.LogError($"[MEGA] Unexpected error: {ex}"); }
                    finally { RealPlayExecutionContext.Clear(); }
                }
                await Task.Yield();
            }
        });

        private static IEnumerator ToCoroutine(System.Func<Task> action)
        {
            var task = action();
            while (!task.IsCompleted) yield return null;
            if (task.IsFaulted) throw task.Exception.InnerException;
        }
    }
}
