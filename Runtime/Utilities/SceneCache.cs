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

        private readonly List<GameObject> _allActiveObjects = new List<GameObject>();
        private readonly List<Rigidbody> _rigidbodies3D = new List<Rigidbody>();
        private readonly List<Rigidbody2D> _rigidbodies2D = new List<Rigidbody2D>();
        private readonly List<Animator> _animators = new List<Animator>();
        private readonly List<ParticleSystem> _particleSystems = new List<ParticleSystem>();

        private int _lastRefreshFrame = -1;
        private float _lastRefreshTime = -1f;

        public IReadOnlyList<GameObject> AllActiveObjects => GetOrRefresh(_allActiveObjects);
        public IReadOnlyList<Rigidbody> Rigidbodies3D => GetOrRefresh(_rigidbodies3D);
        public IReadOnlyList<Rigidbody2D> Rigidbodies2D => GetOrRefresh(_rigidbodies2D);
        public IReadOnlyList<Animator> Animators => GetOrRefresh(_animators);
        public IReadOnlyList<ParticleSystem> ParticleSystems => GetOrRefresh(_particleSystems);

        public void Initialize()
        {
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
            _lastRefreshFrame = -1;
            RefreshIfStale();
        }

        private T GetOrRefresh<T>(T list) where T : class
        {
            RefreshIfStale();
            return list;
        }

        private void RefreshIfStale()
        {
            if (Time.frameCount == _lastRefreshFrame && _lastRefreshFrame != -1) return;
            DoFullScan();
            _lastRefreshFrame = Time.frameCount;
            _lastRefreshTime = Time.realtimeSinceStartup;
        }

        private void DoFullScan()
        {
            _allActiveObjects.Clear();
            _rigidbodies3D.Clear();
            _rigidbodies2D.Clear();
            _animators.Clear();
            _particleSystems.Clear();

            var scenes = new List<Scene>();
            for(int i=0; i<SceneManager.sceneCount; i++) scenes.Add(SceneManager.GetSceneAt(i));
            
            foreach(var scene in scenes)
            {
                if (!scene.isLoaded) continue;
                foreach(var root in scene.GetRootGameObjects())
                {
                    if (root != null) CaptureRecursive(root.transform);
                }
            }
        }

        private void CaptureRecursive(Transform t)
        {
            var go = t.gameObject;
            if (go == null || !go.activeInHierarchy) return;

            _allActiveObjects.Add(go);

            var rb3 = go.GetComponent<Rigidbody>();
            if (rb3 != null) _rigidbodies3D.Add(rb3);

            var rb2 = go.GetComponent<Rigidbody2D>();
            if (rb2 != null) _rigidbodies2D.Add(rb2);

            var anim = go.GetComponent<Animator>();
            if (anim != null) _animators.Add(anim);

            var ps = go.GetComponent<ParticleSystem>();
            if (ps != null) _particleSystems.Add(ps);

            for (int i = 0; i < t.childCount; i++) CaptureRecursive(t.GetChild(i));
        }
    }
}
