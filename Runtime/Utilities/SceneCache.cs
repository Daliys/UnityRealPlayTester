using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using RealPlayTester.Core;

namespace RealPlayTester.Utilities
{
    public class SceneCache : MonoBehaviour
    {
        private static SceneCache _instance;
        private static readonly object _cacheLock = new object();
        private static int _mainThreadId;

        public static SceneCache Instance
        {
            get
            {
                if (_instance == null)
                {
                    var host = RealPlayTesterHost.Instance;
                    if (host != null)
                    {
                        _instance = host.GetComponent<SceneCache>() ?? host.gameObject.AddComponent<SceneCache>();
                    }
                }
                return _instance;
            }
        }

        private List<GameObject> _allActiveObjects = new List<GameObject>();
        private List<GameObject> _allObjects = new List<GameObject>();
        private List<Rigidbody> _rigidbodies3D = new List<Rigidbody>();
        private List<Rigidbody2D> _rigidbodies2D = new List<Rigidbody2D>();
        private List<Animator> _animators = new List<Animator>();
        private List<ParticleSystem> _particleSystems = new List<ParticleSystem>();

        private int _lastRefreshFrame = -1;
        private float _lastRefreshTime = -1f;

        public IReadOnlyList<GameObject> AllActiveObjects { get { lock(_cacheLock) { RefreshIfStaleOnMain(); return _allActiveObjects; } } }
        public IReadOnlyList<GameObject> AllObjects { get { lock(_cacheLock) { RefreshIfStaleOnMain(); return _allObjects; } } }
        public IReadOnlyList<Rigidbody> Rigidbodies3D { get { lock(_cacheLock) { RefreshIfStaleOnMain(); return _rigidbodies3D; } } }
        public IReadOnlyList<Rigidbody2D> Rigidbodies2D { get { lock(_cacheLock) { RefreshIfStaleOnMain(); return _rigidbodies2D; } } }
        public IReadOnlyList<Animator> Animators { get { lock(_cacheLock) { RefreshIfStaleOnMain(); return _animators; } } }
        public IReadOnlyList<ParticleSystem> ParticleSystems { get { lock(_cacheLock) { RefreshIfStaleOnMain(); return _particleSystems; } } }

        public void Initialize()
        {
            _mainThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
            SceneManager.sceneLoaded += (s, m) => ForceRefreshDelayed();
            SceneManager.sceneUnloaded += (s) => ForceRefreshDelayed();
            ForceRefresh();
        }

        private async void ForceRefreshDelayed()
        {
            await Task.Yield();
            ForceRefresh();
        }

        public void ForceRefresh()
        {
            if (System.Threading.Thread.CurrentThread.ManagedThreadId == _mainThreadId)
            {
                lock (_cacheLock)
                {
                    _lastRefreshFrame = -1;
                    RefreshIfStale();
                }
            }
        }

        private void RefreshIfStaleOnMain()
        {
            if (System.Threading.Thread.CurrentThread.ManagedThreadId == _mainThreadId)
            {
                RefreshIfStale();
            }
        }

        private void RefreshIfStale()
        {
            if (Time.frameCount == _lastRefreshFrame && _lastRefreshFrame != -1) return;
            DoFullScan();
            _lastRefreshFrame = Time.frameCount;
            _lastRefreshTime = Time.realtimeSinceStartup;
        }

        private class ScanResults
        {
            public List<GameObject> active = new List<GameObject>();
            public List<GameObject> all = new List<GameObject>();
            public List<Rigidbody> rb3 = new List<Rigidbody>();
            public List<Rigidbody2D> rb2 = new List<Rigidbody2D>();
            public List<Animator> anim = new List<Animator>();
            public List<ParticleSystem> ps = new List<ParticleSystem>();
        }

        private void DoFullScan()
        {
            var next = new ScanResults();

            // O011: Use optimized native calls instead of manual recursion
            var all = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            
            foreach (var go in all)
            {
                if (go == null) continue;
                next.all.Add(go);

                if (!go.activeInHierarchy) continue;

                next.active.Add(go);

                if (go.TryGetComponent<Rigidbody>(out var r3)) next.rb3.Add(r3);
                if (go.TryGetComponent<Rigidbody2D>(out var r2)) next.rb2.Add(r2);
                if (go.TryGetComponent<Animator>(out var a)) next.anim.Add(a);
                if (go.TryGetComponent<ParticleSystem>(out var p)) next.ps.Add(p);
            }

            lock (_cacheLock)
            {
                _allActiveObjects = next.active;
                _allObjects = next.all;
                _rigidbodies3D = next.rb3;
                _rigidbodies2D = next.rb2;
                _animators = next.anim;
                _particleSystems = next.ps;
            }
        }

        // Remove CaptureRecursive as it's no longer used
    }
}
