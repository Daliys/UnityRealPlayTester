using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools;
using RealPlayTester.Core;
using RealPlayTester.Core.Perception;
using NUnitAssert = NUnit.Framework.Assert;

namespace RealPlayTester.Tests.Verification
{
    public class DOMFilteringTests
    {
        private GameObject _testRoot;
        private Camera _cam;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            _testRoot = new GameObject("DOMFilteringTestRoot");
            
            if (Camera.main == null)
            {
                var camGo = new GameObject("MainCamera", typeof(Camera));
                camGo.tag = "MainCamera";
                _cam = camGo.GetComponent<Camera>();
                _cam.transform.position = new Vector3(0, 0, -10);
                _cam.transform.LookAt(Vector3.zero);
            }
            else
            {
                _cam = Camera.main;
            }
            
            Screen.SetResolution(1024, 768, false);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            Object.DestroyImmediate(_testRoot);
            yield return null;
        }

        [UnityTest]
        public IEnumerator DOM_Filtering_ExcludesOffScreen3DObject()
        {
            var onScreen = GameObject.CreatePrimitive(PrimitiveType.Cube);
            onScreen.name = "OnScreenCube";
            onScreen.transform.position = Vector3.zero;
            onScreen.transform.SetParent(_testRoot.transform);

            var offScreen = GameObject.CreatePrimitive(PrimitiveType.Cube);
            offScreen.name = "OffScreenCube";
            offScreen.transform.position = new Vector3(1000, 1000, 0); // Far away
            offScreen.transform.SetParent(_testRoot.transform);

            yield return null;

            // 1. No Filtering
            string domFull = RealPlaySemanticDOMDumper.Dump(filterToViewport: false);
            NUnitAssert.IsTrue(domFull.Contains("OnScreenCube"), "Full DOM missing on-screen cube");
            NUnitAssert.IsTrue(domFull.Contains("OffScreenCube"), "Full DOM missing off-screen cube");

            // 2. With Filtering
            string domFiltered = RealPlaySemanticDOMDumper.Dump(filterToViewport: true);
            NUnitAssert.IsTrue(domFiltered.Contains("OnScreenCube"), "Filtered DOM missing on-screen cube");
            NUnitAssert.IsFalse(domFiltered.Contains("OffScreenCube"), "Filtered DOM should NOT contain off-screen cube");
        }

        [UnityTest]
        public IEnumerator DOM_Filtering_ExcludesOffScreenUI()
        {
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(GraphicRaycaster), typeof(CanvasScaler));
            canvasGo.transform.SetParent(_testRoot.transform);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var onScreenUI = new GameObject("OnScreenUI", typeof(RectTransform), typeof(Image));
            onScreenUI.transform.SetParent(canvasGo.transform);
            var rtOn = onScreenUI.GetComponent<RectTransform>();
            rtOn.sizeDelta = new Vector2(100, 100);
            // Center of 640x480 is 320, 240
            rtOn.position = new Vector3(320, 240, 0);

            var offScreenUI = new GameObject("OffScreenUI", typeof(RectTransform), typeof(Image));
            offScreenUI.transform.SetParent(canvasGo.transform);
            var rtOff = offScreenUI.GetComponent<RectTransform>();
            rtOff.sizeDelta = new Vector2(100, 100);
            rtOff.position = new Vector3(5000, 5000, 0);

            yield return null; // Wait for layout
            yield return new WaitForSeconds(0.1f);

            string domFiltered = RealPlaySemanticDOMDumper.Dump(filterToViewport: true);
            
            Debug.Log($"[DOMTest] Filtered DOM content: {domFiltered}");
            
            NUnitAssert.IsTrue(domFiltered.Contains("OnScreenUI"), "Filtered DOM missing on-screen UI");
            NUnitAssert.IsFalse(domFiltered.Contains("OffScreenUI"), "Filtered DOM should NOT contain off-screen UI");
        }
    }
}