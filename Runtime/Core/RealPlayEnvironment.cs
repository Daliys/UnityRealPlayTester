using UnityEngine;

namespace RealPlayTester.Core
{
    public static class RealPlayEnvironment
    {
        public static bool IsEnabled
        {
            get
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                return true;
#else
                return false;
#endif
            }
        }

        public static string ProjectRoot
        {
            get
            {
#if UNITY_EDITOR
                return System.IO.Path.GetDirectoryName(Application.dataPath);
#else
                return Application.persistentDataPath;
#endif
            }
        }

        public static string TestReportsPath => System.IO.Path.Combine(ProjectRoot, "TestReports");
    }
}