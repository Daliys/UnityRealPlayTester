using System;
using System.Threading.Tasks;
using UnityEngine;
using RealPlayTester.Core;
using RealPlayTester.Diagnostics;

namespace RealPlayTester.Await
{
    public static partial class Wait
    {
        public static async Task UntilWithDiagnostics(Func<bool> predicate, float timeout, string context = null)
        {
            if (!RealPlayEnvironment.IsEnabled || predicate == null) return;

            float startTime = Time.realtimeSinceStartup;
            var token = RealPlayExecutionContext.Token;
            
            while (!predicate())
            {
                token.ThrowIfCancellationRequested();
                if (Time.realtimeSinceStartup - startTime >= timeout)
                {
                    string predicateName = predicate.Method?.Name ?? "unknown";
                    string contextInfo = string.IsNullOrEmpty(context) ? "" : $" Context: {context}";
                    string message = $"Wait.UntilWithDiagnostics timed out after {timeout}s. Predicate: {predicateName}.{contextInfo}";
                    TestLog.Error(message);
                    throw new TimeoutException(message);
                }

                await Task.Yield();
            }
        }

        public static void Step(string label)
        {
            if (!RealPlayEnvironment.IsEnabled) return;
            TestLog.Info($"Test Step: {label}");
            TestRunContextTracker.UpdateAction(label);
        }
    }
}
