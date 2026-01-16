using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using RealPlayTester.Core;
using RealPlayTester.Input;
using RealPlayTester.UI;
using RealPlayTester.Utilities;
using NUnitAssert = NUnit.Framework.Assert;

namespace RealPlayTester.Tests.Verification
{
    public class NavigationLearningTests
    {
        private GameObject _testRoot;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            _testRoot = new GameObject("NavLearningRoot");
            Tester.Settings.EnableNavigationLearning = true;
            // Ensure we are in Mouse mode for deterministic clicks in batchmode
            RealPlaySettings.PreferredMode = PreferredInputMode.Mouse;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            Object.DestroyImmediate(_testRoot);
            Tester.Settings.EnableNavigationLearning = false;
            RealPlaySettings.PreferredMode = PreferredInputMode.Auto;
            yield return null;
        }

        [UnityTest]
        public IEnumerator Navigation_LearnsAndReplays_Path() => ToCoroutine(async () =>
        {
            var canvas = CreateTestCanvas();
            var panelA = CreatePanel("PanelA", canvas, true);
            var panelB = CreatePanel("PanelB", canvas, false);
            var btn = CreateTransitionButton(panelA, panelB);

            await PrepareScene();

            string from = PanelStateMonitor.GetActiveStateName();
            await Tester.Interaction.Perform("Click", btn);
            
            string to = PanelStateMonitor.GetActiveStateName();
            NUnitAssert.AreNotEqual(from, to, "State should have changed");

            ResetToStartState(panelA, panelB);
            await PrepareScene();

            bool success = await Tester.Navigation.Navigate(from, to);
            NUnitAssert.IsTrue(success, "Auto-discovered navigation should succeed");
            NUnitAssert.IsTrue(panelB.activeInHierarchy, "Panel B should be active after replay");
        });

        private Transform CreateTestCanvas()
        {
            var go = new GameObject("Canvas", typeof(Canvas), typeof(GraphicRaycaster), typeof(CanvasScaler));
            go.transform.SetParent(_testRoot.transform);
            return go.transform;
        }

        private GameObject CreateTransitionButton(GameObject from, GameObject to)
        {
            var btn = new GameObject("GoToB", typeof(RectTransform), typeof(Image), typeof(Button));
            btn.transform.SetParent(from.transform, false);
            btn.GetComponent<Button>().onClick.AddListener(() => {
                from.SetActive(false);
                to.SetActive(true);
                Canvas.ForceUpdateCanvases();
            });
            return btn;
        }

        private async Task PrepareScene()
        {
            await Task.Yield();
            Canvas.ForceUpdateCanvases();
            SceneCache.Instance.ForceRefresh();
        }

        private void ResetToStartState(GameObject a, GameObject b)
        {
            b.SetActive(false);
            a.SetActive(true);
        }

        private GameObject CreatePanel(string name, Transform parent, bool active)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            go.SetActive(active);
            return go;
        }

        private static IEnumerator ToCoroutine(System.Func<Task> action)
        {
            var task = action();
            while (!task.IsCompleted) yield return null;
            if (task.IsFaulted) throw task.Exception.InnerException;
        }
    }
}