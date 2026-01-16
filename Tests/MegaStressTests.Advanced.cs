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
using RealPlayTester.Diagnostics;

namespace RealPlayTester.Tests.Verification
{
    public partial class MegaStressTests
    {
        /// <summary>
        /// Stresses the memory and LogInterceptor.
        /// </summary>
        [UnityTest]
        public IEnumerator MEGA_003_Log_Flood_Stress() => ToCoroutine(async () =>
        {
            TestRunContextTracker.BeginTest("LogFloodTest", "None");
            var tasks = new List<Task>();
            for (int t = 0; t < 10; t++)
            {
                int threadIdx = t;
                tasks.Add(Task.Run(() => {
                    for (int i = 0; i < 500; i++) UnityEngine.Debug.Log($"Flood Log {i} from Thread {threadIdx}");
                }));
            }
            await Task.WhenAll(tasks);
            await Task.Delay(1000);
            
            int count = TestRunContextTracker.Current?.GetLogCount() ?? 0;
            NUnit.Framework.Assert.LessOrEqual(count, 1001, "LogInterceptor cap failed!");
            TestRunContextTracker.EndTest();
        });

        /// <summary>
        /// Attempts to crash the library with massive GC.
        /// </summary>
        [UnityTest]
        public IEnumerator MEGA_004_DOM_Memory_Pressure() => ToCoroutine(async () =>
        {
            var massiveRoot = new GameObject("MassiveRoot");
            massiveRoot.transform.SetParent(_root.transform);
            for (int i = 0; i < 1000; i++) new GameObject($"Obj_{i}", typeof(Image)).transform.SetParent(massiveRoot.transform);

            for (int i = 0; i < 50; i++)
            {
                Tester.Perception.DumpHierarchyJson();
                await Task.Yield();
            }
        });

        /// <summary>
        /// Rapidly switches scenes while a test is active to find cleanup/initialization races.
        /// </summary>
        [UnityTest]
        public IEnumerator MEGA_005_Scene_Switch_Chaos() => ToCoroutine(async () =>
        {
            string originalScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            
            for (int i = 0; i < 5; i++)
            {
                var probeTask = Task.Run(async () => {
                    for(int j=0; j<100; j++) {
                        try { Tester.Perception.Find("NonExistent"); } catch { }
                        await Task.Delay(1);
                    }
                });

                UnityEngine.SceneManagement.SceneManager.LoadScene(originalScene);
                await Task.Yield();
                await Task.Yield();
                
                await probeTask;
            }
        });

        /// <summary>
        /// Bloats the navigation graph with thousands of unique states to check for lookup performance.
        /// </summary>
        [UnityTest]
        public IEnumerator MEGA_006_Navigation_Graph_Bloat() => ToCoroutine(async () =>
        {
            Tester.Settings.EnableNavigationLearning = true;
            for (int i = 0; i < 1000; i++)
            {
                Tester.Navigation.RecordStateTransition($"State_{i}", $"State_{i + 1}", "Click", "FakeBtn");
            }

            var sw = System.Diagnostics.Stopwatch.StartNew();
            string json = Tester.Navigation.ExportToJson();
            UnityEngine.Debug.Log($"[MEGA] NavGraph JSON Export took {sw.ElapsedMilliseconds}ms, Size: {json.Length}");
            
            NUnit.Framework.Assert.IsNotEmpty(json);
            Tester.Navigation.ImportFromJson(json);
        });

        /// <summary>
        /// Checks for virtual device leaks by repeatedly initializing the framework.
        /// </summary>
        [UnityTest]
        public IEnumerator MEGA_007_Device_Leak_Stress() => ToCoroutine(async () =>
        {
            for (int i = 0; i < 50; i++)
            {
                RealPlayTester.Input.Internal.VirtualDeviceManager.CleanupDevices();
                RealPlayTester.Input.Internal.VirtualDeviceManager.EnsureKeyboard();
                RealPlayTester.Input.Internal.VirtualDeviceManager.EnsureMouse();
                await Task.Yield();
            }
        });

        /// <summary>
        /// Measures performance of hierarchy dumping with 5000+ objects.
        /// </summary>
        [UnityTest]
        public IEnumerator MEGA_008_Large_Hierarchy_Performance() => ToCoroutine(async () =>
        {
            var root = new GameObject("LargeRoot");
            root.transform.SetParent(_root.transform);
            
            for (int i = 0; i < 5000; i++)
            {
                var go = new GameObject($"Item_{i}");
                go.transform.SetParent(root.transform);
                if (i % 10 == 0) go.AddComponent<Button>();
            }

            var sw = System.Diagnostics.Stopwatch.StartNew();
            string dom = Tester.Perception.DumpHierarchyJson();
            long elapsed = sw.ElapsedMilliseconds;
            
            UnityEngine.Debug.Log($"[MEGA] 5000 object DOM dump took {elapsed}ms");
            NUnit.Framework.Assert.Less(elapsed, 1000, "Hierarchy dump is too slow!");
        });

        /// <summary>
        /// Stresses SmartFind with multiple parallel requests to find threading issues.
        /// </summary>
        [UnityTest]
        public IEnumerator MEGA_009_Perception_Race_Condition() => ToCoroutine(async () =>
        {
            for (int i = 0; i < 10; i++)
            {
                var go = new GameObject($"Target_{i}");
                go.transform.SetParent(_root.transform);
            }

            var tasks = new List<Task>();
            for (int i = 0; i < 20; i++)
            {
                int idx = i % 10;
                tasks.Add(Task.Run(() => {
                    try { Tester.Perception.Find($"Target_{idx}"); } catch { }
                }));
            }

            await Task.WhenAll(tasks);
        });

        /// <summary>
        /// Tests if interaction heuristics correctly identify non-standard components.
        /// </summary>
        [UnityTest]
        public IEnumerator MEGA_010_Interaction_Heuristic_Breaker() => ToCoroutine(async () =>
        {
            var go = new GameObject("CustomObj");
            go.transform.SetParent(_root.transform);
            var comp = go.AddComponent<CustomInteractiveComponent>();
            
            var dom = Tester.Perception.DumpHierarchyJson();
            NUnit.Framework.Assert.IsTrue(dom.Contains("Click"), "Custom component Click affordance not found!");
            
            await Tester.Interaction.Perform("Click", go);
            NUnit.Framework.Assert.IsTrue(comp.WasClicked, "Custom component was not clicked!");
        });

        public class CustomInteractiveComponent : MonoBehaviour
        {
            public bool WasClicked;
            public void OnClick() { WasClicked = true; }
        }

        /// <summary>
        /// Tests if HardReset actually resets global settings.
        /// </summary>
        [UnityTest]
        public IEnumerator MEGA_011_Settings_Reset_Flaw() => ToCoroutine(async () =>
        {
            bool original = Tester.Settings.ShowVisualPointer;
            Tester.Settings.ShowVisualPointer = !original;
            
            await HardReset.Execute();
            
            NUnit.Framework.Assert.AreEqual(original, Tester.Settings.ShowVisualPointer, "HardReset failed to reset settings to default!");
        });
    }
}