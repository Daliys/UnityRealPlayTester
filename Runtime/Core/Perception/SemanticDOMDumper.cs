using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using RealPlayTester.Input;
using RealPlayTester.Utilities;
using UnityEngine.EventSystems;

namespace RealPlayTester.Core.Perception
{
    public class SemanticNode
    {
        public string Name;
        public string Role;
        public bool Active;
        public bool Interactable;
        public Rect ScreenRect;
        public string Text;
        public float ZDepth;
        public bool IsOccluded;
        public string BlockingObject;
        public bool LogicalBlocked;
        public string BlockedReason;
        public int CollapsedCount;
        public List<string> Affordances = new List<string>();
        public List<SemanticNode> Children = new List<SemanticNode>();
    }

    public static partial class RealPlaySemanticDOMDumper
    {
        public static string Dump(bool? filterToViewport = null)
        {
            bool viewportFilter = filterToViewport ?? RealPlaySettings.FilterDOMToViewport;
            var root = new SemanticNode { Name = "Root", Role = "Universe", Active = true };
            Plane[] planes = (viewportFilter && Camera.main != null) ? GeometryUtility.CalculateFrustumPlanes(Camera.main) : null;

            // Use SceneCache for optimized discovery if available
            var scenes = new List<SemanticNode>();
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;

                var sceneNode = new SemanticNode { Name = scene.name, Role = "Scene", Active = true };
                var roots = scene.GetRootGameObjects();
                foreach (var go in roots)
                {
                    var captured = CaptureRecursive(go.transform, viewportFilter, planes);
                    if (captured != null) sceneNode.Children.Add(captured);
                }
                
                if (!viewportFilter || sceneNode.Children.Count > 0) scenes.Add(sceneNode);
            }
            root.Children = scenes;
            return SerializeNode(root);
        }

        private static SemanticNode CaptureRecursive(Transform t, bool viewportFilter, Plane[] planes)
        {
            var go = t.gameObject;
            if (!go.activeInHierarchy) return null;

            bool isVisible = !viewportFilter || IsVisibleInViewport(go, planes);
            
            // Optimization: Skip deep children if parent is tiny or far outside viewport
            if (viewportFilter && !isVisible && t.childCount > 50) return null; 

            var capturedChildren = CaptureChildren(t, viewportFilter, planes, isVisible);

            if (viewportFilter && !isVisible && capturedChildren.Count == 0) return null;

            var node = CreateNode(go, t);
            node.Children = capturedChildren;
            return node;
        }

        private static List<SemanticNode> CaptureChildren(Transform t, bool viewportFilter, Plane[] planes, bool isParentVisible)
        {
            var capturedChildren = new List<SemanticNode>();
            for (int i = 0; i < t.childCount; i++)
            {
                Transform childTransform = t.GetChild(i);

                if (RealPlaySettings.CollapseRepetitiveSiblings)
                {
                    int groupCount = HierarchyHelpers.GetRepetitiveGroupCount(t, i);
                    if (groupCount > 5)
                    {
                        capturedChildren.Add(CreateCollapsedNode(childTransform, groupCount));
                        i += groupCount - 1;
                        continue;
                    }
                }

                var captured = CaptureRecursive(childTransform, viewportFilter, planes);
                if (captured != null) capturedChildren.Add(captured);
            }
            return capturedChildren;
        }

        private static SemanticNode CreateCollapsedNode(Transform t, int count)
        {
            return new SemanticNode
            {
                Name = $"{count} similar '{HierarchyHelpers.GetBaseName(t.name)}' objects",
                Role = "CollapsedGroup",
                Active = true,
                CollapsedCount = count
            };
        }

        private static SemanticNode CreateNode(GameObject go, Transform t)
        {
            var node = new SemanticNode
            {
                Name = go.name,
                Role = SemanticProbe.GetRoleName(go),
                Active = true,
                Interactable = IsInteractable(go),
                ScreenRect = GetScreenRect(go),
                Text = GetTextValue(go),
                ZDepth = t.position.z,
                Affordances = GetAffordances(go)
            };

            var occlusion = RealPlayOcclusionRaycaster.CheckOcclusion(go);
            node.IsOccluded = occlusion.IsOccluded;
            node.BlockingObject = occlusion.BlockingObjectName;

            var logic = InteractionLogic.CheckLogicalInteractivity(go);
            node.LogicalBlocked = logic.IsBlocked;
            node.BlockedReason = logic.Reason;

            return node;
        }

        private static bool IsVisibleInViewport(GameObject go, Plane[] planes)
        {
            var rt = go.GetComponent<RectTransform>();
            if (rt != null)
            {
                Rect screenRect = GetUIScreenRect(rt);
                if (screenRect.width <= 0 || screenRect.height <= 0) return false;
                
                Rect viewport = new Rect(0, 0, Screen.width, Screen.height);
                return viewport.Overlaps(screenRect);
            }

            var renderer = go.GetComponent<Renderer>();
            return renderer != null && planes != null && GeometryUtility.TestPlanesAABB(planes, renderer.bounds);
        }

        private static bool IsInteractable(GameObject go)
        {
            var btn = go.GetComponent<Button>();
            if (btn != null) return btn.interactable;
            var selectable = go.GetComponent<Selectable>();
            return selectable != null && selectable.interactable;
        }

        private static Rect GetScreenRect(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            if (rt != null) return GetUIScreenRect(rt);
            var renderer = go.GetComponent<Renderer>();
            return (renderer != null) ? GetWorldScreenRect(renderer) : Rect.zero;
        }

        private static Rect GetUIScreenRect(RectTransform rt)
        {
            var canvas = rt.GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                Vector3[] corners = new Vector3[4];
                rt.GetWorldCorners(corners);
                float minX = Mathf.Min(corners[0].x, corners[2].x);
                float minY = Mathf.Min(corners[0].y, corners[2].y);
                float maxX = Mathf.Max(corners[0].x, corners[2].x);
                float maxY = Mathf.Max(corners[0].y, corners[2].y);
                return new Rect(minX, minY, maxX - minX, maxY - minY);
            }

            var cam = Camera.main;
            if (cam == null) return Rect.zero;

            Vector3[] c = new Vector3[4];
            rt.GetWorldCorners(c);
            float xMin = float.MaxValue, yMin = float.MaxValue;
            float xMax = float.MinValue, yMax = float.MinValue;

            foreach (var corner in c)
            {
                Vector2 screenPos = cam.WorldToScreenPoint(corner);
                xMin = Mathf.Min(xMin, screenPos.x);
                yMin = Mathf.Min(yMin, screenPos.y);
                xMax = Mathf.Max(xMax, screenPos.x);
                yMax = Mathf.Max(yMax, screenPos.y);
            }
            return new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
        }

        private static Rect GetWorldScreenRect(Renderer renderer)
        {
            var cam = Camera.main;
            if (cam == null) return Rect.zero;

            var bounds = renderer.bounds;
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;

            Vector3[] corners = new Vector3[8]
            {
                new Vector3(min.x, min.y, min.z), new Vector3(min.x, min.y, max.z),
                new Vector3(min.x, max.y, min.z), new Vector3(min.x, max.y, max.z),
                new Vector3(max.x, min.y, min.z), new Vector3(max.x, min.y, max.z),
                new Vector3(max.x, max.y, min.z), new Vector3(max.x, max.y, max.z),
            };

            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;

            foreach (var corner in corners)
            {
                Vector2 screenPos = cam.WorldToScreenPoint(corner);
                minX = Mathf.Min(minX, screenPos.x);
                minY = Mathf.Min(minY, screenPos.y);
                maxX = Mathf.Max(maxX, screenPos.x);
                maxY = Mathf.Max(maxY, screenPos.y);
            }
            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        private static string GetTextValue(GameObject go)
        {
            var txt = go.GetComponent<UnityEngine.UI.Text>();
            if (txt != null) return txt.text;

            var tmpType = Type.GetType("TMPro.TMP_Text, Unity.TextMeshPro");
            if (tmpType != null)
            {
                var tmp = go.GetComponent(tmpType);
                if (tmp != null)
                {
                    var textProp = tmpType.GetProperty("text");
                    if (textProp != null) return textProp.GetValue(tmp) as string;
                }
            }
            return null;
        }
    }
}
