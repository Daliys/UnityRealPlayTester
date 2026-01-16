using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using RealPlayTester.Core;

namespace RealPlayTester.Tests.Verification
{
    public class ReportCleanupTests
    {
        [UnityTest]
        public IEnumerator CleanupReports_RemovesSubdirectories()
        {
            // 1. Setup: Create dummy file and folder in TestReports
            string reportDir = RealPlayEnvironment.TestReportsPath;
            string subDir = Path.Combine(reportDir, "DummyFolder");
            string dummyFile = Path.Combine(subDir, "dummy.txt");
            string rootFile = Path.Combine(reportDir, "root.txt");

            Directory.CreateDirectory(subDir);
            File.WriteAllText(dummyFile, "test");
            File.WriteAllText(rootFile, "root");

            NUnit.Framework.Assert.IsTrue(Directory.Exists(subDir));
            NUnit.Framework.Assert.IsTrue(File.Exists(dummyFile));
            NUnit.Framework.Assert.IsTrue(File.Exists(rootFile));

            // 2. Action: Invoke private CleanupReports via reflection
            // Note: RealPlayTesterHost is the class containing the method
            var method = typeof(RealPlayTesterHost).GetMethod("CleanupReports", BindingFlags.Static | BindingFlags.NonPublic);
            NUnit.Framework.Assert.IsNotNull(method, "Could not find CleanupReports method");
            
            method.Invoke(null, null);

            // 3. Verify: Everything is gone
            NUnit.Framework.Assert.IsFalse(Directory.Exists(subDir), "Subdirectory should be deleted");
            NUnit.Framework.Assert.IsFalse(File.Exists(dummyFile), "File in subdirectory should be deleted");
            NUnit.Framework.Assert.IsFalse(File.Exists(rootFile), "File in root should be deleted");
            
            // 4. Verify: Root folder still exists (optional, but good practice)
            // The method checks Directory.Exists(path) at start, so it assumes root exists.
            // DirectoryInfo.GetFiles/Directories doesn't delete the root itself unless we call Delete on root.
            // We called Delete on children.
            NUnit.Framework.Assert.IsTrue(Directory.Exists(reportDir), "Root TestReports folder should still exist");

            yield return null;
        }
    }
}
