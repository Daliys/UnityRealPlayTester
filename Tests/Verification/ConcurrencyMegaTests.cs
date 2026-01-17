using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using RealPlayTester.Core;
using RealPlayTester.Input;
using RealPlayTester.Core.Perception;
using RealPlayTester.Utilities;
using NUnitAssert = NUnit.Framework.Assert;

namespace RealPlayTester.Tests.Verification
{
    public class ConcurrencyMegaTests
    {
        [Test]
        [TestCase(1), TestCase(2), TestCase(3), TestCase(4), TestCase(5)]
        [TestCase(6), TestCase(7), TestCase(8), TestCase(9), TestCase(10)]
        public async Task Concurrency_Stress_SmartFind(int iteration)
        {
            bool caught = false;
            await Task.Run(() => {
                try { SmartFind.Object("Target" + iteration); }
                catch (InvalidOperationException) { caught = true; }
            });
            NUnitAssert.IsTrue(caught, $"Iteration {iteration}: SmartFind did not catch background access!");
        }

        [Test]
        [TestCase(1), TestCase(2), TestCase(3), TestCase(4), TestCase(5)]
        [TestCase(6), TestCase(7), TestCase(8), TestCase(9), TestCase(10)]
        public async Task Concurrency_Stress_DOMDumper(int iteration)
        {
            bool caught = false;
            await Task.Run(() => {
                try { RealPlaySemanticDOMDumper.Dump(); }
                catch (InvalidOperationException) { caught = true; }
            });
            NUnitAssert.IsTrue(caught, $"Iteration {iteration}: Dumper did not catch background access!");
        }

        [Test]
        [TestCase(1), TestCase(2), TestCase(3), TestCase(4), TestCase(5)]
        [TestCase(6), TestCase(7), TestCase(8), TestCase(9), TestCase(10)]
        public async Task Concurrency_Stress_StateTracker(int iteration)
        {
            bool caught = false;
            await Task.Run(() => {
                try { StateTracker.Capture(); }
                catch (InvalidOperationException) { caught = true; }
            });
            NUnitAssert.IsTrue(caught, $"Iteration {iteration}: StateTracker did not catch background access!");
        }

        [Test]
        [TestCase(1), TestCase(2), TestCase(3), TestCase(4), TestCase(5)]
        [TestCase(6), TestCase(7), TestCase(8), TestCase(9), TestCase(10)]
        public async Task Concurrency_Stress_Occlusion(int iteration)
        {
            bool caught = false;
            await Task.Run(() => {
                try { RealPlayOcclusionRaycaster.CheckOcclusion(null); }
                catch (InvalidOperationException) { caught = true; }
            });
            NUnitAssert.IsTrue(caught, $"Iteration {iteration}: Occlusion did not catch background access!");
        }

        [Test]
        [TestCase(1), TestCase(2), TestCase(3), TestCase(4), TestCase(5)]
        [TestCase(6), TestCase(7), TestCase(8), TestCase(9), TestCase(10)]
        public async Task Concurrency_Stress_Perform(int iteration)
        {
            bool caught = false;
            await Task.Run(async () => {
                try { await Tester.Interaction.Perform("click", "Btn"); }
                catch (InvalidOperationException) { caught = true; }
            });
            NUnitAssert.IsTrue(caught, $"Iteration {iteration}: Perform did not catch background access!");
        }
    }
}