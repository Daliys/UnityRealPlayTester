using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RealPlayTester.Core
{
    internal static class RealPlayEnvironment
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
    }

    internal static class RealPlayLog
    {
        private const string Prefix = "[RealPlayTester] ";

        public static void Info(string message)
        {
            Debug.Log(Prefix + message);
        }

        public static void Warn(string message)
        {
            Debug.LogWarning(Prefix + message);
        }

        public static void Error(string message)
        {
            Debug.LogError(Prefix + message);
        }
    }

    [ExecuteAlways]
    public sealed class RealPlayTesterHost : MonoBehaviour
    {
        private static RealPlayTesterHost _instance;
        private static SynchronizationContext _mainContext;

        public static RealPlayTesterHost Instance
        {
            get
            {
                if (_instance == null)
                {
                    EnsureHost();
                }

                return _instance;
            }
        }

        public static SynchronizationContext MainContext
        {
            get
            {
                if (_mainContext == null)
                {
                    _mainContext = SynchronizationContext.Current ?? new SynchronizationContext();
                }

                return _mainContext;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureHost()
        {
            if (_instance != null)
            {
                return;
            }

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
                }
            }

            _mainContext = SynchronizationContext.Current ?? new SynchronizationContext();
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
            if (action == null)
            {
                return;
            }

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

    internal static class RealPlayExecutionContext
    {
        private static CancellationToken _token;

        public static CancellationToken Token
        {
            get { return _token; }
        }

        public static void SetToken(CancellationToken token)
        {
            _token = token;
        }

        public static void Clear()
        {
            _token = CancellationToken.None;
        }
    }
}
