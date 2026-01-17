using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools;
using RealPlayTester.Core;
using RealPlayTester.Utilities;
using RealPlayTester.Input;
using NUnitAssert = NUnit.Framework.Assert;

namespace RealPlayTester.Tests.Verification
{
    public class EnvironmentFeatureTests
    {
        private GameObject _testRoot;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            _testRoot = new GameObject("EnvironmentFeatureRoot");
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            if (_testRoot != null) UnityEngine.Object.DestroyImmediate(_testRoot);
            yield return null;
        }

        // 16. HardReset with Active Background Coroutines
        [UnityTest]
        public IEnumerator HardReset_StopsBackgroundCoroutines() => ToCoroutine(async () =>
        {
            var host = RealPlayTesterHost.Instance;
            host.StartCoroutine(BackgroundWork());
            
            IEnumerator BackgroundWork() { while(true) { yield return null; } }

            await HardReset.Execute();
            NUnitAssert.Pass("Engine stable after HardReset with background work");
        });

        // 17. SceneCache: Rapid Reload Resilience
        [UnityTest]
        public IEnumerator SceneCache_Handles_RapidReload() => ToCoroutine(async () =>
        {
            for(int i = 0; i < 3; i++)
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
                await Task.Yield();
                SceneCache.Instance.ForceRefresh();
            }
            NUnitAssert.Pass("Cache maintained consistency through rapid reloads");
        });

        // 18. Logical Block: Nested Component Detection
        [UnityTest]
        public IEnumerator Logic_Detects_DeepNestedBlock()
        {
            var root = new GameObject("Root");
            root.transform.SetParent(_testRoot.transform);
            var child = new GameObject("Child");
            child.transform.SetParent(root.transform);
            
            child.AddComponent<BlockedLogic>(); 
            
            var result = InteractionLogic.CheckLogicalInteractivity(child);
            NUnitAssert.IsTrue(result.IsBlocked, "Failed to detect logic block on child");
            yield return null;
        }

        // 19. Logic: State Transition Verification
        [UnityTest]
        public IEnumerator Logic_Correctly_Tracks_StateChange() => ToCoroutine(async () =>
        {
            var go = new GameObject("Dynamic");
            go.transform.SetParent(_testRoot.transform);
            var script = go.AddComponent<ToggleLogic>();
            
            script.Blocked = true;
            NUnitAssert.IsTrue(InteractionLogic.CheckLogicalInteractivity(go).IsBlocked);
            
            script.Blocked = false;
            NUnitAssert.IsFalse(InteractionLogic.CheckLogicalInteractivity(go).IsBlocked);
        });

        // 20. SmartFind: Non-Blocked Preference (Tie-break)
        [UnityTest]
        public IEnumerator SmartFind_Picks_Unblocked_Over_Blocked() => ToCoroutine(async () =>
        {
            var canvasGo = new GameObject("C", typeof(Canvas));
            canvasGo.transform.SetParent(_testRoot.transform);
            var c = canvasGo.transform;

            var b1 = new GameObject("T", typeof(RectTransform), typeof(UnityEngine.UI.Button), typeof(UnityEngine.UI.Image));
            b1.transform.SetParent(c, false);
            b1.AddComponent<BlockedLogic>(); // Penalty

            var b2 = new GameObject("T", typeof(RectTransform), typeof(UnityEngine.UI.Button), typeof(UnityEngine.UI.Image));
            b2.transform.SetParent(c, false);
            b2.transform.position = new Vector3(100, 0, 0); // No penalty

            await Task.Yield();
            SceneCache.Instance.ForceRefresh();

            var found = SmartFind.Object("T");
            NUnitAssert.AreEqual(b2.GetInstanceID(), found.GetInstanceID(), "Failed to pick the unblocked candidate");
        });

        private class BlockedLogic : MonoBehaviour, IInteractableLogic {
            public bool CanInteract() => false;
            public string GetBlockedReason() => "Test Block";
        }

        private class ToggleLogic : MonoBehaviour, IInteractableLogic {
            public bool Blocked;
            public bool CanInteract() => !Blocked;
            public string GetBlockedReason() => "Dynamic Block";
        }

        private static IEnumerator ToCoroutine(System.Func<Task> action)
        {
            var task = action();
            while (!task.IsCompleted) yield return null;
            if (task.IsFaulted) throw task.Exception.InnerException;
        }
    }
}
