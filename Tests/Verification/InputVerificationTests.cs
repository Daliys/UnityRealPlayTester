using System;
using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using RealPlayTester.Core;
using RealPlayTester.Input;
using NAssert = NUnit.Framework.Assert;

namespace RealPlayTester.Tests.Verification
{
    public class InputVerificationTests
    {
        private VerificationSceneSetup _scene;

        [SetUp]
        public void Setup()
        {
            Time.timeScale = 1.0f;
            
            // Cleanup any leftover failure overlay from previous tests
            var overlay = GameObject.Find("RealPlayTester_FailureOverlay");
            if (overlay != null) UnityEngine.Object.DestroyImmediate(overlay);

            _scene = VerificationSceneSetup.Create();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_scene != null) _scene.Cleanup();
            yield return null;
        }

        [UnityTest]
        public IEnumerator Touch_Tap_StatefulLogic_Works()
        {
            // Verify our fix for "click failing if camera/UI moves between Down and Up"
            // We simulate this by moving the object AFTER down but BEFORE up (in a real scenario, input happens over time)
            
            bool clicked = false;
            _scene.TestButton.onClick.AddListener(() => clicked = true);

            // We can't easily inject a move in the middle of Touch.Tap purely from outside without hooks,
            // but we CAN verify that Tap works normally first.
            yield return null;
            
            yield return null; // Wait for UI layout
            yield return null; // Extra frame for safety

            // 1. Determine screen pos (null camera for Overlay Canvas)
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(null, _scene.TestButton.transform.position);

            // 2. Perform Tap
            var task = RealPlayTester.Input.Touch.Tap(screenPos, 0.2f); // Increased duration for stability
            yield return WaitForTask(task);
            
            NAssert.IsTrue(clicked, "Button should be clicked via Touch.Tap");
        }

        [UnityTest]
        public IEnumerator Touch_Swipe_TriggerDrag_Works()
        {
            // Verify drag handler execution
            var dragHandler = _scene.DraggableObj.AddComponent<SimpleDragHandler>();
            
            yield return null; // Wait for init

            Vector2 screenStart = RectTransformUtility.WorldToScreenPoint(null, _scene.DraggableObj.transform.position);
            Vector2 screenEnd = screenStart + new Vector2(100, 0); // Swipe right

            var task = RealPlayTester.Input.Touch.Swipe(screenStart, screenEnd, 0.4f); // Slower swipe for drag detection
            yield return WaitForTask(task);

            NAssert.IsTrue(dragHandler.IsDragging, "Drag handler should have been triggered");
            NAssert.Greater(dragHandler.DragCount, 1, "Should have received multiple drag events");
            NAssert.IsTrue(dragHandler.Ended, "Should have received EndDrag");
        }

        [UnityTest]
        public IEnumerator SmartFind_MultiScene_Discovery_Works()
        {
            // Create a second scene additively
            var newScene = SceneManager.CreateScene("AdditiveTestScene");
            var go = new GameObject("UniqueAdditiveObject");
            SceneManager.MoveGameObjectToScene(go, newScene);
            
            yield return null; // Wait for scene ops

            // Try to find it using SmartFind
            var found = SmartFind.Object("UniqueAdditiveObject");
            
            NAssert.IsNotNull(found, "SmartFind should find object in additive scene");
            NAssert.AreEqual(go, found);

            // Cleanup
            SceneManager.UnloadSceneAsync(newScene);
        }

        private IEnumerator WaitForTask(Task task)
        {
            while (!task.IsCompleted) yield return null;
            if (task.IsFaulted) throw task.Exception;
        }

        // Helper for drag test
        private class SimpleDragHandler : MonoBehaviour, IDragHandler, IEndDragHandler
        {
            public bool IsDragging = false;
            public int DragCount = 0;
            public bool Ended = false;

            public void OnDrag(PointerEventData eventData)
            {
                IsDragging = true;
                DragCount++;
            }

            public void OnEndDrag(PointerEventData eventData)
            {
                Ended = true;
            }
        }
    }
}
