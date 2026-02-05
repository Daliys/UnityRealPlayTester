using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using RealPlayTester.Diagnostics;

namespace RealPlayTester.Core
{
    public class FatalAutomationException : Exception
    {
        public FatalAutomationException(string message) : base(message) { }
    }

    public sealed partial class TestRunner
    {
        public async Task RunAll(bool quitOnFinish)
        {
            if (_running)
            {
                RealPlayLog.Warn("TestRunner is already running.");
                return;
            }

            SetRunning(true, "FullSuite");
            _cts = new CancellationTokenSource();

            var tests = BuildTestCases(Discover());
            RealPlayLog.Info("Starting RealPlayTester run. Test cases: " + tests.Count);
            var results = new List<TestResult>(tests.Count);

            foreach (var testCase in tests)
            {
                if (_cts.Token.IsCancellationRequested) break;
                var result = await RunTestWithTimeout(testCase);
                results.Add(result);
            }

            SetRunning(false);
            OnAllTestsCompleted?.Invoke(results);
            GenerateJSONReport(results);

            int failures = results.Count(r => !r.Passed);
            if (quitOnFinish) FinishAndQuit(failures);
            else Environment.ExitCode = failures;
        }

        private async Task<TestResult> RunTestWithTimeout(TestCaseDescriptor testCase)
        {
            using (var perTestCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token))
            {
                perTestCts.CancelAfter(TimeSpan.FromSeconds(DefaultTimeoutSeconds));
                RealPlayExecutionContext.SetToken(perTestCts.Token);
                try
                {
                    return await RunSingleTest(testCase, perTestCts.Token);
                }
                finally
                {
                    perTestCts.Cancel();
                    RealPlayExecutionContext.Clear();
                }
            }
        }

        public async Task<TestResult> RunSingleTest(TestCaseDescriptor testCase, CancellationToken token)
        {
            SetRunning(true, testCase.DisplayName);
            var context = TestRunContextTracker.BeginTest(testCase.DisplayName, UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            var sw = System.Diagnostics.Stopwatch.StartNew();
            bool passed = false;
            string error = null;
            int attempt = 0;

            RealPlayTest instance = CreateProtectedInstance(testCase);

            try
            {
                if (instance == null) throw new Exception("Test asset is null or was unloaded/destroyed before execution.");

                await HardReset.Execute();
                await PrepareEnvironment(testCase.Asset, token);
                (passed, error, attempt) = await ExecuteWithInstance(instance, testCase, token);
            }
            catch (Exception ex)
            {
                error = ex.ToString();
                RealPlayLog.Error($"Test failed: {testCase.DisplayName} -> {ex}");
                passed = false;
            }
            finally
            {
                sw.Stop();
                if (instance != null) ScriptableObject.Destroy(instance);
                CleanupTestRun(testCase, passed, attempt, (float)sw.Elapsed.TotalSeconds);
            }

            return CreateResult(testCase, passed, (float)sw.Elapsed.TotalSeconds, error);
        }

        private RealPlayTest CreateProtectedInstance(TestCaseDescriptor testCase)
        {
            if (testCase.Asset == null) return null;
            var instance = ScriptableObject.Instantiate(testCase.Asset);
            instance.hideFlags |= HideFlags.HideAndDontSave;
            return instance;
        }

        private void CleanupTestRun(TestCaseDescriptor testCase, bool passed, int attempt, float duration)
        {
            TestRunContextTracker.EndTest();
            NotifyProgress(testCase, passed, attempt, duration);
            if (!_running) SetRunning(false);
        }

        private async Task<(bool, string, int)> ExecuteWithInstance(RealPlayTest instance, TestCaseDescriptor testCase, CancellationToken token)
        {
            int attempt = 0;
            string error = null;
            ApplyTestData(instance, testCase);

            // H062: Clamp retries to valid range to prevent infinite loops from reflection
            int safeRetries = Mathf.Clamp(maxRetries, 0, 100);

            try
            {
                while (attempt <= safeRetries)
                {
                    attempt++;
                    try
                    {
                        RealPlayLog.Info($"Running: {testCase.DisplayName} (attempt {attempt})");
                        await instance.Execute();

                        if (RealPlayEnvironment.GlobalDisable)
                        {
                            throw new FatalAutomationException("Automation was GLOBALLY DISABLED during test execution (likely due to a thread violation).");
                        }

                        return (true, null, attempt);
                    }
                    catch (Exception ex) when (attempt <= safeRetries && !token.IsCancellationRequested)
                    {
                        error = ex.ToString();
                        RealPlayLog.Warn($"Retrying {testCase.DisplayName} ({attempt}/{safeRetries + 1})");
                        await HardReset.Execute();
                        await PrepareEnvironment(testCase.Asset, token);
                    }
                }
            }
            catch (Exception finalEx)
            {
                error = finalEx.ToString();
                await HandleFailure(testCase, TestRunContextTracker.Current, error);
            }
            return (false, error, attempt);
        }

        private async Task<(bool, string, int)> ExecuteWithRetries(TestCaseDescriptor testCase, CancellationToken token)
        {
            // This is now legacy/internal wrapper or can be removed if unused.
            // For now, let's keep it but redirect or just rely on RunSingleTest.
            var inst = ScriptableObject.Instantiate(testCase.Asset);
            try { return await ExecuteWithInstance(inst, testCase, token); }
            finally { ScriptableObject.Destroy(inst); }
        }

        private async Task HandleFailure(TestCaseDescriptor testCase, TestRunContext context, string error)
        {
            var dump = RealPlayTester.Utilities.VisualTreeLogger.DumpHierarchy();
            await FailureBundleWriter.WriteFailureBundleAsync(testCase.DisplayName, context, null, dump);
            _ = HardReset.Execute();
        }

        private void NotifyProgress(TestCaseDescriptor testCase, bool passed, int attempt, float duration)
        {
            OnTestProgress?.Invoke(new TestProgressInfo { TestName = testCase.DisplayName, Passed = passed, Attempt = attempt, DurationSeconds = duration });
        }

        private TestResult CreateResult(TestCaseDescriptor testCase, bool passed, float duration, string error)
        {
            return new TestResult { Name = testCase.DisplayName, Passed = passed, DurationSeconds = duration, ErrorMessage = error, ScreenshotPath = string.Empty };
        }
    }
}