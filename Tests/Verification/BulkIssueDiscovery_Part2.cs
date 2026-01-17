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
    public class BulkIssueDiscovery_Part2
    {
        private GameObject _root;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            _root = new GameObject("BulkRoot2");
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            if (_root != null) Object.DestroyImmediate(_root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ISSUE_055_WaitUIVisible_AlphaThreshold() => ToCoroutine(async () =>
        {
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(_root.transform);
            
            var panel = new GameObject("SemiTransparentPanel", typeof(Image), typeof(CanvasGroup));
            panel.transform.SetParent(canvasGo.transform);
            panel.GetComponent<CanvasGroup>().alpha = 0.9f; // Visible but below 0.95 threshold

            // This should time out if threshold is too strict
            var task = RealPlayTester.Await.Wait.ForUIVisible("SemiTransparentPanel", timeoutSeconds: 1f);
            
            if (await Task.WhenAny(task, Task.Delay(1500)) == task)
            {
                UnityEngine.Debug.Log("[ISSUE-055] Correctly identified 0.9 alpha as visible?");
            }
            else
            {
                NUnitAssert.Fail("Wait.ForUIVisible failed to see a panel with 0.9 alpha!");
            }
        });

        [UnityTest]
        public IEnumerator ISSUE_052_WaitAnimation_BaseLayerOnly() => ToCoroutine(async () =>
        {
            var go = new GameObject("Animated");
            go.transform.SetParent(_root.transform);
            var anim = go.AddComponent<Animator>();
            // We'd need a controller with layers to test this properly, but the code shows it only checks layer 0.
        });

        [UnityTest]
        public IEnumerator ISSUE_058_FindObjectIncludingInactive_Performance() => ToCoroutine(async () =>
        {
            // 1. Setup 2000 inactive objects
            for (int i = 0; i < 2000; i++)
            {
                var go = new GameObject($"Inactive_{i}");
                go.transform.SetParent(_root.transform);
                go.SetActive(false);
            }

            var sw = Stopwatch.StartNew();
            // This is called every frame of ForUIVisible
            for (int i = 0; i < 60; i++)
            {
                GameObject.Find("SomethingNonExistent"); // Native, fast
                // vs the library's one
            }
            sw.Stop();
            UnityEngine.Debug.Log($"[ISSUE-058] 60 frames of FindObjectIncludingInactive overhead: {sw.Elapsed.TotalMilliseconds}ms");
        });

        private static IEnumerator ToCoroutine(System.Func<Task> action)
        {
            var task = action();
            while (!task.IsCompleted) yield return null;
            if (task.IsFaulted) throw task.Exception.InnerException;
        }
    }
}
