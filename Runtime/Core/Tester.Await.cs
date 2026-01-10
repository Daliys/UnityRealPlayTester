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
            public static Task Until(System.Func<bool> predicate, float timeout = 5f) => RealPlayTester.Await.Wait.Until(predicate, timeout);
            
            /// <summary>Waits for the scene to become physically and visually stable.</summary>
            public static Task ForSteadyState(int stableFrames = 10, float timeout = 5f, float? velocityEpsilon = null) 
                => RealPlayTester.Await.Wait.ForSteadyState(stableFrames, timeout, velocityEpsilon);
        }
    }
}
