using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using RealPlayTester.Core;
using RealPlayTester.Input;
using NUnitAssert = NUnit.Framework.Assert;

namespace RealPlayTester.Tests.Verification
{
    public class Lifecycle_Interactions
    {
        private GameObject _root;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            _root = new GameObject("Root");
            RealInputUtility.EnsureEventSystem();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            if (_root != null) Object.DestroyImmediate(_root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Perform_During_SceneUnload() => ToCoroutine(async () =>
        {
            var btn = new GameObject("T", typeof(RectTransform), typeof(Image), typeof(Button));
            btn.transform.SetParent(_root.transform);
            var task = Tester.Interaction.Perform("Click", btn);
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            try { await task; } catch { }
        });

        [UnityTest]
        public IEnumerator Drag_Across_SceneChange() => ToCoroutine(async () =>
        {
            var task = Tester.Advanced.Drag(Vector2.zero, Vector2.one * 100, 0.5f);
            await Task.Delay(20);
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            try { await task; } catch { }
        });

        [UnityTest]
        public IEnumerator Keyboard_Type_During_Unload() => ToCoroutine(async () =>
        {
            var inputGo = new GameObject("In", typeof(InputField));
            inputGo.transform.SetParent(_root.transform);
            inputGo.GetComponent<InputField>().Select();
            var task = Tester.Interaction.Keyboard.Type("Text...");
            await Task.Delay(10);
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            try { await task; } catch { }
        });

        private static IEnumerator ToCoroutine(System.Func<Task> action)
        {
            var task = action();
            while (!task.IsCompleted) yield return null;
            if (task.IsFaulted) throw task.Exception.InnerException;
        }
    }
}
