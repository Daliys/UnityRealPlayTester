using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using RealPlayTester.Core.Perception;
using RealPlayTester.Core;
using UnityEngine.TestTools;

namespace RealPlayTester.Tests.Verification
{
    public partial class FeatureVerificationV22Tests
    {
        private static SemanticNode FindNodeByName(string json, string name)
        {
            var root = SemanticDOMParser.Parse(json);
            return FindRecursive(root, name);
        }

        private static SemanticNode FindRecursive(SemanticNode node, string name)
        {
            if (node == null) return null;
            if (node.Name == name) return node;
            if (node.Children != null)
            {
                foreach (var child in node.Children)
                {
                    var found = FindRecursive(child, name);
                    if (found != null) return found;
                }
            }
            return null;
        }

        private class TestLogicBlocker : MonoBehaviour, IAffordanceValidator
        {
            public bool ShouldBlock;
            public string Reason;

            public bool CanInteract(string intent) => !ShouldBlock;
            public string GetBlockReason() => Reason;
        }

        private static IEnumerator ToCoroutine(System.Func<Task> action)
        {
            var task = action();
            while (!task.IsCompleted) yield return null;
            if (task.IsFaulted) throw task.Exception.InnerException;
        }
    }
}
