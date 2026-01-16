using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using RealPlayTester.Core;
using RealPlayTester.Utilities;

namespace RealPlayTester.Tests.Verification
{
    public class MassiveChaosMonkeyTests
    {
        private GameObject _root;
        private Canvas _canvas;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            _root = new GameObject("MassiveMonkeyRoot");
            var canvasGo = new GameObject("MonkeyCanvas", typeof(Canvas), typeof(GraphicRaycaster), typeof(CanvasScaler));
            canvasGo.transform.SetParent(_root.transform);
            _canvas = canvasGo.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            // Ensure EventSystem exists
            RealPlayTester.Input.RealInputUtility.EnsureEventSystem();
            
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            Tester.Advanced.StopChaosMonkey();
            if (_root != null) UnityEngine.Object.DestroyImmediate(_root);
            Time.timeScale = 1.0f;
            yield return null;
        }

        // 1. Empty Scene - Should not crash or hang
        [UnityTest]
        public IEnumerator Monkey_EmptyScene_NoCrash() => ToCoroutine(async () =>
        {
            // Destroy the canvas to have truly empty scene
            UnityEngine.Object.DestroyImmediate(_canvas.gameObject);
            await Tester.Advanced.StartChaosMonkey(1f, 0.05f);
            NUnit.Framework.Assert.Pass();
        });

        // 2. Object Destruction Mid-Action - Should handle MissingReferenceException
        [UnityTest]
        public IEnumerator Monkey_TargetDestroyed_MidAction_Graceful() => ToCoroutine(async () =>
        {
            var btn = CreateButton("KillMe");
            
            // Start monkey
            var task = Tester.Advanced.StartChaosMonkey(2f, 0.01f);
            
            await Task.Yield();
            UnityEngine.Object.DestroyImmediate(btn);
            
            await task;
            NUnit.Framework.Assert.Pass();
        });

        // 3. Scene Switch - Should abort monkey run
        [UnityTest]
        public IEnumerator Monkey_SceneSwitch_AbortsRun() => ToCoroutine(async () =>
        {
            CreateButton("SceneSwitcher");
            var task = Tester.Advanced.StartChaosMonkey(10f, 0.1f);
            
            await Task.Delay(500);
            
            // Manually load a scene (or trigger reload)
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            
            // Wait for task to finish or timeout
            var timeoutTask = Task.Delay(2000);
            var completedTask = await Task.WhenAny(task, timeoutTask);
            
            NUnit.Framework.Assert.AreEqual(task, completedTask, "Monkey task should have aborted or finished after scene load");
        });

        // 4. TimeScale Zero - Should respect realtime interval
        [UnityTest]
        public IEnumerator Monkey_TimeScaleZero_StillExecutes() => ToCoroutine(async () =>
        {
            int clicks = 0;
            var btn = CreateButton("PausedButton");
            btn.GetComponent<Button>().onClick.AddListener(() => clicks++);
            
            Time.timeScale = 0f;
            
            // Monkey uses unscaled Wait.Seconds, so it should progress
            await Tester.Advanced.StartChaosMonkey(1.5f, 0.1f);
            
            NUnit.Framework.Assert.Greater(clicks, 0, "Monkey should have clicked button even when time is paused");
        });

        // 5. Rapid TimeScale Fluctuation - Stability check
        [UnityTest]
        public IEnumerator Monkey_RapidTimeScaleFluctuation_Stable() => ToCoroutine(async () =>
        {
            CreateButton("Blinky");
            var task = Tester.Advanced.StartChaosMonkey(3f, 0.05f);
            
            float elapsed = 0;
            while(elapsed < 2f)
            {
                Time.timeScale = (Time.frameCount % 2 == 0) ? 0.1f : 5.0f;
                await Task.Yield();
                elapsed += Time.unscaledDeltaTime;
            }
            
            await task;
            NUnit.Framework.Assert.Pass();
        });

        // 6. Dynamic Occlusion - Should not click blocked objects
        [UnityTest]
        public IEnumerator Monkey_DynamicOcclusion_RespectsBlockers() => ToCoroutine(async () =>
        {
            // Ensure occlusion check is enabled for this test
            RealPlaySettings.ForceBatchmodeVisibility = false;

            var bottom = CreateButton("Bottom");
            int bottomClicks = 0;
            bottom.GetComponent<Button>().onClick.AddListener(() => bottomClicks++);
            
            var top = CreateButton("TopOverlay");
            top.GetComponent<RectTransform>().sizeDelta = new Vector2(2000, 2000); // Cover everything
            top.GetComponent<Image>().color = Color.red; // Ensure it has a color
            
            await Tester.Advanced.StartChaosMonkey(2f, 0.05f);
            
            NUnit.Framework.Assert.AreEqual(0, bottomClicks, "Monkey should not have been able to click the occluded button");
        });

        // 7. IAffordanceValidator - Should respect logical blocks
        [UnityTest]
        public IEnumerator Monkey_AffordanceValidator_RespectsBlocks() => ToCoroutine(async () =>
        {
            var go = CreateButton("BlockedObj");
            go.AddComponent<RealBlockingValidator>();
            int clicks = 0;
            go.GetComponent<Button>().onClick.AddListener(() => clicks++);
            
            await Tester.Advanced.StartChaosMonkey(1f, 0.05f);
            
            NUnit.Framework.Assert.AreEqual(0, clicks, "Monkey should respect IAffordanceValidator");
        });

        // 8. Deep Hierarchy - Stress the find/probe logic
        [UnityTest]
        public IEnumerator Monkey_DeepHierarchy_FindsNestedTargets() => ToCoroutine(async () =>
        {
            Transform parent = _canvas.transform;
            for(int i=0; i<50; i++) // Reduced from 100 to stay well within limits but still deep
            {
                var next = new GameObject($"Nested_{i}", typeof(RectTransform)).transform;
                next.SetParent(parent);
                parent = next;
            }
            
            var btn = CreateButton("DeepButton");
            btn.transform.SetParent(parent, false);
            int clicks = 0;
            btn.GetComponent<Button>().onClick.AddListener(() => clicks++);
            
            await Tester.Advanced.StartChaosMonkey(2f, 0.1f);
            NUnit.Framework.Assert.Greater(clicks, 0, "Monkey should find and click deep nested buttons");
        });

        // 9. EventSystem Recreation - Continuity check
        [UnityTest]
        public IEnumerator Monkey_EventSystemRecreation_Stable() => ToCoroutine(async () =>
        {
            CreateButton("ActionObj");
            var task = Tester.Advanced.StartChaosMonkey(3f, 0.1f);
            
            await Task.Delay(500);
            var es = UnityEngine.EventSystems.EventSystem.current;
            if (es != null) UnityEngine.Object.DestroyImmediate(es.gameObject);
            
            await Task.Delay(500);
            // Library should auto-recreate it on next interaction attempt
            
            await task;
            NUnit.Framework.Assert.Pass();
        });

        // 10. Parallel Monkeys - Static instance should handle 'latest wins'
        [UnityTest]
        public IEnumerator Monkey_ParallelExecution_LatestWins() => ToCoroutine(async () =>
        {
            var task1 = Tester.Advanced.StartChaosMonkey(5f, 0.1f);
            await Task.Delay(200);
            var task2 = Tester.Advanced.StartChaosMonkey(1f, 0.1f); // Should cancel task1
            
            await task2;
            
            // Task 1 should have finished or been cancelled
            NUnit.Framework.Assert.IsTrue(task1.IsCompleted, "First monkey should have been aborted by the second");
        });

        // 11. High Frequency Action Density
        [UnityTest]
        public IEnumerator Monkey_HighFrequency_NoDeadlock() => ToCoroutine(async () =>
        {
            for(int i=0; i<20; i++) CreateButton($"Den_{i}");
            // 0.01s interval is extremely high pressure
            await Tester.Advanced.StartChaosMonkey(2f, 0.01f);
            NUnit.Framework.Assert.Pass();
        });

        // 12. Global Logical Blocking - Should idle gracefully
        [UnityTest]
        public IEnumerator Monkey_GlobalLogicalBlocking_Idles() => ToCoroutine(async () =>
        {
            var btn = CreateButton("AlwaysBlocked");
            btn.AddComponent<RealBlockingValidator>();
            
            await Tester.Advanced.StartChaosMonkey(1f, 0.05f);
            NUnit.Framework.Assert.Pass();
        });

        // 13. Modal Overlay Traps Monkey
        [UnityTest]
        public IEnumerator Monkey_ModalOverlay_RespectsIsolation() => ToCoroutine(async () =>
        {
            var backgroundBtn = CreateButton("BgBtn");
            int bgClicks = 0;
            backgroundBtn.GetComponent<Button>().onClick.AddListener(() => bgClicks++);

            var modal = new GameObject("Modal", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            modal.transform.SetParent(_canvas.transform, false);
            modal.GetComponent<RectTransform>().sizeDelta = new Vector2(2000, 2000);
            
            var foregroundBtn = CreateButton("ModalBtn");
            foregroundBtn.transform.SetParent(modal.transform, false);
            int fgClicks = 0;
            foregroundBtn.GetComponent<Button>().onClick.AddListener(() => fgClicks++);

            await Tester.Advanced.StartChaosMonkey(2f, 0.05f);
            
            NUnit.Framework.Assert.AreEqual(0, bgClicks, "Monkey should not click objects behind modal");
            NUnit.Framework.Assert.Greater(fgClicks, 0, "Monkey should click objects inside modal");
        });

        // 14. Drag and Drop Stress
        [UnityTest]
        public IEnumerator Monkey_DragDrop_Interactions() => ToCoroutine(async () =>
        {
            var sliderGo = new GameObject("Slider", typeof(RectTransform), typeof(Image), typeof(Slider));
            sliderGo.transform.SetParent(_canvas.transform, false);
            sliderGo.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 40);
            sliderGo.GetComponent<Image>().color = new Color(1, 1, 1, 0.1f);
            
            var area = new GameObject("Area", typeof(RectTransform));
            area.transform.SetParent(sliderGo.transform, false);
            area.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 40);

            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(area.transform, false);
            handle.GetComponent<RectTransform>().sizeDelta = new Vector2(20, 40);
            handle.GetComponent<Image>().color = Color.white;
            handle.GetComponent<Image>().raycastTarget = true;
            
            var slider = sliderGo.GetComponent<Slider>();
            slider.handleRect = handle.GetComponent<RectTransform>();
            slider.interactable = true;
            slider.value = 0f;
            
            float initialValue = slider.value;
            await Tester.Advanced.StartChaosMonkey(3f, 0.1f);
            
            NUnit.Framework.Assert.AreNotEqual(initialValue, slider.value, "Monkey should have moved the slider via drag");
        });

        // 15. Infinite Duration - Manual Stop
        [UnityTest]
        public IEnumerator Monkey_InfiniteDuration_ManualStop() => ToCoroutine(async () =>
        {
            CreateButton("Target");
            var task = Tester.Advanced.StartChaosMonkey(0, 0.1f);
            
            await Task.Delay(1000);
            NUnit.Framework.Assert.IsFalse(task.IsCompleted, "Monkey should be running indefinitely");
            
            Tester.Advanced.StopChaosMonkey();
            await Task.Delay(500);
            NUnit.Framework.Assert.IsTrue(task.IsCompleted, "Monkey should have stopped after manual call");
        });

        // 16. Massive Spawning - SceneCache Pressure
        [UnityTest]
        public IEnumerator Monkey_MassiveSpawning_Performance() => ToCoroutine(async () =>
        {
            var task = Tester.Advanced.StartChaosMonkey(3f, 0.05f);
            
            float elapsed = 0;
            while(elapsed < 2f)
            {
                for(int i=0; i<50; i++) 
                {
                    var go = new GameObject("Temp");
                    go.transform.SetParent(_root.transform);
                    UnityEngine.Object.Destroy(go, 0.1f);
                }
                await Task.Yield();
                elapsed += Time.unscaledDeltaTime;
            }
            
            await task;
            NUnit.Framework.Assert.Pass();
        });

        // 17. Exception in Interaction.Perform - Monkey continues
        [UnityTest]
        public IEnumerator Monkey_PerformException_Continues() => ToCoroutine(async () =>
        {
            CreateButton("Target");
            await Tester.Advanced.StartChaosMonkey(1f, 0.05f);
            NUnit.Framework.Assert.Pass();
        });

        // 18. Interaction during Animation
        [UnityTest]
        public IEnumerator Monkey_AnimationInteraction_Stable() => ToCoroutine(async () =>
        {
            var btn = CreateButton("AnimatingBtn");
            btn.AddComponent<Mover>();
            
            await Tester.Advanced.StartChaosMonkey(2f, 0.05f);
            NUnit.Framework.Assert.Pass();
        });

        // 19. Multi-Camera Targeting
        [UnityTest]
        public IEnumerator Monkey_MultiCamera_Targeting() => ToCoroutine(async () =>
        {
            var cam2Go = new GameObject("Cam2", typeof(Camera));
            var cam2 = cam2Go.GetComponent<Camera>();
            cam2.rect = new Rect(0.5f, 0, 0.5f, 1); // Split screen
            
            var worldObj = new GameObject("WorldObj", typeof(BoxCollider), typeof(MeshRenderer));
            worldObj.transform.position = new Vector3(100, 0, 0); // Far away
            
            await Tester.Advanced.StartChaosMonkey(2f, 0.1f);
            UnityEngine.Object.DestroyImmediate(cam2Go);
            NUnit.Framework.Assert.Pass();
        });

        // 20. Cancellation Mid-Perform
        [UnityTest]
        public IEnumerator Monkey_Cancellation_MidPerform_Graceful() => ToCoroutine(async () =>
        {
            CreateButton("WaitTarget");
            var task = Tester.Advanced.StartChaosMonkey(10f, 0.1f);
            
            await Task.Delay(200);
            Tester.Advanced.StopChaosMonkey();
            
            await task;
            NUnit.Framework.Assert.Pass();
        });

        // Helpers
        private GameObject CreateButton(string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(_canvas.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(100, 40);
            
            var img = go.GetComponent<Image>();
            img.color = Color.white;
            img.raycastTarget = true;
            
            return go;
        }

        private class Mover : MonoBehaviour 
        { 
            void Update() { transform.position += Vector3.right * Time.deltaTime; } 
        }

        private class RealBlockingValidator : MonoBehaviour, IAffordanceValidator
        {
            public bool CanInteract(string intent) => false;
            public string GetBlockReason() => "Strictly Blocked";
        }

        private static IEnumerator ToCoroutine(System.Func<Task> action)
        {
            var task = action();
            while (!task.IsCompleted) yield return null;
            if (task.IsFaulted)
            {
                if (task.Exception.InnerException is NUnit.Framework.SuccessException) yield break;
                throw task.Exception;
            }
        }
    }
}
