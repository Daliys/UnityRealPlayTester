using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RealPlayTester.Core
{
    public static class RealPlayEnvironment
    {
        public static bool IsEnabled
        {
            get
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                return true;
#else
                return false;
#endif
            }
        }

        public static string ProjectRoot
        {
            get
            {
#if UNITY_EDITOR
                return System.IO.Path.GetDirectoryName(Application.dataPath);
#else
                return Application.persistentDataPath;
#endif
            }
        }

        public static string TestReportsPath => System.IO.Path.Combine(ProjectRoot, "TestReports");
    }

    public static class RealPlayLog
    {
        private const string Prefix = "[RealPlayTester] ";

        public static void Info(string message)
        {
            Debug.Log(Prefix + message);
            if (Tester.Settings.MirrorLogsToStdout) System.Console.WriteLine(Prefix + message);
        }

        public static void Warn(string message)
        {
            Debug.LogWarning(Prefix + message);
            if (Tester.Settings.MirrorLogsToStdout) System.Console.WriteLine(Prefix + "WARN: " + message);
        }

        public static void Error(string message)
        {
            Debug.LogError(Prefix + message);
            if (Tester.Settings.MirrorLogsToStdout) System.Console.WriteLine(Prefix + "ERROR: " + message);
        }
    }

    [ExecuteAlways]
    public sealed class RealPlayTesterHost : MonoBehaviour
    {
        private static RealPlayTesterHost _instance;
        private static SynchronizationContext _mainContext;
        private static readonly object _initLock = new object();
        private static volatile bool _isInitialized = false;

        public static RealPlayTesterHost Instance
        {
            get
            {
                if (_instance == null) EnsureHost();
                return _instance;
            }
        }

        public static SynchronizationContext MainContext
        {
            get
            {
                if (_mainContext == null)
                {
                    _mainContext = SynchronizationContext.Current;
                    if (_mainContext == null && Application.isPlaying)
                    {
                        RealPlayLog.Warn("SynchronizationContext.Current is null. Attempting to capture from main thread.");
                    }
                }
                return _mainContext;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CaptureMainThread()
        {
            _mainContext = SynchronizationContext.Current;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            _instance = null;
            _mainContext = null;
            _isInitialized = false;
            RealPlayExecutionContext.Clear();
            
            // Cascade reset to all modules
            RealPlaySettings.Initialize(true);
            EventTracker.Clear();
            RealPlayTester.Diagnostics.TestRunContextTracker.EndTest(); // Clear context
            RealPlayTester.Diagnostics.LogInterceptor.Shutdown();
            RealPlayTester.Input.Internal.VirtualDeviceManager.CleanupDevices();
            RealPlayTester.Input.InputSystemShim.ClearEvents();
            
            RealPlayLog.Info("[HEALER] Global Static State Reset (SubsystemRegistration).");
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureHost()
        {
            if (_instance != null && _isInitialized) return;

            lock (_initLock)
            {
                if (_instance != null && _isInitialized) return;

                if (_mainContext == null)
                {
                    _mainContext = SynchronizationContext.Current;
                }

                RealPlaySettings.Initialize();
                ApplyGlobalSettings();
                CleanupReports();
                
                if (Application.isPlaying)
                {
                    FindOrCreateHostInstance();
                }
            }
        }

        private static void ApplyGlobalSettings()
        {
            RealPlaySettings.Initialize();
            if (RealPlaySettings.DisableLogStackTraces)
            {
                Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
                Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);
            }
        }

        private static void FindOrCreateHostInstance()
        {
            var existing = GameObject.Find(nameof(RealPlayTesterHost));
            if (existing != null)
            {
                _instance = existing.GetComponent<RealPlayTesterHost>();
            }

            if (_instance == null)
            {
                var go = new GameObject(nameof(RealPlayTesterHost));
                go.hideFlags = HideFlags.HideAndDontSave;
                _instance = go.AddComponent<RealPlayTesterHost>();
                if (Application.isPlaying)
                {
                    DontDestroyOnLoad(go);
                    InitializeRuntimeServices(_instance);
                }
            }
            else if (!_isInitialized)
            {
                InitializeRuntimeServices(_instance);
            }
        }

        private static void InitializeRuntimeServices(RealPlayTesterHost host)
        {
            if (_isInitialized) return;
            _isInitialized = true;

            host.gameObject.AddComponent<RealPlayTester.Utilities.SceneCache>().Initialize();
            host.gameObject.AddComponent<RealPlayTester.Input.VisualPointer>().Initialize();
            host.gameObject.AddComponent<RealPlayTester.Input.VisualAnchorManager>().Initialize();
            host.gameObject.AddComponent<RealPlayTester.Input.InteractionHeatmap>().Initialize();

            RealPlayTester.Diagnostics.LogInterceptor.Initialize();
            host.StartCoroutine(HeartbeatRoutine());
        }

        private static void CleanupReports()
        {
            try
            {
                string path = RealPlayEnvironment.TestReportsPath;
                if (!System.IO.Directory.Exists(path)) return;

                var di = new System.IO.DirectoryInfo(path);
                foreach (var file in di.GetFiles()) file.Delete();
                foreach (var dir in di.GetDirectories()) dir.Delete(true);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RealPlayTester] Failed to cleanup reports: {ex.Message}");
            }
        }

        private static IEnumerator HeartbeatRoutine()
        {
            var wait = new WaitForSecondsRealtime(5.0f); // CACHED
            while (true)
            {
                var context = RealPlayTester.Diagnostics.TestRunContextTracker.Current;
                if (context != null)
                {
                    RealPlayLog.Info($"[HEARTBEAT] Running: {context.Info.TestName} (Frame: {Time.frameCount})");
                }
                yield return wait;
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                RealPlayTester.Diagnostics.LogInterceptor.Shutdown();
            }
        }

        public Task RunCoroutineTask(IEnumerator routine, CancellationToken token)
        {
            var tcs = new TaskCompletionSource<bool>();
            var wrapped = WrapRoutine(routine, tcs, token);
            StartCoroutine(wrapped);
            return tcs.Task;
        }

        public void RunOnMainThread(Action action)
        {
            if (action == null) return;
            if (SynchronizationContext.Current == MainContext)
            {
                action();
                return;
            }
            MainContext.Post(_ => action(), null);
        }

        private IEnumerator WrapRoutine(IEnumerator routine, TaskCompletionSource<bool> tcs, CancellationToken token)
        {
            while (true)
            {
                if (token.IsCancellationRequested)
                {
                    tcs.TrySetCanceled(token);
                    yield break;
                }

                bool hasNext;
                object current;
                try
                {
                    hasNext = routine.MoveNext();
                    current = routine.Current;
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                    yield break;
                }

                if (!hasNext)
                {
                    tcs.TrySetResult(true);
                    yield break;
                }

                yield return current;
            }
        }
    }

    public static class RealPlayExecutionContext
    {
        private static readonly AsyncLocal<CancellationToken> _token = new AsyncLocal<CancellationToken>();
        public static CancellationToken Token => _token.Value;
        public static void SetToken(CancellationToken token) => _token.Value = token;
        public static void Clear() => _token.Value = CancellationToken.None;
    }
}