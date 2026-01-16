using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using RealPlayTester.Core;
using RealPlayTester.Tests.Verification;

namespace RealPlayTester.Tests.Verification
{
    public class FrameworkEvolutionAutomationTests
    {
        [UnityTest]
        public IEnumerator RunFrameworkEvolutionVerification()
        {
            yield return RunAssetTest("FrameworkEvolution_Verification");
        }

        [UnityTest]
        public IEnumerator Player_Movement_Cardinal()
        {
            yield return RunMovementTest(t => t.Test_CardinalMovement());
        }

        [UnityTest]
        public IEnumerator Player_Movement_Diagonal()
        {
            yield return RunMovementTest(t => t.Test_DiagonalMovement());
        }

        [UnityTest]
        public IEnumerator Player_Movement_SpecialP()
        {
            yield return RunMovementTest(t => t.Test_SpecialKeyP());
        }

        [UnityTest]
        public IEnumerator Player_Movement_Quadrants()
        {
            yield return RunMovementTest(t => t.Test_DiagonalQuadrants());
        }

        [UnityTest]
        public IEnumerator Player_Movement_LongDistance()
        {
            yield return RunMovementTest(t => t.Test_LongMovement());
        }

        [UnityTest]
        public IEnumerator Player_Movement_Sequence()
        {
            yield return RunMovementTest(t => t.Test_MovementSequence());
        }

        [UnityTest]
        public IEnumerator Physics_Simulation_Toggle()
        {
            yield return RunConfigTest(t => (t as ConfigurationVerificationTests).Test_PhysicsToggles());
        }

        private IEnumerator RunConfigTest(System.Func<RealPlayTest, Task> testAction)
        {
            string fullName = "RealPlayTester.Tests.Verification.ConfigurationVerificationTests";
            var testInstance = ScriptableObject.CreateInstance(fullName) as RealPlayTest;
            if (testInstance == null)
            {
                NUnit.Framework.Assert.Fail($"Could not create ConfigurationVerificationTests instance");
                yield break;
            }

            var descriptor = new TestCaseDescriptor(testInstance, null, null);
            var task = RealPlayTester.Core.TestRunner.Instance.RunSingleTest(descriptor, default);
            while (!task.IsCompleted) yield return null;
            
            var subTask = testAction(testInstance);
            while (!subTask.IsCompleted) yield return null;
            
            Object.DestroyImmediate(testInstance);
        }

        private IEnumerator RunMovementTest(System.Func<PlayerMovementTests, Task> testAction)
        {
            string fullName = "RealPlayTester.Tests.Verification.PlayerMovementTests";
            var testInstance = ScriptableObject.CreateInstance(fullName) as PlayerMovementTests;
            if (testInstance == null)
            {
                NUnit.Framework.Assert.Fail($"Could not create PlayerMovementTests instance");
                yield break;
            }

            var descriptor = new TestCaseDescriptor(testInstance, null, null);
            var task = RealPlayTester.Core.TestRunner.Instance.RunSingleTest(descriptor, default);
            
            // Wait for internal initialization if needed
            while (!task.IsCompleted) yield return null;
            
            // Now run the specific action manually to ensure it's exposed
            var subTask = testAction(testInstance);
            while (!subTask.IsCompleted) yield return null;
            
            Object.DestroyImmediate(testInstance);
        }

        private IEnumerator RunAssetTest(string testName)
        {
            string fullName = testName;
            if (!testName.Contains("."))
            {
                // Try to find the full name
                if (testName == "PlayerMovementTests") fullName = "RealPlayTester.Tests.Verification.PlayerMovementTests";
                else if (testName == "FrameworkEvolution_Verification") fullName = "RealPlayTester.Tests.Verification.FrameworkEvolution_Verification";
            }

            // Create the test instance directly
            var testInstance = ScriptableObject.CreateInstance(fullName) as RealPlayTest;
            if (testInstance == null)
            {
                NUnit.Framework.Assert.Fail($"Could not create test instance for: {fullName} (input: {testName})");
                yield break;
            }

            var descriptor = new TestCaseDescriptor(testInstance, null, null);
            
            // Run it via TestRunner to leverage the new framework lifecycle (attributes, filters, etc.)
            var task = RealPlayTester.Core.TestRunner.Instance.RunSingleTest(descriptor, default);
            
            while (!task.IsCompleted) yield return null;
            
            var result = task.Result;
            NUnit.Framework.Assert.IsNotNull(result, "Test result should not be null");
            NUnit.Framework.Assert.IsTrue(result.Passed, $"{testName} failed: {result.ErrorMessage}");
            
            Object.DestroyImmediate(testInstance);
        }

        private IEnumerator WaitForTask<T>(Task<T> task)
        {
            while (!task.IsCompleted) yield return null;
        }
    }
}
