using UnityEngine;
using UnityEngine.UI;

namespace RealPlayTester.Input
{
    /// <summary>
    /// Internal component that renders the virtual cursor for simulated inputs.
    /// </summary>
    internal class VisualPointer : MonoBehaviour
    {
        private RectTransform _rectTransform;
        private Image _image;
        private Outline _outline;

        private static readonly Color IdleColor = new Color(1f, 0f, 0f, 0.6f);
        private static readonly Color PressColor = new Color(1f, 1f, 0f, 0.9f); // Yellow when pressed
        private static readonly Vector2 NormalSize = new Vector2(20, 20);
        private static readonly Vector2 PressSize = new Vector2(25, 25);

        public void Initialize()
        {
            gameObject.name = "RealPlay_VisualPointer";
            
            // Ensure we start at center to avoid teleport from (0,0)
            RealInputUtility.SimulateMouseMove(new Vector2(Screen.width / 2f, Screen.height / 2f));

            var canvasGo = new GameObject("PointerCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30000; // Always on top

            var cursorGo = new GameObject("Cursor", typeof(Image));
            cursorGo.transform.SetParent(canvasGo.transform);
            _rectTransform = cursorGo.GetComponent<RectTransform>();
            _rectTransform.sizeDelta = NormalSize;
            
            _image = cursorGo.GetComponent<Image>();
            _image.color = IdleColor;
            _image.raycastTarget = false;

            _outline = cursorGo.AddComponent<Outline>();
            _outline.effectColor = Color.white;
            _outline.effectDistance = new Vector2(2, 2);

            // Start invisible
            _image.enabled = false;
            _outline.enabled = false;

            FixURPCamera();
        }

        private void FixURPCamera()
        {
            var cam = Camera.main;
            if (cam == null) return;

            // Check if we are in URP
            var urpType = System.Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
            if (urpType != null)
            {
                if (cam.GetComponent(urpType) == null)
                {
                    cam.gameObject.AddComponent(urpType);
                }
            }
        }

        private void Update()
        {
            // Only visible if simulation is active or we recently had one
            bool shouldBeVisible = RealPlayTester.Core.Tester.Settings.ShowVisualPointer;
            
            _image.enabled = shouldBeVisible;
            _outline.enabled = shouldBeVisible;

            if (shouldBeVisible)
            {
                _rectTransform.position = RealInputUtility.LastSimulatedPosition;
                
                bool isPressed = RealInputUtility.IsSimulatedButtonPressed || RealInputUtility.IsSimulatedDragging;
                _image.color = isPressed ? PressColor : IdleColor;
                _rectTransform.sizeDelta = Vector2.Lerp(_rectTransform.sizeDelta, isPressed ? PressSize : NormalSize, Time.unscaledDeltaTime * 15f);
            }
        }
    }
}
