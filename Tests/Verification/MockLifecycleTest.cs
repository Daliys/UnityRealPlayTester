using System.Threading.Tasks;
using RealPlayTester.Core;

namespace RealPlayTester.Tests.Verification
{
    public class MockLifecycleTest : RealPlayTest
    {
        public bool WasRun;
        protected override Task Run() 
        { 
            WasRun = true; 
            return Task.CompletedTask; 
        }
    }
}
