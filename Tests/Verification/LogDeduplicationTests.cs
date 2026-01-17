using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using RealPlayTester.Core;
using RealPlayTester.Diagnostics;
using System.IO;
using NUnitAssert = NUnit.Framework.Assert;

namespace RealPlayTester.Tests.Verification
{
    public class LogDeduplicationTests
    {
        [UnityTest]
        public IEnumerator RedundantLogs_AreDeduplicated()
        {
            // 1. Start Context
            TestRunContextTracker.BeginTest("DeduplicationTest", "EmptyScene");
            
            // 2. Emit identical logs
            for(int i = 0; i < 10; i++)
            {
                Debug.Log("[RealPlayTester] Redundant Message");
            }
            
            // 3. Emit a different log
            Debug.Log("[RealPlayTester] Different Message");
            
            // 4. Emit identical logs again
            for(int i = 0; i < 5; i++)
            {
                Debug.Log("[RealPlayTester] Another Redundant Message");
            }

            TestRunContextTracker.EndTest();

            // 5. Verify JSON
            string jsonPath = Path.Combine(RealPlayEnvironment.TestReportsPath, "current-test-context.json");
            string json = File.ReadAllText(jsonPath);

            // "Redundant Message" should appear once with "repeats": 10
            NUnitAssert.IsTrue(json.Contains("\"message\": \"[RealPlayTester] Redundant Message\", \"repeats\": 10"), "First deduplication failed");
            NUnitAssert.IsTrue(json.Contains("\"message\": \"[RealPlayTester] Different Message\", \"repeats\": 1"), "Non-redundant log affected");
            NUnitAssert.IsTrue(json.Contains("\"message\": \"[RealPlayTester] Another Redundant Message\", \"repeats\": 5"), "Second deduplication failed");
            
            yield return null;
        }
    }
}
