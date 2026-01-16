using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using RealPlayTester.Core;
using RealPlayTester.Input;
using RealPlayTester.Utilities;
using NUnitAssert = NUnit.Framework.Assert;

namespace RealPlayTester.Tests.Verification
{
    public partial class ZOrderAmbiguityTests
    {
        private GameObject _testRoot;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            _testRoot = new GameObject("ZOrderTestRoot");
            var host = RealPlayTesterHost.Instance;
            host.gameObject.SetActive(true);
            
            var camGo = GameObject.FindWithTag("MainCamera");
            if (camGo == null)
            {
                camGo = new GameObject("MainCamera", typeof(Camera));
                camGo.tag = "MainCamera";
            }
            var cam = camGo.GetComponent<Camera>();
            cam.transform.position = new Vector3(0, 0, -10);
            cam.transform.LookAt(Vector3.zero);
            cam.enabled = true;

            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            if (_testRoot != null) Object.DestroyImmediate(_testRoot);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SmartFind_PrioritizesHigherSortingOrder() => ToCoroutine(async () =>
        {
            var c1 = CreateCanvas("LowCanvas", 0);
            var btn1 = CreateButton("Duplicate", Vector2.zero, c1.transform);

            var c2 = CreateCanvas("HighCanvas", 1000);
            var btn2 = CreateButton("Duplicate", Vector2.zero, c2.transform);

            await Tester.Await.Seconds(0.1f);
            SceneCache.Instance.ForceRefresh();

            var found = SmartFind.Object("Duplicate");
            NUnitAssert.IsNotNull(found);
            NUnitAssert.AreEqual(btn2.GetInstanceID(), found.GetInstanceID(), "Should have picked HighCanvas button");
        });

        [UnityTest]
        public IEnumerator SmartFind_PrioritizesCloserWorldObject() => ToCoroutine(async () =>
        {
            var far = GameObject.CreatePrimitive(PrimitiveType.Cube);
            far.name = "WorldObj";
            far.transform.position = new Vector3(0, 0, 50); 
            far.transform.SetParent(_testRoot.transform);

            var close = GameObject.CreatePrimitive(PrimitiveType.Cube);
            close.name = "WorldObj";
            close.transform.position = new Vector3(0, 0, 0); 
            close.transform.SetParent(_testRoot.transform);

            await Tester.Await.Frames(2);
            SceneCache.Instance.ForceRefresh();

            var found = SmartFind.Object("WorldObj");
            NUnitAssert.IsNotNull(found);
            NUnitAssert.AreEqual(close.GetInstanceID(), found.GetInstanceID(), "Should have picked closer world object");
        });

        [UnityTest]
        public IEnumerator SmartFind_PartialVisible_WinsOver_ExactOccluded() => ToCoroutine(async () =>
        {
            var c1 = CreateCanvas("Canvas1", 0);
            var exact = CreateButton("ExactName", Vector2.zero, c1.transform);
            
            var blocker = new GameObject("Blocker", typeof(RectTransform), typeof(Image));
            blocker.transform.SetParent(c1.transform, false);
            blocker.GetComponent<RectTransform>().sizeDelta = new Vector2(2000, 2000);
            blocker.transform.SetAsLastSibling();

            var partial = CreateButton("ExactName_Partial", new Vector2(300, 0), c1.transform);
            var highC = CreateCanvas("HighCanvas", 1000);
            partial.transform.SetParent(highC.transform, false);

            await Tester.Await.Frames(5);
            SceneCache.Instance.ForceRefresh();

            var found = SmartFind.Object("ExactName");
            NUnitAssert.AreEqual(partial.GetInstanceID(), found.GetInstanceID(), "Visible partial match should win over occluded exact match");
        });

        [UnityTest]
        public IEnumerator SmartFind_UIPriority_WinsOverWorld() => ToCoroutine(async () =>
        {
            var world = GameObject.CreatePrimitive(PrimitiveType.Cube);
            world.name = "OverlapTarget";
            world.transform.position = Vector3.zero;
            world.transform.SetParent(_testRoot.transform);

            var c = CreateCanvas("UICanvas", 10);
            var ui = CreateButton("OverlapTarget", Vector2.zero, c.transform);

            await Task.Yield();
            SceneCache.Instance.ForceRefresh();

            var found = SmartFind.Object("OverlapTarget");
            NUnitAssert.AreEqual(ui.GetInstanceID(), found.GetInstanceID(), "UI should have priority over world objects");
        });

        [UnityTest]
        public IEnumerator SmartFind_DeeperHierarchy_WinsTie() => ToCoroutine(async () =>
        {
            var c = CreateCanvas("Canvas", 0);
            var shallow = CreateButton("Tie", Vector2.zero, c.transform);
            
            var nestedParent = new GameObject("Nested", typeof(RectTransform));
            nestedParent.transform.SetParent(c.transform, false);
            var deep = CreateButton("Tie", Vector2.zero, nestedParent.transform);

            await Task.Yield();
            SceneCache.Instance.ForceRefresh();

            var found = SmartFind.Object("Tie");
            NUnitAssert.AreEqual(deep.GetInstanceID(), found.GetInstanceID(), "Deeper object in hierarchy should win tie");
        });

        [UnityTest]
        public IEnumerator SmartFind_IgnoresObjectsInInactiveHierarchy() => ToCoroutine(async () =>
        {
            var parent = new GameObject("Parent");
            parent.transform.SetParent(_testRoot.transform);
            var child = new GameObject("HiddenChild");
            child.transform.SetParent(parent.transform);
            
            parent.SetActive(false);
            await Task.Yield();
            SceneCache.Instance.ForceRefresh();

            var found = SmartFind.Object("HiddenChild");
            NUnitAssert.IsNull(found, "Should not find child of inactive parent");
        });

        [UnityTest]
        public IEnumerator SmartFind_RespectsNestedCanvasSorting() => ToCoroutine(async () =>
        {
            var rootCanvas = CreateCanvas("Root", 100);
            var childCanvasObj = new GameObject("ChildCanvas", typeof(Canvas));
            childCanvasObj.transform.SetParent(rootCanvas.transform, false);
            var childCanvas = childCanvasObj.GetComponent<Canvas>();
            childCanvas.overrideSorting = true;
            childCanvas.sortingOrder = 500;

            var btn = CreateButton("NestedBtn", Vector2.zero, childCanvas.transform);

            await Task.Yield();
            SceneCache.Instance.ForceRefresh();

            var found = SmartFind.Object("NestedBtn");
            NUnitAssert.IsNotNull(found);
        });

        [UnityTest]
        public IEnumerator SteadyState_MultipleMovingObjects_WaitCorrectly() => ToCoroutine(async () =>
        {
            var rb1Go = new GameObject("RB1", typeof(Rigidbody2D));
            var rb2Go = new GameObject("RB2", typeof(Rigidbody2D));
            rb1Go.transform.SetParent(_testRoot.transform);
            rb2Go.transform.SetParent(_testRoot.transform);
            var rb1 = rb1Go.GetComponent<Rigidbody2D>();
            var rb2 = rb2Go.GetComponent<Rigidbody2D>();
            rb1.bodyType = RigidbodyType2D.Dynamic;
            rb2.bodyType = RigidbodyType2D.Dynamic;
            rb1.gravityScale = 0;
            rb2.gravityScale = 0;
            
            rb1.linearVelocity = Vector2.one * 10f;
            rb2.linearVelocity = Vector2.one * 10f;

            await Task.Yield();
            SceneCache.Instance.ForceRefresh();

            var waitTask = Tester.Await.ForSteadyState(stableFrames: 5, timeout: 2f);
            
            await Tester.Await.Frames(5);
            NUnitAssert.IsFalse(waitTask.IsCompleted, "Should be waiting for both");

            rb1.linearVelocity = Vector2.zero;
            await Task.Yield();
            SceneCache.Instance.ForceRefresh();

            await Tester.Await.Frames(5);
            NUnitAssert.IsFalse(waitTask.IsCompleted, "Should still wait for RB2");

            rb2.linearVelocity = Vector2.zero;
            await Task.Yield();
            SceneCache.Instance.ForceRefresh();

            await waitTask;
            NUnitAssert.IsTrue(waitTask.IsCompleted, "Should have finished after both stopped");
        });

        private GameObject CreateCanvas(string name, int order)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(GraphicRaycaster), typeof(CanvasScaler));
            go.transform.SetParent(_testRoot.transform);
            var c = go.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = order;
            return go;
        }

        private GameObject CreateButton(string name, Vector2 pos, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Button), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(200, 200); 
            return go;
        }

        private static IEnumerator ToCoroutine(System.Func<Task> action)
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
    }
}