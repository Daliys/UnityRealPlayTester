using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace RealPlayTester.Core.Navigation
{
    public class NavigationGraph
    {
        private class NavigationPath
        {
            public string Destination;
            public Func<Task> Action;
        }

        private readonly Dictionary<string, List<NavigationPath>> _graph = new Dictionary<string, List<NavigationPath>>();

        public void RegisterPath(string from, string to, Func<Task> navigationAction)
        {
            if (!_graph.ContainsKey(from))
            {
                _graph[from] = new List<NavigationPath>();
            }
            _graph[from].Add(new NavigationPath { Destination = to, Action = navigationAction });
        }

        public async Task<bool> Navigate(string from, string to)
        {
            if (from == to) return true;

            // Simple BFS to find path
            var queue = new Queue<List<NavigationPath>>();
            var visited = new HashSet<string>();

            if (_graph.TryGetValue(from, out var initialPaths))
            {
                foreach (var p in initialPaths)
                {
                    queue.Enqueue(new List<NavigationPath> { p });
                    visited.Add(p.Destination);
                }
            }

            while (queue.Count > 0)
            {
                var currentPath = queue.Dequeue();
                var lastStep = currentPath[currentPath.Count - 1];

                if (lastStep.Destination == to)
                {
                    // Execute Path
                    foreach (var step in currentPath)
                    {
                        RealPlayTester.Diagnostics.TestRunContextTracker.RecordBreadcrumb("Navigation", $"Navigating from ... to {step.Destination}");
                        await step.Action();
                    }
                    return true;
                }

                if (_graph.TryGetValue(lastStep.Destination, out var nextSteps))
                {
                    foreach (var next in nextSteps)
                    {
                        if (!visited.Contains(next.Destination))
                        {
                            visited.Add(next.Destination);
                            var newPath = new List<NavigationPath>(currentPath);
                            newPath.Add(next);
                            queue.Enqueue(newPath);
                        }
                    }
                }
            }

            RealPlayLog.Warn($"[Navigation] No path found from '{from}' to '{to}'");
            return false;
        }
    }
}