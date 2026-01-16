using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools;
using RealPlayTester.Input;
using RealPlayTester.Utilities;
using NUnitAssert = NUnit.Framework.Assert;

namespace RealPlayTester.Tests.Verification
{
    public partial class ZOrderAmbiguityTests
    {
        [UnityTest]
        public IEnumerator SmartFind_SortingOrder_WinsOver_HierarchyDepth() => ToCoroutine(async () =>
        {
            var lowCanvas = CreateCanvas("Low", 10);
            var nested = new GameObject("Nested", typeof(RectTransform));
            nested.transform.SetParent(lowCanvas.transform, false);
            var deeperLow = CreateButton("Winner", Vector2.zero, nested.transform);

            var highCanvas = CreateCanvas("High", 100);
            var shallowHigh = CreateButton("Winner", Vector2.zero, highCanvas.transform);

            await Task.Yield();
            SceneCache.Instance.ForceRefresh();

            var found = SmartFind.Object("Winner");
            NUnitAssert.AreEqual(shallowHigh.GetInstanceID(), found.GetInstanceID(), "High sorting order canvas should win even if shallower");
        });

        [UnityTest]
        public IEnumerator SmartFind_WorldDepth_PicksClosest() => ToCoroutine(async () =>
        {
            var far = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            far.name = "Sphere";
            far.transform.position = new Vector3(0, 0, 50);
            far.transform.SetParent(_testRoot.transform);

            var mid = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            mid.name = "Sphere";
            mid.transform.position = new Vector3(0, 0, 10);
            mid.transform.SetParent(_testRoot.transform);

            var close = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            close.name = "Sphere";
            close.transform.position = new Vector3(0, 0, 5);
            close.transform.SetParent(_testRoot.transform);

            await Task.Yield();
            SceneCache.Instance.ForceRefresh();

            var found = SmartFind.Object("Sphere");
            NUnitAssert.AreEqual(close.GetInstanceID(), found.GetInstanceID(), "Should pick the closest world object");
        });

        [UnityTest]
        public IEnumerator SmartFind_TieBreak_IsDeterministic() => ToCoroutine(async () =>
        {
            var c = CreateCanvas("Tie", 0);
            var a = CreateButton("Same", Vector2.zero, c.transform);
            var b = CreateButton("Same", Vector2.zero, c.transform);

            await Task.Yield();
            SceneCache.Instance.ForceRefresh();

            var found1 = SmartFind.Object("Same");
            var found2 = SmartFind.Object("Same");

            NUnitAssert.AreEqual(found1.GetInstanceID(), found2.GetInstanceID(), "Multiple finds should return same object for identical scores");
        });
    }
}
