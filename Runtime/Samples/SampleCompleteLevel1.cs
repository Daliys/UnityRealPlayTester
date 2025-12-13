using System.Threading.Tasks;
using UnityEngine;
using RealPlayTester.Await;
using RealPlayTester.Core;
using RealPlayTester.Input;
using Assert = RealPlayTester.Assert.Assert;

namespace RealPlayTester.Samples
{
    /// <summary>
    /// Sample test demonstrating RealPlayTester API usage.
    /// Replace references with real scene objects when authoring your own tests.
    /// </summary>
    [CreateAssetMenu(menuName = "RealPlay Tests/Complete Level 1")]
    public class CompleteLevel1 : RealPlayTest
    {
        [SerializeField] private GameObject enemyBoss;
        [SerializeField] private GameObject player;
#pragma warning disable CS0414 // Field is assigned but its value is never used
        [SerializeField] private int requiredScore = 50000;
#pragma warning restore CS0414

        protected override async Task Run()
        {
            // Example: Click button at bottom center of screen
            await Click.ScreenPercent(0.5f, 0.8f);

            // Wait for level to load
            await Wait.Seconds(0.1f);

            // Example assertion
            RealPlayTester.Assert.Assert.IsTrue(true, "Sample test placeholder - replace with real assertions");
        }
    }
}
