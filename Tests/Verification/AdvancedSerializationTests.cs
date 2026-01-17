using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using RealPlayTester.Core;
using RealPlayTester.Core.Perception;
using NUnitAssert = NUnit.Framework.Assert;

namespace RealPlayTester.Tests.Verification
{
    public partial class AdvancedSerializationTests
    {
        // --- 3.1 SERIALIZATION VECTORS ---

        [Test]
        public void Serialization_Depth_Extreme_50()
        {
            var root = new SemanticNode { Name = "Root" };
            var current = root;
            for (int i = 0; i < 50; i++)
            {
                var child = new SemanticNode { Name = "Level_" + (i + 1) };
                current.Children.Add(child);
                current = child;
            }
            string json = RealPlaySemanticDOMDumper.SerializeNode(root);
            var parsed = SemanticDOMParser.Parse(json);
            int depth = 0;
            var node = parsed;
            while (node.Children.Count > 0) { node = node.Children[0]; depth++; }
            NUnitAssert.AreEqual(50, depth);
        }

        [Test]
        public void Serialization_Circular_Safety()
        {
            var a = new SemanticNode { Name = "A" };
            var b = new SemanticNode { Name = "B" };
            a.Children.Add(b);
            b.Children.Add(a);
            string json = RealPlaySemanticDOMDumper.SerializeNode(a);
            NUnitAssert.IsTrue(json.Length > 0);
            var parsed = SemanticDOMParser.Parse(json);
            NUnitAssert.IsNotNull(parsed);
        }

        [Test]
        public void Serialization_Escape_Chars()
        {
            var node = new SemanticNode { Name = "Q\"B\\N\nT\t" };
            string json = RealPlaySemanticDOMDumper.SerializeNode(node);
            var parsed = SemanticDOMParser.Parse(json);
            NUnitAssert.AreEqual(node.Name, parsed.Name);
        }

        [Test]
        public void Serialization_Unicode()
        {
            var node = new SemanticNode { Name = "🚀 AI 🤖" };
            string json = RealPlaySemanticDOMDumper.SerializeNode(node);
            var parsed = SemanticDOMParser.Parse(json);
            NUnitAssert.AreEqual(node.Name, parsed.Name);
        }

        [Test]
        public void Serialization_Empty_Collections()
        {
            var node = new SemanticNode { Name = "Empty", Children = null };
            string json = RealPlaySemanticDOMDumper.SerializeNode(node);
            var parsed = SemanticDOMParser.Parse(json);
            NUnitAssert.IsNotNull(parsed.Children);
        }

        [Test]
        public void Serialization_Rect_Precision()
        {
            var node = new SemanticNode { ScreenRect = new Rect(1.234f, 5.678f, 0, 0) };
            string json = RealPlaySemanticDOMDumper.SerializeNode(node);
            var parsed = SemanticDOMParser.Parse(json);
            NUnitAssert.AreEqual(1.234f, parsed.ScreenRect.x, 0.001f);
        }

        [Test]
        public void Serialization_07_Parser_Resilience()
        {
            string malformed = "{\"Name\": \"Broken\", \"Children\": [ { \"Name\": \"MissingBrace\" ";
            var parsed = SemanticDOMParser.Parse(malformed);
            NUnit.Framework.Assert.IsNotNull(parsed);
        }

        [Test]
        public void Serialization_08_Missing_Keys()
        {
            string minimal = "{\"Name\": \"Min\"}";
            var parsed = SemanticDOMParser.Parse(minimal);
            NUnit.Framework.Assert.AreEqual("Min", parsed.Name);
        }

        [Test]
        public void Serialization_Siblings_Stress()
        {
            var root = new SemanticNode { Name = "Root" };
            for (int i = 0; i < 100; i++) root.Children.Add(new SemanticNode { Name = "C" + i });
            string json = RealPlaySemanticDOMDumper.SerializeNode(root);
            var parsed = SemanticDOMParser.Parse(json);
            NUnitAssert.AreEqual(100, parsed.Children.Count);
        }

        [Test]
        public void Serialization_ZDepth_Integrity()
        {
            var node = new SemanticNode { ZDepth = -100.5f };
            string json = RealPlaySemanticDOMDumper.SerializeNode(node);
            var parsed = SemanticDOMParser.Parse(json);
            NUnitAssert.AreEqual(-100.5f, parsed.ZDepth, 0.01f);
        }

        // --- 3.3 LIFECYCLE VECTORS ---

        [UnityTest]
        public IEnumerator Lifecycle_Unload_Execution() => ToCoroutine(async () =>
        {
            var testAsset = ScriptableObject.CreateInstance<MockLifecycleTest>();
            testAsset.hideFlags = HideFlags.HideAndDontSave;
            var descriptor = new TestCaseDescriptor(testAsset, null, null);
            var runner = new RealPlayTester.Core.TestRunner();
            var runTask = runner.RunSingleTest(descriptor, default);
            await Resources.UnloadUnusedAssets();
            GC.Collect();
            var result = await runTask;
            NUnitAssert.IsTrue(result.Passed);
            ScriptableObject.DestroyImmediate(testAsset);
        });

        [UnityTest]
        public IEnumerator Lifecycle_GC_Pressure() => ToCoroutine(async () =>
        {
            for (int i = 0; i < 5; i++)
            {
                var tmp = ScriptableObject.CreateInstance<MockLifecycleTest>();
                tmp.hideFlags = HideFlags.HideAndDontSave;
                GC.Collect();
                var res = await new RealPlayTester.Core.TestRunner().RunSingleTest(new TestCaseDescriptor(tmp, null, null), default);
                NUnitAssert.IsTrue(res.Passed);
                ScriptableObject.DestroyImmediate(tmp);
            }
        });

        [Test]
        public void Lifecycle_Null_Descriptor()
        {
            var descriptor = new TestCaseDescriptor(null, null, null);
            NUnitAssert.AreEqual("NullAsset", descriptor.DisplayName);
        }

        [UnityTest]
        public IEnumerator Lifecycle_Destroy_Source() => ToCoroutine(async () =>
        {
            var testAsset = ScriptableObject.CreateInstance<MockLifecycleTest>();
            testAsset.name = "Source";
            var descriptor = new TestCaseDescriptor(testAsset, null, null);
            var runner = new RealPlayTester.Core.TestRunner();
            var runTask = runner.RunSingleTest(descriptor, default);
            await Task.Yield();
            ScriptableObject.DestroyImmediate(testAsset);
            var result = await runTask;
            NUnitAssert.IsTrue(result.Passed);
        });

        [UnityTest]
        public IEnumerator Lifecycle_Unity_Null_Check() => ToCoroutine(async () =>
        {
            var testAsset = ScriptableObject.CreateInstance<MockLifecycleTest>();
            var descriptor = new TestCaseDescriptor(testAsset, null, null);
            ScriptableObject.DestroyImmediate(testAsset);
            NUnitAssert.IsTrue(descriptor.Asset == null);
        });

        [UnityTest]
        public IEnumerator Lifecycle_Parallel_Runners() => ToCoroutine(async () =>
        {
            var tA = ScriptableObject.CreateInstance<MockLifecycleTest>();
            var tB = ScriptableObject.CreateInstance<MockLifecycleTest>();
            tA.hideFlags = tB.hideFlags = HideFlags.HideAndDontSave;
            var task1 = new RealPlayTester.Core.TestRunner().RunSingleTest(new TestCaseDescriptor(tA, null, null), default);
            var task2 = new RealPlayTester.Core.TestRunner().RunSingleTest(new TestCaseDescriptor(tB, null, null), default);
            var results = await Task.WhenAll(task1, task2);
            NUnitAssert.IsTrue(results[0].Passed && results[1].Passed);
            ScriptableObject.DestroyImmediate(tA);
            ScriptableObject.DestroyImmediate(tB);
        });

        [UnityTest]
        public IEnumerator Lifecycle_HideFlags_Protection() => ToCoroutine(async () =>
        {
            var testAsset = ScriptableObject.CreateInstance<MockLifecycleTest>();
            testAsset.hideFlags = HideFlags.HideAndDontSave;
            var clone = ScriptableObject.Instantiate(testAsset);
            NUnitAssert.IsTrue((clone.hideFlags & HideFlags.HideAndDontSave) != 0);
            ScriptableObject.DestroyImmediate(testAsset);
            ScriptableObject.DestroyImmediate(clone);
            await Task.Yield();
        });

        [UnityTest]
        public IEnumerator Lifecycle_Repeated_Cleanup() => ToCoroutine(async () =>
        {
            var testAsset = ScriptableObject.CreateInstance<MockLifecycleTest>();
            for(int i=0; i<10; i++)
            {
                var clone = ScriptableObject.Instantiate(testAsset);
                ScriptableObject.DestroyImmediate(clone);
            }
            ScriptableObject.DestroyImmediate(testAsset);
            await Task.Yield();
        });

        [UnityTest]
        public IEnumerator Lifecycle_Context_PostUnload() => ToCoroutine(async () =>
        {
            RealPlayTester.Diagnostics.TestRunContextTracker.BeginTest("P", "N");
            await Resources.UnloadUnusedAssets();
            GC.Collect();
            NUnitAssert.IsNotNull(RealPlayTester.Diagnostics.TestRunContextTracker.Current);
            RealPlayTester.Diagnostics.TestRunContextTracker.EndTest();
        });

        [UnityTest]
        public IEnumerator Lifecycle_InstanceID_Persistence() => ToCoroutine(async () =>
        {
            var testAsset = ScriptableObject.CreateInstance<MockLifecycleTest>();
            int id = testAsset.GetInstanceID();
            await Resources.UnloadUnusedAssets();
            NUnitAssert.AreEqual(id, testAsset.GetInstanceID());
            ScriptableObject.DestroyImmediate(testAsset);
            await Task.Yield();
        });

        private static IEnumerator ToCoroutine(System.Func<Task> action)
        {
            var task = action();
            while (!task.IsCompleted) yield return null;
            if (task.IsFaulted)
            {
                if (task.Exception != null)
                    System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(task.Exception.InnerException).Throw();
                else
                    throw new System.Exception("Task failed without exception");
            }
        }
    }
}
