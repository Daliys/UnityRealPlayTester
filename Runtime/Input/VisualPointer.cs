using UnityEngine;
using UnityEngine.UI;

namespace RealPlayTester.Input
{
    /// <summary>
    /// Internal component that renders the virtual cursor for simulated inputs.
    /// </summary>
    internal class VisualPointer : MonoBehaviour
    {
        private class CursorInstance
        {
            public RectTransform Rect;
            public Image Img;
            public Outline Outline;
        }

        private readonly System.Collections.Generic.Dictionary<int, CursorInstance> _cursors = new System.Collections.Generic.Dictionary<int, CursorInstance>();
        
        private static readonly Color IdleColor = new Color(1f, 0f, 0f, 0.6f);
        private static readonly Color PressColor = new Color(1f, 1f, 0f, 0.9f); // Yellow when pressed
        private static readonly Vector2 NormalSize = new Vector2(20, 20);
        private static readonly Vector2 PressSize = new Vector2(25, 25);

        private Canvas _canvas;

        public void Initialize()
        {
            gameObject.name = "RealPlay_VisualPointer";
            
            // Ensure we start at center to avoid teleport from (0,0)
            RealInputUtility.SimulateMouseMove(new Vector2(Screen.width / 2f, Screen.height / 2f));

            var canvasGo = new GameObject("PointerCanvas", typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(transform);
            _canvas = canvasGo.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 32767; // Max standard sorting order
            _canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.None;

            FixURPCamera();
        }

        private CursorInstance CreateCursor(int id)
        {
            var cursorGo = new GameObject($"Cursor_{id}", typeof(Image));
            cursorGo.transform.SetParent(_canvas.transform);
            var rect = cursorGo.GetComponent<RectTransform>();
            rect.sizeDelta = NormalSize;
            
            var img = cursorGo.GetComponent<Image>();
            img.color = IdleColor;
            img.raycastTarget = false;

            var outline = cursorGo.AddComponent<Outline>();
            outline.effectColor = Color.white;
            outline.effectDistance = new Vector2(2, 2);

            return new CursorInstance { Rect = rect, Img = img, Outline = outline };
        }

        private void FixURPCamera()
        {
            // ...
        }

        private float _lastSortTime;

        private void Update()
        {
            if (!RealPlayTester.Core.Tester.Settings.ShowVisualPointer)
            {
                HideAllCursors();
                return;
            }

            SyncCursorsWithActivePointers();
            EnforceTopMostSorting();
        }

        private void HideAllCursors()
        {
            foreach (var c in _cursors.Values) { c.Img.enabled = false; c.Outline.enabled = false; }
        }

        private void SyncCursorsWithActivePointers()
        {
            var active = RealInputUtility.GetActivePointers();
            
            // Cleanup stale
            var toRemove = new System.Collections.Generic.List<int>();
            foreach (var id in _cursors.Keys) if (!active.ContainsKey(id)) toRemove.Add(id);
            foreach (var id in toRemove) { Destroy(_cursors[id].Rect.gameObject); _cursors.Remove(id); }

            // Update active
            foreach (var kvp in active)
            {
                if (!_cursors.TryGetValue(kvp.Key, out var c)) { c = CreateCursor(kvp.Key); _cursors[kvp.Key] = c; }
                UpdateCursorState(c, kvp.Key, kvp.Value);
            }
        }

        private void UpdateCursorState(CursorInstance c, int id, Vector2 pos)
        {
            c.Img.enabled = true;
            c.Outline.enabled = true;
            c.Rect.position = pos;

            bool isPressed = RealInputUtility.IsSimulatedButtonPressed || RealInputUtility.IsSimulatedDragging;
            if (id != -1) isPressed = true; 

            c.Img.color = isPressed ? PressColor : IdleColor;
            c.Rect.sizeDelta = Vector2.Lerp(c.Rect.sizeDelta, isPressed ? PressSize : NormalSize, Time.unscaledDeltaTime * 15f);
        }

        private void EnforceTopMostSorting()
        {
            if (Time.unscaledTime - _lastSortTime > 1.0f)
            {
                if (_canvas != null)
                {
                    if (_canvas.sortingOrder != 32767) _canvas.sortingOrder = 32767;
                    if (transform.parent == null) transform.SetAsLastSibling();
                }
                _lastSortTime = Time.unscaledTime;
            }
        }
    }
}
