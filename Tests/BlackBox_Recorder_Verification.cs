using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using RealPlayTester.Core;
using RealPlayTester.Await;
using RealPlayTester.Diagnostics;
using RealPlayTester.Input;
using UnityEngine.UI;

namespace RealPlayTester.Tests.Verification
{
    public class BlackBox_Recorder_Verification
    {
        private GameObject _testRoot;

        [SetUp]
        public void Setup()
        {
            _testRoot = new GameObject("BlackBox_TestRoot");
            var camGo = new GameObject("TestCamera");
            camGo.transform.SetParent(_testRoot.transform);
            var cam = camGo.AddComponent<Camera>();
            cam.tag = "MainCamera";

            var canvas = _testRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _testRoot.AddComponent<CanvasScaler>();
            _testRoot.AddComponent<GraphicRaycaster>();
        }

        [TearDown]
        public void Teardown()
        {
            Object.DestroyImmediate(_testRoot);
            TestRunContextTracker.EndTest();
        }

        [UnityTest]
        public IEnumerator LogInterceptor_CapturesGameLogs()
        {
            TestRunContextTracker.BeginTest("LogInterceptor_CapturesGameLogs", "BlackBoxScene");
            var context = TestRunContextTracker.Current;

            string testMessage = "Test Game Log " + System.Guid.NewGuid().ToString();
            Debug.Log(testMessage);

            // Give it a frame to process
            yield return null;

            bool found = false;
            foreach (var log in context.Logs)
            {
                if (log.Message.Contains(testMessage))
                {
                    found = true;
                    NUnit.Framework.Assert.AreEqual("Game", log.Type);
                    break;
                }
            }

            NUnit.Framework.Assert.IsTrue(found, "Log message should be captured in TestRunContext.");
        }

        [UnityTest]
        public IEnumerator AlphaChain_ReportsOnTimeout()
        {
            TestRunContextTracker.BeginTest("AlphaChain_ReportsOnTimeout", "BlackBoxScene");
            
            bool originalForce = RealPlaySettings.ForceBatchmodeVisibility;
            RealPlaySettings.ForceBatchmodeVisibility = false;

            try
            {
                var panel = new GameObject("HiddenPanel");
                panel.transform.SetParent(_testRoot.transform);
                var group = panel.AddComponent<CanvasGroup>();
                group.alpha = 0f; // Hidden

                var btnGo = new GameObject("TargetBtn");
                btnGo.transform.SetParent(panel.transform);
                btnGo.AddComponent<Image>();
                btnGo.AddComponent<Button>();

                // We expect a timeout with the visibility trace
                var task = Wait.ForUIVisible("TargetBtn", 0.5f);
                
                while (!task.IsCompleted) yield return null;

                NUnit.Framework.Assert.IsTrue(task.IsFaulted, "Task should have faulted with timeout");
                var ex = task.Exception.Flatten().InnerException as System.TimeoutException;
                NUnit.Framework.Assert.IsNotNull(ex, "Exception should be TimeoutException");
                NUnit.Framework.StringAssert.Contains("Visibility Trace:", ex.Message);
                NUnit.Framework.StringAssert.Contains("HiddenPanel", ex.Message);
                NUnit.Framework.StringAssert.Contains("α:0.0", ex.Message);
            }
            finally
            {
                RealPlaySettings.ForceBatchmodeVisibility = originalForce;
            }
        }

        [UnityTest]
        public IEnumerator RaycastBlocker_ReportsInLogs()
        {
            TestRunContextTracker.BeginTest("RaycastBlocker_ReportsInLogs", "BlackBoxScene");
            var context = TestRunContextTracker.Current;

            CreateTargetButton();
            CreateBlockerImage();

            Canvas.ForceUpdateCanvases();
            yield return null;

            var task = Click.ObjectNamed("TargetBtn");
            while (!task.IsCompleted) yield return null;

            NUnit.Framework.Assert.IsTrue(HasBlockerLog(context), "Should log blocker name when preferred target is blocked.");
        }

        private void CreateTargetButton()
        {
            var btnGo = new GameObject("TargetBtn");
            btnGo.transform.SetParent(_testRoot.transform);
            btnGo.AddComponent<Image>().color = Color.green;
            btnGo.AddComponent<Button>();
            var btnRect = btnGo.GetComponent<RectTransform>();
            btnRect.anchorMin = btnRect.anchorMax = btnRect.pivot = new Vector2(0.5f, 0.5f);
            btnRect.anchoredPosition = Vector2.zero;
            btnRect.sizeDelta = new Vector2(100, 100);
        }

        private void CreateBlockerImage()
        {
            var blocker = new GameObject("BlockerImage");
            blocker.transform.SetParent(_testRoot.transform);
            var img = blocker.AddComponent<Image>();
            img.color = new Color(1, 0, 0, 0.01f);
            img.raycastTarget = true;
            var blockerRect = blocker.GetComponent<RectTransform>();
            blockerRect.anchorMin = blockerRect.anchorMax = blockerRect.pivot = new Vector2(0.5f, 0.5f);
            blockerRect.anchoredPosition = Vector2.zero;
            blockerRect.sizeDelta = new Vector2(200, 200);
        }

        private bool HasBlockerLog(TestRunContext context)
        {
            foreach (var log in context.Logs)
            {
                if (log.Message.Contains("[BLOCKER]") && log.Message.Contains("BlockerImage"))
                    return true;
            }
            return false;
        }
    }
}
