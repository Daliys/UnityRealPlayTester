using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using RealPlayTester.Core;
using RealPlayTester.Assert;
using NUnitAssert = NUnit.Framework.Assert;

namespace RealPlayTester.Tests.Verification
{
    public class LogicAssertionTests
    {
        // Test 7: Assert.InRange
        [Test]
        public void Assert_InRange_ValidatesCorrectly()
        {
            RealPlayTester.Assert.Assert.InRange(5, 0, 10);
            RealPlayTester.Assert.Assert.InRange(0.5f, 0f, 1f);
        }

        // Test 8: Assert.IsNull / IsNotNull
        [Test]
        public void Assert_NullChecks_Work()
        {
            RealPlayTester.Assert.Assert.IsNull(null);
            RealPlayTester.Assert.Assert.IsNotNull(new object());
        }

        // Test 9: Assert.Contains
        [Test]
        public void Assert_Contains_ValidatesString()
        {
            RealPlayTester.Assert.Assert.Contains("Hello World", "World");
        }

        // Test 10: Assert.Throws
        [Test]
        public void Assert_Throws_DetectsException()
        {
            RealPlayTester.Assert.Assert.Throws<System.DivideByZeroException>(() => {
                int x = 0;
                int y = 10 / x;
            });
        }

        // Test 11: Assert.NoMissingMaterials (Negative)
        [UnityTest]
        public IEnumerator Assert_NoMissingMaterials_DetectsIssue()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            var renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = null; // Forces failure

            bool caught = false;
            try { RealPlayTester.Assert.Assert.NoMissingMaterials("Material is missing"); }
            catch (UnityEngine.Assertions.AssertionException) { caught = true; }

            NUnitAssert.IsTrue(caught, "Should have caught missing material");
            Object.DestroyImmediate(go);
            yield return null;
        }
    }
}
