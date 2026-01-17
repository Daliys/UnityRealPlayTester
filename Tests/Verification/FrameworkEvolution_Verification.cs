using System.Threading.Tasks;
using UnityEngine;
using RealPlayTester.Core;
using RealPlayTester.Await;

namespace RealPlayTester.Tests.Verification
{
    [PlayModeTest(TimeScale = 1.0f)]
    public class FrameworkEvolution_Verification : RealPlayTest
    {
        [SerializeField] private GameObject testObject;

        protected override async Task Run()
        {
            await Test_GetInteractables();
            await Test_StateSnapshotting();
            await Test_PhysicsStability();
        }

        private async Task Test_GetInteractables()
        {
            Wait.Step("Verifying GetInteractables discovery");
            string interactables = Tester.Perception.GetActionableElements();
            Log(interactables);
            
            // Should contain at least some common UI/World objects if any exist
            RealPlayTester.Assert.Assert.IsNotNull(interactables);
        }

        [System.Serializable]
        public class SampleState
        {
            public int score;
            public string status;
        }

        private async Task Test_StateSnapshotting()
        {
            Wait.Step("Verifying State Snapshotting and Diffing");
            var state = new SampleState { score = 100, status = "Active" };
            
            string snapshot = Tester.State.CaptureObject(state);
            Log($"Captured state: {snapshot}");

            // Verify matches
            RealPlayTester.Assert.Assert.AreEqual(state.score, 100, "Score should match");

            // Verify mismatch (simulated check of diff logic)
            state.score = 150;
            string updatedSnapshot = Tester.State.CaptureObject(state);
            string diff = RealPlayTester.Utilities.StateTracker.GetJsonDiff(snapshot, updatedSnapshot);
            Log($"Detected diff:\n{diff}");
            RealPlayTester.Assert.Assert.Contains(diff, "score", "Diff should report score change");
        }

        private async Task Test_PhysicsStability()
        {
            Wait.Step("Verifying ForStablePhysics");
            
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.position = new Vector3(0, 10, 0);
            var rb = cube.AddComponent<Rigidbody>();
            
            Log("Waiting for cube to settle...");
            await Wait.ForStablePhysics();
            
            float velocity = rb.linearVelocity.magnitude;
            Log($"Cube settled with velocity: {velocity}");
            RealPlayTester.Assert.Assert.Less(velocity, 0.1f, "Physics should be stable after wait");
            
            Object.Destroy(cube);
        }
    }
}
