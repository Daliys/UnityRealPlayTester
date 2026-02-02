using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using InputShim = RealPlayTester.Input.InputSystemShim;
using RealPlayTester.Diagnostics;

namespace RealPlayTester.Core
{
    /// <summary>
    /// Discovers and runs all RealPlayTest assets. Triggered via F12 or -runRealTests CLI.
    /// Supports filtering, retries, JSON reporting, and progress callbacks.
    /// </summary>
    public sealed partial class TestRunner : MonoBehaviour, ISerializationCallbackReceiver
    {
        private const float DefaultTimeoutSeconds = 120f;
        [SerializeField] private int maxRetries = 0;
        private bool _running;
        private CancellationTokenSource _cts;
        private static TestRunner _instance;
        public static TestRunner Instance => _instance;
        private List<string> _filterTags = new List<string>();

        /// <summary>Progress callback for each test completion.</summary>
        public event Action<TestProgressInfo> OnTestProgress;

        public static string ReportOutputPath { get; set; } = null;
        public static event Action<TestReport> OnReportGenerated;
        public static event Action<List<TestResult>> OnAllTestsCompleted;
        public static ITestReportHandler CustomReportHandler { get; set; } = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (!RealPlayEnvironment.IsEnabled) return;

            Application.runInBackground = true;
            Application.targetFrameRate = 60;
            InputShim.InitializeDevices();

            var host = RealPlayTesterHost.Instance;
            if (_instance == null) _instance = host.gameObject.AddComponent<TestRunner>();
            _instance.HandleCommandLine();
        }

        private void Update()
        {
            if (!RealPlayEnvironment.IsEnabled) return;

            if (Tester.Settings.EnableInputHeartbeat)
            {
                InputShim.UpdateInput();
            }

            bool f9 = InputShim.IsAvailable ? InputShim.GetKeyDown(KeyCode.F9) : SafeLegacyKey(KeyCode.F9);
            if (f9) RunAllAsyncSafe();
        }

        private async void RunAllAsyncSafe()
        {
            try { await RunAll(false); }
            catch (Exception ex) { RealPlayLog.Error("TestRunner unhandled exception: " + ex); }
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("RealPlayTester/Run All (F9)")]
        private static void RunAllMenu()
        {
            if (_instance == null) Bootstrap();
            _ = _instance.RunAll(false);
        }
#endif

        private void HandleCommandLine(bool allowQuit = true)
        {
            string[] args = Environment.GetCommandLineArgs();
            ProcessArgs(args, allowQuit);
        }

        private void ProcessArgs(string[] args, bool allowQuit)
        {
            foreach (string arg in args)
            {
                if (arg.Equals("-runRealTests", StringComparison.OrdinalIgnoreCase)) _ = RunAll(allowQuit);
                if (arg.StartsWith("--tags=", StringComparison.OrdinalIgnoreCase))
                {
                    var tags = arg.Substring("--tags=".Length).Split(',');
                    _filterTags = tags.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim().ToLowerInvariant()).ToList();
                }
                if (arg.Equals("-realplay-stdout", StringComparison.OrdinalIgnoreCase))
                {
                    Tester.Settings.MirrorLogsToStdout = true;
                    RealPlayLog.Info("MirrorLogsToStdout enabled via CLI.");
                }
                if (arg.Equals("-realplay-silence", StringComparison.OrdinalIgnoreCase))
                {
                    Tester.Settings.SilenceLogs = true;
                }
                if (arg.Equals("-realplay-compact", StringComparison.OrdinalIgnoreCase))
                {
                    Tester.Settings.CompactJSON = true;
                }
            }
        }

        public async Task RunTestsWithTag(string tag)
        {
            _filterTags = new List<string> { tag.ToLowerInvariant() };
            await RunAll(false);
        }

        public async Task RunTestByName(string testName)
        {
            var result = await RunSingleAssetTest(testName);
            if (result != null && !string.IsNullOrEmpty(result.Name))
            {
                GenerateJSONReport(new List<TestResult> { result });
            }
        }

        public async Task<TestResult> RunSingleAssetTest(string testName)
        {
            var asset = Discover().FirstOrDefault(t => t != null && t.name.Equals(testName, StringComparison.OrdinalIgnoreCase));
            if (asset == null)
            {
                RealPlayLog.Error($"Test asset not found: {testName}");
                return null;
            }

            var cases = BuildTestCases(new List<RealPlayTest> { asset });
            if (cases.Count == 0) return null;

            _cts = new CancellationTokenSource();
            return await RunSingleTest(cases[0], _cts.Token);
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
            var list = Resources.LoadAll<RealPlayTest>("RealPlayTests").ToList();
#if UNITY_EDITOR
            var assets = UnityEditor.AssetDatabase.FindAssets("t:RealPlayTest");
            foreach (string guid in assets)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<RealPlayTest>(path);
                if (asset != null && !list.Contains(asset)) list.Add(asset);
            }
#endif
            return list;
        }

        private static bool SafeLegacyKey(KeyCode key)
        {
            try { return UnityEngine.Input.GetKeyDown(key); }
            catch (InvalidOperationException) { return false; }
        }

        public async Task PrepareEnvironment(RealPlayTest instance, CancellationToken token)
        {
            var attr = instance.GetType().GetCustomAttribute<PlayModeTestAttribute>(true);
            if (attr == null) return;

            if (!string.IsNullOrEmpty(attr.Scene) && UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != attr.Scene)
            {
                RealPlayLog.Info($"[FRAMEWORK] Loading scene: {attr.Scene}");
                RealPlayTester.Diagnostics.TestRunContextTracker.RecordBreadcrumb("Scene", $"Loading scene: {attr.Scene}");
                var op = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(attr.Scene);
                while (op != null && !op.isDone)
                {
                    await Task.Yield();
                    if (token.IsCancellationRequested) return;
                }
                await Task.Yield();
            }

            Time.timeScale = attr.TimeScale;
            if (attr.WaitForDI) await WaitForDIContainer(token);
        }

        private async Task WaitForDIContainer(CancellationToken token)
        {
            RealPlayLog.Info("[FRAMEWORK] Waiting for DI Container...");
            float start = Time.realtimeSinceStartup;
            while (GameObject.Find("CompositionRoot") == null && Time.realtimeSinceStartup - start < 10f)
            {
                await Task.Yield();
                if (token.IsCancellationRequested) return;
            }
        }

        private List<TestCaseDescriptor> BuildTestCases(List<RealPlayTest> assets)
        {
            var cases = new List<TestCaseDescriptor>();
            foreach (var asset in assets)
            {
                var tags = asset.GetType().GetCustomAttributes(typeof(TestTagAttribute), true).Cast<TestTagAttribute>().Select(t => t.Tag.ToLowerInvariant()).ToList();
                if (_filterTags.Count > 0 && !tags.Any(t => _filterTags.Contains(t))) continue;

                var dataAttrs = asset.GetType().GetCustomAttributes(typeof(TestDataAttribute), true).Cast<TestDataAttribute>().ToList();
                if (dataAttrs.Count == 0) cases.Add(new TestCaseDescriptor(asset, null, null));
                else foreach (var attr in dataAttrs) foreach (var d in attr.Data) cases.Add(new TestCaseDescriptor(asset, d, attr.PropertyName));
            }
            return cases;
        }

        private void ApplyTestData(RealPlayTest instance, TestCaseDescriptor testCase)
        {
            if (testCase.Data == null) return;
            var type = instance.GetType();
            var prop = string.IsNullOrEmpty(testCase.DataPropertyName) 
                ? type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).FirstOrDefault(p => p.CanWrite && p.PropertyType.IsInstanceOfType(testCase.Data))
                : type.GetProperty(testCase.DataPropertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            
            if (prop != null && prop.CanWrite) prop.SetValue(instance, testCase.Data);
        }
    }

    [Serializable]
    public class TestResult
    {
        public string Name;
        public bool Passed;
        public float DurationSeconds;
        public string ErrorMessage;
        public string ScreenshotPath;
    }

    [Serializable]
    public class TestReport
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
        public TestTagAttribute(string tag) { Tag = tag ?? string.Empty; }
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

    public class TestCaseDescriptor
    {
        public readonly RealPlayTest Asset;
        public readonly object Data;
        public readonly string DataPropertyName;
        public readonly string DisplayName;

        public TestCaseDescriptor(RealPlayTest asset, object data, string dataPropertyName)
        {
            Asset = asset;
            Data = data;
            DataPropertyName = dataPropertyName;
            DisplayName = Data == null ? (asset != null ? asset.name : "NullAsset") : $"{(asset != null ? asset.name : "NullAsset")} [{Data}]";
        }
    }
}
