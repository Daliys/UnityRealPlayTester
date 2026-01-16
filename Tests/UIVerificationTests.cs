using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools;
using RealPlayTester.Core;
using RealPlayTester.Input;
using TMPro;

namespace RealPlayTester.Tests.Verification
{
    public class UIVerificationTests
    {
        [UnitySetUp]
        public IEnumerator Setup()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("Assets/Scenes/Tests.unity");
            yield return null; 
            
            // Force resolution for valid interaction coordinates
            Screen.SetResolution(1024, 768, false);
            yield return null;
            
            // Wait for resolution (vital for batchmode/CI)
            float waitStart = Time.realtimeSinceStartup;
            while (Screen.width <= 1 && Time.realtimeSinceStartup - waitStart < 5f) yield return null;
            
            yield return new WaitForSecondsRealtime(1.0f);
            
            // Feature A & C: Enable optimizations for reliable batchmode verification
            Tester.Settings.ForceBatchmodeVisibility = true;
            Tester.Settings.EnableInputHeartbeat = true;
        }

        [UnityTest]
        public IEnumerator Button_Click_Works() => ToCoroutine(async () =>
        {
            var btnGo = await WaitForObject("Button");
            var btn = btnGo.GetComponent<Button>();
            bool clicked = false;
            btn.onClick.AddListener(() => clicked = true);

            await Tester.Interaction.Mouse.ClickObject(Tester.Perception.Find(btnGo.name));
            
            NUnit.Framework.Assert.IsTrue(clicked, "Button should have been clicked via simulated mouse input");
        });

        [UnityTest]
        public IEnumerator InputField_Type_Works() => ToCoroutine(async () =>
        {
            var inputGo = await WaitForObject("InputField");
            var input = inputGo.GetComponent<TMP_InputField>();
            string testValue = "Hello RealPlay";
            
            await Tester.Interaction.Keyboard.TypeIntoField(inputGo.name, testValue);
            
            NUnit.Framework.Assert.AreEqual(testValue, input.text, "InputField text should match the typed value");
        });

        [UnityTest]
        public IEnumerator Toggle_ChangeState_Works() => ToCoroutine(async () =>
        {
            var toggleGo = await WaitForObject("Toggle");
            var toggle = toggleGo.GetComponent<Toggle>();
            bool initialState = toggle.isOn;
            
            await Tester.Interaction.Mouse.ClickObject(Tester.Perception.Find(toggleGo.name));
            
            NUnit.Framework.Assert.AreNotEqual(initialState, toggle.isOn, "Toggle state should have flipped after click");
        });

        [UnityTest]
        public IEnumerator Slider_Drag_Works() => ToCoroutine(async () =>
        {
            var sliderGo = await WaitForObject("Slider");
            var slider = sliderGo.GetComponent<Slider>();
            slider.value = 0f;
            
            var handleGo = await WaitForObject("Handle");
            var startPos = RealInputUtility.GetScreenCenter(handleGo);
            // Drag right by 30% of screen width to ensure we cross a threshold
            var endPos = startPos + new Vector2(Screen.width * 0.3f, 0f);
            
            await Tester.Advanced.Drag(startPos, endPos, 1.0f);
            
            bool increased = false;
            float timeout = 2f;
            float start = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - start < timeout)
            {
                if (slider.value > 0.01f) { increased = true; break; }
                await Task.Yield();
            }
            
            NUnit.Framework.Assert.IsTrue(increased, $"Slider value ({slider.value}) should have increased after dragging handle. Initial: 0");
        });

        [UnityTest]
        public IEnumerator Dropdown_SelectOption_Works() => ToCoroutine(async () =>
        {
            var dropdownGo = await WaitForObject("Dropdown");
            var dropdown = dropdownGo.GetComponent<TMP_Dropdown>();
            dropdown.value = 0; // "Option A"
            
            await Tester.Interaction.Mouse.ClickObject(Tester.Perception.Find(dropdownGo.name));
            await Tester.Await.Seconds(1.0f);
            
            // TMP Dropdowns create "Option B" inside the overlay. 
            // We use a broader search because naming can be "Item 1: Option B" in some TMP versions
            var optionB = await WaitUntilFound("Option B");
            await Tester.Interaction.Mouse.ClickObject(optionB); 
            
            NUnit.Framework.Assert.AreEqual(1, dropdown.value, "Dropdown should have selected Option B (index 1)");
        });

        [UnityTest]
        public IEnumerator ScrollView_Scroll_Works() => ToCoroutine(async () =>
        {
            // The ScrollView might be named "ScrollView" or similar. 
            // Look for any component with ScrollRect if not found by name.
            var scrollGo = await WaitForObject("Scroll View");
            var scroll = scrollGo.GetComponent<ScrollRect>();
            NUnit.Framework.Assert.IsNotNull(scroll, "Scroll View GameObject missing ScrollRect component");
            float initialPos = scroll.verticalNormalizedPosition;
            
            var rect = scroll.GetComponent<RectTransform>();
            var center = RealInputUtility.GetScreenCenter(scrollGo);
            
            // Drag UP by 50% of screen height to scroll content DOWN
            var endPos = center + new Vector2(0, Screen.height * 0.5f);
            await Tester.Advanced.Drag(center, endPos, 1.0f);
            
            // Wait for position to change (handles batchmode frame variability)
            bool scrolled = false;
            float timeout = 2f;
            float start = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - start < timeout)
            {
                if (scroll.verticalNormalizedPosition < initialPos - 0.01f) { scrolled = true; break; }
                await Task.Yield();
            }
            
            NUnit.Framework.Assert.IsTrue(scrolled, $"ScrollView should have scrolled. Initial: {initialPos}, Current: {scroll.verticalNormalizedPosition}");
        });

        private async Task<GameObject> WaitForObject(string name) => await WaitUntilFound(name);

        private async Task<GameObject> WaitUntilFound(string name, float timeout = 5f)
        {
            float start = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - start < timeout)
            {
                var go = Tester.Perception.Find(name);
                if (go != null && go.activeInHierarchy) return go;
                await Task.Yield();
            }
            
            Debug.LogError($"[Verification] Failed to find active object '{name}' within {timeout}s. Current hierarchy:\n{Tester.Perception.DumpHierarchy()}");
            throw new System.Exception($"Object '{name}' not found");
        }

        private static IEnumerator ToCoroutine(System.Func<Task> action)
        {
            var task = action();
            while (!task.IsCompleted) yield return null;
            if (task.IsFaulted)
            {
                if (task.Exception != null)
                    System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(task.Exception.InnerException).Throw();
                else
                    throw new System.Exception("Task failed without exception");
            }
        }
    }
}
