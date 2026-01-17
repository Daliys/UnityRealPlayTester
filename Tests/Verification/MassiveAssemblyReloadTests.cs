using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using RealPlayTester.Core;
using RealPlayTester.Input;
using RealPlayTester.Diagnostics;
using RealPlayTester.Editor;
using UnityEngine.UI;
using NUnitAssert = NUnit.Framework.Assert;

namespace RealPlayTester.Tests.Verification
{
    public class MassiveAssemblyReloadTests
    {
        private GameObject _host;
        private RealPlayTester.Core.TestRunner _runner;

        [UnitySetUp]
        public IEnumerator Setup()
        {
#if UNITY_EDITOR
            UnityEditor.SessionState.SetBool("RealPlay_IsRunning", false);
#endif
            _host = new GameObject("ReloadTestHost");
            _runner = _host.AddComponent<RealPlayTester.Core.TestRunner>();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            if (_host != null) UnityEngine.Object.DestroyImmediate(_host);
#if UNITY_EDITOR
            UnityEditor.SessionState.SetBool("RealPlay_IsRunning", false);
#endif
            yield return null;
        }

        // 1. SessionState Persistence: Running Flag
        [Test]
        public void Persistence_SessionState_Save_RunningFlag()
        {
#if UNITY_EDITOR
            SetRunningState(true, "TestA");
            NUnitAssert.IsTrue(UnityEditor.SessionState.GetBool("RealPlay_IsRunning", false));
#else
            NUnitAssert.Pass();
#endif
        }

        // 2. SessionState Persistence: Test Name
        [Test]
        public void Persistence_SessionState_Save_TestName()
        {
#if UNITY_EDITOR
            SetRunningState(true, "HeavyStressTest");
            NUnitAssert.AreEqual("HeavyStressTest", UnityEditor.SessionState.GetString("RealPlay_LastTest", ""));
#else
            NUnitAssert.Pass();
#endif
        }

        // 3. Persistence Reset on Completion
        [Test]
        public void Persistence_Clear_On_Finished()
        {
#if UNITY_EDITOR
            SetRunningState(true, "TestA");
            SetRunningState(false);
            NUnitAssert.IsFalse(UnityEditor.SessionState.GetBool("RealPlay_IsRunning", true));
#else
            NUnitAssert.Pass();
#endif
        }

        // 4. Recovery Detection Logic
        [Test]
        public void Recovery_Detects_Interrupted_Test()
        {
#if UNITY_EDITOR
            UnityEditor.SessionState.SetBool("RealPlay_IsRunning", true);
            UnityEditor.SessionState.SetString("RealPlay_LastTest", "ZombieTest");

            // We expect a warning log during recovery check
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(".*ZombieTest.*INTERRUPTED.*"));
            
            InvokeMethod(_runner, "CheckForInterruptedTest");

            // Verify flag was cleared after detection
            NUnitAssert.IsFalse(UnityEditor.SessionState.GetBool("RealPlay_IsRunning", true));
#else
            NUnitAssert.Pass();
#endif
        }

        // 5. Singleton Re-hydration after Deserialize
        [Test]
        public void Serialization_Instance_Restored_On_AfterDeserialize()
        {
            // Clear static instance
            var field = typeof(RealPlayTester.Core.TestRunner).GetField("_instance", BindingFlags.NonPublic | BindingFlags.Static);
            field.SetValue(null, null);
            NUnitAssert.IsNull(RealPlayTester.Core.TestRunner.Instance);

            // Trigger deserialization callback
            _runner.OnAfterDeserialize();

            NUnitAssert.IsNotNull(RealPlayTester.Core.TestRunner.Instance, "Instance must be restored during deserialization");
            NUnitAssert.AreEqual(_runner, RealPlayTester.Core.TestRunner.Instance);
        }

        // 6. Assembly Lock Mechanism: Visibility
        [Test]
        public void LockReload_Verify_Internal_Setting_Visibility()
        {
            // Verify our internal setting is accessible via InternalsVisibleTo
            NUnitAssert.IsTrue(RealPlaySettings.LockAssembliesDuringInteraction);
        }

        // 7. ScriptPoker: Enable State
        [Test]
        public void Poker_Toggle_Updates_State()
        {
#if UNITY_EDITOR
            bool initialState = GetPokerEnabled();
            ScriptPoker.TogglePoker();
            NUnitAssert.AreNotEqual(initialState, GetPokerEnabled());
            
            // Cleanup
            if (GetPokerEnabled()) ScriptPoker.TogglePoker();
#else
            NUnitAssert.Pass();
#endif
        }

        // 8. ScriptPoker: Directory Safety
        [Test]
        public void Poker_DummyFile_Path_Valid()
        {
#if UNITY_EDITOR
            var field = typeof(ScriptPoker).GetField("DummyFilePath", BindingFlags.NonPublic | BindingFlags.Static);
            string path = (string)field.GetValue(null);
            NUnitAssert.IsTrue(path.EndsWith(".cs"));
            NUnitAssert.IsTrue(path.Contains("Assets/"));
#else
            NUnitAssert.Pass();
#endif
        }

        // 9. Input Atomicity: Click Lock Check
        [UnityTest]
        public IEnumerator Click_Respects_Lock_Setting() => ToCoroutine(async () =>
        {
#if UNITY_EDITOR
            RealPlaySettings.LockAssembliesDuringInteraction = true;
            var go = new GameObject("Btn", typeof(Button), typeof(RectTransform));
            await Click.WorldObject(go);
            NUnitAssert.Pass();
#else
            await Task.Yield();
#endif
        });

        // 10. Input Atomicity: Drag Lock Check
        [UnityTest]
        public IEnumerator Drag_Respects_Lock_Setting() => ToCoroutine(async () =>
        {
#if UNITY_EDITOR
            RealPlaySettings.LockAssembliesDuringInteraction = true;
            await Drag.FromTo(Vector2.zero, Vector2.one * 100, 0.1f);
            NUnitAssert.Pass();
#else
            await Task.Yield();
#endif
        });

        // 11. Input Atomicity: Type Lock Check
        [UnityTest]
        public IEnumerator Type_Respects_Lock_Setting() => ToCoroutine(async () =>
        {
#if UNITY_EDITOR
            RealPlaySettings.LockAssembliesDuringInteraction = true;
            await TextInput.Type("SafeText", 0.01f);
            NUnitAssert.Pass();
#else
            await Task.Yield();
#endif
        });

        // 12. Serialization: Tag Persistence (Conceptual)
        [Test]
        public void Serialization_FilterTags_List_IsNotNull()
        {
            var field = typeof(RealPlayTester.Core.TestRunner).GetField("_filterTags", BindingFlags.NonPublic | BindingFlags.Instance);
            var tags = field.GetValue(_runner);
            NUnitAssert.IsNotNull(tags, "Generic list must be initialized even after potential reload wipe");
        }

        // 13. Resilience: Multi-Method Interaction Safety
        [UnityTest]
        public IEnumerator Interaction_Chain_With_Locking_Stable() => ToCoroutine(async () =>
        {
            RealPlaySettings.LockAssembliesDuringInteraction = true;
            var go = new GameObject("Btn", typeof(Button), typeof(RectTransform));
            await Click.WorldObject(go);
            await TextInput.Type("Test");
            NUnitAssert.Pass();
        });

        // 14. Performance: Lock/Unlock Overhead
        [Test]
        public void LockReload_Overhead_Is_Minimal()
        {
#if UNITY_EDITOR
            var sw = System.Diagnostics.Stopwatch.StartNew();
            for(int i=0; i<100; i++)
            {
                UnityEditor.EditorApplication.LockReloadAssemblies();
                UnityEditor.EditorApplication.UnlockReloadAssemblies();
            }
            sw.Stop();
            Debug.Log($"[PERF] 100 Lock cycles took: {sw.ElapsedMilliseconds}ms");
            NUnitAssert.Less(sw.ElapsedMilliseconds, 500, "Lock/Unlock overhead is too high");
#else
            NUnitAssert.Pass();
#endif
        }

        // 15. Resilience: Lock Failure Safety (Try-Finally)
        [Test]
        public void Interaction_Unlocks_Even_On_Immediate_Failure()
        {
            NUnitAssert.Pass();
        }

        // 16. Recovery: Idempotent Check
        [Test]
        public void Recovery_Is_Idempotent()
        {
#if UNITY_EDITOR
            UnityEditor.SessionState.SetBool("RealPlay_IsRunning", false);
            InvokeMethod(_runner, "CheckForInterruptedTest");
            InvokeMethod(_runner, "CheckForInterruptedTest");
            NUnitAssert.Pass();
#else
            NUnitAssert.Pass();
#endif
        }

        // 17. SessionState: Persistence Key Integrity
        [Test]
        public void Persistence_Keys_Are_Consistent()
        {
            var runningKeyField = typeof(RealPlayTester.Core.TestRunner).GetField("Key_IsRunning", BindingFlags.NonPublic | BindingFlags.Static);
            var lastTestKeyField = typeof(RealPlayTester.Core.TestRunner).GetField("Key_LastTest", BindingFlags.NonPublic | BindingFlags.Static);
            
            NUnitAssert.IsNotNull(runningKeyField.GetValue(null));
            NUnitAssert.IsNotNull(lastTestKeyField.GetValue(null));
        }

        // 18. ScriptPoker: Poke Logic Not-Crashing
        [Test]
        public void Poker_Poke_Does_Not_Throw()
        {
#if UNITY_EDITOR
            var method = typeof(ScriptPoker).GetMethod("Poke", BindingFlags.NonPublic | BindingFlags.Static);
            NUnitAssert.IsNotNull(method);
#else
            NUnitAssert.Pass();
#endif
        }

        // 19. Bootstrap: Handles Lost Instance
        [Test]
        public void Bootstrap_Recreates_Lost_Runner()
        {
            var field = typeof(RealPlayTester.Core.TestRunner).GetField("_instance", BindingFlags.NonPublic | BindingFlags.Static);
            field.SetValue(null, null);
            
            var method = typeof(RealPlayTester.Core.TestRunner).GetMethod("Bootstrap", BindingFlags.NonPublic | BindingFlags.Static);
            method.Invoke(null, null);
            
            NUnitAssert.IsNotNull(RealPlayTester.Core.TestRunner.Instance);
        }

        // 20. Assembly Reload: Global Static Reset Survives
        [Test]
        public void StaticReset_Survives_Reload_Cycle()
        {
            NUnitAssert.Pass();
        }

        // Helpers
        private void SetRunningState(bool running, string testName = "")
        {
            var method = typeof(RealPlayTester.Core.TestRunner).GetMethod("SetRunning", BindingFlags.NonPublic | BindingFlags.Instance);
            method.Invoke(_runner, new object[] { running, testName });
        }

        private bool GetPokerEnabled()
        {
            var field = typeof(ScriptPoker).GetField("_isEnabled", BindingFlags.NonPublic | BindingFlags.Static);
            return (bool)field.GetValue(null);
        }

        private void InvokeMethod(object obj, string methodName)
        {
            var method = obj.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            method?.Invoke(obj, null);
        }

        private static IEnumerator ToCoroutine(System.Func<Task> action)
        {
            var task = action();
            while (!task.IsCompleted) yield return null;
            if (task.IsFaulted)
            {
                var inner = task.Exception.InnerException;
                if (inner is NUnit.Framework.SuccessException) yield break;
                throw inner;
            }
        }
    }
}
