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
            public string Intent;
            public string TargetName;
            public Func<Task> Action;
        }

        private readonly Dictionary<string, List<NavigationPath>> _graph = new Dictionary<string, List<NavigationPath>>();

        public void RegisterPath(string from, string to, Func<Task> navigationAction)
        {
            RegisterPathInternal(from, to, navigationAction, null, null);
        }

        public void Clear()
        {
            _graph.Clear();
        }

        private void RegisterPathInternal(string from, string to, Func<Task> action, string intent, string targetName)
        {
            if (!_graph.ContainsKey(from))
            {
                _graph[from] = new List<NavigationPath>();
            }
            _graph[from].Add(new NavigationPath 
            { 
                Destination = to, 
                Action = action,
                Intent = intent,
                TargetName = targetName
            });
        }

        public void RecordStateTransition(string from, string to, string intent, string targetName)
        {
            if (!RealPlaySettings.Perception.EnableNavigationLearning || from == to) return;

            if (_graph.TryGetValue(from, out var paths))
            {
                if (paths.Exists(p => p.Destination == to)) return;
            }

            RealPlayLog.Info($"[Navigation] Learning path: {from} -> {to} via {intent}('{targetName}')");
            RegisterPathInternal(from, to, async () => await Tester.Interaction.Perform(intent, targetName), intent, targetName);
        }

        public async Task<bool> Navigate(string from, string to)
        {
            if (from == to) return true;

            var path = FindPath(from, to);
            if (path == null)
            {
                RealPlayLog.Warn($"[Navigation] No path found from '{from}' to '{to}'");
                return false;
            }

            await ExecutePath(path);
            return true;
        }

        private List<NavigationPath> FindPath(string from, string to)
        {
            var queue = new Queue<List<NavigationPath>>();
            var visited = new HashSet<string>();

            if (!_graph.TryGetValue(from, out var initialPaths)) return null;

            foreach (var p in initialPaths)
            {
                queue.Enqueue(new List<NavigationPath> { p });
                visited.Add(p.Destination);
            }

            int iterations = 0;
            while (queue.Count > 0 && iterations < 1000) // LIMIT ITERATIONS
            {
                iterations++;
                var currentPath = queue.Dequeue();
                if (currentPath.Count > 20) continue; // LIMIT DEPTH

                var lastStep = currentPath[currentPath.Count - 1];

                if (lastStep.Destination == to) return currentPath;

                if (_graph.TryGetValue(lastStep.Destination, out var nextSteps))
                {
                    foreach (var next in nextSteps)
                    {
                        if (visited.Add(next.Destination))
                        {
                            var newPath = new List<NavigationPath>(currentPath);
                            newPath.Add(next);
                            queue.Enqueue(newPath);
                        }
                    }
                }
            }
            return null;
        }

        private async Task ExecutePath(List<NavigationPath> path)
        {
            foreach (var step in path)
            {
                RealPlayTester.Diagnostics.TestRunContextTracker.RecordBreadcrumb("Navigation", $"Navigating to {step.Destination}");
                await step.Action();
            }
        }

        #region Persistence

        [Serializable]
        private class SerializableEdge { public string to; public string intent; public string target; }
        [Serializable]
        private class SerializableNode { public string from; public List<SerializableEdge> edges = new List<SerializableEdge>(); }
        [Serializable]
        private class SerializableGraph { public List<SerializableNode> nodes = new List<SerializableNode>(); }

        public string ExportToJson()
        {
            var data = new SerializableGraph();
            foreach (var kvp in _graph)
            {
                var node = new SerializableNode { from = kvp.Key };
                foreach (var edge in kvp.Value)
                {
                    if (!string.IsNullOrEmpty(edge.Intent))
                    {
                        node.edges.Add(new SerializableEdge { to = edge.Destination, intent = edge.Intent, target = edge.TargetName });
                    }
                }
                if (node.edges.Count > 0) data.nodes.Add(node);
            }
            return JsonUtility.ToJson(data, true);
        }

        public void ImportFromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return;
            try
            {
                var data = JsonUtility.FromJson<SerializableGraph>(json);
                foreach (var node in data.nodes)
                {
                    foreach (var edge in node.edges)
                    {
                        RegisterPathInternal(node.from, edge.to, async () => await Tester.Interaction.Perform(edge.intent, edge.target), edge.intent, edge.target);
                    }
                }
            }
            catch (Exception ex) { RealPlayLog.Error($"[Navigation] Failed to import JSON: {ex.Message}"); }
        }

        public void SaveToFile(string path)
        {
            try { System.IO.File.WriteAllText(path, ExportToJson()); RealPlayLog.Info($"[Navigation] Saved graph to {path}"); }
            catch (Exception ex) { RealPlayLog.Error($"[Navigation] Save failed: {ex.Message}"); }
        }

        public void LoadFromFile(string path)
        {
            if (!System.IO.File.Exists(path)) return;
            try { ImportFromJson(System.IO.File.ReadAllText(path)); RealPlayLog.Info($"[Navigation] Loaded graph from {path}"); }
            catch (Exception ex) { RealPlayLog.Error($"[Navigation] Load failed: {ex.Message}"); }
        }

        #endregion
    }
}