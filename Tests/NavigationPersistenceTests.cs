using System.Collections;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using RealPlayTester.Core;

namespace RealPlayTester.Tests.Verification
{
    public class NavigationPersistenceTests
    {
        [UnitySetUp]
        public IEnumerator Setup()
        {
            Tester.Navigation.Clear();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            Tester.Navigation.Clear();
            yield return null;
        }

        [UnityTest]
        public IEnumerator Navigation_ExportsAndImports_Json() => ToCoroutine(async () =>
        {
            string from = "StateA", to = "StateB", intent = "Click", target = "SomeBtn";
            Tester.Settings.EnableNavigationLearning = true;
            Tester.Navigation.RecordStateTransition(from, to, intent, target);

            string json = Tester.Navigation.ExportToJson();
            VerifyJsonContent(json, from, to, intent, target);

            Tester.Navigation.Clear();
            Tester.Navigation.ImportFromJson(json);

            VerifyGraphContains(from);
        });

        private void VerifyJsonContent(string json, string from, string to, string intent, string target)
        {
            NUnit.Framework.Assert.IsNotNull(json);
            NUnit.Framework.Assert.IsTrue(json.Contains(from));
            NUnit.Framework.Assert.IsTrue(json.Contains(to));
            NUnit.Framework.Assert.IsTrue(json.Contains(intent));
            NUnit.Framework.Assert.IsTrue(json.Contains(target));
        }

        private void VerifyGraphContains(string from)
        {
            var graphField = typeof(Tester.Navigation).GetField("_graph", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var graphObj = graphField.GetValue(null);
            var dictField = graphObj.GetType().GetField("_graph", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var dict = (System.Collections.IDictionary)dictField.GetValue(graphObj);
            
            NUnit.Framework.Assert.IsTrue(dict.Contains(from), "Imported graph should contain the node");
        }

        [UnityTest]
        public IEnumerator Navigation_SavesAndLoads_File() => ToCoroutine(async () =>
        {
            string path = Path.Combine(Application.temporaryCachePath, "nav_test.json");
            if (File.Exists(path)) File.Delete(path);

            Tester.Settings.EnableNavigationLearning = true;
            Tester.Navigation.RecordStateTransition("Start", "End", "Click", "Btn");
            
            Tester.Navigation.SaveToFile(path);
            NUnit.Framework.Assert.IsTrue(File.Exists(path), "File should be created");

            Tester.Navigation.Clear();
            Tester.Navigation.LoadFromFile(path);

            // Verify
            var graphField = typeof(Tester.Navigation).GetField("_graph", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var graphObj = graphField.GetValue(null);
            var dictField = graphObj.GetType().GetField("_graph", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var dict = (System.Collections.IDictionary)dictField.GetValue(graphObj);
            
            NUnit.Framework.Assert.IsTrue(dict.Contains("Start"), "Loaded graph should contain the data");

            if (File.Exists(path)) File.Delete(path);
        });

        private static IEnumerator ToCoroutine(System.Func<Task> action)
        {
            var task = action();
            while (!task.IsCompleted) yield return null;
            if (task.IsFaulted) throw task.Exception.InnerException;
        }
    }
}
