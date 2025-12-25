using System;
using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using RealPlayTester.Core;
using Assert = RealPlayTester.Assert.Assert;
using NAssert = NUnit.Framework.Assert;
using UnityEngine.Assertions;

namespace RealPlayTester.Tests.PlayMode
{
    public class FeatureVerificationTests
    {
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1.0f; // Reset if assertion paused
            Tester.Events.Clear();
            Tester.Monitoring.StopVisualHealthCheck();
            yield return null;
        }

        [UnityTest]
        public IEnumerator VisualAssertions_IsVisible_Works()
        {
            // Setup
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "VisibleCube";
            go.transform.position = new Vector3(0, 0, 5); // In front of camera
            var cam = new GameObject("Camera").AddComponent<Camera>();
            cam.transform.position = Vector3.zero;
            cam.transform.LookAt(go.transform);

            yield return null;

            // Test Success
            NAssert.DoesNotThrow(() => Assert.IsVisible(go));

            // Test Failure (Inactive)
            go.SetActive(false);
            yield return null;
            NAssert.Throws<AssertionException>(() => Assert.IsVisible(go));

            // Cleanup
            UnityEngine.Object.Destroy(go);
            UnityEngine.Object.Destroy(cam.gameObject);
        }

        [UnityTest]
        public IEnumerator EventTracking_RecordsAndVerifies()
        {
            Tester.Events.Clear();
            Tester.Events.Record("HeroSpawn");
            
            // Should verify success
            NAssert.DoesNotThrow(() => Assert.EventFired("HeroSpawn", 1));

            // Should fail if not fired
            NAssert.Throws<AssertionException>(() => Assert.EventFired("EnemyDeath"));

            yield return null;
        }

        [UnityTest]
        public IEnumerator InteractionGate_PerformAndVerify_Works()
        {
            bool flag = false;
            Func<Task> interaction = async () => 
            {
                await Task.Yield();
                flag = true;
            };

            // Success case
            var task = Tester.PerformAndVerify(interaction, () => flag, timeout: 1.0f);
            yield return WaitForTask(task);
            NAssert.IsTrue(task.IsCompletedSuccessfully, "Interaction should succeed");
            NAssert.IsTrue(flag);

            // Failure case
            flag = false;
            Func<Task> noOp = async () => await Task.Yield();
            var failTask = Tester.PerformAndVerify(noOp, () => flag, timeout: 0.1f);
            
            // We expect the task to fault or the Assert inside to throw. 
            // Since PerformAndVerify is async and calls Assert.Fail which throws, the task will be faulted.
            yield return WaitForTask(failTask);
            NAssert.IsTrue(failTask.IsFaulted, "Task should fail when verification fails");
        }

        [UnityTest]
        public IEnumerator VisualHealthMonitor_DetectsMissingMaterial()
        {
            Tester.Monitoring.StartVisualHealthCheck(0.1f);

            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = null; // Simulate missing material

            // Monitor runs in background. We expect it to eventually trigger Assert.Fail, 
            // which in Unity Test Runner might capture as an exception on a thread or coroutine.
            // Since Assert.Fail sets Time.timeScale = 0, we can check that.
            
            float start = Time.realtimeSinceStartup;
            bool failed = false;
            while (Time.realtimeSinceStartup - start < 1.0f)
            {
                if (Time.timeScale == 0f) 
                {
                    failed = true;
                    break;
                }
                yield return null;
            }

            // Cleanup before Assert
            UnityEngine.Object.Destroy(go);
            Tester.Monitoring.StopVisualHealthCheck();
            Time.timeScale = 1.0f; // Reset

            NAssert.IsTrue(failed, "VisualHealthMonitor should have paused time due to failure.");
        }

        [UnityTest]
        public IEnumerator AssetValidation_LoadsSampleAsset()
        {
            // We know "CompleteLevel1Sample" exists in Resources/RealPlayTests
            // Path is "RealPlayTests/CompleteLevel1Sample"
            NAssert.DoesNotThrow(() => Assert.AssetLoaded<ScriptableObject>("RealPlayTests/CompleteLevel1Sample"));
            
            // Fail case
            NAssert.Throws<AssertionException>(() => Assert.AssetLoaded<GameObject>("NonExistentAsset"));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Screenshot_CaptureCreatesFile()
        {
            string testName = "VerifyScreenshot_" + Guid.NewGuid().ToString();
            
            // This should create a baseline since none exists
            Tester.Screenshot.CaptureAndCompare(testName);
            
            string baselinePath = System.IO.Path.Combine(RealPlayEnvironment.ProjectRoot, "TestBaselines", testName + ".png");
            NAssert.IsTrue(System.IO.File.Exists(baselinePath), "Baseline screenshot should be created.");

            // Cleanup
            if (System.IO.File.Exists(baselinePath)) System.IO.File.Delete(baselinePath);
            yield return null;
        }

        private IEnumerator WaitForTask(Task task)
        {
            while (!task.IsCompleted) yield return null;
        }
    }
}
