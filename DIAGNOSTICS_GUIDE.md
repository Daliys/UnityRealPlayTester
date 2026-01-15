# RealPlayTester Diagnostics - Usage Guide

This document provides guidance on using the diagnostics features added in v1.2.x.

## Overview

The diagnostics system helps you:
- **Track test execution context** with TestRunContext
- **Collect comprehensive failure bundles** automatically
- **Log with external system integration** via TestLog
- **Monitor UI panel states** for validation
- **Guard against input system issues**

## TestRunContext - Tracking Test State

`TestRunContext` automatically tracks:
- Test identity (name, ID, timestamps)
- Environment (scene, Unity version, input mode)
- Test state (last action, last panel, last placement attempt)

### Basic Usage

```csharp
using RealPlayTester.Diagnostics;

var context = new TestRunContext
{
    TestName = "MyTest",
    SceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
};

// Update during test execution
context.LastAction = "Clicking start button";
context.LastPanel = "MainMenu";

// At test end
context.EndTime = DateTime.Now;

// Export for logging
string json = context.ToJson();
string markdown = context.ToMarkdown();
```

### TestRunContextTracker - Automatic Snapshots

`TestRunContextTracker` automatically tracks the active test and writes snapshots to:
- `TestReports/current-test-context.json`
- `TestReports/current-test-context.md`

This is updated whenever you call `Wait.Step`, update panels, or record placement attempts.

```csharp
using RealPlayTester.Diagnostics;

// Start tracking (handled by TestRunner automatically)
TestRunContextTracker.BeginTest("MyTest", SceneManager.GetActiveScene().name);

// Update state
TestRunContextTracker.UpdateAction("Opening main menu");
TestRunContextTracker.UpdatePanel("MainMenuPanel");
TestRunContextTracker.UpdatePlacementAttempt(new Vector2Int(2, 3), "iron_miner", "queued");
```

### Tracking Placement Attempts

```csharp
context.LastPlacementAttempt = new PlacementAttempt(
    new Vector2Int(5, 10),
    "Factory",
    "Success" // or error reason
);
```

## FailureBundleWriter - Automatic Artifact Collection

When a test fails, call `FailureBundleWriter.WriteFailureBundle()` to collect all diagnostic artifacts.
In v1.2.1+, `TestRunner` calls it automatically on timeouts and exceptions.

### Basic Usage

```csharp
string bundlePath = FailureBundleWriter.WriteFailureBundle(
    testName: "Test4_FactoryProduction",
    context: testContext,
    screenshotPath: screenshotPath,  // optional
    hierarchyDump: Tester.DumpHierarchy()  // optional
);

// Bundle created at: TestReports/FailureBundles/{timestamp}/Test4_FactoryProduction/
```

### Bundle Contents

```
FailureBundles/2025-12-20_16-30-45/Test4_FactoryProduction/
├── diagnostics.json          # Machine-readable context
├── diagnostics.md            # Human-readable context
├── test-results.json         # Overall test results
├── failure_screenshot.png    # Screenshot at failure
├── hierarchy.txt             # Scene hierarchy dump
└── Logs/
    ├── game.log
    ├── game_session.log
    └── Editor.log
```

## TestLog - Centralized Logging

`TestLog` automatically routes to GameLogger/EventAggregator if available, otherwise uses Debug.Log.

### Basic Usage

```csharp
using RealPlayTester.Diagnostics;

TestLog.Info("Starting building placement");
TestLog.Warn("Resource count low");
TestLog.Error("Placement failed");
```

### Integration with GameLogger

If your project has a `GameLogger` class:
```csharp
public class GameLogger
{
    public static GameLogger Instance { get; }
    public void Log(string message) { /* ... */ }
}
```

TestLog will automatically detect and use it.

## Wait.UntilWithDiagnostics - Enhanced Timeout Messages

Replace `Wait.Until` with `Wait.UntilWithDiagnostics` for better error messages on timeout.

### Usage

```csharp
// Old way
await Wait.Until(() => building != null, 5f);
// Timeout: "Wait.Until timed out."

// New way with diagnostics
await Wait.UntilWithDiagnostics(
    () => building != null,
    timeout: 5f,
    context: $"Building ID: {buildingId}, Grid: {gridSize}"
);
// Timeout: "Wait.UntilWithDiagnostics timed out after 5s. Predicate: <CheckBuilding>b__0. Context: Building ID: Factory, Grid: 10x10"
```

## Wait.Step - Test Progress Tracking

Use `Wait.Step()` to log test progress:

```csharp
Wait.Step("Opening pause menu");
await Click.ButtonWithText("Pause");

Wait.Step("Saving game");
await Click.ButtonWithText("Save");

Wait.Step("Verifying save completed");
await Wait.Until(() => saveCompleted);
```

## PanelStateMonitor - UI Validation

Check panel visibility and interactability state.

### Basic Usage

```csharp
using RealPlayTester.UI;

// Check panel state
var state = PanelStateMonitor.CheckPanelState("PauseMenu");

TestLog.Info($"Panel state: {state}");
// Output: "Visible:True, Alpha:1.00, Interactable:True, Active:True"

// Validate panel is ready
bool ready = PanelStateMonitor.IsPanelReady("PauseMenu");
Assert.IsTrue(ready, "Pause menu should be visible and interactable");
```

### Advanced Usage - Panel Transitions

```csharp
// Before opening panel
var stateBefore = PanelStateMonitor.CheckPanelState(pauseMenu);
TestLog.Info($"Panel before: {stateBefore}");

await Click.ButtonWithText("Pause");
await Wait.Seconds(0.5f);

// After opening panel
var stateAfter = PanelStateMonitor.CheckPanelState(pauseMenu);
TestLog.Info($"Panel after: {stateAfter}");

// Validate transition
Assert.IsFalse(stateBefore.IsVisible, "Panel should start hidden");
Assert.IsTrue(stateAfter.IsVisible, "Panel should be visible after click");
Assert.IsTrue(stateAfter.Interactable, "Panel should be interactable");
```

## SimulatedInputGuard - Input System Checks

Detect input system availability and UI overlap.

### Basic Usage

```csharp
using RealPlayTester.Input;

// Check if Input System is ready
bool ready = SimulatedInputGuard.InputSystemReady();
if (!ready)
{
    TestLog.Warn("Input System not available, using legacy input");
}

// Check if pointer is over UI (to avoid clicking through)
if (SimulatedInputGuard.IsPointerOverUI())
{
    TestLog.Warn("Pointer is over UI, click may not reach game world");
}

// Ensure input is ready (throws if not)
SimulatedInputGuard.EnsureInputReady();
```

## Complete Example - Test with Full Diagnostics

```csharp
using System.Threading.Tasks;
using UnityEngine;
using RealPlayTester.Core;
using RealPlayTester.Diagnostics;
using RealPlayTester.Await;
using RealPlayTester.Input;
using RealPlayTester.UI;

[CreateAssetMenu(menuName = "RealPlay Tests/Diagnostic Example")]
public class DiagnosticExampleTest : RealPlayTest
{
    private TestRunContext context;

    protected override async Task Run()
    {
        // Initialize context
        context = new TestRunContext
        {
            TestName = "DiagnosticExample",
            SceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        };

        try
        {
            // Check input system
            SimulatedInputGuard.EnsureInputReady();

            // Step 1: Open menu
            Wait.Step("Opening main menu");
            await Click.ButtonWithText("Start");
            context.LastAction = "Clicked Start button";

            // Validate menu opened
            var menuState = PanelStateMonitor.CheckPanelState("MainMenu");
            TestLog.Info($"Menu state: {menuState}");
            Assert.IsTrue(menuState.IsVisible, "Main menu should be visible");

            // Step 2: Wait for game to load
            Wait.Step("Waiting for game load");
            await Wait.UntilWithDiagnostics(
                () => GameObject.Find("GameManager") != null,
                timeout: 10f,
                context: "After clicking Start button"
            );

            // Step 3: Perform game action
            Wait.Step("Performing game action");
            await Click.WorldPosition(new Vector3(5, 0, 10));
            context.LastPlacementAttempt = new PlacementAttempt(
                new Vector2Int(5, 10),
                "Building",
                "Success"
            );

            TestLog.Info("Test completed successfully");
        }
        catch (System.Exception ex)
        {
            // On failure, write diagnostics bundle
            context.EndTime = System.DateTime.Now;
            
            string screenshotPath = Capture.Screenshot("failure");
            string hierarchyDump = Tester.DumpHierarchy();
            
            string bundlePath = FailureBundleWriter.WriteFailureBundle(
                context.TestName,
                context,
                screenshotPath,
                hierarchyDump
            );

            TestLog.Error($"Test failed. Diagnostics bundle: {bundlePath}");
            throw;
        }
        finally
        {
            context.EndTime = System.DateTime.Now;
            TestLog.Info($"Test duration: {(context.EndTime - context.StartTime).TotalSeconds:F2}s");
        }
    }
}
```

## Workflow Scripts

Two shell scripts are provided for batch artifact collection:

### realplay_bundle.sh

Runs all RealPlay tests and collects artifacts into a timestamped bundle:

```bash
cd /path/to/project
./.claude/scripts/realplay_bundle.sh
```

Output: `TestBundles/{timestamp}/`

### collect_logs.sh

Collects current logs and screenshots without running tests:

```bash
./.claude/scripts/collect_logs.sh
```

Output: `CollectedLogs/{timestamp}/`

## Best Practices

1. **Always use TestRunContext** in test classes to track state
2. **Call Wait.Step()** at major test milestones
3. **Use Wait.UntilWithDiagnostics** instead of Wait.Until for important waits
4. **Validate UI panels** with PanelStateMonitor before interaction
5. **Log placement attempts** in context for building/unit placement tests
6. **Write failure bundles** in catch blocks for comprehensive diagnostics
7. **Check input system readiness** before tests that rely on input simulation

## Troubleshooting

**Q: FailureBundleWriter isn't copying my logs**
- A: Check that log paths in your project match the paths in FailureBundleWriter.CopyGameLogs()
- Customize the paths if your project uses different locations

**Q: TestLog isn't routing to GameLogger**
- A: Ensure your GameLogger class has a public static `Instance` property
- Check that the class is in the Assembly-CSharp assembly

**Q: PanelStateMonitor shows wrong alpha**
- A: Check parent CanvasGroups - the monitor combines alpha from all parents
- Use `state.HasCanvasGroup` to check if CanvasGroup is present

**Q: Wait.UntilWithDiagnostics timeout message doesn't show context**
- A: Ensure you pass the `context` parameter with relevant debugging information
