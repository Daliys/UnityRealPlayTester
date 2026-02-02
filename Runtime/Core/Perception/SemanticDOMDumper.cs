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
    [Serializable]
    public class SemanticNode
    {
        public string Name;
        public string Role;
        public string VisualID; // NEW: Cross-modal stable ID (e.g. #A1)
        public string Command;  // NEW: Copy-pasteable AI interaction hint
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

    [AttributeUsage(AttributeTargets.Class)]
    public class RealPlayRoleAttribute : Attribute
    {
        public string RoleName;
        public RealPlayRoleAttribute(string roleName) => RoleName = roleName;
    }

    public static partial class RealPlaySemanticDOMDumper
    {
        private static int _nextVisualId = 1;
        private static char _nextVisualSeries = 'A';

        private static string GetID(GameObject go)
        {
            if (!IsInteractable(go)) return "";
            string id = $"#{_nextVisualSeries}{_nextVisualId++}";
            if (_nextVisualId > 9) { _nextVisualId = 1; _nextVisualSeries++; }
            return id;
        }

        private static string GetCommand(GameObject go)
        {
            if (!IsInteractable(go)) return "";
            var affordances = GetAffordances(go);
            string action = affordances.Count > 0 ? affordances[0].ToLower() : "click";
            return $"await Tester.Interaction.Perform(\"{action}\", \"{go.name}\")";
        }

        private static void ResetIDSeries()
        {
            _nextVisualId = 1;
            _nextVisualSeries = 'A';
        }

        public static string Dump(bool? filterToViewport = null)
        {
            RealPlayTester.Input.SimulatedInputGuard.EnsureMainThread();
            ResetIDSeries(); // Reset series for each dump
            // O016/O020: Force update once at start of dump to ensure layout accuracy 
            // while preventing redundant updates during recursion.
            RealPlayTester.Input.RealInputUtility.ThrottleCanvasUpdate();

            bool viewportFilter = filterToViewport ?? RealPlaySettings.Perception.FilterToViewport;
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
                    var captured = CaptureRecursive(go.transform, viewportFilter, planes, 0);
                    if (captured != null) sceneNode.Children.Add(captured);
                }
                
                if (!viewportFilter || sceneNode.Children.Count > 0) scenes.Add(sceneNode);
            }
            root.Children = scenes;
            return SerializeNode(root);
        }

        private static SemanticNode CaptureRecursive(Transform t, bool viewportFilter, Plane[] planes, int depth)
        {
            var go = t.gameObject;
            if (!go.activeInHierarchy) return null;

            // M002: Hard depth limit to prevent StackOverflow
            if (depth > 1024) return null;

            bool isVisible = !viewportFilter || IsVisibleInViewport(go, planes);
            
            // Optimization: Skip deep children if parent is tiny or far outside viewport
            if (viewportFilter && !isVisible && t.childCount > 50) return null; 

            var node = CreateNode(go, t);
            node.Children = CaptureChildren(t, viewportFilter, planes, isVisible, depth + 1);

            if (viewportFilter && !isVisible && node.Children.Count == 0) return null;
            return node;
        }

        private static List<SemanticNode> CaptureChildren(Transform t, bool viewportFilter, Plane[] planes, bool isParentVisible, int depth)
        {
            var capturedChildren = new List<SemanticNode>();
            for (int i = 0; i < t.childCount; i++)
            {
                Transform childTransform = t.GetChild(i);

                if (RealPlaySettings.Perception.CollapseRepetitiveSiblings)
                {
                    int groupCount = HierarchyHelpers.GetRepetitiveGroupCount(t, i);
                    // M018: Lower threshold for collapse and allow more types
                    if (groupCount > 5)
                    {
                        capturedChildren.Add(CreateCollapsedNode(childTransform, groupCount));
                        i += groupCount - 1;
                        continue;
                    }
                }

                var captured = CaptureRecursive(childTransform, viewportFilter, planes, depth);
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
            RealInputUtility.EnsureEventSystem();
            var node = new SemanticNode
            {
                Name = go.name,
                Role = SemanticProbe.GetRoleName(go),
                VisualID = GetID(go),
                Command = GetCommand(go),
                Active = go.activeInHierarchy
            };

            FillInteractivity(node, go);
            FillGeometry(node, go, t);
            return node;
        }

        private static void FillInteractivity(SemanticNode node, GameObject go)
        {
            node.Interactable = IsInteractable(go);
            
            // Check for physical occlusion
            var occ = RealPlayOcclusionRaycaster.CheckOcclusion(go);
            node.IsOccluded = occ.IsOccluded;
            node.BlockingObject = occ.BlockingObjectName ?? "";
            
            // Check for logical blocking (custom component logic)
            var logic = InteractionLogic.CheckLogicalInteractivity(go);
            
            // Combined logical blocked state
            node.LogicalBlocked = logic.IsBlocked || (node.IsOccluded && !(go.transform is RectTransform));
            
            if (logic.IsBlocked)
            {
                node.BlockedReason = logic.Reason;
            }
            else if (node.IsOccluded && !(go.transform is RectTransform))
            {
                node.BlockedReason = "Occluded by " + node.BlockingObject;
            }
            else
            {
                node.BlockedReason = "";
            }

            node.Affordances = GetAffordances(go);
        }

        private static void FillGeometry(SemanticNode node, GameObject go, Transform t)
        {
            var rt = t as RectTransform;
            if (rt != null) node.ScreenRect = RealInputUtility.GetScreenRect(go);
            node.Text = GetTextValue(go);
            node.ZDepth = t.position.z;
        }

        private static bool IsVisibleInViewport(GameObject go, Plane[] planes)
        {
            var rt = go.GetComponent<RectTransform>();
            if (rt != null)
            {
                Rect screenRect = RealInputUtility.GetScreenRect(go);
                if (screenRect.width <= 0 || screenRect.height <= 0) return false;
                
                Rect viewport = new Rect(0, 0, Screen.width, Screen.height);
                return viewport.Overlaps(screenRect);
            }

            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                return planes != null && GeometryUtility.TestPlanesAABB(planes, renderer.bounds);
            }

            // M007: If it has neither RT nor Renderer, it's a logical object.
            // We should NOT prune it if it's top-level or has interesting components.
            return true;
        }

        private static bool IsInteractable(GameObject go)
        {
            // Use the centralized UI state monitor for consistency
            if (!RealPlayTester.UI.PanelStateMonitor.IsPanelReady(go)) return false;

            // NEW: Check if it's a Raycast Target if it has a Graphic component
            var graphic = go.GetComponent<Graphic>();
            if (graphic != null && !graphic.raycastTarget) return false;

            return true;
        }

        private static Rect GetScreenRect(GameObject go) => RealPlayTester.Input.RealInputUtility.GetScreenRect(go);

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
