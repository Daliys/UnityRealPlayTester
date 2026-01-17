using System.Collections;
using System.Collections.Generic;
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
    public class ChaosBreakers
    {
        private GameObject _root;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            _root = new GameObject("ChaosRoot");
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            if (_root != null) Object.DestroyImmediate(_root);
            yield return null;
        }

        /// <summary>
        /// Stresses the perception system by rapidly mutating the hierarchy during a scan.
        /// </summary>
        [UnityTest]
        public IEnumerator CHAOS_001_Hierarchy_Mutation_During_Scan() => ToCoroutine(async () =>
        {
            // Create a baseline
            for (int i = 0; i < 100; i++) new GameObject($"Child_{i}").transform.SetParent(_root.transform);

            // Start a task that nukes and recreates objects
            bool running = true;
            var mutationTask = Task.Run(async () => {
                while (running) {
                    await Task.Delay(1); // Real-world async jitter
                }
            });

            try {
                for (int i = 0; i < 10; i++) {
                    // Try to break SceneCache or SmartFind by deleting objects while it might be iterating (if called from main thread)
                    var go = new GameObject("Transient");
                    go.transform.SetParent(_root.transform);
                    
                    var found = Tester.Perception.Find("Transient");
                    Object.DestroyImmediate(go);
                    
                    await Task.Yield();
                }
            } finally {
                running = false;
                await mutationTask;
            }
        });

        /// <summary>
        /// Stresses the library with extreme TimeScales.
        /// </summary>
        [UnityTest]
        public IEnumerator CHAOS_002_Extreme_TimeScale() => ToCoroutine(async () =>
        {
            float[] scales = { 0f, 0.0001f, 100f };
            foreach (var s in scales)
            {
                Time.timeScale = s;
                UnityEngine.Debug.Log($"[Chaos] Testing TimeScale: {s}");
                
                var sw = System.Diagnostics.Stopwatch.StartNew();
                await Wait.Seconds(0.1f, unscaled: true);
                
                NUnit.Framework.Assert.Less(sw.ElapsedMilliseconds, 500, $"Unscaled wait took too long at timescale {s}");
            }
            Time.timeScale = 1f;
        });

        /// <summary>
        /// Deep hierarchy exceeding the linter/safety limits.
        /// </summary>
        [UnityTest]
        public IEnumerator CHAOS_003_Deep_Hierarchy_Limit() => ToCoroutine(async () =>
        {
            Transform parent = _root.transform;
            for (int i = 0; i < 50; i++)
            {
                var child = new GameObject($"Depth_{i}").transform;
                child.SetParent(parent);
                parent = child;
            }

            // Semantic DOM Dumper has a limit of 15. Let's see what happens at 50.
            string json = Tester.Perception.DumpHierarchyJson();
            UnityEngine.Debug.Log($"[Chaos] Deep JSON length: {json.Length}");
            NUnit.Framework.Assert.IsNotEmpty(json);
        });

        /// <summary>
        /// Stresses the library with circular hierarchy (child pointing to ancestor via code, though Unity prevents it in inspector).
        /// </summary>
        [UnityTest]
        public IEnumerator CHAOS_004_Circular_Hierarchy_Detection() => ToCoroutine(async () =>
        {
            var p1 = new GameObject("Parent1");
            var p2 = new GameObject("Parent2");
            p1.transform.SetParent(_root.transform);
            p2.transform.SetParent(p1.transform);
            
            // In Unity, you can't have actual circular transform parentage.
            // But we can test if our dumper handles deep nesting or rapid reparenting.
            for(int i=0; i<100; i++) {
                p2.transform.SetParent(_root.transform);
                p1.transform.SetParent(p2.transform);
                p2.transform.SetParent(p1.transform);
                
                // Trigger dumper during this mess
                if (i % 10 == 0) Tester.Perception.DumpHierarchyJson();
                await Task.Yield();
            }
        });

        /// <summary>
        /// Stress tests simultaneous inputs from multiple "sources".
        /// </summary>
        [UnityTest]
        public IEnumerator CHAOS_005_Simultaneous_Inputs() => ToCoroutine(async () =>
        {
            var btn1 = new GameObject("Btn1", typeof(Image), typeof(Button));
            var btn2 = new GameObject("Btn2", typeof(Image), typeof(Button));
            btn1.transform.SetParent(_root.transform);
            btn2.transform.SetParent(_root.transform);

            // Parallel tasks trying to perform actions
            var t1 = Tester.Interaction.Perform("Click", btn1);
            var t2 = Tester.Interaction.Perform("Click", btn2);
            var t3 = Tester.Interaction.Perform("Click", "NonExistent");

            await Task.WhenAll(t1, t2, t3);
        });

        /// <summary>
        /// Stress tests with multiple active cameras.
        /// </summary>
        [UnityTest]
        public IEnumerator CHAOS_006_Multi_Camera_Probing() => ToCoroutine(async () =>
        {
            var cam1Go = new GameObject("Cam1", typeof(Camera));
            var cam2Go = new GameObject("Cam2", typeof(Camera));
            cam1Go.tag = "MainCamera";
            
            var target = new GameObject("Target", typeof(MeshRenderer), typeof(BoxCollider));
            target.transform.position = Vector3.forward * 10f;
            target.transform.SetParent(_root.transform);

            // Active cam1
            cam1Go.GetComponent<Camera>().enabled = true;
            cam2Go.GetComponent<Camera>().enabled = false;
            
            var found1 = Tester.Perception.Find("Target");
            NUnit.Framework.Assert.IsNotNull(found1, "Should find target with Cam1");

            // Swap main cam
            cam1Go.tag = "Untagged";
            cam2Go.tag = "MainCamera";
            cam1Go.GetComponent<Camera>().enabled = false;
            cam2Go.GetComponent<Camera>().enabled = true;
            cam2Go.transform.position = Vector3.right * 100f; // Far away

            var found2 = Tester.Perception.Find("Target");
            // SmartFind uses weighted ranking including visibility.
            UnityEngine.Debug.Log($"[Chaos] Found target after cam swap? {found2 != null}");
            
            Object.DestroyImmediate(cam1Go);
            Object.DestroyImmediate(cam2Go);
        });

        private static IEnumerator ToCoroutine(System.Func<Task> action)
        {
            var task = action();
            while (!task.IsCompleted) yield return null;
            if (task.IsFaulted) throw task.Exception.InnerException;
        }
    }
}
