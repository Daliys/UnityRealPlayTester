using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using InputShim = RealPlayTester.Input.InputSystemShim;

namespace RealPlayTester.Core
{
    /// <summary>
    /// Discovers and runs all RealPlayTest assets. Triggered via F12 or -runRealTests CLI.
    /// Supports filtering, retries, JSON reporting, and progress callbacks.
    /// </summary>
    public sealed class TestRunner : MonoBehaviour
    {
        private const float DefaultTimeoutSeconds = 120f;
        [SerializeField] private int maxRetries = 0;
        private bool _running;
        private CancellationTokenSource _cts;
        private static TestRunner _instance;
        private List<string> _filterTags = new List<string>();

        // LogAssert Hooks
        private void OnEnable() => RealPlayTester.Assert.LogAssert.StartListening();
        private void OnDisable() => RealPlayTester.Assert.LogAssert.StopListening();

        /// <summary>Progress callback for each test completion.</summary>
        public event Action<TestProgressInfo> OnTestProgress;

        // ===== REPORT CUSTOMIZATION API =====
        
        /// <summary>
        /// Tier 1: Custom output path for the JSON report.
        /// Set to null to use default (TestReports/test-results.json).
        /// </summary>
        public static string ReportOutputPath { get; set; } = null;

        /// <summary>
        /// Tier 2: Event fired after report is generated. Receives the TestReport object.
        /// Use this for integrations (dashboards, CI/CD, notifications).
        /// </summary>
        public static event Action<TestReport> OnReportGenerated;

        /// <summary>
        /// Tier 2: Event fired with raw test results for custom processing.
        /// </summary>
        public static event Action<List<TestResult>> OnAllTestsCompleted;

        /// <summary>
        /// Tier 3: Set a custom handler to completely replace default report generation.
        /// When set, the default JSON file is NOT written unless your handler does it.
        /// </summary>
        public static ITestReportHandler CustomReportHandler { get; set; } = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (!RealPlayEnvironment.IsEnabled)
            {
                return;
            }

            var host = RealPlayTesterHost.Instance;
            _instance = host.gameObject.AddComponent<TestRunner>();
            _instance.HandleCommandLine();
        }

        private void Update()
        {
            if (!RealPlayEnvironment.IsEnabled)
            {
                return;
            }

            bool f9 = InputShim.IsAvailable ? InputShim.GetKeyDown(KeyCode.F9) : SafeLegacyKey(KeyCode.F9);
            if (f9)
            {
                RunAllAsyncSafe();
            }
        }

        private async void RunAllAsyncSafe()
        {
            try
            {
                await RunAll(false);
            }
            catch (Exception ex)
            {
                RealPlayLog.Error("TestRunner unhandled exception: " + ex);
            }
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("RealPlayTester/Run All (F9)")]
        private static void RunAllMenu()
        {
            if (_instance == null)
            {
                Bootstrap();
            }

            _ = _instance.RunAll(false);
        }
#endif

        private void HandleCommandLine(bool allowQuit = true)
        {
            string[] args = Environment.GetCommandLineArgs();
            ProcessArgs(args, allowQuit);
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor-only hook for tests to simulate command line processing without quitting.
        /// </summary>
        public void ProcessArgsForTests(string[] args)
        {
            ProcessArgs(args, false);
        }
#endif

        private void ProcessArgs(string[] args, bool allowQuit)
        {
            foreach (string arg in args)
            {
                if (arg.Equals("-runRealTests", StringComparison.OrdinalIgnoreCase))
                {
                    _ = RunAll(allowQuit);
                }
                if (arg.StartsWith("--tags=", StringComparison.OrdinalIgnoreCase))
                {
                    var tags = arg.Substring("--tags=".Length).Split(',');
                    _filterTags = new List<string>();
                    foreach (var t in tags)
                    {
                        if (!string.IsNullOrWhiteSpace(t))
                        {
                            _filterTags.Add(t.Trim().ToLowerInvariant());
                        }
                    }
                }
            }
        }

        public async Task RunAll(bool quitOnFinish)
        {
            if (_running)
            {
                RealPlayLog.Warn("TestRunner is already running.");
                return;
            }

            _running = true;
            _cts = new CancellationTokenSource();
            int failures = 0;
            var tests = BuildTestCases(Discover());
            RealPlayLog.Info("Starting RealPlayTester run. Test cases: " + tests.Count);
            var results = new List<TestResult>(tests.Count);

            foreach (var testCase in tests)
            {
                using (var perTestCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token))
                {
                    perTestCts.CancelAfter(TimeSpan.FromSeconds(DefaultTimeoutSeconds));
                    RealPlayExecutionContext.SetToken(perTestCts.Token);

                    if (_cts.IsCancellationRequested)
                    {
                        break;
                    }

                    var result = await RunSingleTest(testCase, perTestCts.Token);
                    results.Add(result);
                    if (!result.Passed)
                    {
                        failures++;
                    }
                }
            }

            RealPlayExecutionContext.Clear();
            _cts.Dispose();
            _running = false;

            GenerateJSONReport(results);

            if (quitOnFinish)
            {
                FinishAndQuit(failures);
            }
            else
            {
                Environment.ExitCode = failures;
                RealPlayLog.Info("RealPlayTester run complete. Failures: " + failures);
            }
        }

        public async Task RunTestsWithTag(string tag)
        {
            _filterTags = new List<string> { tag.ToLowerInvariant() };
            var filtered = BuildTestCases(Discover());
            await RunSelection(filtered);
        }

        public async Task RunTestByName(string testName)
        {
            var filteredAssets = Discover().Where(t => t != null && t.name.Equals(testName, StringComparison.OrdinalIgnoreCase)).ToList();
            var filtered = BuildTestCases(filteredAssets);
            await RunSelection(filtered);
        }

        private async Task RunSelection(List<TestCaseDescriptor> selection)
        {
            if (_running)
            {
                RealPlayLog.Warn("TestRunner is already running.");
                return;
            }

            _running = true;
            _cts = new CancellationTokenSource();
            int failures = 0;
            var results = new List<TestResult>(selection.Count);

            foreach (var testCase in selection)
            {
                using (var perTestCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token))
                {
                    perTestCts.CancelAfter(TimeSpan.FromSeconds(DefaultTimeoutSeconds));
                    RealPlayExecutionContext.SetToken(perTestCts.Token);

                    var result = await RunSingleTest(testCase, perTestCts.Token);
                    results.Add(result);
                    if (!result.Passed)
                    {
                        failures++;
                    }
                }
            }

            RealPlayExecutionContext.Clear();
            _cts.Dispose();
            _running = false;

            GenerateJSONReport(results);
            Environment.ExitCode = failures;
        }

        private async Task<TestResult> RunSingleTest(TestCaseDescriptor testCase, CancellationToken token)
        {
            RealPlayTest asset = testCase.Asset;
            RealPlayTest instance = ScriptableObject.Instantiate(asset);
            instance.Initialize(token);
            ApplyTestData(instance, testCase);
            var sw = Stopwatch.StartNew();
            bool passed = false;
            string error = null;
            int attempt = 0;

            try
            {
                do
                {
                    attempt++;
                    try
                    {
                        RealPlayLog.Info("Running test: " + testCase.DisplayName + $" (attempt {attempt})");
                        await instance.Execute();
                        passed = true;
                        break;
                    }
                    catch (Exception retryEx)
                    {
                        error = retryEx.ToString();
                        if (attempt > maxRetries)
                        {
                            throw;
                        }
                        RealPlayLog.Warn($"Retrying test {testCase.DisplayName}, attempt {attempt}/{maxRetries + 1}");
                    }
                } while (attempt <= maxRetries);
            }
            catch (OperationCanceledException)
            {
                error = "Timeout";
                RealPlayLog.Error("Failed (timeout): " + testCase.DisplayName);
            }
            catch (Exception ex)
            {
                error = ex.ToString();
                
                // Append Hierarchy Dump for context
                var dump = RealPlayTester.Utilities.VisualTreeLogger.DumpHierarchy();
                if (!string.IsNullOrEmpty(dump))
                {
                     error += "\n\n[Hierarchy Dump]\n" + dump;
                }

                RealPlayLog.Error("Failed: " + testCase.DisplayName + " -> " + ex);
            }
            finally
            {
                sw.Stop();
                OnTestProgress?.Invoke(new TestProgressInfo
                {
                    TestName = testCase.DisplayName,
                    Passed = passed,
                    Attempt = attempt,
                    DurationSeconds = (float)sw.Elapsed.TotalSeconds
                });

                ScriptableObject.Destroy(instance);
                RealPlayExecutionContext.SetToken(_cts.Token);
            }

            return new TestResult
            {
                Name = testCase.DisplayName,
                Passed = passed,
                DurationSeconds = (float)sw.Elapsed.TotalSeconds,
                ErrorMessage = error,
                ScreenshotPath = string.Empty
            };
        }

        private static void FinishAndQuit(int failures)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(failures);
#else
            Application.Quit(failures);
#endif
        }

        private static List<RealPlayTest> Discover()
        {
            var list = new List<RealPlayTest>();
            var resources = Resources.LoadAll<RealPlayTest>("RealPlayTests");
            list.AddRange(resources);

#if UNITY_EDITOR
            var assets = UnityEditor.AssetDatabase.FindAssets("t:RealPlayTest");
            foreach (string guid in assets)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<RealPlayTest>(path);
                if (asset != null && !list.Contains(asset))
                {
                    list.Add(asset);
                }
            }
#endif

            return list;
        }

        private static bool SafeLegacyKey(KeyCode key)
        {
            try
            {
                return UnityEngine.Input.GetKeyDown(key);
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        private void GenerateJSONReport(List<TestResult> results)
        {
            // Build report object
            var report = new TestReport
            {
                totalTests = results.Count,
                passed = results.Count(r => r.Passed),
                failed = results.Count(r => !r.Passed),
                duration = results.Sum(r => r.DurationSeconds),
                results = results
            };

            // Tier 2: Fire events first
            try
            {
                OnAllTestsCompleted?.Invoke(results);
                OnReportGenerated?.Invoke(report);
            }
            catch (Exception ex)
            {
                RealPlayLog.Warn("Report event handler error: " + ex.Message);
            }

            // Tier 3: Custom handler completely replaces default
            if (CustomReportHandler != null)
            {
                try
                {
                    CustomReportHandler.HandleReport(report, results);
                    RealPlayLog.Info("Custom report handler executed.");
                }
                catch (Exception ex)
                {
                    RealPlayLog.Warn("Custom report handler error: " + ex.Message);
                }
                return; // Skip default JSON write
            }

            // Default: Write JSON to file
            try
            {
                // Tier 1: Use custom path or default
                string path = string.IsNullOrEmpty(ReportOutputPath)
                    ? Path.Combine(RealPlayEnvironment.TestReportsPath, "test-results.json")
                    : ReportOutputPath;

                // Ensure directory exists
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                File.WriteAllText(path, JsonUtility.ToJson(report, true));
                RealPlayLog.Info("Wrote test report to " + path);
            }
            catch (Exception ex)
            {
                RealPlayLog.Warn("Failed to write test report: " + ex.Message);
            }
        }

        private List<TestCaseDescriptor> BuildTestCases(List<RealPlayTest> assets)
        {
            var cases = new List<TestCaseDescriptor>();
            foreach (var asset in assets)
            {
                var tags = asset.GetType().GetCustomAttributes(typeof(TestTagAttribute), true).Cast<TestTagAttribute>().Select(t => t.Tag.ToLowerInvariant()).ToList();
                if (_filterTags.Count > 0 && !tags.Any(t => _filterTags.Contains(t)))
                {
                    continue;
                }

                var dataAttrs = asset.GetType().GetCustomAttributes(typeof(TestDataAttribute), true).Cast<TestDataAttribute>().ToList();
                if (dataAttrs.Count == 0)
                {
                    cases.Add(new TestCaseDescriptor(asset, null, null));
                }
                else
                {
                    foreach (var dataAttr in dataAttrs)
                    {
                        foreach (var data in dataAttr.Data)
                        {
                            cases.Add(new TestCaseDescriptor(asset, data, dataAttr.PropertyName));
                        }
                    }
                }
            }

            return cases;
        }

        private void ApplyTestData(RealPlayTest instance, TestCaseDescriptor testCase)
        {
            if (testCase.Data == null)
            {
                return;
            }

            string propName = testCase.DataPropertyName;
            var type = instance.GetType();
            PropertyInfo targetProp = null;

            if (!string.IsNullOrEmpty(propName))
            {
                targetProp = type.GetProperty(propName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            }

            if (targetProp == null)
            {
                targetProp = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .FirstOrDefault(p => p.CanWrite && p.PropertyType.IsInstanceOfType(testCase.Data));
            }

            if (targetProp != null && targetProp.CanWrite)
            {
                targetProp.SetValue(instance, testCase.Data);
            }
        }
    }

    [Serializable]
    public struct TestResult
    {
        public string Name;
        public bool Passed;
        public float DurationSeconds;
        public string ErrorMessage;
        public string ScreenshotPath;
    }

    [Serializable]
    public struct TestReport
    {
        public int totalTests;
        public int passed;
        public int failed;
        public float duration;
        public List<TestResult> results;
    }

    public struct TestProgressInfo
    {
        public string TestName;
        public bool Passed;
        public int Attempt;
        public float DurationSeconds;
    }

    [Serializable]
    public sealed class TestTagAttribute : Attribute
    {
        public string Tag { get; private set; }

        public TestTagAttribute(string tag)
        {
            Tag = tag ?? string.Empty;
        }
    }

    [Serializable]
    public sealed class TestDataAttribute : Attribute
    {
        public object[] Data { get; private set; }
        public string PropertyName { get; private set; }

        public TestDataAttribute(params object[] data)
        {
            Data = data ?? Array.Empty<object>();
            PropertyName = null;
        }

        public TestDataAttribute(string propertyName, params object[] data)
        {
            PropertyName = propertyName;
            Data = data ?? Array.Empty<object>();
        }
    }

    internal readonly struct TestCaseDescriptor
    {
        public readonly RealPlayTest Asset;
        public readonly object Data;
        public readonly string DataPropertyName;

        public string DisplayName
        {
            get
            {
                if (Data == null)
                {
                    return Asset.name;
                }

                return Asset.name + " [" + Data + "]";
            }
        }

        public TestCaseDescriptor(RealPlayTest asset, object data, string dataPropertyName)
        {
            Asset = asset;
            Data = data;
            DataPropertyName = dataPropertyName;
        }
    }
}
