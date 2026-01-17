using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using RealPlayTester.Core;
using RealPlayTester.Utilities;
using NUnitAssert = NUnit.Framework.Assert;

namespace RealPlayTester.Tests.Verification
{
    public class SceneCacheTests
    {
        private GameObject _testRoot;

        [SetUp]
        public void Setup()
        {
            _testRoot = new GameObject("SceneCacheTestRoot");
            // Ensure Host and Cache exist via the main Instance entry points
            var host = RealPlayTesterHost.Instance;
            NUnitAssert.IsNotNull(host, "Host should exist");
            var cache = SceneCache.Instance;
            NUnitAssert.IsNotNull(cache, "Cache should exist");
        }

        [TearDown]
        public void Teardown()
        {
            Object.DestroyImmediate(_testRoot);
        }

        [UnityTest]
        public IEnumerator SceneCache_MaintainsActiveObjects()
        {
            var cache = SceneCache.Instance;
            cache.ForceRefresh();
            
            int initialCount = cache.AllActiveObjects.Count;
            
            var go = new GameObject("NewObject");
            go.transform.SetParent(_testRoot.transform);
            
            // Should not update immediately due to throttling (MinRefreshInterval = 0.5s)
            NUnitAssert.AreEqual(initialCount, cache.AllActiveObjects.Count, "Cache should be throttled and not update immediately");
            
            // Force update
            cache.ForceRefresh();
            NUnitAssert.AreEqual(initialCount + 1, cache.AllActiveObjects.Count, "Cache should update after ForceRefresh");
            yield return null;
        }

        [UnityTest]
        public IEnumerator SceneCache_TracksSpecificComponents()
        {
            var cache = SceneCache.Instance;
            
            var rbGo = new GameObject("RB", typeof(Rigidbody));
            rbGo.transform.SetParent(_testRoot.transform);
            
            var animGo = new GameObject("Anim", typeof(Animator));
            animGo.transform.SetParent(_testRoot.transform);
            
            cache.ForceRefresh();
            
            NUnitAssert.IsTrue(ContainsComponent(cache.Rigidbodies3D, rbGo.GetComponent<Rigidbody>()), "Should track Rigidbody");
            NUnitAssert.IsTrue(ContainsComponent(cache.Animators, animGo.GetComponent<Animator>()), "Should track Animator");
            yield return null;
        }

        private bool ContainsComponent<T>(IReadOnlyList<T> list, T item) where T : Component
        {
            if (list == null) return false;
            foreach (var x in list) if (x == item) return true;
            return false;
        }
    }
}