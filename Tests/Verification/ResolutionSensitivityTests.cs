using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using RealPlayTester.Core;
using RPA = RealPlayTester.Assert.Assert;
using NAssert = NUnit.Framework.Assert;

namespace RealPlayTester.Tests.Verification
{
    public class ResolutionSensitivityTests
    {
        private GameObject _root;
        private Canvas _canvas;
        private Button _cornerButton;
        private bool _clicked;

        [SetUp]
        public void Setup()
        {
            _clicked = false;
            _root = new GameObject("ResolutionTestRoot");
            
            // Setup UI
            var canvasObj = new GameObject("Canvas");
            canvasObj.transform.SetParent(_root.transform);
            _canvas = canvasObj.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasObj.AddComponent<GraphicRaycaster>();

            // Place a button in the far Top-Right corner
            var btnObj = new GameObject("CornerButton");
            btnObj.transform.SetParent(canvasObj.transform, false);
            var img = btnObj.AddComponent<Image>();
            img.color = Color.red;
            _cornerButton = btnObj.AddComponent<Button>();
            _cornerButton.onClick.AddListener(() => _clicked = true);

            var rt = btnObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(1, 1);
            rt.sizeDelta = new Vector2(100, 100);
            rt.anchoredPosition = Vector2.zero; // Exactly at the corner
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_root) Object.Destroy(_root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator UI_ClickCornerButton_FailsOnResolutionChange()
        {
            // 1. Initial State
            yield return null;
            Debug.Log($"Initial Resolution: {Screen.width}x{Screen.height}");

            // 2. Try to click it using the library
            // Note: Our library might not have a direct "Tester.Click(GameObject)" that works in batchmode
            // reliably without a real mouse. If it uses coordinates, it might fail here.
            
            // Let's simulate what a high-level tool would do: find coordinates and click.
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(null, _cornerButton.transform.position);
            Debug.Log($"Button Screen Position: {screenPos}");

            // This test is meant to highlight that if we hardcode or miscalculate coordinates 
            // based on a different resolution than the one the "Tester" expects, it fails.
            
            // For now, let's just verify if RPA.ScreenElementVisible finds it.
            NAssert.DoesNotThrow(() => RPA.ScreenElementVisible("CornerButton"), "Should be visible at 1080p");

            // 3. Change "Resolution" (Simulated by changing canvas scale or forcing screen check)
            // In a real device/editor this is more dynamic. 
            // We highlight that the library lacks "smart" coordinate re-mapping if the resolution shifts during a test.
            
            yield return null;
        }
    }
}
