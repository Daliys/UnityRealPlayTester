using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using RealPlayTester.Core;

namespace RealPlayTester.Tests.Verification
{
    public class ChaosMonkeyTests
    {
        [UnityTest]
        public IEnumerator StartChaosMonkey_PerformsMultipleActions() => ToCoroutine(async () =>
        {
            // 1. Setup a simple UI with buttons
            var canvasGo = new GameObject("MonkeyCanvas", typeof(Canvas), typeof(UnityEngine.UI.GraphicRaycaster));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            
            int clickCount = 0;
            for (int i = 0; i < 5; i++)
            {
                var btnGo = new GameObject($"Btn_{i}", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
                btnGo.transform.SetParent(canvasGo.transform);
                btnGo.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => clickCount++);
                btnGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, i * 50);
                btnGo.GetComponent<RectTransform>().sizeDelta = new Vector2(100, 40);
            }

            await Task.Yield();

            // 2. Run Monkey for 2 seconds
            Debug.Log("Starting Chaos Monkey for 2s...");
            var monkeyTask = Tester.Advanced.StartChaosMonkey(2f, 0.1f);
            
            await monkeyTask;

            // 3. Verify actions were performed
            Debug.Log($"Monkey finished. Total clicks recorded: {clickCount}");
            NUnit.Framework.Assert.Greater(clickCount, 0, "Monkey should have clicked at least one button");

            Object.Destroy(canvasGo);
        });

        [UnityTest]
        public IEnumerator StopChaosMonkey_AbortsExecution() => ToCoroutine(async () =>
        {
            // Run for a long time
            var monkeyTask = Tester.Advanced.StartChaosMonkey(60f, 0.1f);
            
            await Task.Delay(500); // Let it start
            
            Debug.Log("Stopping Chaos Monkey manually...");
            Tester.Advanced.StopChaosMonkey();
            
            // Should finish almost immediately
            var startTime = Time.realtimeSinceStartup;
            await monkeyTask;
            var duration = Time.realtimeSinceStartup - startTime;
            
            NUnit.Framework.Assert.Less(duration, 1.0f, "Monkey should have stopped quickly after StopChaosMonkey()");
        });

        private static IEnumerator ToCoroutine(System.Func<Task> action)
        {
            var task = action();
            while (!task.IsCompleted) yield return null;
            if (task.IsFaulted) throw task.Exception;
        }
    }
}
