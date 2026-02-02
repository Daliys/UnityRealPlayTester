using UnityEngine;

namespace RealPlayTester.Core
{
    public static class RealPlayLog
    {
        private const string Prefix = "[RealPlayTester] ";
        private static int _frameLogCount = 0;
        private static int _lastFrame = -1;
        private const int MaxLogsPerFrame = 50;
        private const int MaxLogLength = 1000;

        private static string _lastLogMessage;
        private static LogType _lastLogType;
        private static int _repeatCount = 0;

        /// <summary> If true, all non-error logs will be silenced. </summary>
        public static bool Silence = false;

        public static void Info(string message) => Log(LogType.Log, message);
        public static void Warn(string message) => Log(LogType.Warning, message);
        public static void Error(string message) => Log(LogType.Error, message);

        private static void Log(LogType type, string message)
        {
            if (Silence && type != LogType.Error) return;

            if (message == _lastLogMessage && type == _lastLogType)
            {
                _repeatCount++;
                return;
            }

            FlushRepeats();

            _lastLogMessage = message;
            _lastLogType = type;
            _repeatCount = 1;

            if (ShouldThrottle(type)) return;

            string finalMsg = message.Length > MaxLogLength ? message.Substring(0, MaxLogLength) + "... [TRUNCATED]" : message;
            string fullMsg = Prefix + finalMsg;

            DispatchLog(type, fullMsg);

            if (Tester.Settings.MirrorLogsToStdout) 
                System.Console.WriteLine(Prefix + (type != LogType.Log ? type.ToString().ToUpper() + ": " : "") + finalMsg);
        }

        private static bool ShouldThrottle(LogType type)
        {
            if (Time.frameCount != _lastFrame)
            {
                _lastFrame = Time.frameCount;
                _frameLogCount = 0;
            }

            if (_frameLogCount >= MaxLogsPerFrame)
            {
                if (_frameLogCount == MaxLogsPerFrame)
                    Debug.LogWarning(Prefix + "[LOGS] Throttling triggered: Exceeded 50 logs this frame.");
                _frameLogCount++;
                return true;
            }
            _frameLogCount++;
            return false;
        }

        private static void DispatchLog(LogType type, string message)
        {
            switch (type)
            {
                case LogType.Log: Debug.Log(message); break;
                case LogType.Warning: Debug.LogWarning(message); break;
                case LogType.Error: Debug.LogError(message); break;
            }
        }

        private static void FlushRepeats()
        {
            if (_repeatCount > 1)
            {
                string msg = $"{Prefix} (Repeated {_repeatCount} times)";
                Debug.Log(msg);
                if (Tester.Settings.MirrorLogsToStdout) System.Console.WriteLine(msg);
            }
            _repeatCount = 0;
        }
    }
}