using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools;
using RealPlayTester.Core;
using RealPlayTester.Await;
using System;
using NUnitAssert = NUnit.Framework.Assert;

namespace RealPlayTester.Tests.Verification
{
    public class WaitVerificationTests
    {
        private GameObject _testRoot;

        [SetUp]
        public void Setup()
        {
            _testRoot = new GameObject("WaitTestRoot");
        }

        [TearDown]
        public void Teardown()
        {
            UnityEngine.Object.DestroyImmediate(_testRoot);
        }

        // Test 1: Wait.ForUIVisible (Positive)
        [UnityTest]
        public IEnumerator Wait_ForUIVisible_SucceedsWhenVisible() => ToCoroutine(async () =>
        {
            var go = new GameObject("TargetUI", typeof(RectTransform), typeof(CanvasGroup));
            go.transform.SetParent(_testRoot.transform);
            var cg = go.GetComponent<CanvasGroup>();
            cg.alpha = 0;

            var waitTask = RealPlayTester.Await.Wait.ForUIVisible("TargetUI", 2f);
            await Tester.Await.Frames(2);
            cg.alpha = 1;
            
            await waitTask;
            NUnitAssert.IsTrue(waitTask.IsCompleted);
        });

        // Test 2: Wait.ForUIVisible (Timeout)
        [UnityTest]
        public IEnumerator Wait_ForUIVisible_TimesOutWhenHidden() => ToCoroutine(async () =>
        {
            var go = new GameObject("HiddenUI", typeof(RectTransform), typeof(CanvasGroup));
            go.transform.SetParent(_testRoot.transform);
            var cg = go.GetComponent<CanvasGroup>();
            cg.alpha = 0;

            // In batchmode, ForceBatchmodeVisibility might make it "visible" immediately.
            // We disable it for this specific test to ensure timeout logic works.
            bool prevForce = Tester.Settings.ForceBatchmodeVisibility;
            Tester.Settings.ForceBatchmodeVisibility = false;

            bool timedOut = false;
            try { await RealPlayTester.Await.Wait.ForUIVisible("HiddenUI", 0.5f); }
            catch (TimeoutException) { timedOut = true; }
            finally { Tester.Settings.ForceBatchmodeVisibility = prevForce; }

            NUnitAssert.IsTrue(timedOut, "Should have timed out for hidden UI when ForceBatchmodeVisibility is off");
        });

        // Test 3: Wait.ForInteractable (Component & Text)
        [UnityTest]
        public IEnumerator Wait_ForInteractable_FindsButtonWithText() => ToCoroutine(async () =>
        {
            var btnGo = new GameObject("MyButton", typeof(Button), typeof(RectTransform));
            btnGo.transform.SetParent(_testRoot.transform);
            var txtGo = new GameObject("Text", typeof(Text));
            txtGo.transform.SetParent(btnGo.transform);
            var txt = txtGo.GetComponent<Text>();
            txt.text = "Submit Order";
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var waitTask = RealPlayTester.Await.Wait.ForInteractable<Button>("Submit", 2f);
            await waitTask;
            NUnitAssert.IsTrue(waitTask.IsCompleted);
        });

        // Test 4: Wait.ForAnimationState
        [UnityTest]
        public IEnumerator Wait_ForAnimationState_Succeeds() => ToCoroutine(async () =>
        {
            var animGo = new GameObject("AnimObj", typeof(Animator));
            animGo.transform.SetParent(_testRoot.transform);
            var animator = animGo.GetComponent<Animator>();
            await RealPlayTester.Await.Wait.ForAnimationState(animator, "", 1f); 
        });

        // Test 5: Wait.ForAudioComplete
        [UnityTest]
        public IEnumerator Wait_ForAudioComplete_Succeeds() => ToCoroutine(async () =>
        {
            var audioGo = new GameObject("Audio", typeof(AudioSource));
            audioGo.transform.SetParent(_testRoot.transform);
            var source = audioGo.GetComponent<AudioSource>();
            await RealPlayTester.Await.Wait.ForAudioComplete(source, 1f);
        });

        // Test 6: Wait.ForObject
        [UnityTest]
        public IEnumerator Wait_ForObject_FindsDynamicallyCreatedObject() => ToCoroutine(async () =>
        {
            var waitTask = RealPlayTester.Await.Wait.ForObject("DynamicObj", 2f);
            await Tester.Await.Frames(5);
            new GameObject("DynamicObj").transform.SetParent(_testRoot.transform);
            await waitTask;
        });

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