using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using InputShim = RealPlayTester.Input.InputSystemShim;

namespace RealPlayTester.Core
{
    /// <summary>
    /// Discovers and runs all RealPlayTest assets. Triggered via F12 or -runRealTests CLI.
    /// </summary>
    public sealed class TestRunner : MonoBehaviour
    {
        private const float DefaultTimeoutSeconds = 120f;
        private bool _running;
        private CancellationTokenSource _cts;
        private static TestRunner _instance;

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

            bool f12 = InputShim.IsAvailable ? InputShim.GetKeyDown(KeyCode.F12) : SafeLegacyKey(KeyCode.F12);
            if (f12)
            {
                _ = RunAll(false);
            }
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("RealPlayTester/Run All (F12)")]
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
                    break;
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
            var tests = Discover();
            RealPlayLog.Info("Starting RealPlayTester run. Tests found: " + tests.Count);

            foreach (var asset in tests)
            {
                using (var perTestCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token))
                {
                    perTestCts.CancelAfter(TimeSpan.FromSeconds(DefaultTimeoutSeconds));
                    RealPlayExecutionContext.SetToken(perTestCts.Token);

                    if (_cts.IsCancellationRequested)
                    {
                        break;
                    }

                    RealPlayTest instance = ScriptableObject.Instantiate(asset);
                    instance.Initialize(perTestCts.Token);
                    try
                    {
                        RealPlayLog.Info("Running test: " + asset.name);
                        await instance.Execute();
                        RealPlayLog.Info("Passed: " + asset.name);
                    }
                    catch (OperationCanceledException) when (perTestCts.IsCancellationRequested)
                    {
                        failures++;
                        RealPlayLog.Error("Failed (timeout): " + asset.name);
                    }
                    catch (Exception ex)
                    {
                        failures++;
                        RealPlayLog.Error("Failed: " + asset.name + " -> " + ex);
                    }
                    finally
                    {
                        ScriptableObject.Destroy(instance);
                        RealPlayExecutionContext.SetToken(_cts.Token);
                    }
                }
            }

            RealPlayExecutionContext.Clear();
            _cts.Dispose();
            _running = false;

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
            var resources = Resources.LoadAll<RealPlayTest>(string.Empty);
            list.AddRange(resources);

#if UNITY_EDITOR
            // Editor-only fallback using AssetDatabase for Addressables or non-Resources assets.
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
    }
}
