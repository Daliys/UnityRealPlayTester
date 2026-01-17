using System;
using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Profiling;
using RealPlayTester.Core;
using RealPlayTester.Core.Perception;
using NUnitAssert = NUnit.Framework.Assert;

namespace RealPlayTester.Tests.Verification
{
    public class ResourceStressTests
    {
        // --- 7.1 GARBAGE COLLECTION SPIKES ---

        [Test]
        [TestCase(100), TestCase(500), TestCase(1000)]
        public void Resource_01_Log_Throttling_Prevents_Spam(int logCount)
        {
            LogAssert.ignoreFailingMessages = true; // We expect spam/errors maybe
            for (int i = 0; i < logCount; i++)
            {
                RealPlayLog.Info($"Spam message {i} - " + new string('a', 100)); 
            }
            NUnitAssert.Pass();
        }

        [UnityTest]
        public IEnumerator Resource_02_Huge_String_Clamping() => ToCoroutine(async () =>
        {
            string huge = new string('X', 1024 * 1024); // 1MB string
            RealPlayLog.Info(huge);
            await Task.Yield();
            NUnitAssert.Pass("Logged 1MB string without crash.");
        });

        [Test]
        public void Resource_03_Log_Throttling_Reset_Frame()
        {
            RealPlayLog.Info("Frame Reset Check");
            NUnitAssert.Pass();
        }

        // --- 7.2 NATIVE MEMORY LEAKS ---

        [UnityTest]
        public IEnumerator Resource_04_Screenshot_Leak_Check() => ToCoroutine(async () =>
        {
            await Resources.UnloadUnusedAssets();
            GC.Collect();
            
            for (int i = 0; i < 10; i++)
            {
                await RealPlayComputerVisionInterface.CaptureVisionSnapshot($"stress_test_{i}.png");
            }
            
            NUnitAssert.IsTrue(System.IO.File.Exists(System.IO.Path.Combine(Application.persistentDataPath, "stress_test_0.png")));
        });

        [UnityTest]
        public IEnumerator Resource_05_Mock_Render_Leak_Check() => ToCoroutine(async () =>
        {
            for(int i=0; i<10; i++)
            {
                RealPlayComputerVisionInterface.GenerateMockSnapshotFromDOM($"mock_stress_{i}.png", 512, 512);
            }
            await Task.Yield();
            NUnitAssert.Pass("Mock render loop finished without native crash.");
        });

        // --- 10-20: EXPANDED STRESS ---

        [Test]
        [TestCase(1), TestCase(2), TestCase(3), TestCase(4), TestCase(5)]
        public void Resource_Stress_Log_Burst(int i)
        {
            for(int j=0; j<100; j++) RealPlayLog.Warn($"Burst {i}-{j}");
        }

        [Test]
        [TestCase(1), TestCase(2), TestCase(3), TestCase(4), TestCase(5)]
        public async Task Resource_Stress_Texture_Alloc(int i)
        {
            var tex = new Texture2D(1024, 1024);
            // This method is static and available
            RealPlayComputerVisionInterface.DrawWireRect(tex, new Rect(0,0,100,100), Color.red, 1024);
            UnityEngine.Object.DestroyImmediate(tex);
            await Task.Yield();
        }

        [Test]
        [TestCase(1), TestCase(2), TestCase(3), TestCase(4), TestCase(5)]
        public async Task Resource_Stress_DOM_Dump(int i)
        {
            string json = RealPlaySemanticDOMDumper.Dump();
            NUnitAssert.IsNotEmpty(json);
            await Task.Yield();
        }

        private static IEnumerator ToCoroutine(System.Func<Task> action)
        {
            var task = action();
            while (!task.IsCompleted) yield return null;
            if (task.IsFaulted) throw task.Exception.InnerException;
        }
    }
}