using System.Collections.Generic;

namespace RealPlayTester.Core
{
    /// <summary>
    /// Interface for custom test report handlers.
    /// Implement this to completely replace the default JSON report generation.
    /// </summary>
    public interface ITestReportHandler
    {
        /// <summary>
        /// Called after all tests complete. Implement custom report logic here.
        /// </summary>
        /// <param name="report">Aggregated test report with summary statistics.</param>
        /// <param name="results">Individual test results for detailed processing.</param>
        void HandleReport(TestReport report, List<TestResult> results);
    }
}
