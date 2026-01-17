using System;
using UnityEngine;

namespace RealPlayTester.Core
{
    public sealed partial class TestRunner
    {
        // PERSISTENCE KEYS
        private const string Key_IsRunning = "RealPlay_IsRunning";
        private const string Key_LastTest = "RealPlay_LastTest";

        public void OnBeforeSerialize() { }
        public void OnAfterDeserialize() { if (_instance == null) _instance = this; }

        private void OnEnable()
        {
            RealPlayTester.Assert.LogAssert.StartListening();
            CheckForInterruptedTest();
        }

        private void OnDisable() => RealPlayTester.Assert.LogAssert.StopListening();

        private void CheckForInterruptedTest()
        {
#if UNITY_EDITOR
            if (UnityEditor.SessionState.GetBool(Key_IsRunning, false))
            {
                string testName = UnityEditor.SessionState.GetString(Key_LastTest, "Unknown");
                UnityEditor.SessionState.SetBool(Key_IsRunning, false);
                RealPlayLog.Warn("[RECOVERY] Test '" + testName + "' was INTERRUPTED by an Assembly Reload. Tasks were aborted.");
            }
#endif
        }

        private void SetRunning(bool running, string testName = "")
        {
            _running = running;
#if UNITY_EDITOR
            UnityEditor.SessionState.SetBool(Key_IsRunning, running);
            if (running) UnityEditor.SessionState.SetString(Key_LastTest, testName);
#endif
        }
    }
}