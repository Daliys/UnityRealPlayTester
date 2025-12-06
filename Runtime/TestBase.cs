using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace RealPlayTester.Core
{
    /// <summary>
    /// Base type for all RealPlayTester assets. Override <see cref="Run"/> to implement a play test.
    /// </summary>
    public abstract class TestBase : ScriptableObject
    {
        protected CancellationToken Token { get; private set; }

        internal void Initialize(CancellationToken token)
        {
            Token = token;
        }

        /// <summary>
        /// Optional hook before each test. Default no-op.
        /// </summary>
        protected virtual Task SetUp()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Optional hook after each test. Default no-op.
        /// </summary>
        protected virtual Task TearDown()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Implement the test body here using Click/Press/Drag/Wait/Assert helpers.
        /// </summary>
        protected abstract Task Run();

        internal async Task Execute()
        {
            await SetUp();
            await Run();
            await TearDown();
        }

        protected RealPlayTesterHost Host
        {
            get { return RealPlayTesterHost.Instance; }
        }

        protected void Log(string message)
        {
            RealPlayLog.Info(message);
        }
    }

    /// <summary>
    /// Public base class for authoring tests. Derive from this in ScriptableObject assets.
    /// </summary>
    public abstract class RealPlayTest : TestBase
    {
        protected abstract override Task Run();
    }
}
