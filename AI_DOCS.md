# AI Documentation: UnityRealPlayTester

## 1. Core Testing Architecture

### Feature: RealPlayTest (ScriptableObject)
**Architecture Intent:**
Decouple testing from the Unity Editor's ephemeral state. By using ScriptableObjects, tests exist as permanent assets in the project, allowing for serialization of test parameters, drag-and-drop configuration, and independence from scene reloads. This is the entry point for the "Ironclad Pipeline".

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
Use this when the user requests a new Play Mode test. Always prefer inheriting from `RealPlayTest` over `MonoBehaviour` or `NUnit` classes for integration with the RealPlay runner.

**Edge Case Handling:**
- **Scene Transition**: If the test spans multiple scenes, ensure `PlayModeTestAttribute` is set to the *initial* scene.
- **Async Void**: Never use `async void` in tests; strictly use `protected override async Task Run()`.

---

### Feature: TestRunner
**Architecture Intent:**
A unified execution engine that handles discovery, dependency injection waiting, and crash recovery. It enforces the "Ironclad" philosophy by wrapping every test in a `try-catch` block that captures failure screenshots and diagnostic bundles before bubbling the exception.

**Implementation Syntax:**
```csharp
// CLI usage for CI/CD
// -runRealTests
// -realplay-stdout

// Editor usage
TestRunner.Instance.RunTestByName("MyFeatureTest");
```

**AI Trigger Conditions:**
Use this when the user asks how to run tests in CI/CD, or needs to programmatically execute a specific test suite from an editor script.

**Edge Case Handling:**
- **Infinite Loops**: The runner enforces a default 120s timeout (configurable via attribute).
- **Domain Reloads**: The runner persists state across domain reloads using static GUID tracking (internally managed).

---

## 2. Synchronization Primitives

### Feature: Wait System (Async/Await)
**Architecture Intent:**
Replace unstable Coroutines and `Update()` loops with linear, readable `async/await` flows. The system handles `Time.timeScale` internally, preventing tests from hanging when the game is paused.

**Implementation Syntax:**
```csharp
await Tester.Wait.Seconds(1.5f);
await Tester.Wait.Until(() => player.Health > 0, timeoutSeconds: 5f);
await Tester.Wait.ForObject("VictoryScreen");
```

**AI Trigger Conditions:**
Use this for **ALL** temporal logic. Never use `Thread.Sleep` (freezes Unity) or `Task.Delay` (ignores Unity time scale).

**Edge Case Handling:**
- **Paused Game**: Use `unscaled: true` in `Wait.Seconds` if the game might be paused (`Time.timeScale = 0`).
- **Object Destruction**: `Wait.Until` handles MissingReferenceException internally if checking a destroyed object, but `Wait.ForObject` is safer for existence checks.

---

## 3. Interaction & Input

### Feature: Semantic Interaction (Tester.Interaction.Perform)
**Architecture Intent:**
Abstract low-level input (mouse clicks, touch taps) into high-level user intents ("Click", "Submit", "Drag"). This ensures tests verify *gameplay logic*, not just screen coordinates. It automatically handles device differences (Mouse vs Touch vs Gamepad).

**Implementation Syntax:**
```csharp
// High-level intent
await Tester.Interaction.Perform("Click", "StartButton");

// Low-level fallback
await Tester.Interaction.Tap(new Vector2(500, 500));
```

**AI Trigger Conditions:**
Use `Perform` when the user wants to interact with UI or game objects by name. Use low-level inputs (Mouse/Touch/Gamepad) only when testing specific input mechanics (e.g., "drag exactly 10 pixels").

**Edge Case Handling:**
- **Occlusion**: The system checks `RealPlayOcclusionRaycaster` before clicking. If an object is covered by UI, `Perform` will fail with a warning log rather than clicking the wrong thing.
- **Off-Screen**: The interaction system does *not* auto-scroll. You must `await Tester.Advanced.ScrollUntilVisible(...)` first.

---

## 4. Verification & Assertions

### Feature: Ironclad Assertions
**Architecture Intent:**
Fail loud and fast. Unlike NUnit, `Tester.Assert` failures immediately:
1. Pause the game (`Time.timeScale = 0`).
2. Capture a high-res screenshot.
3. Draw a red overlay on the screen with the error message.
4. Throw an exception to stop execution.

**Implementation Syntax:**
```csharp
Tester.Assert.IsTrue(player.IsAlive, "Player should be alive");
Tester.Assert.ScreenElementVisible("InventoryPanel");
Tester.Assert.VisualStateMatches("MainMenu"); // Comparison against baseline JSON
```

**AI Trigger Conditions:**
Use this for all verification steps. Do not use `Debug.Assert` or standard NUnit asserts, as they do not trigger the diagnostic artifact generation.

**Edge Case Handling:**
- **Flaky UI**: Use `await Tester.Wait.ForUIVisible(...)` *before* asserting visibility to account for fade-in animations.

---

## 5. Perception & Diagnostics

### Feature: Semantic DOM
**Architecture Intent:**
Provide a structured, text-based representation of the visual scene for AI agents. It maps the Unity Hierarchy to a simplified "DOM" containing screen coordinates, visibility, and interactability flags.

**Implementation Syntax:**
```csharp
string dom = RealPlaySemanticDOMDumper.Dump(filterToViewport: true);
// Output: JSON-like structure with "VisualID", "Role", "Command"
```

**AI Trigger Conditions:**
Use this when an AI agent needs to "see" the screen to make a decision, or when debugging why an interaction failed (check `Interactable` and `IsOccluded` flags).

**Edge Case Handling:**
- **Large Scenes**: Always set `filterToViewport: true` for large open worlds to avoid performance spikes.
- **Dynamic Content**: The DOM is a snapshot. If the UI changes (e.g., a popup appears), you must dump it again.

### Feature: TestRunContext (Diagnostics)
**Architecture Intent:**
Attach rich metadata (logs, breadcrumbs, last known state) to failure reports. This allows for post-mortem debugging without re-running the test.

**Implementation Syntax:**
```csharp
TestRunContextTracker.RecordBreadcrumb("Action", "Opened Inventory");
// Automatically included in failure bundles
```

**AI Trigger Conditions:**
Use `RecordBreadcrumb` inside complex test logic to leave a trail of "what happened" before a crash.

---

## 6. Integration Guide (Plug-and-Play)

To integrate UnityRealPlayTester into a new project:

1.  **Installation**:
    Copy the `RealPlayTester` folder to `Packages/` or `Assets/Plugins/`.

2.  **Assembly Definition**:
    Ensure your test assembly references `RealPlayTester`.
    ```json
    {
        "name": "MyGame.Tests",
        "references": [ "RealPlayTester" ],
        "includePlatforms": [],
        "excludePlatforms": []
    }
    ```

3.  **Folder Structure**:
    Create `Assets/Resources/RealPlayTests/`. The `TestRunner` automatically scans this specific folder for `RealPlayTest` assets.

4.  **First Run**:
    Press **F12** (default hotkey) or use `Tools > RealPlayTester > Run All` to initialize the bootstrap system.
