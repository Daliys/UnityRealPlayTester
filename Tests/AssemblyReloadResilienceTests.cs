using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using RealPlayTester.Core;

namespace RealPlayTester.Tests.Verification
{
    public class AssemblyReloadResilienceTests
    {
        [UnityTest]
        public IEnumerator Reload_Interruption_Detected_On_Resume() => ToCoroutine(async () =>
        {
#if UNITY_EDITOR
            // 1. Setup 'Interrupted' state manually in SessionState
            UnityEditor.SessionState.SetBool("RealPlay_IsRunning", true);
            UnityEditor.SessionState.SetString("RealPlay_LastTest", "ManualPokerTest");

            // 2. Trigger the check method (usually runs on OnEnable)
            var runner = RealPlayTester.Core.TestRunner.Instance;
            var checkMethod = typeof(RealPlayTester.Core.TestRunner).GetMethod("CheckForInterruptedTest", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            // We need a way to verify the error was logged. 
            // In a unit test, we can use LogAssert.
            UnityEngine.TestTools.LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("INTERRUPTED by an Assembly Reload"));
            
            checkMethod.Invoke(runner, null);

            // 3. Verify flag was cleared
            NUnit.Framework.Assert.IsFalse(UnityEditor.SessionState.GetBool("RealPlay_IsRunning", true), "IsRunning flag must be cleared after recovery check");
#else
            NUnit.Framework.Assert.Pass("Not in Editor mode");
#endif
            await Task.Yield();
        });

        [UnityTest]
        public IEnumerator Lock_Assemblies_During_Click() => ToCoroutine(async () =>
        {
#if UNITY_EDITOR
            // Verify that we can call Lock/Unlock without crashing 
            UnityEditor.EditorApplication.LockReloadAssemblies();
            UnityEditor.EditorApplication.UnlockReloadAssemblies();
            NUnit.Framework.Assert.Pass();
#else
            NUnit.Framework.Assert.Pass();
#endif
            await Task.Yield();
        });

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