using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using RealPlayTester.Diagnostics;

namespace RealPlayTester.Core
{
    public sealed partial class TestRunner
    {
        public async Task RunAll(bool quitOnFinish)
        {
            if (_running)
            {
                RealPlayLog.Warn("TestRunner is already running.");
                return;
            }

            _running = true;
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

            _running = false;
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
            var context = TestRunContextTracker.BeginTest(testCase.DisplayName, UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            var sw = System.Diagnostics.Stopwatch.StartNew();
            bool passed = false;
            string error = null;
            int attempt = 0;

            try
            {
                await PrepareEnvironment(testCase.Asset, token);
                (passed, error, attempt) = await ExecuteWithRetries(testCase, token);
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
                TestRunContextTracker.EndTest();
                NotifyProgress(testCase, passed, attempt, (float)sw.Elapsed.TotalSeconds);
            }

            return CreateResult(testCase, passed, (float)sw.Elapsed.TotalSeconds, error);
        }

        private async Task<(bool, string, int)> ExecuteWithRetries(TestCaseDescriptor testCase, CancellationToken token)
        {
            int attempt = 0;
            string error = null;
            RealPlayTest instance = ScriptableObject.Instantiate(testCase.Asset);
            ApplyTestData(instance, testCase);

            try
            {
                while (attempt <= maxRetries)
                {
                    attempt++;
                    try
                    {
                        RealPlayLog.Info($"Running: {testCase.DisplayName} (attempt {attempt})");
                        await instance.Execute();
                        return (true, null, attempt);
                    }
                    catch (Exception ex) when (attempt <= maxRetries && !token.IsCancellationRequested)
                    {
                        error = ex.ToString();
                        RealPlayLog.Warn($"Retrying {testCase.DisplayName} ({attempt}/{maxRetries + 1})");
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
            finally
            {
                ScriptableObject.Destroy(instance);
            }
            return (false, error, attempt);
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
