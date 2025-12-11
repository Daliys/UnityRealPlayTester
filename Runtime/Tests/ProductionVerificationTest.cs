using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using RealPlayTester.Core;
using RealPlayTester.Input;
using RealPlayTester.Await;
using RealPlayTester.Assert;

namespace RealPlayTester.Tests
{
    /// <summary>
    /// A self-contained verification test that builds a temporary UI scene
    /// and exercises critical RealPlayTester features:
    /// - TMP Support (Reflection)
    /// - Scroll.EnsureVisible (Horizontal & Vertical)
    /// - Capture.Screenshot (Synchronous)
    /// - Universal Input (Click/Wait)
    /// </summary>
    [CreateAssetMenu(menuName = "RealPlay Tests/Production Verification")]
    public class ProductionVerificationTest : RealPlayTest
    {
        protected override async Task Run()
        {
            RealPlayLog.Info("=== Starting Production Verification Test ===");

            // 1. Setup Test Scene
            var root = new GameObject("Verification_Root");
            var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGO.transform.SetParent(root.transform);
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            // Create Scroll View
            var scrollObj = new GameObject("ScrollRect", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollObj.transform.SetParent(canvasGO.transform, false);
            var scrollRect = scrollObj.GetComponent<ScrollRect>();
            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image));
            viewport.transform.SetParent(scrollObj.transform, false);
            scrollRect.viewport = viewport.GetComponent<RectTransform>();
            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            scrollRect.content = content.GetComponent<RectTransform>();
            scrollRect.horizontal = true;
            scrollRect.vertical = true;

            // Layout
            ((RectTransform)scrollObj.transform).sizeDelta = new Vector2(200, 200);
            ((RectTransform)scrollObj.transform).anchoredPosition = Vector2.zero; // Center
            viewport.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            viewport.GetComponent<RectTransform>().anchorMax = Vector2.one;
            content.GetComponent<RectTransform>().sizeDelta = new Vector2(1000, 1000);

            // Create Target Button "Out of View"
            var targetBtnObj = new GameObject("TargetButton", typeof(RectTransform), typeof(Image), typeof(Button));
            targetBtnObj.transform.SetParent(content.transform, false);
            ((RectTransform)targetBtnObj.transform).anchoredPosition = new Vector2(400, -400); // Need to scroll
            ((RectTransform)targetBtnObj.transform).sizeDelta = new Vector2(100, 50);
            targetBtnObj.GetComponent<Image>().color = Color.green;

            // Add Text (Simulate Standard Text first, checking TMP logic validity indirectly by absence of error)
            var textObj = new GameObject("Text", typeof(RectTransform), typeof(UnityEngine.UI.Text));
            textObj.transform.SetParent(targetBtnObj.transform, false);
            var txt = textObj.GetComponent<UnityEngine.UI.Text>();
            txt.text = "Click Me";
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.black;

            await Task.Yield(); // Wait for layout

            try
            {
                // 2. Verify Screenshot (Sync)
                RealPlayLog.Info("Verifying Screenshot...");
                string shotPath = Capture.Screenshot("Verification_Start");
                Assert.IsFalse(string.IsNullOrEmpty(shotPath), "Screenshot path should not be empty");
                Assert.IsTrue(System.IO.File.Exists(shotPath), "Screenshot file must exist immediately (synchronous check)");

                // 3. Verify Wait.ForInteractable (exercises TMP reflection logic safely even if TMP not present)
                RealPlayLog.Info("Verifying Wait.ForInteractable...");
                await Wait.ForInteractable<Button>("Click Me", 2f);

                // 4. Verify Scroll.EnsureVisible & Click
                RealPlayLog.Info("Verifying Scroll & Click...");
                
                // This click should trigger Scroll.EnsureVisible internally
                bool clicked = false;
                targetBtnObj.GetComponent<Button>().onClick.AddListener(() => clicked = true);
                
                await Click.ButtonWithText("Click Me");
                
                Assert.IsTrue(clicked, "Button should have been clicked after auto-scroll");

                RealPlayLog.Info("=== Verification PASSED ===");
            }
            finally
            {
                // Cleanup
                Destroy(root);
            }
        }
    }
}
