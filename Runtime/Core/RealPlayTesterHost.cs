using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using RealPlayTester.Input;

namespace RealPlayTester.Core
{
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
                if (_instance != null && (_instance.gameObject == null)) EnsureHost();
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
            SimulatedInputGuard.InitThreadId();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            _instance = null;
            _mainContext = null;
            _isInitialized = false;
            RealPlayExecutionContext.Clear();
            
            // Clear main thread queue
            while (_mainThreadQueue != null && _mainThreadQueue.TryDequeue(out _)) { }

            RealPlaySettings.Initialize(true);
            EventTracker.Clear();
            RealPlayTester.Assert.FailureOverlay.Clear();
            RealPlayTester.Diagnostics.TestRunContextTracker.EndTest(); 
            RealPlayTester.Diagnostics.LogInterceptor.Shutdown();
            RealPlayTester.Input.Internal.VirtualDeviceManager.CleanupDevices();
            RealPlayTester.Input.InputSystemShim.ClearEvents();
            
            RealPlayLog.ResetInternalState();
            RealPlayLog.Info("[HEALER] Global Static State Reset (SubsystemRegistration).");
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureHost()
        {
            if (_instance != null && _isInitialized) return;

            lock (_initLock)
            {
                if (_instance != null && _isInitialized) return;
                if (_mainContext == null) _mainContext = SynchronizationContext.Current;

                RealPlaySettings.Initialize();
                ApplyGlobalSettings();
                CleanupReports();
                
                if (Application.isPlaying) FindOrCreateHostInstance();
            }
        }

        private static void ApplyGlobalSettings()
        {
            RealPlaySettings.Initialize();
            RealPlayLog.Silence = RealPlaySettings.SilenceLogs;
            if (RealPlaySettings.DisableLogStackTraces)
            {
                Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
                Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);
            }
        }

        private static void FindOrCreateHostInstance()
        {
            var all = FindObjectsByType<RealPlayTesterHost>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var h in all) { if (h != null) { _instance = h; break; } }

            if (_instance == null)
            {
                var go = new GameObject(nameof(RealPlayTesterHost)) { hideFlags = HideFlags.HideAndDontSave };
                _instance = go.AddComponent<RealPlayTesterHost>();
                DontDestroyOnLoad(go);
                InitializeRuntimeServices(_instance);
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
            catch (Exception ex) { Debug.LogWarning($"[RealPlayTester] Failed to cleanup reports: {ex.Message}"); }
        }

        private static IEnumerator HeartbeatRoutine()
        {
            var wait = new WaitForSecondsRealtime(5.0f); 
            while (true)
            {
                if (!RealPlayLog.Silence)
                {
                    var context = RealPlayTester.Diagnostics.TestRunContextTracker.Current;
                    if (context != null)
                        RealPlayLog.Info($"[HEARTBEAT] Running: {context.Info.TestName} (Frame: {Time.frameCount})");
                }
                yield return wait;
            }
        }

        private void OnDestroy() { if (_instance == this) RealPlayTester.Diagnostics.LogInterceptor.Shutdown(); }

        public Task RunCoroutineTask(IEnumerator routine, CancellationToken token)
        {
            var tcs = new TaskCompletionSource<bool>();
            StartCoroutine(WrapRoutine(routine, tcs, token));
            return tcs.Task;
        }

        private static readonly System.Collections.Concurrent.ConcurrentQueue<Action> _mainThreadQueue = new System.Collections.Concurrent.ConcurrentQueue<Action>();

        public void Update()
        {
            if (!RealPlayEnvironment.IsEnabled) return;

            int processedCount = 0;
            const int MaxActionsPerFrame = 50;

            while (processedCount < MaxActionsPerFrame && _mainThreadQueue.TryDequeue(out var action))
            {
                processedCount++;
                try { action?.Invoke(); }
                catch (Exception ex) { RealPlayLog.Error($"[DISPATCHER] Error executing action on main thread: {ex.Message}"); }
            }
        }

        internal static void ResetInternalState()
        {
            while (_mainThreadQueue.TryDequeue(out _)) { }
        }

        public void RunOnMainThread(Action action)
        {
            if (action == null) return;
            if (!RealPlayTester.Input.SimulatedInputGuard.IsThreadingSupported || System.Threading.Thread.CurrentThread.ManagedThreadId == SimulatedInputGuard.MainThreadId)
            {
                action();
                return;
            }
            _mainThreadQueue.Enqueue(action);
        }

        private IEnumerator WrapRoutine(IEnumerator routine, TaskCompletionSource<bool> tcs, CancellationToken token)
        {
            while (true)
            {
                if (token.IsCancellationRequested) { tcs.TrySetCanceled(token); yield break; }
                bool hasNext; object current;
                try { hasNext = routine.MoveNext(); current = routine.Current; }
                catch (Exception ex) { tcs.TrySetException(ex); yield break; }
                if (!hasNext) { tcs.TrySetResult(true); yield break; }
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