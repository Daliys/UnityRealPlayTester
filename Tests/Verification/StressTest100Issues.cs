using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using RealPlayTester.Core;
using RealPlayTester.Utilities;
using NUnitAssert = NUnit.Framework.Assert;

namespace RealPlayTester.Tests.Verification
{
    public class StressTest100Issues
    {
        private GameObject _root;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            _root = new GameObject("StressRoot");
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            if (_root != null) Object.DestroyImmediate(_root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ISSUE_001_HierarchyDump_PerformanceDegradation() => ToCoroutine(async () =>
        {
            // Disable filtering so we actually process all objects even in headless
            RealPlaySettings.FilterDOMToViewport = false;
            RealPlaySettings.CollapseRepetitiveSiblings = false; // Force full dump

            // 1. Setup 5000 objects
            const int count = 5000;
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject($"Obj_{i}");
                go.transform.SetParent(_root.transform);
                // Add a renderer so it has something to "see" if it was filtering
                var r = go.AddComponent<MeshRenderer>(); 
            }
            SceneCache.Instance.ForceRefresh();

            // 2. Measure DumpHierarchyJson
            var sw = Stopwatch.StartNew();
            string json = Tester.Perception.DumpHierarchyJson();
            sw.Stop();

            UnityEngine.Debug.Log($"[ISSUE-001] DumpHierarchyJson for {count} objects took {sw.Elapsed.TotalMilliseconds}ms. JSON length: {json.Length}");
            
            NUnitAssert.Greater(json.Length, 100000, "JSON dump is suspiciously small - did it filter everything?");
            NUnitAssert.Less(sw.Elapsed.TotalMilliseconds, 500, "Hierarchy dump is too slow for large scenes");
        });

        [UnityTest]
        public IEnumerator ISSUE_003_DeepHierarchy_StackOverflow() => ToCoroutine(async () =>
        {
            // Create a very deep hierarchy
            Transform current = _root.transform;
            for (int i = 0; i < 1000; i++)
            {
                var next = new GameObject($"Depth_{i}").transform;
                next.SetParent(current);
                current = next;
            }

            // This might cause StackOverflow if recursion isn't handled or depth limited
            var sw = Stopwatch.StartNew();
            string dump = Tester.Perception.DumpHierarchy();
            sw.Stop();

            UnityEngine.Debug.Log($"[ISSUE-003] Deep dump took {sw.Elapsed.TotalMilliseconds}ms");
            NUnitAssert.IsNotEmpty(dump);
        });

        [UnityTest]
        public IEnumerator ISSUE_013_SceneCache_Staleness() => ToCoroutine(async () =>
        {
            // 1. Initial scan
            SceneCache.Instance.ForceRefresh();
            
            // 2. Wait a bit but less than MinRefreshInterval (0.5s)
            await Task.Yield();
            
            // 3. Spawn a new object
            var newGo = new GameObject("ImmediateTarget");
            newGo.transform.SetParent(_root.transform);
            
            // 4. Try to find it
            var found = Tester.Perception.Find("ImmediateTarget");
            
            UnityEngine.Debug.Log($"[ISSUE-013] Found ImmediateTarget: {found != null}");
            
            NUnitAssert.IsNotNull(found, "SceneCache returned stale results (MinRefreshInterval blocked update)");
        });

        [UnityTest]
        public IEnumerator ISSUE_002_Perform_RaceCondition_StatePollution() => ToCoroutine(async () =>
        {
            // Setup two buttons that change state
            var go1 = new GameObject("Button1", typeof(UnityEngine.UI.Button));
            var go2 = new GameObject("Button2", typeof(UnityEngine.UI.Button));
            go1.transform.SetParent(_root.transform);
            go2.transform.SetParent(_root.transform);

            // Mock state changes (usually happens via UI interaction)
            // We'll just run them concurrently and see if summaries get mixed up.
            
            var task1 = Tester.Interaction.Perform("Click", "Button1");
            var task2 = Tester.Interaction.Perform("Click", "Button2");

            string[] results = await Task.WhenAll(task1, task2);

            UnityEngine.Debug.Log($"Result 1: {results[0]}");
            UnityEngine.Debug.Log($"Result 2: {results[1]}");

            // If summaries are identical or missing diffs, there's a race condition in StateTracker
        });

        [UnityTest]
        public IEnumerator ISSUE_006_Click_Through_Occlusion() => ToCoroutine(async () =>
        {
            // 1. Setup UI with a blocker
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            canvasGo.transform.SetParent(_root.transform);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var buttonGo = new GameObject("Button", typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
            buttonGo.transform.SetParent(canvasGo.transform);
            var button = buttonGo.GetComponent<UnityEngine.UI.Button>();
            bool clicked = false;
            button.onClick.AddListener(() => clicked = true);

            var blockerGo = new GameObject("Blocker", typeof(UnityEngine.UI.Image));
            blockerGo.transform.SetParent(canvasGo.transform);
            var blockerImage = blockerGo.GetComponent<UnityEngine.UI.Image>();
            blockerImage.rectTransform.sizeDelta = new Vector2(1000, 1000); // Big blocker

            // Ensure blocker is in front of button in hierarchy
            blockerGo.transform.SetAsLastSibling();

            await Task.Yield(); // Wait for UI layout

            // 2. Try to click the button
            string result = await Tester.Interaction.Perform("Click", "Button");
            
            UnityEngine.Debug.Log($"[ISSUE-006] Click Result: {result}, Clicked: {clicked}");

            // In a high-fidelity simulator, clicking an occluded button should fail or click the blocker instead.
            NUnitAssert.IsFalse(clicked, "Clicked through an occluding UI element!");
        });

        private static IEnumerator ToCoroutine(System.Func<Task> action)
        {
            var task = action();
            while (!task.IsCompleted) yield return null;
            if (task.IsFaulted) throw task.Exception.InnerException;
        }
    }
}
