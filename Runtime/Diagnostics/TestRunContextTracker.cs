using System;
using System.IO;
using RealPlayTester.Core;
using UnityEngine;

namespace RealPlayTester.Diagnostics
{
    /// <summary>
    /// Tracks the active test run context and writes snapshots for debugging.
    /// </summary>
    public static class TestRunContextTracker
    {
        private static readonly object Sync = new object();
        private static TestRunContext _current;

        /// <summary>
        /// The currently active test context, if any.
        /// </summary>
        public static TestRunContext Current
        {
            get
            {
                lock (Sync)
                {
                    return _current;
                }
            }
        }

        /// <summary>
        /// Starts a new context for the given test.
        /// </summary>
        public static TestRunContext BeginTest(string testName, string sceneName)
        {
            if (!RealPlayEnvironment.IsEnabled)
            {
                return null;
            }

            var context = new TestRunContext
            {
                TestName = testName,
                SceneName = sceneName,
                StartTime = DateTime.Now
            };

            lock (Sync)
            {
                _current = context;
            }

            WriteSnapshot(context);
            return context;
        }

        /// <summary>
        /// Marks the current test as complete and writes a final snapshot.
        /// </summary>
        public static void EndTest()
        {
            if (!RealPlayEnvironment.IsEnabled)
            {
                return;
            }

            TestRunContext context;
            lock (Sync)
            {
                context = _current;
            }

            if (context == null)
            {
                return;
            }

            context.EndTime = DateTime.Now;
            WriteSnapshot(context);
        }

        /// <summary>
        /// Updates the last action for the active test.
        /// </summary>
        public static void UpdateAction(string action)
        {
            UpdateContext(ctx => ctx.LastAction = action);
        }

        /// <summary>
        /// Updates the last panel interaction for the active test.
        /// </summary>
        public static void UpdatePanel(string panelName)
        {
            UpdateContext(ctx => ctx.LastPanel = panelName);
        }

        /// <summary>
        /// Updates the last placement attempt for the active test.
        /// </summary>
        public static void UpdatePlacementAttempt(Vector2Int position, string definitionId, string result)
        {
            UpdateContext(ctx => ctx.LastPlacementAttempt = new PlacementAttempt(position, definitionId, result));
        }

        private static void UpdateContext(Action<TestRunContext> update)
        {
            if (!RealPlayEnvironment.IsEnabled)
            {
                return;
            }

            TestRunContext context;
            lock (Sync)
            {
                context = _current;
            }

            if (context == null)
            {
                return;
            }

            update(context);
            WriteSnapshot(context);
        }

        private static void WriteSnapshot(TestRunContext context)
        {
            if (context == null)
            {
                return;
            }

            try
            {
                string dir = RealPlayEnvironment.TestReportsPath;
                Directory.CreateDirectory(dir);

                string jsonPath = Path.Combine(dir, "current-test-context.json");
                File.WriteAllText(jsonPath, context.ToJson());

                string mdPath = Path.Combine(dir, "current-test-context.md");
                File.WriteAllText(mdPath, context.ToMarkdown());
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RealPlayTester] Failed to write test context snapshot: {ex.Message}");
            }
        }
    }
}
