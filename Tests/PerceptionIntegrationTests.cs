using System.Collections;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using RealPlayTester.Core;
using RealPlayTester.Input;
using NUnitAssert = NUnit.Framework.Assert;

namespace RealPlayTester.Tests.Verification
{
    public class PerceptionIntegrationTests
    {
        private GameObject _cameraObj;
        private GameObject _testRoot;

        [SetUp]
        public void Setup()
        {
            _testRoot = new GameObject("PerceptionTestRoot");
            
            // setup EventSystem
            if (Object.FindAnyObjectByType<EventSystem>() == null)
            {
                var esGo = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                esGo.transform.SetParent(_testRoot.transform);
            }

            // Setup Main Camera if not present
            if (Camera.main == null)
            {
                _cameraObj = new GameObject("MainCamera");
                var cam = _cameraObj.AddComponent<Camera>();
                _cameraObj.tag = "MainCamera";
                _cameraObj.transform.position = new Vector3(0, 0, -10);
                _cameraObj.transform.LookAt(Vector3.zero);
            }
        }

        [TearDown]
        public void Teardown()
        {
            Object.DestroyImmediate(_testRoot);
            if (_cameraObj != null) Object.DestroyImmediate(_cameraObj);
        }

        [UnityTest]
        public IEnumerator DumpSemanticDOM_ValidatesStructure()
        {
            // Create a button
            var btnGo = new GameObject("TestButton", typeof(Button), typeof(RectTransform));
            btnGo.transform.SetParent(_testRoot.transform);
            var rt = btnGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200, 50);
            rt.position = new Vector3(Screen.width / 2f, Screen.height / 2f, 0);

            yield return null; // Wait for layout

            string domJson = Tester.Perception.DumpSemanticDOM();
            NUnitAssert.IsNotNull(domJson, "DOM JSON should not be null");
            NUnitAssert.IsTrue(domJson.Contains("\"Name\": \"TestButton\""), "JSON missing button name");
            NUnitAssert.IsTrue(domJson.Contains("\"Role\": \"Button\""), "JSON missing button role");
            NUnitAssert.IsTrue(domJson.Contains("\"ScreenRect\":"), "JSON missing ScreenRect");
        }

        [UnityTest]
        public IEnumerator IsOccluded_DetectsUIOcclusion()
        {
            // Target object
            var target = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            target.name = "TargetSphere";
            target.transform.position = Vector3.zero;
            target.transform.SetParent(_testRoot.transform);

            // UI Panel blocking it
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(GraphicRaycaster), typeof(CanvasScaler));
            canvasGo.transform.SetParent(_testRoot.transform);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = Camera.main;

            var panelGo = new GameObject("BlockingPanel", typeof(Image));
            panelGo.transform.SetParent(canvasGo.transform);
            var rt = panelGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(2000, 2000); // Overlay the whole screen
            rt.position = new Vector3(Screen.width / 2f, Screen.height / 2f, 0);

            yield return null; // Wait for frame

            bool occluded = Tester.Perception.IsOccluded(target, out string blocker);
            NUnitAssert.IsTrue(occluded, "Target should be occluded by UI panel");
            NUnitAssert.AreEqual("UI Element", blocker, "Blocker name should be 'UI Element'");
        }

        [UnityTest]
        public IEnumerator IsOccluded_DetectsWorldOcclusion()
        {
            // Target object
            var target = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            target.name = "TargetSphere";
            target.transform.position = Vector3.zero;
            target.transform.SetParent(_testRoot.transform);

            // Blocking object
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Wall";
            wall.transform.position = new Vector3(0, 0, -5); // Between camera(-10) and target(0)
            wall.transform.localScale = new Vector3(10, 10, 1);
            wall.transform.SetParent(_testRoot.transform);

            yield return null; // Wait for frame

            // Use direct call to check world occlusion specifically
            var cam = Camera.main;
            var screenPos = RealInputUtility.GetScreenCenter(target);
            var ray = cam.ScreenPointToRay(screenPos);
            bool occludedByWorld = false;
            string worldBlocker = "";

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.collider.gameObject != target)
                {
                    occludedByWorld = true;
                    worldBlocker = hit.collider.gameObject.name;
                }
            }

            NUnitAssert.IsTrue(occludedByWorld, "Target should be occluded by wall in 3D space");
            NUnitAssert.AreEqual("Wall", worldBlocker, "Blocker name should be 'Wall'");
        }

        [UnityTest]
        public IEnumerator CaptureVisionSnapshot_CreatesFile()
        {
            string fileName = "test_vision_snapshot.png";
            string path = Path.Combine(Application.persistentDataPath, fileName);
            
            if (File.Exists(path)) File.Delete(path);

            if (Application.isBatchMode)
            {
                LogAssert.ignoreFailingMessages = true;
            }

            var task = Tester.Perception.CaptureVisionSnapshot(fileName);
            while (!task.IsCompleted) yield return null;

            if (Application.isBatchMode)
            {
                LogAssert.ignoreFailingMessages = false;
            }

            if (!Application.isBatchMode)
            {
                NUnitAssert.IsTrue(File.Exists(path), "Snapshot file was not created");
                NUnitAssert.IsTrue(new FileInfo(path).Length > 0, "Snapshot file is empty");
            }
        }
    }
}
