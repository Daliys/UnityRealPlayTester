using UnityEngine;

namespace RealPlayTester.Core
{
    public static class RealPlayEnvironment
    {
        private static bool _globalDisable = false;

        /// <summary>
        /// Globally disables all RealPlayTester functionality at runtime.
        /// </summary>
        public static bool GlobalDisable
        {
            get
            {
#if DISABLE_REALPLAY
                return true;
#else
                return _globalDisable;
#endif
            }
            set
            {
#if !DISABLE_REALPLAY
                if (_globalDisable == value) return;
                _globalDisable = value;
                if (_globalDisable) RealPlayLog.Warn("RealPlayTester has been GLOBALLY DISABLED.");
#endif
            }
        }

        public static bool IsEnabled
        {
            get
            {
                if (_globalDisable) return false;

#if DISABLE_REALPLAY
                return false;
#elif UNITY_EDITOR || DEVELOPMENT_BUILD
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