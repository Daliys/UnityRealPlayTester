using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using RealPlayTester.Core;
using RPA = RealPlayTester.Assert.Assert;
using NAssert = NUnit.Framework.Assert;
using UnityEngine.Assertions;

namespace RealPlayTester.Tests.Verification
{
    public class OcclusionVerificationTests
    {
        private GameObject _cameraObj;
        private GameObject _targetObj;
        private GameObject _wallObj;
        private GameObject _lightObj;

        [SetUp]
        public void Setup()
        {
            Time.timeScale = 1.0f;
            
            // Setup Camera
            _cameraObj = new GameObject("MainCamera");
            var cam = _cameraObj.AddComponent<Camera>();
            _cameraObj.tag = "MainCamera";
            _cameraObj.transform.position = new Vector3(0, 0, -10);
            _cameraObj.transform.LookAt(Vector3.zero);

            // Setup Light (so we aren't dark)
            _lightObj = new GameObject("Light");
            var l = _lightObj.AddComponent<Light>();
            l.type = LightType.Directional;

            // Setup Target (The object we want to see)
            _targetObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _targetObj.name = "TargetCube";
            _targetObj.transform.position = Vector3.zero; // At (0,0,0)

            // Setup Wall (The object blocking the view)
            _wallObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _wallObj.name = "Wall";
            _wallObj.transform.position = new Vector3(0, 0, -5); // Between camera (-10) and target (0)
            _wallObj.transform.localScale = new Vector3(5, 5, 1); // Make it big enough to hide the target
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_cameraObj) Object.Destroy(_cameraObj);
            if (_targetObj) Object.Destroy(_targetObj);
            if (_wallObj) Object.Destroy(_wallObj);
            if (_lightObj) Object.Destroy(_lightObj);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Visuals_IsVisible_BlockedByWall_ShouldFail()
        {
            // Allow a frame for updates
            yield return null;

            // The Target is Active and In Frustum, BUT it is behind a wall.
            // A human cannot see it.
            // Therefore, IsVisible SHOULD throw an assertion exception.
            
            // Current Behavior expectation: This will FAIL (The test will fail)
            // because RPA.IsVisible uses renderer.isVisible (Frustum check) or just active check in batchmode.
            // It does not perform a Raycast/Occlusion check.
            
            Debug.Log("Testing Occlusion: Expecting RPA.IsVisible to throw because object is hidden by wall.");
            
            NAssert.Throws<UnityEngine.Assertions.AssertionException>(() => RPA.IsVisible(_targetObj), 
                "RPA.IsVisible passed (said object is visible) even though it is behind a wall! This highlights the lack of occlusion checking.");
        }
    }
}
