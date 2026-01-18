# AI Documentation: UnityRealPlayTester

**Version:** 2.9.0
**Purpose:** This document is the primary instruction manual for AI Agents operating with or within the `UnityRealPlayTester` library.

---

## 1. Core Directives

### The "Ironclad" Pipeline
1.  **Fail Fast**: Tests must immediately halt on failure (Assertion exception) to preserve state.
2.  **No Flakiness**: Use `Tester.Wait` for *everything*. Never assume a frame has passed.
3.  **Thread Safety**: All Unity API calls must run on the Main Thread. Use `SimulatedInputGuard.EnsureMainThread()` if in doubt.
4.  **Semantic First**: Interact with objects by *intent* ("Click PlayButton"), not coordinates ("Click 100,200").

---

## 2. Core Testing Architecture

### Feature: RealPlayTest (ScriptableObject)
**Architecture Intent:**
Decouple testing from the Unity Editor's ephemeral state. Tests are assets (`.asset`), not code files alone. This allows for serialization of test parameters and persistence across domain reloads.

**Implementation Syntax:**
```csharp
[PlayModeTest(Scene = "GameScene", TimeScale = 1.0f)]
public class MyFeatureTest : RealPlayTest {
    protected override async Task Run() {
        await Tester.Wait.ForObject("PlayerCharacter");
        // Test logic here
    }
}
```

**AI Trigger Conditions:**
Use this when creating a new Play Mode test. Always inherit from `RealPlayTest`.

**Edge Case Handling:**
- **Scene Transition**: If testing a flow across scenes, `PlayModeTestAttribute` sets the *starting* scene only.
- **Async Void**: Strictly forbidden. Use `protected override async Task Run()`.

---

### Feature: TestRunner
**Architecture Intent:**
A robust execution engine that handles crash recovery, timeout enforcement (default 120s), and dependency injection waiting. It wraps tests in a safety net that captures diagnostic bundles upon failure.

**Implementation Syntax:**
```csharp
// Programmatic Run
TestRunner.Instance.RunTestByName("MyFeatureTest");

// CLI for CI/CD
// -runRealTests -realplay-stdout
```

**AI Trigger Conditions:**
Use when orchestrating test execution (e.g., "Run all tests tagged 'UI'").

**Edge Case Handling:**
- **Infinite Loops**: Hard timeout prevents CI hangs.
- **Reloads**: Supports "Hot Reload" via `ISerializationCallbackReceiver` state persistence.

---

## 3. Synchronization Primitives

### Feature: Wait System (Async/Await)
**Architecture Intent:**
Replace `Coroutine` hell with linear `async` code. Handles `Time.timeScale` automatically (e.g., waits longer if game is in slow-motion).

**Implementation Syntax:**
```csharp
// Basic Time
await Tester.Wait.Seconds(1.5f);
await Tester.Wait.Frames(10);

// Semantic Waits
await Tester.Wait.Until(() => player.Health > 0, timeoutSeconds: 5f);
await Tester.Wait.ForObject("VictoryScreen");
await Tester.Wait.ForUIVisible("PauseMenu");
await Tester.Wait.ForStablePhysics();
```

**AI Trigger Conditions:**
**ALWAYS** use `Wait` before checking a condition or after an interaction.

**Edge Case Handling:**
- **Paused Game**: Use `Wait.Seconds(1f, unscaled: true)` if `Time.timeScale` might be 0.
- **Destroyed Objects**: `Wait.Until` safely handles `MissingReferenceException`.

---

## 4. Interaction & Input

### Feature: Semantic Interaction (Perform)
**Architecture Intent:**
High-level intents ("Click", "Submit", "Drag") are translated into device-specific inputs (Mouse, Touch, Gamepad). This ensures cross-platform coverage without changing test code.

**Implementation Syntax:**
```csharp
// Semantic
await Tester.Interaction.Perform("Click", "StartButton");

// Low-Level (Use sparingly)
await Tester.Interaction.Mouse.Click(new Vector2(500, 500));
```

**AI Trigger Conditions:**
Use `Perform` for all standard gameplay actions.

**Edge Case Handling:**
- **Occlusion**: The system raycasts before clicking. If blocked, it warns and aborts.
- **Input Flooding**: Throttled to `RealPlaySettings.MaxInputEventsPerFrame` (default 1000) to prevent buffer overflows.

### Feature: SmartFind (Spatial Ranking)
**Architecture Intent:**
When multiple objects have the same name (e.g., "Confirm"), `SmartFind` ranks them to pick the "most interactable" one.
**Ranking Logic:**
- **+50,000 pts**: Is UI Element.
- **+10,000 pts**: Is Visible (not occluded).
- **-20,000 pts**: Logically Blocked.

**Implementation Syntax:**
```csharp
// Implicitly used by Perform("Click", "Confirm")
// Explicit use:
var target = RealPlayTester.Input.SmartFind.Find("Confirm");
```

**AI Trigger Conditions:**
Use when `Perform` is selecting the wrong object or when you need to target a specific instance in a list.

**Edge Case Handling:**
- **Ambiguity**: If multiple objects have identical scores, the system uses sorting order or depth.
- **Occlusion**: `SmartFind` will prioritize visible objects but may still return an occluded one if no better options exist. Check `IsOccluded` manually if strict visibility is required.

---

## 5. Perception & Vision

### Feature: Semantic DOM (VLM Ready)
**Architecture Intent:**
Provide a text-based "vision" of the scene for LLMs. Includes `VisualID` tags (e.g., `#A1`) that match annotated screenshots.

**Implementation Syntax:**
```csharp
string dom = RealPlaySemanticDOMDumper.Dump(filterToViewport: true);
// Output: JSON with "VisualID": "#A1", "Command": "click('Button')"
```

**AI Trigger Conditions:**
Use when an AI Agent needs to "see" the screen layout or debug why an element is unreachable.

**Edge Case Handling:**
- **Large Scenes**: Always set `filterToViewport: true` for large open worlds.
- **Depth**: The dumper has a recursion limit (1024) to prevent stack overflows.

### Feature: Annotated Screenshots
**Architecture Intent:**
Bridge the gap between pixels and code. Generates a screenshot where every interactable object is labeled with its `VisualID`.

**Implementation Syntax:**
```csharp
Dictionary<string, string> labels;
Texture2D annotated = Tester.Screenshot.CaptureWithAnnotations(out labels);
// labels["#A1"] == "PlayButton"
```

**AI Trigger Conditions:**
Use when feeding visual data to a Vision-Language Model (e.g., GPT-4o, Gemini).

**Edge Case Handling:**
- **Empty Map**: If no objects are interactable, `labelMap` will be empty.
- **Off-Screen**: Annotations are only drawn for objects within the screen bounds.

---

## 6. Autonomous Testing Tools

### Feature: Chaos Monkey
**Architecture Intent:**
Automated fuzzing. The "Monkey" randomly interacts with valid targets to find crashes, memory leaks, and logic errors without a script.

**Implementation Syntax:**
```csharp
// Run for 60 seconds, action every ~0.3s
await Tester.Advanced.StartChaosMonkey(durationSeconds: 60f, interactionInterval: 0.3f);
```

**AI Trigger Conditions:**
Use when the user asks to "stress test" or "find edge cases" in a specific scene.

**Edge Case Handling:**
- **Infinite Loop**: If `durationSeconds` is 0, it runs forever until `StopChaosMonkey()` is called.
- **Destructive Actions**: The monkey cannot distinguish between "Save Game" and "Delete Save". Use in safe environments only.

### Feature: Visual Health Monitor
**Architecture Intent:**
Continuous background scanning for rendering errors (Pink shaders, null materials).

**Implementation Syntax:**
```csharp
Tester.Monitoring.StartVisualHealthCheck(interval: 2.0f);
// Runs in background until stopped or test ends
Tester.Monitoring.StopVisualHealthCheck();
```

**AI Trigger Conditions:**
Use in long-running tests or "Smoke Tests" to catch asset corruption.

**Edge Case Handling:**
- **False Positives**: Custom shaders that intentionally look pink (magenta) might trigger this.
- **Performance**: High-frequency checks (>10Hz) may impact frame rate.

### Feature: Interaction Heatmap
**Architecture Intent:**
Visualize where the agent/test has clicked. Useful for verifying coverage.

**Implementation Syntax:**
```csharp
Tester.Settings.ShowInteractionHeatmap = true;
// ... Run test ...
Tester.Screenshot.SaveHeatmap("MyTestCoverage");
```

**AI Trigger Conditions:**
Use when analyzing test coverage or debugging "ghost clicks" (clicks that hit nothing).

**Edge Case Handling:**
- **Persistence**: Heatmap points are cleared on scene load unless explicitly persisted.
- **Overdraw**: Too many points can obscure the screen. Max point limit is enforced internally.

---

## 7. Verification & Assertions

### Feature: Ironclad Assertions
**Architecture Intent:**
On failure: **Pause Time**, **Capture Screenshot**, **Draw Red Overlay**, **Throw Exception**.

**Implementation Syntax:**
```csharp
Tester.Assert.IsTrue(hp > 0, "Hero died");
Tester.Assert.ScreenElementVisible("HUD");
Tester.Assert.VisualStateMatches("MainMenu_Baseline"); // Image Comparison
Tester.Assert.NoMissingMaterials(); // Scan for pink shaders
```

**AI Trigger Conditions:**
Use for *all* pass/fail checks.

**Edge Case Handling:**
- **Time Scale**: Asserts freeze the game (`timeScale = 0`). You must manually unpause or reload the scene to recover if continuing in the same session.

---

## 8. Configuration & Security

### Feature: RealPlaySettings
**Architecture Intent:**
Central configuration hub.

**Key Settings:**
- `PreferredMode`: `Auto` (default), `Mouse`, `Touch`, `Gamepad`.
- `MaxInputEventsPerFrame`: Flood protection (Default 1000).
- `ForceBatchmodeVisibility`: Bypass UI animations in CI (Headless).
- `EnableNavigationLearning`: Record state transitions for AI training.

**Implementation Syntax:**
```csharp
Tester.Settings.PreferredMode = PreferredInputMode.Touch;
Tester.Settings.ForceBatchmodeVisibility = true;
```

**AI Trigger Conditions:**
Use to customize behavior for specific platforms (e.g., forcing Touch mode on desktop) or CI environments.

**Edge Case Handling:**
- **Reset**: Settings persist until domain reload or `ResetStaticState` is called. Ensure you reset modified settings in `TearDown` if sharing the session.

### Feature: Thread Safety Guards
**Architecture Intent:**
Unity API is not thread-safe. `SimulatedInputGuard` protects against accidental background thread calls that would crash the engine.

**Implementation Syntax:**
```csharp
// Internal use, but good to know:
SimulatedInputGuard.EnsureMainThread();
```

**AI Trigger Conditions:**
Use when implementing custom async logic or background tasks.

**Edge Case Handling:**
- **WebGL**: On WebGL, threading is single-threaded, so this check is bypassed or adapted.

---

## 9. Diagnostics

### Feature: TestRunContext
**Architecture Intent:**
Track the "Story" of the test. What happened before the crash?

**Implementation Syntax:**
```csharp
TestRunContextTracker.RecordBreadcrumb("Action", "Opened Inventory");
// Automatically included in Failure Bundles
```

**AI Trigger Conditions:**
Use to add context to logs, especially before complex operations.

**Edge Case Handling:**
- **Overflow**: Breadcrumbs are circular buffers (cap ~2000) to prevent memory leaks in long runs.

### Feature: TestLog
**Architecture Intent:**
Centralized logging that routes to `Debug.Log`, but also tries to hook into `GameLogger` or `EventAggregator` if they exist in the project.

**Implementation Syntax:**
```csharp
TestLog.Info("Starting Phase 2");
TestLog.Error("Spawn failed");
```

**AI Trigger Conditions:**
Use for high-level status updates. Prefer this over `Debug.Log` for test-related output.

**Edge Case Handling:**
- **Missing Hooks**: If `GameLogger` is missing, it gracefully degrades to standard Unity `Debug.Log`.

---

## 10. Integration Guide

To install UnityRealPlayTester in a new project:

1.  **Copy Files**: Place the `RealPlayTester` folder in `Assets/Plugins/` or `Packages/`.
2.  **Assembly Definition**: Add reference to `RealPlayTester` in your test assembly's `.asmdef`.
3.  **Test Directory**: Create `Assets/Resources/RealPlayTests/`.
4.  **Bootstrap**: Run **Tools > RealPlayTester > Run All** (or F9) to initialize.

**Required Packages:**
- `com.unity.ugui` (for UI testing)
- `com.unity.textmeshpro` (optional, better text support)
- `com.unity.inputsystem` (optional, enhanced input simulation)
