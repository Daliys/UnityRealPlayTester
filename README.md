# RealPlayTester (v2.0)

## Overview
RealPlayTester is a high-fidelity automation library designed exclusively for **Autonomous AI Agents** and **LLM-driven testing**. It provides agents with "eyes" (semantic probes), "ears" (structured log streams), and "hands" (deterministic human-like input) to navigate and test Unity applications as a human would.

**Compatibility**: Unity 6000.2+ | URP / Built-in | Editor | Development Builds
**Orientation**: **AI-Native** (Optimized for JSON-parsing and vision-based reasoning)

---

## 🤖 AI-Native Design Principles
Unlike traditional frameworks designed for humans, RealPlayTester prioritize **context-rich observability**:

- **Deterministic Perception**: `Tester.Perception.DumpHierarchyJson()` provides a token-efficient, coordinate-aware JSON tree optimized for LLM "System Prompts".
- **Interaction Probes**: `Tester.Perception.ProbeScreen(pos)` allows agents to "look" at specific pixels and receive semantic descriptions (e.g., "Primary Action Button").
- **Visual Anchors**: Semantic ID tagging (`#A1`, `#A2`) that persists across frames, helping vision agents maintain spatial awareness.
- **Black Box Recorder**: Unified, categorized log streams (`[INPUT]`, `[BLOCKER]`, `[STEP]`) enable agents to reconstruct the "causal chain" of their actions.
- **Causal Tracking**: `Tester.State.Capture()` and `GetDiff()` allow agents to measure the direct impact of an interaction on the scene state.

---

## Installation
1. Copy the `RealPlayTester/` folder into your project's `Assets/`.
2. No configuration required.

---

## Feature Highlights (v2.0.0)

### 📦 Black Box Recorder & Diagnostics
The library acts as a stateful recorder, providing a continuous diagnostic narrative:
- **Unified Stream**: Automatic categorization of logs into `Input`, `Test`, `Step`, `Game`, `Library`, and `Heartbeat`.
- **Blocker Awareness**: Real-time raycast diagnostic logs `[BLOCKER]` warnings if an interaction target is physically obscured.
- **Failure Bundles v2**: On failure, a bundle is exported with `hierarchy.json`, `diagnostics.md`, screenshots, and session logs, ready for LLM triage.
- **Intent Logging**: Agents can call `Tester.RecordIntent("Checking if the login button works")` to persist their reasoning in the failure report.

### 🚀 CI/CD & Headless Robustness
- **Bypass Heuristics**: Automatically forces UI interactability and visibility in batchmode, bypassing animation bottlenecks.
- **Input Heartbeat**: Forced event processing ensures zero-lag simulation even when the OS provides no hardware input.
- **Coordinate Clamping**: Automatic target calculation handles edge cases like zero-pixel windows or background blocks.

### 🧠 AI-Vision & Perception
- 🧠 **AI-Vision & Perception**: Semantic role identification (Button, InputField, Dialog) and coordinate-aware hierarchy dumps.
- 📍 **Visual Anchors**: Semantic tagging of interactable elements to help vision-based agents pinpoint targets.

### 📡 Advanced Observability
- 📡 **Advanced Observability**: Automatic failure reporting with screenshots, semantic context, and interaction heatmaps for LLM-friendly debugging.
- 🚀 **CI/CD Optimized**: Dedicated headless modes, blocker-bypass strategy, and unified input ownership.
- 🕒 **Input Heartbeat**: Forced event processing during test execution to ensure zero-lag simulation.

### Visual Pointer & Simulation
- **Visual Cursor**: A virtual red dot tracks all simulated movements, providing real-time feedback on hovers and clicks.
- **Realistic Demo**: Press **`P`** in Play Mode to see an automated demonstration of realistic UI interactions (Button, Scroll, Slider, etc.).
- **Continuity**: Simulated mouse movements are continuous. The cursor persists between actions, avoiding unrealistic "teleports".

### URP & Modern Unity
- **Auto-Fix URP**: Automatically handles `UniversalAdditionalCameraData` for URP projects.
- **Smart Targeting**: Automatically calculates precision screen centers for UI elements (even in Overlay mode) and World objects.

---

## Creating a Test
```csharp
using System.Threading.Tasks;
using UnityEngine;
using RealPlayTester.Core;

[CreateAssetMenu(menuName = "RealPlay Tests/MyTest")]
public class MyTest : RealPlayTest
{
    protected override async Task Run()
    {
        // Deterministic interaction via the nested facade
        await Tester.Interaction.Mouse.MoveToCenter(GameObject.Find("StartGame"));
        await Tester.Interaction.Mouse.Click(Input.mousePosition); 
        
        await Tester.Await.Seconds(1f);
        
        // Semantic validation
        var state = Tester.Perception.DumpHierarchyJson();
        Tester.Assert.IsNotNull(state, "Scene state should be accessible");
    }
}
```
Place test assets in `Resources/RealPlayTests/` for runtime discovery.

---

## Running Tests
| Trigger | Description |
|---------|-------------|
| `F9` | Run all authored tests (Editor + Dev Builds) |
| `P` | Run **Realistic UI Demo** (visual verification) |
| `-runRealTests` | CLI: Run all, then quit with exit code = failure count |
| `--tags=smoke,ui` | CLI: Filter by tags |
| Menu: `RealPlayTester/Run All (F9)` | Editor menu item |

---

| `Tester.ResetCursor()` | Safely center the cursor. |

### Diagnostics

| Method | Description |
|--------|-------------|
| `TestLog.Info(string)` | Log informational message (routes to GameLogger/EventAggregator if available). |
| `TestLog.Warn(string)` | Log warning message. |
| `TestLog.Error(string)` | Log error message. |
| `Wait.UntilWithDiagnostics(predicate, timeout, context)` | Wait with enhanced timeout diagnostics. |
| `Wait.Step(string label)` | Update test context with current step. |
| `Tester.RecordIntent(string)` | Record agent's reasoning (included in AI reports). |

---

## 🏗️ Core API Facade (v2.0)
The legacy flat API is superseded by this nested structure, optimized for AI discovery.

### 🕹️ Tester.Interaction
Primary interface for deterministic input simulation.

| Method | Description |
|--------|-------------|
| `Mouse.Click(Vector2)` | Click at screen pixels (consistently normalized). |
| `Mouse.MoveTo(Vector2, float)` | Move mouse smoothly over duration. |
| `Mouse.MoveToCenter(GameObject, float)` | Precision move to object screen center. |
| `Keyboard.Type(string, float)` | Simulate typing into focused field. |
| `Keyboard.TypeIntoField(string, string)` | Find field by name and type. |
| `Touch.Pinch(PinchParams)` | Simulate pinch gesture using consolidated params. |

### 👁️ Tester.Perception
The "Eyes" of the agent.

| Method | Description |
|--------|-------------|
| `Find(string)` | Fuzzy-find active object in hierarchy. |
| `ProbeScreen(Vector2)` | Get semantic info for pixel position. |
| `DumpHierarchyJson()` | Export scene as minified, AI-friendly JSON. |
| `GetActionableElements()` | List all currently interactable targets. |

### 📊 Tester.State
Causal analysis and state snapshotting.

| Method | Description |
|--------|-------------|
| `Capture()` | Capture serializable JSON snapshot of full scene. |
| `CaptureObject(object)` | Snapshot a specific state object. |
| `GetDiff(string, string)` | Generate semantic diff between snapshots. |

### ⏳ Tester.Await
Deterministic synchronization.

| Method | Description |
|--------|-------------|
| `Seconds(float)` | Wait for scaled/unscaled time. |
| `Frames(int)` | Wait for specific frame count. |
| `Until(Func<bool>)` | Wait until predicate is true. |
| `Step(string)` | Record logical test step for diagnostics. |

### 🛡️ Tester.Assert
High-fidelity validation library.

| Method | Description |
|--------|-------------|
| `IsTrue/AreEqual` | Standard functional assertions. |
| `IsVisible(GameObject)` | Verify spatial/UI visibility. |
| `VisualStateMatches(name)` | Screen-diff against baseline image. |
| `NoMissingMaterials()` | Scan for shader/material errors ("Pink"). |

**TestRunContext**: Tracks test execution state:
- Test identity: TestName, TestId, StartTime, EndTime
- Environment: SceneName, UnityVersion, PackageVersion, ActiveInputMode
- State: LastAction, LastPanel, LastPlacementAttempt
- Export: `ToJson()`, `ToMarkdown()`

**TestRunContextTracker**: Automatically tracks the active test and writes snapshots to:
- `TestReports/current-test-context.json`
- `TestReports/current-test-context.md`

- **FailureBundle v2**: On test failure, generates bundle at `TestReports/FailureBundles/{timestamp}/{testName}/` containing:
    - `diagnostics.json` / `diagnostics.md`: Test context
    - `hierarchy.json`: **[New]** Full scene state in structured JSON format
    - `Logs/`: game.log, categorized session logs
    - `test-results.json`: Overall test results
    - Screenshots and interaction heatmaps

### Input Guards

| Method | Description |
|--------|-------------|
| `SimulatedInputGuard.InputSystemReady()` | Check if Input System is active and ready. |
| `SimulatedInputGuard.IsPointerOverUI()` | Check if pointer is over UI element. |
| `SimulatedInputGuard.EnsureInputReady()` | Throw error if input not ready. |

### UI Panel Monitoring

| Method | Description |
|--------|-------------|
| `PanelStateMonitor.CheckPanelState(GameObject)` | Get structured panel state info. |
| `PanelStateMonitor.CheckPanelState(string name)` | Get panel state by name. |
| `PanelStateMonitor.IsPanelReady(GameObject)` | Check if panel is visible and interactable. |
| `PanelStateMonitor.IsPanelReady(string name)` | Check panel readiness by name. |

**PanelState** struct:
- `IsVisible`: Panel active and alpha > 0.01
- `Alpha`: Combined alpha from all parent CanvasGroups  
- `Interactable`: Combined interactable state
- `ActiveInHierarchy`: GameObject active state

---

## Known Limitations & Batchmode
### Visibility (The "X-Ray" Problem)
`Assert.IsVisible(GameObject)` currently uses Unity's `renderer.isVisible`, which checks if an object is within the camera's **frustum**. It does **not** perform raycasting to check for physical occlusion (e.g., if the object is behind a wall).

### Headless / Batchmode Execution
When running tests via CLI (`-batchmode`):
- `renderer.isVisible` is unreliable because cameras do not render. The library automatically skips this specific check in batchmode to prevent false failures.
- `WaitForEndOfFrame` is not supported by Unity in batchmode. Use `yield return null` to wait for a frame instead.

---

## AI & Debugging Tools

These tools help AI agents diagnose test failures autonomously.

| Method | Description |
|--------|-------------|
| `Tester.DumpHierarchyJson()` | Returns minified JSON of scene hierarchy (best for LLM context). |
| `Tester.DumpHierarchy()` | Returns human-readable string dump of scene hierarchy. |
| `Tester.FindObject(string fuzzyName)` | Fuzzy find: exact → case-insensitive → contains match. |
| `Tester.ProbeScreen(Vector2 pos)` | Returns semantic description of what is under pixel. |
| `Tester.GetActionableElements()` | Returns JSON list of all buttons/inputs visible on screen. |
| `Tester.AssertNoLogErrors()` | Fail if any unexpected `Debug.LogError` occurred. |
| `Tester.ExpectLog(string regex)` | Mark expected error pattern (ignored by AssertNoLogErrors). |

**JSON Hierarchy Format**:
```json
{
  "name": "Canvas",
  "type": "Canvas",
  "visible": true,
  "children": [
    {
      "name": "SubmitButton",
      "tag": "Button",
      "text": "Login",
      "screenPos": [320, 240],
      "interactable": true
    }
  ]
}
```

---

## Report Customization

By default, test results are saved to `Application.persistentDataPath/test-results.json`. You can customize this behavior:

### Tier 1: Change Output Path
```csharp
TestRunner.ReportOutputPath = "/custom/path/my-results.json";
```

### Tier 2: Subscribe to Events
```csharp
// Receive the full report object
TestRunner.OnReportGenerated += (report) => {
    Debug.Log($"Tests: {report.totalTests}, Passed: {report.passed}");
    MyDashboard.Upload(report);
};

// Receive raw results list
TestRunner.OnAllTestsCompleted += (results) => {
    foreach (var r in results) { /* process */ }
};
```

### Tier 3: Custom Report Handler
```csharp
public class MyReporter : ITestReportHandler
{
    public void HandleReport(TestReport report, List<TestResult> results)
    {
        // Generate XML, send to Slack, store in DB, etc.
        // Default JSON is NOT written when custom handler is set.
    }
}

TestRunner.CustomReportHandler = new MyReporter();
```

---

## Runner Attributes
| Attribute | Description |
|-----------|-------------|
| `[TestTag("smoke")]` | Tag tests for filtering with `--tags=smoke`. |
| `[TestData("easy", "hard")]` | Parameterized test data. |
| `[TestData("PropertyName", values...)]` | Data injected into property. |

---

## Tester Facade
All API methods are now logically grouped under `Tester.*` nested categories:
- `Tester.Interaction.*`: Input simulation (Mouse, Keyboard, Touch).
- `Tester.Perception.*`: Scene discovery and semantic probing.
- `Tester.State.*`: Consequence analysis and snapshotting.
- `Tester.Await.*`: Deterministic synchronization.
- `Tester.Assert.*`: Comprehensive validation.

---

## Behavior & Guarantees
- **Real Inputs**: Uses `EventSystem`, `ExecuteEvents`, `PointerEventData`. Never calls `.onClick.Invoke()`.
- **Auto-Scroll & Clamp**: Automatically scrolls targets into view and clamps clicks to screen valid area.
- **Input System**: Supports both Legacy Input Manager (limited) and new Input System (full).
- **Dev-Build Guard**: Package is auto-disabled in non-development builds.
- **TearDown Safety**: `TearDown()` is always called, even on test failure.
- **No Dependencies**: Self-contained, no external packages required.
- **Modern Unity APIs**: Uses `FindFirstObjectByType` / `FindObjectsByType` (Unity 2022.1+) for optimal performance.

---

## File Structure
```
RealPlayTester/
├── Runtime/          # Core Tools, Input (Click, Press, Drag, Text, Touch, Scroll, SmartFind)
├── Editor/           # Build & Scene Setup Helpers
├── Await/            # Wait Utilities
├── Assert/           # Assertion Library, Capture, LogAssert
└── Utilities/        # DevTools, VisualTreeLogger, InteractionProbe
```
*Note: This package is a **Tools-Only** library. All internal verification tests and sample assets are kept external to ensure a lean distribution.*

## Version History
See [CHANGELOG.md](CHANGELOG.md) for version history.

---

**Note**: For production verification, create test assets in your project's `Assets/Resources/RealPlayTests/` folder, not in the package itself.
