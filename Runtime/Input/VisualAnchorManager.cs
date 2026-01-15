using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using RealPlayTester.Core;
using RealPlayTester.Utilities;

namespace RealPlayTester.Input
{
    /// <summary>
    /// Renders floating ID tags (anchors) over interactable elements.
    /// Helped vision-based AI recognize targets in screenshots.
    /// </summary>
    internal class VisualAnchorManager : MonoBehaviour
    {
        private Canvas _canvas;
        private List<AnchorTag> _activeTags = new List<AnchorTag>();
        private List<AnchorTag> _pool = new List<AnchorTag>();
        private float _lastScanTime;
        private const float ScanInterval = 1.0f;

        private class AnchorTag
        {
            public GameObject Root;
            public RectTransform Rect;
            public UnityEngine.UI.Text Label;
            public Image Background;
            public GameObject Target;
        }

        public void Initialize()
        {
            var canvasGo = new GameObject("AnchorCanvas", typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(transform);
            _canvas = canvasGo.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 32766; // Just below the pointer
        }

        private void Update()
        {
            if (!Tester.Settings.ShowVisualAnchors)
            {
                ClearTags();
                return;
            }

            if (Time.unscaledTime - _lastScanTime > ScanInterval)
            {
                RefreshAnchors();
                _lastScanTime = Time.unscaledTime;
            }

            UpdateTagPositions();
        }

        private void RefreshAnchors()
        {
            // PERFORMANCE FIX: Use SceneCache instead of FindObjectsByType
            var all = SceneCache.Instance.AllActiveObjects;
            var candidates = new List<GameObject>();
            
            foreach (var go in all)
            {
                if (go == null) continue;
                var role = SemanticProbe.GetRole(go);
                if (role != SemanticRole.None && role != SemanticRole.WorldObject && role != SemanticRole.View)
                {
                    candidates.Add(go);
                }
            }

            // Sync tags
            ClearTags();
            for (int i = 0; i < candidates.Count; i++)
            {
                var tag = GetTag();
                tag.Target = candidates[i];
                tag.Label.text = $"#{i}";
                tag.Label.fontSize = 12;
                tag.Root.SetActive(true);
                _activeTags.Add(tag);
            }
        }

        private void UpdateTagPositions()
        {
            foreach (var tag in _activeTags)
            {
                if (tag.Target == null || !tag.Target.activeInHierarchy)
                {
                    tag.Root.SetActive(false);
                    continue;
                }

                Vector2 center = RealInputUtility.GetScreenCenter(tag.Target);
                tag.Rect.position = center;
            }
        }

        private void ClearTags()
        {
            foreach (var tag in _activeTags)
            {
                tag.Root.SetActive(false);
                _pool.Add(tag);
            }
            _activeTags.Clear();
        }

        private AnchorTag GetTag()
        {
            if (_pool.Count > 0)
            {
                var tag = _pool[_pool.Count - 1];
                _pool.RemoveAt(_pool.Count - 1);
                return tag;
            }

            var go = new GameObject("Anchor", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_canvas.transform);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(24, 16);

            var bg = go.GetComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.7f);

            var textGo = new GameObject("Text", typeof(UnityEngine.UI.Text));
            textGo.transform.SetParent(go.transform);
            var txt = textGo.GetComponent<UnityEngine.UI.Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.cyan;
            txt.raycastTarget = false;

            var txtRt = textGo.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.sizeDelta = Vector2.zero;

            return new AnchorTag { Root = go, Rect = rt, Label = txt, Background = bg };
        }
    }
}
