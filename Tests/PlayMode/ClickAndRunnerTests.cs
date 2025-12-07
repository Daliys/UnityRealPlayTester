using System;
using System.Collections;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using RealPlayTester.Core;
using RealPlayTester.Input;
using NAssert = NUnit.Framework.Assert;

namespace RealPlayTester.Tests.PlayMode
{
    public class ClickAndRunnerTests
    {
        [UnityTest]
        public IEnumerator ClickScreenPercent_TriggersButton()
        {
            var canvas = BuildCanvas();
            var button = CreateButton(canvas.transform, new Vector2(0.5f, 0.5f));
            bool clicked = false;
            button.onClick.AddListener(() => clicked = true);

            yield return null;

            var task = Click.ScreenPercent(0.5f, 0.5f);
            yield return WaitForTask(task, 2f);

            NAssert.IsTrue(clicked);
            Cleanup(canvas.gameObject);
        }

        [UnityTest]
        public IEnumerator ClickWorldObject_TriggersPointerHandler()
        {
            var cameraGo = new GameObject("TestCamera");
            var camera = cameraGo.AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.gameObject.AddComponent<PhysicsRaycaster>();

            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.position = Vector3.zero;
            var spy = cube.AddComponent<PointerSpy>();
            var mouseSpy = cube.AddComponent<MouseSpy>();

            yield return null;

            var task = Click.WorldObject(cube, camera);
            yield return WaitForTask(task, 2f);

            NAssert.IsTrue(spy.Clicked || mouseSpy.UpAsButton, "Expected pointer click or mouse callback.");
            Cleanup(cameraGo);
            Cleanup(cube);
        }

        [UnityTest]
        public IEnumerator DragFromTo_InvokesHandlers()
        {
            var canvas = BuildCanvas();
            var target = new GameObject("DragTarget", typeof(RectTransform), typeof(Image), typeof(DragSpy));
            target.transform.SetParent(canvas.transform, false);
            var rect = target.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200f, 200f);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            var spy = target.GetComponent<DragSpy>();

            yield return null;

            Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, rect.position);
            var task = Drag.FromTo(screen, screen + new Vector2(100f, 0f), 0.1f);
            yield return WaitForTask(task, 2f);

            NAssert.IsTrue(spy.Dragged);
            Cleanup(canvas.gameObject);
        }

        [UnityTest]
        public IEnumerator Runner_RunAll_CompletesWithoutAssets()
        {
            var runnerGo = new GameObject("RunnerHarness");
            var runner = runnerGo.AddComponent<RealPlayTester.Core.TestRunner>();

            var task = runner.RunAll(false);
            yield return WaitForTask(task, 5f);

            NAssert.IsTrue(task.IsCompleted);
            Cleanup(runnerGo);
        }

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        [UnityTest]
        public IEnumerator Runner_F12_TriggersRunAll_WithInputSystem()
        {
            var runnerGo = new GameObject("RunnerHarnessF12");
            var runner = runnerGo.AddComponent<RealPlayTester.Core.TestRunner>();

            yield return null;

            var shimType = Type.GetType("RealPlayTester.Input.InputSystemShim, RealPlayTester");
            shimType?.GetMethod("KeyDown", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                ?.Invoke(null, new object[] { KeyCode.F12 });

            yield return null;
            yield return null;

            yield return new WaitForSeconds(0.2f);
            Cleanup(runnerGo);
        }
#endif

        [UnityTest]
        public IEnumerator Runner_CommandLine_RunRealTests_DoesNotQuitAndSetsExitCode()
        {
            var runnerGo = new GameObject("RunnerHarnessCLI");
            var runner = runnerGo.AddComponent<RealPlayTester.Core.TestRunner>();

            yield return null;

#if UNITY_EDITOR
            runner.ProcessArgsForTests(new[] { "app", "-runRealTests" });
#endif

            yield return new WaitForSeconds(0.5f);

            NAssert.AreEqual(0, Environment.ExitCode, "CLI run should finish with exit code 0.");
            Cleanup(runnerGo);
        }

        [UnityTest]
        public IEnumerator TextType_TypesIntoFocusedInputField()
        {
            var canvas = BuildCanvas();
            var es = new GameObject("ES", typeof(EventSystem), typeof(StandaloneInputModule));
            var fieldGo = new GameObject("Field", typeof(RectTransform), typeof(Image), typeof(InputField));
            fieldGo.transform.SetParent(canvas.transform, false);
            var rect = fieldGo.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200f, 40f);
            var field = fieldGo.GetComponent<InputField>();
            var textChild = new GameObject("Text", typeof(RectTransform), typeof(UnityEngine.UI.Text));
            textChild.transform.SetParent(fieldGo.transform, false);
            var uiTextComp = textChild.GetComponent<UnityEngine.UI.Text>();
            uiTextComp.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            uiTextComp.color = Color.black;
            field.textComponent = uiTextComp;
            field.text = string.Empty;

            yield return null;

            var currentES = EventSystem.current ?? es.GetComponent<EventSystem>();
            currentES.SetSelectedGameObject(fieldGo);
            field.Select();
            field.ActivateInputField();

            var task = RealPlayTester.Input.Text.TypeIntoField("Field", "hello", 0f);
            yield return WaitForTask(task, 2f);

            NAssert.AreEqual("hello", field.text);
            Cleanup(fieldGo);
            Cleanup(es);
            Cleanup(canvas.gameObject);
        }

        private static Canvas BuildCanvas()
        {
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            return canvas;
        }

        private static Button CreateButton(Transform parent, Vector2 anchor)
        {
            var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchor;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(160f, 40f);
            return go.GetComponent<Button>();
        }

        private static IEnumerator WaitForTask(Task task, float timeoutSeconds)
        {
            float elapsed = 0f;
            while (!task.IsCompleted && elapsed < timeoutSeconds)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            NAssert.IsTrue(task.IsCompleted, "Task did not complete within timeout.");
        }

        private static void Cleanup(UnityEngine.Object go)
        {
            if (go != null)
            {
                UnityEngine.Object.Destroy(go);
            }
        }

        private class PointerSpy : MonoBehaviour, IPointerClickHandler
        {
            public bool Clicked { get; private set; }

            public void OnPointerClick(PointerEventData eventData)
            {
                Clicked = true;
            }
        }

        private class DragSpy : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
        {
            public bool Dragged { get; private set; }

            public void OnBeginDrag(PointerEventData eventData)
            {
            }

            public void OnDrag(PointerEventData eventData)
            {
                Dragged = true;
            }

            public void OnEndDrag(PointerEventData eventData)
            {
            }
        }

        private class MouseSpy : MonoBehaviour
        {
            public bool Down { get; private set; }
            public bool Up { get; private set; }
            public bool UpAsButton { get; private set; }

            private void OnMouseDown()
            {
                Down = true;
            }

            private void OnMouseUp()
            {
                Up = true;
            }

            private void OnMouseUpAsButton()
            {
                UpAsButton = true;
            }
        }
    }
}
