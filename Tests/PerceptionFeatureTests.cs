using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools;
using RealPlayTester.Core;
using RealPlayTester.Core.Perception;
using RealPlayTester.Input;
using NUnitAssert = NUnit.Framework.Assert;

namespace RealPlayTester.Tests.Verification
{
    public class PerceptionFeatureTests
    {
        private GameObject _testRoot;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            _testRoot = new GameObject("PerceptionFeatureRoot");
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            if (_testRoot != null) UnityEngine.Object.DestroyImmediate(_testRoot);
            yield return null;
        }

        // 1. Deep Hierarchy Serialization (using safe limit for JsonUtility)
        [UnityTest]
        public IEnumerator DOM_Handles_DeepHierarchy()
        {
            Transform parent = _testRoot.transform;
            for(int i = 0; i < 8; i++)
            {
                var go = new GameObject($"Depth_{i}");
                go.transform.SetParent(parent);
                parent = go.transform;
            }

            string dom = RealPlaySemanticDOMDumper.Dump();
            NUnitAssert.IsTrue(dom.Contains("Depth_7"), "DOM failed to reach depth level 7");
            yield return null;
        }

        // 2. Occlusion by 2D Sprites
        [UnityTest]
        public IEnumerator Occlusion_Detects_SpriteInterference()
        {
            var target = new GameObject("Target2D", typeof(SpriteRenderer), typeof(BoxCollider2D));
            target.transform.SetParent(_testRoot.transform);
            
            var blocker = new GameObject("Blocker2D", typeof(SpriteRenderer), typeof(BoxCollider2D));
            blocker.transform.SetParent(_testRoot.transform);
            blocker.transform.position = new Vector3(0, 0, -1); // In front of target

            // Temporarily disable batchmode visibility override to test occlusion
            bool prev = Tester.Settings.ForceBatchmodeVisibility;
            Tester.Settings.ForceBatchmodeVisibility = false;

            yield return null;

            try
            {
                var result = RealPlayOcclusionRaycaster.CheckOcclusion(target);
                NUnitAssert.IsTrue(result.IsOccluded, "Should detect 2D blocker via raycast if it has a collider");
            }
            finally
            {
                Tester.Settings.ForceBatchmodeVisibility = prev;
            }
        }

        // 3. Mock Render Aspect Ratio
        [UnityTest]
        public IEnumerator MockRender_Respects_CustomResolution()
        {
            string file = "aspect_test.png";
            RealPlayComputerVisionInterface.GenerateMockSnapshotFromDOM(file, 500, 1000); // Vertical
            
            string path = System.IO.Path.Combine(Application.persistentDataPath, file);
            var bytes = System.IO.File.ReadAllBytes(path);
            var tex = new Texture2D(2, 2);
            tex.LoadImage(bytes);
            
            NUnitAssert.AreEqual(500, tex.width);
            NUnitAssert.AreEqual(1000, tex.height);
            yield return null;
        }

        // 4. Filtering with Multiple Cameras
        [UnityTest]
        public IEnumerator DOM_Filtering_RespectsMainCameraOnly()
        {
            var cam1Go = new GameObject("Cam1", typeof(Camera));
            cam1Go.tag = "MainCamera";
            var cam1 = cam1Go.GetComponent<Camera>();
            cam1.transform.position = new Vector3(0, 0, -10);

            var cam2Go = new GameObject("Cam2", typeof(Camera));
            var cam2 = cam2Go.GetComponent<Camera>();
            cam2.transform.position = new Vector3(100, 100, 100);

            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = "OnlyVisibleToCam2";
            obj.transform.position = new Vector3(100, 100, 110);
            obj.transform.SetParent(_testRoot.transform);

            yield return null;

            string dom = RealPlaySemanticDOMDumper.Dump(filterToViewport: true);
            NUnitAssert.IsFalse(dom.Contains("OnlyVisibleToCam2"), "Pruning failed to ignore object invisible to MainCamera");
            
            UnityEngine.Object.DestroyImmediate(cam1Go);
            UnityEngine.Object.DestroyImmediate(cam2Go);
        }

        // 5. Orthographic Depth Ranking
        [UnityTest]
        public IEnumerator SmartFind_OrthoCamera_CorrectPriority()
        {
            var camGo = new GameObject("OrthoCam", typeof(Camera));
            camGo.tag = "MainCamera";
            var cam = camGo.GetComponent<Camera>();
            cam.orthographic = true;
            cam.transform.position = new Vector3(0, 0, -10);

            var far = new GameObject("OrthoObj");
            far.transform.position = new Vector3(0, 0, 5);
            far.transform.SetParent(_testRoot.transform);

            var close = new GameObject("OrthoObj");
            close.transform.position = new Vector3(0, 0, -5); // Closer to cam at -10
            close.transform.SetParent(_testRoot.transform);

            yield return null;
            RealPlayTester.Utilities.SceneCache.Instance.ForceRefresh();

            var found = SmartFind.Object("OrthoObj");
            NUnitAssert.AreEqual(close.GetInstanceID(), found.GetInstanceID(), "Failed to prioritize closer object in orthographic space");
            
            UnityEngine.Object.DestroyImmediate(camGo);
        }

        private static IEnumerator ToCoroutine(System.Func<Task> action)
        {
            var task = action();
            while (!task.IsCompleted) yield return null;
            if (task.IsFaulted) throw task.Exception.InnerException;
        }
    }
}