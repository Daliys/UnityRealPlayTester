using System.Threading.Tasks;
using RealPlayTester.Await;

namespace RealPlayTester.Core
{
    public static partial class Tester
    {
        public static class Await
        {
            public static Task Seconds(float seconds) => Wait.Seconds(seconds);
            public static Task Frames(int frames) => Wait.Frames(frames);
            public static Task Until(System.Func<bool> predicate, float timeout = 5f) => Wait.Until(predicate, timeout);
        }
    }
}
