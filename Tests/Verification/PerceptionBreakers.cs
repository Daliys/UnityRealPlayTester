using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using RealPlayTester.Core;
using RealPlayTester.Utilities;
using NUnitAssert = NUnit.Framework.Assert;

namespace RealPlayTester.Tests.Verification
{
    public class PerceptionBreakers
    {
        private GameObject _root;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            _root = new GameObject("PerceptionBreakersRoot");
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            if (_root != null) UnityEngine.Object.DestroyImmediate(_root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ISSUE_005_ZOrder_Ambiguity() => ToCoroutine(async () =>
        {
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(_root.transform);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var btn1Go = CreateButton("Candidate_1", canvasGo.transform, new Vector2(0, 0));
            var btn2Go = CreateButton("Candidate_2", canvasGo.transform, new Vector2(0, 0));
            btn2Go.transform.SetAsLastSibling();

            var found = Tester.Perception.Find("Candidate");
            NUnitAssert.AreEqual("Candidate_2", found.name, "SmartFind picked the obscured button!");
        });

        [UnityTest]
        public IEnumerator ISSUE_007_Incorrect_ScreenRect_WorldCanvas() => ToCoroutine(async () =>
        {
            var camGo = new GameObject("MainCamera", typeof(Camera));
            camGo.tag = "MainCamera";
            camGo.transform.position = new Vector3(0, 0, -10);

            var canvasGo = new GameObject("WorldCanvas", typeof(Canvas), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(_root.transform);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasGo.transform.position = new Vector3(100, 100, 100); // Far away
            canvasGo.transform.localScale = Vector3.one * 0.01f;

            var buttonGo = CreateButton("Target", canvasGo.transform, Vector2.zero);
            
            string dom = Tester.Perception.DumpHierarchyJson();
            UnityEngine.Debug.Log($"[ISSUE-007] DOM length: {dom.Length}");
        });

        [UnityTest]
        public IEnumerator ISSUE_016_AlphaZero_Interactivity() => ToCoroutine(async () =>
        {
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(_root.transform);
            
            var buttonGo = CreateButton("GhostButton", canvasGo.transform, Vector2.zero);
            var canvasGroup = buttonGo.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;

            var affordances = Tester.Perception.GetActionableElements();
            NUnitAssert.IsFalse(affordances.Contains("GhostButton"), "Button with Alpha=0 should not be actionable!");
        });

        [UnityTest]
        public IEnumerator ISSUE_020_RaycastTarget_False_Interactivity() => ToCoroutine(async () =>
        {
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(_root.transform);
            
            var imageGo = new GameObject("NonTarget", typeof(Image));
            imageGo.transform.SetParent(canvasGo.transform);
            imageGo.GetComponent<Image>().raycastTarget = false;

            var affordances = Tester.Perception.GetActionableElements();
            NUnitAssert.IsFalse(affordances.Contains("NonTarget"), "Object with raycastTarget=false should not be reported as interactable!");
        });

        private GameObject CreateButton(string name, Transform parent, Vector2 pos)
        {
            var go = new GameObject(name, typeof(Image), typeof(Button));
            go.transform.SetParent(parent);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(100, 100);
            return go;
        }

        private static IEnumerator ToCoroutine(System.Func<Task> action)
        {
            var task = action();
            while (!task.IsCompleted) yield return null;
            if (task.IsFaulted) throw task.Exception.InnerException;
        }
    }
}
