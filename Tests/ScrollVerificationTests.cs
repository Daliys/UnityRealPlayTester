using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using RealPlayTester.Core;
using RealPlayTester.Input;
using NAssert = NUnit.Framework.Assert;

namespace RealPlayTester.Tests.Verification
{
    public class ScrollVerificationTests
    {
        private GameObject _root;
        private Camera _camera;
        private ScrollRect _scrollRect;
        private RectTransform _content;
        private RectTransform _viewport;

        [SetUp]
        public void Setup()
        {
            _root = new GameObject("ScrollTestRoot");
            _camera = new GameObject("MainCamera").AddComponent<Camera>();
            _camera.transform.SetParent(_root.transform);
            
            var canvasObj = CreateCanvas();
            CreateScrollView(canvasObj);
            CreateViewport();
            CreateContent();

            _scrollRect.viewport = _viewport;
            _scrollRect.content = _content;
            _scrollRect.vertical = true;
            _scrollRect.horizontal = false;
        }

        private GameObject CreateCanvas()
        {
            var canvasObj = new GameObject("Canvas");
            canvasObj.transform.SetParent(_root.transform);
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            if (UnityEngine.Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }
            return canvasObj;
        }

        private void CreateScrollView(GameObject canvasObj)
        {
            var svObj = new GameObject("ScrollView");
            svObj.transform.SetParent(canvasObj.transform, false);
            _scrollRect = svObj.AddComponent<ScrollRect>();
            var svRect = svObj.GetComponent<RectTransform>();
            svRect.sizeDelta = new Vector2(200, 200);
        }

        private void CreateViewport()
        {
            var vpObj = new GameObject("Viewport");
            vpObj.transform.SetParent(_scrollRect.transform, false);
            _viewport = vpObj.AddComponent<RectTransform>();
            _viewport.anchorMin = Vector2.zero;
            _viewport.anchorMax = Vector2.one;
            _viewport.sizeDelta = Vector2.zero;
            vpObj.AddComponent<Image>().color = Color.gray;
            vpObj.AddComponent<Mask>().showMaskGraphic = true;
        }

        private void CreateContent()
        {
            var contentObj = new GameObject("Content");
            contentObj.transform.SetParent(_viewport.transform, false);
            _content = contentObj.AddComponent<RectTransform>();
            _content.anchorMin = new Vector2(0, 1);
            _content.anchorMax = new Vector2(1, 1);
            _content.pivot = new Vector2(0.5f, 1);
            _content.sizeDelta = new Vector2(200, 1000);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_root != null) Object.Destroy(_root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Scroll_LargeElement_ScrollsToView() => ToCoroutine(async () =>
        {
            // Create a large element (larger than viewport 200x200)
            var targetObj = new GameObject("LargeTarget");
            targetObj.transform.SetParent(_content, false);
            var targetRect = targetObj.AddComponent<RectTransform>();
            targetRect.sizeDelta = new Vector2(180, 400); // Taller than viewport
            targetRect.anchoredPosition = new Vector2(0, -600); // Way down

            var img = targetObj.AddComponent<Image>();
            img.color = Color.red;

            // Should scroll until at least partially visible or fully covering viewport
            await Scroll.UntilVisible(_scrollRect, targetRect, timeoutSeconds: 2f);

            // Assert: Check intersection
            bool visible = IsVisibleRecursive(_scrollRect, targetRect);
            NAssert.IsTrue(visible, "Large element should be considered visible after scroll");
        });

        [UnityTest]
        public IEnumerator Scroll_NormalElement_ScrollsToView() => ToCoroutine(async () =>
        {
            var targetObj = new GameObject("NormalTarget");
            targetObj.transform.SetParent(_content, false);
            var targetRect = targetObj.AddComponent<RectTransform>();
            targetRect.sizeDelta = new Vector2(100, 100);
            targetRect.anchoredPosition = new Vector2(0, -400);

            var img = targetObj.AddComponent<Image>();
            img.color = Color.green;

            await Scroll.UntilVisible(_scrollRect, targetRect, timeoutSeconds: 5f);

            bool visible = IsVisibleRecursive(_scrollRect, targetRect);
            NAssert.IsTrue(visible, "Normal element should be visible");
        });

        public static IEnumerator ToCoroutine(System.Func<Task> action)
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

        // Helper to verifying visibility (mirroring Logic we expect)
        private bool IsVisibleRecursive(ScrollRect scroll, RectTransform target)
        {
            Vector3[] viewCorners = new Vector3[4];
            scroll.viewport.GetWorldCorners(viewCorners);
            Rect viewRect = CreateRectFromCorners(viewCorners);

            Vector3[] targetCorners = new Vector3[4];
            target.GetWorldCorners(targetCorners);
            Rect targetRect = CreateRectFromCorners(targetCorners);

            return viewRect.Overlaps(targetRect);
        }

        private Rect CreateRectFromCorners(Vector3[] corners)
        {
            float xMin = Mathf.Min(corners[0].x, corners[1].x, corners[2].x, corners[3].x);
            float xMax = Mathf.Max(corners[0].x, corners[1].x, corners[2].x, corners[3].x);
            float yMin = Mathf.Min(corners[0].y, corners[1].y, corners[2].y, corners[3].y);
            float yMax = Mathf.Max(corners[0].y, corners[1].y, corners[2].y, corners[3].y);
            return new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
        }
    }
}
