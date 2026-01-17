using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RealPlayTester.Core;
using RealPlayTester.Input;

namespace RealPlayTester.Tests.Verification
{
    /// <summary>
    /// Runtime component that performs a realistic UI interaction demo when [P] is pressed.
    /// </summary>
    public class UIVerificationDemo : MonoBehaviour
    {
        private bool _isRunning = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            var go = new GameObject("UIVerificationDemo");
            go.AddComponent<UIVerificationDemo>();
            DontDestroyOnLoad(go);
        }

        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current != null && 
                UnityEngine.InputSystem.Keyboard.current.pKey.wasPressedThisFrame && 
                !_isRunning)
            {
                StartCoroutine(RunDemoRoutine());
            }
#else
            if (UnityEngine.Input.GetKeyDown(KeyCode.P) && !_isRunning)
            {
                StartCoroutine(RunDemoRoutine());
            }
#endif
        }

        private IEnumerator RunDemoRoutine()
        {
            _isRunning = true;
            Debug.Log("[RealPlayDemo] Starting Realistic UI Demo...");

            yield return RunAsyncTask(ExecuteDemo());

            Debug.Log("[RealPlayDemo] Demo Complete.");
            _isRunning = false;
        }

        private async Task ExecuteDemo()
        {
            await Tester.Await.Seconds(0.5f);

            await DemoButton();
            await DemoToggle();
            await DemoInputField();
            await DemoSlider();
            await DemoDropdown();
            await DemoScroll();

            await Tester.Interaction.Mouse.MoveTo(new Vector2(Screen.width / 2f, Screen.height / 2f), 0.5f);
        }

        private async Task DemoButton()
        {
            var btn = await FindAndMove("Button");
            if (btn != null)
            {
                await Tester.Await.Seconds(0.3f);
                await Tester.Interaction.Mouse.ClickObject(btn);
                await Tester.Await.Seconds(1.0f);
            }
        }

        private async Task DemoToggle()
        {
            var toggle = await FindAndMove("Toggle");
            if (toggle != null)
            {
                await Tester.Await.Seconds(0.3f);
                await Tester.Interaction.Mouse.ClickObject(toggle);
                await Tester.Await.Seconds(1.0f);
            }
        }

        private async Task DemoInputField()
        {
            var input = await FindAndMove("InputField");
            if (input != null)
            {
                await Tester.Await.Seconds(0.3f);
                await Tester.Interaction.Mouse.ClickObject(input);
                await Tester.Await.Seconds(0.5f);
                await Tester.Interaction.Keyboard.Type("Interactive Demo!", 0.08f);
                await Tester.Await.Seconds(1.0f);
            }
        }

        private async Task DemoSlider()
        {
            var handle = await FindAndMove("Handle");
            if (handle != null)
            {
                await Tester.Await.Seconds(0.3f);
                var start = RealInputUtility.LastSimulatedPosition;
                var end = start + new Vector2(200f, 0f);
                await Tester.Advanced.Drag(start, end, 1.5f);
            }
            await Tester.Await.Seconds(1.0f);
        }

        private async Task DemoDropdown()
        {
            var dropdown = await FindAndMove("Dropdown");
            if (dropdown != null)
            {
                await Tester.Await.Seconds(0.3f);
                await Tester.Interaction.Mouse.ClickObject(dropdown);
                await Tester.Await.Seconds(0.8f);

                var option = await FindAndMove("Option B");
                if (option != null)
                {
                    await Tester.Await.Seconds(0.3f);
                    await Tester.Interaction.Mouse.ClickObject(option);
                }
                await Tester.Await.Seconds(1.0f);
            }
        }

        private async Task DemoScroll()
        {
            var scroll = await FindAndMove("Scroll");
            if (scroll != null)
            {
                var scrollRect = scroll.GetComponent<ScrollRect>() ?? scroll.GetComponentInParent<ScrollRect>();
                GameObject scrollTarget = scroll;
                if (scrollRect != null)
                {
                    if (scrollRect.viewport != null) scrollTarget = scrollRect.viewport.gameObject;
                    else if (scrollRect.content != null) scrollTarget = scrollRect.content.gameObject;
                }
                
                var targetCenter = Tester.Utility.GetScreenCenter(scrollTarget);
                var rt = scrollTarget.transform as RectTransform;
                if (rt != null)
                {
                    targetCenter.x -= rt.rect.width * 0.2f;
                }

                await Tester.Interaction.Mouse.MoveTo(targetCenter, 0.5f);
                await Tester.Await.Seconds(0.3f);
                var start = RealInputUtility.LastSimulatedPosition;
                var end = start + new Vector2(0, 350);
                await Tester.Advanced.Drag(start, end, 2.0f);
                await Tester.Await.Seconds(1.0f);
            }
        }

        private async Task<GameObject> FindAndMove(string targetName)
        {
            var go = Tester.Perception.Find(targetName);
            if (go != null)
            {
                var before = RealInputUtility.LastSimulatedPosition;
                await Tester.Interaction.Mouse.MoveToCenter(go, 0.7f);
                var after = RealInputUtility.LastSimulatedPosition;
                Debug.Log($"[RealPlayDemo] Moved to {targetName}: {before} -> {after}");
                await Tester.Await.Seconds(0.1f);
            }
            else
            {
                Debug.LogWarning($"[RealPlayDemo] Could not find '{targetName}'");
            }
            return go;
        }

        private IEnumerator RunAsyncTask(Task task)
        {
            while (!task.IsCompleted) yield return null;
            if (task.IsFaulted) Debug.LogError(task.Exception);
        }
    }
}
