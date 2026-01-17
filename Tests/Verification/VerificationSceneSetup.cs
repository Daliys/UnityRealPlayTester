using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace RealPlayTester.Tests.Verification
{
    public class VerificationSceneSetup : MonoBehaviour
    {
        public Camera MainCamera;
        public GameObject CanvasObj;
        public Button TestButton;
        public Image TestImage;
        public Text StatusText;
        public GameObject DraggableObj;
        public GameObject CubeObj;
        public GameObject BadMaterialObj;
        public ScrollRect ScrollView;
        public RectTransform ScrollTarget;

        public static VerificationSceneSetup Create()
        {
            var go = new GameObject("VerificationSceneRoot");
            var setup = go.AddComponent<VerificationSceneSetup>();
            setup.BuildScene();
            return setup;
        }

        private void BuildScene()
        {
            SetupCamera();
            SetupLighting();
            SetupEventSystem();
            SetupUI();
            Setup3DObjects();
        }

        private void SetupCamera()
        {
            var camObj = new GameObject("MainCamera");
            MainCamera = camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
            camObj.transform.position = new Vector3(0, 0, -10);
            camObj.transform.LookAt(Vector3.zero);
            camObj.transform.SetParent(transform);
        }

        private void SetupLighting()
        {
            var lightObj = new GameObject("Directional Light");
            var light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            lightObj.transform.rotation = Quaternion.Euler(50, -30, 0);
            lightObj.transform.SetParent(transform);
        }

        private void SetupEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var esObj = new GameObject("EventSystem");
                esObj.AddComponent<EventSystem>();
                esObj.AddComponent<InputSystemUIInputModule>();
                esObj.transform.SetParent(transform);
            }
        }

        private void SetupUI()
        {
            CanvasObj = new GameObject("Canvas");
            CanvasObj.transform.SetParent(transform);
            var canvas = CanvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasObj.AddComponent<CanvasScaler>();
            CanvasObj.AddComponent<GraphicRaycaster>();

            CreateButton();
            CreateImage();
            CreateDraggable();
            CreateStatusText();
            CreateScrollView();
        }

        private void CreateButton()
        {
            var btnObj = new GameObject("TestButton");
            btnObj.transform.SetParent(CanvasObj.transform, false);
            var btnImg = btnObj.AddComponent<Image>();
            btnImg.color = Color.green;
            TestButton = btnObj.AddComponent<Button>();
            
            var btnTextObj = new GameObject("Text");
            btnTextObj.transform.SetParent(btnObj.transform, false);
            var btnText = btnTextObj.AddComponent<Text>();
            btnText.text = "Click Me";
            btnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            btnText.color = Color.black;
            btnText.alignment = TextAnchor.MiddleCenter;
            btnText.raycastTarget = false; // Prevent it from occluding parent button
            ((RectTransform)btnTextObj.transform).anchorMin = Vector2.zero;
            ((RectTransform)btnTextObj.transform).anchorMax = Vector2.one;
        }

        private void CreateImage()
        {
            var imgObj = new GameObject("TestImage");
            imgObj.transform.SetParent(CanvasObj.transform, false);
            TestImage = imgObj.AddComponent<Image>();
            TestImage.color = Color.blue;
            ((RectTransform)imgObj.transform).anchoredPosition = new Vector2(0, 100);
        }

        private void CreateDraggable()
        {
            var dragObj = new GameObject("DraggablePanel");
            dragObj.transform.SetParent(CanvasObj.transform, false);
            var dragImg = dragObj.AddComponent<Image>();
            dragImg.color = Color.red;
            ((RectTransform)dragObj.transform).sizeDelta = new Vector2(100, 100);
            ((RectTransform)dragObj.transform).anchoredPosition = new Vector2(200, 0);
            DraggableObj = dragObj;
        }

        private void CreateStatusText()
        {
            var statusObj = new GameObject("StatusText");
            statusObj.transform.SetParent(CanvasObj.transform, false);
            StatusText = statusObj.AddComponent<Text>();
            StatusText.text = "Ready";
            StatusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            StatusText.color = Color.white;
            ((RectTransform)statusObj.transform).anchoredPosition = new Vector2(0, -100);
        }

        private void CreateScrollView()
        {
            var scrollObj = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollObj.transform.SetParent(CanvasObj.transform, false);
            var rt = scrollObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200, 200);
            rt.anchoredPosition = new Vector2(-300, 0);
            
            ScrollView = scrollObj.GetComponent<ScrollRect>();
            
            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Mask), typeof(Image));
            viewport.transform.SetParent(scrollObj.transform, false);
            var viewportRt = viewport.GetComponent<RectTransform>();
            viewportRt.anchorMin = Vector2.zero;
            viewportRt.anchorMax = Vector2.one;
            viewportRt.sizeDelta = Vector2.zero;
            
            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            var contentRt = content.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0, 1);
            contentRt.anchorMax = new Vector2(1, 1);
            contentRt.sizeDelta = new Vector2(0, 1000);
            contentRt.pivot = new Vector2(0.5f, 1);
            
            ScrollView.viewport = viewportRt;
            ScrollView.content = contentRt;
            ScrollView.vertical = true;

            var targetObj = new GameObject("ScrollTarget", typeof(RectTransform), typeof(Text));
            targetObj.transform.SetParent(content.transform, false);
            ScrollTarget = targetObj.GetComponent<RectTransform>();
            ScrollTarget.anchoredPosition = new Vector2(0, -900);
            var txt = targetObj.GetComponent<Text>();
            txt.text = "Found Me";
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private void Setup3DObjects()
        {
            // Cube
            CubeObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            CubeObj.name = "TestCube";
            CubeObj.transform.position = Vector3.zero;
            CubeObj.transform.SetParent(transform);

            // Bad Material Object
            BadMaterialObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            BadMaterialObj.name = "BadSphere";
            BadMaterialObj.transform.position = new Vector3(2, 0, 0);
            BadMaterialObj.transform.SetParent(transform);
            var ren = BadMaterialObj.GetComponent<Renderer>();
            ren.sharedMaterial = null;
            BadMaterialObj.SetActive(false);
        }

        public void Cleanup()
        {
            if (gameObject != null) DestroyImmediate(gameObject);
        }
    }
}
