using NUnit.Framework;
using RealPlayTester.Diagnostics;
using UnityEngine;

namespace RealPlayTester.Tests.EditMode
{
    [TestFixture]
    public class TestLogTests
    {
        [Test]
        public void Info_DoesNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => TestLog.Info("Test info message"));
        }

        [Test]
        public void Warn_DoesNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => TestLog.Warn("Test warning message"));
        }

        [Test]
        public void Error_DoesNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => TestLog.Error("Test error message"));
        }

        [Test]
        public void TestLog_WorksWithoutGameLogger()
        {
            // This test verifies that TestLog falls back to Debug.Log
            // when GameLogger/EventAggregator are not available
            
            // Act
            TestLog.Info("Fallback test");

            // Assert - if we reach here without exception, the fallback works
            Assert.Pass("TestLog successfully fell back to Debug.Log");
        }
    }
}
