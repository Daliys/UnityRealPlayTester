using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using RealPlayTester.Core;

namespace RealPlayTester.Input
{
    /// <summary>
    /// Tracks all simulated interaction points and renders a heatmap.
    /// Useful for visualizing where AI agents or automated tests are focusing.
    /// </summary>
    public class InteractionHeatmap : MonoBehaviour
    {
        private Canvas _canvas;
        private List<GameObject> _points = new List<GameObject>();
        private List<GameObject> _pool = new List<GameObject>();
        
        private static readonly Color HeatColor = new Color(1f, 0.3f, 0f, 0.3f); // Orange-ish blur
        private const int MaxPoints = 200;

        public void Initialize()
        {
            var old = transform.Find("HeatmapCanvas");
            if (old != null) DestroyImmediate(old.gameObject);

            // M012: Ensure lists are cleared during re-initialization
            _points.Clear();
            _pool.Clear();

            var canvasGo = new GameObject("HeatmapCanvas", typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(transform);
            _canvas = canvasGo.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 32765; // Below anchors
        }

        private bool _wasPressed = false;

        private void Update()
        {
            if (!Tester.Settings.ShowInteractionHeatmap)
            {
                if (_points.Count > 0) ClearAll();
                return;
            }

            // Capture new points from RealInputUtility if button was JUST pressed
            bool isPressed = RealInputUtility.IsSimulatedButtonPressed;
            if (isPressed && !_wasPressed && !RealInputUtility.IsSimulatedDragging)
            {
                AddPoint(RealInputUtility.LastSimulatedPosition);
            }
            _wasPressed = isPressed;
        }

        public void AddPoint(Vector2 screenPos)
        {
            if (!Tester.Settings.ShowInteractionHeatmap) return;

            var dot = GetPoint();
            dot.transform.position = screenPos;
            dot.SetActive(true);
            _points.Add(dot);

            // Maintain cap
            if (_points.Count > MaxPoints)
            {
                var old = _points[0];
                _points.RemoveAt(0);
                old.SetActive(false);
                _pool.Add(old);
            }
        }

        private void ClearAll()
        {
            foreach (var p in _points)
            {
                p.SetActive(false);
                _pool.Add(p);
            }
            _points.Clear();
        }

        private GameObject GetPoint()
        {
            if (_pool.Count > 0)
            {
                var p = _pool[_pool.Count - 1];
                _pool.RemoveAt(_pool.Count - 1);
                return p;
            }

            var go = new GameObject("HeatPoint", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_canvas.transform);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(30, 30);

            var img = go.GetComponent<Image>();
            img.color = HeatColor;
            
            // Note: In an ideal world we'd use a soft radial sprite, 
            // but we'll stick to basic colors to avoid asset dependencies.
            
            return go;
        }
    }
}
