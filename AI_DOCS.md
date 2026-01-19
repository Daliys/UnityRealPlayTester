# AI Documentation: UnityRealPlayTester

**Version:** 2.9.0
**Purpose:** Primary instruction manual for AI Agents operating with the `UnityRealPlayTester` library.

---

## 1. Core Directives (The "Rules")

### The "Ironclad" Pipeline
1.  **Fail Fast**: Tests must immediately halt on failure (Assertion exception) to preserve state.
2.  **No Flakiness**: Use `Tester.Wait` for *everything*. Never assume a frame has passed.
3.  **Thread Safety**: All Unity API calls must run on the Main Thread. Use `SimulatedInputGuard.EnsureMainThread()` if in doubt.
4.  **Semantic First**: Interact with objects by *intent* ("Click PlayButton"), not coordinates ("Click 100,200").

---

## 2. Testing Architecture

### Feature: RealPlayTest (ScriptableObject)
**AI Trigger:** Use this when asked to create a new Play Mode test.
**Syntax:**
```csharp
[PlayModeTest(Scene = "Menu", TimeScale = 1.0f)]
public class MyTest : RealPlayTest {
    protected override async Task Run() { /* Logic */ }
}
```

---

## 3. Synchronization (Wait System)

### Feature: Wait Primitives
- `await Tester.Wait.Seconds(f)`: Use for small delays or animations.
- `await Tester.Wait.Until(() => cond)`: Use for logic-based gates.
- `await Tester.Wait.UntilWithDiagnostics(() => cond, 5f, "Context")`: **PREFERRED** for critical gates to provide better failure logs.

### Feature: Steady State & Physics
- `await Tester.Wait.ForSteadyState()`: **AI Trigger:** Use after scene loads or major UI transitions. Ensures physics and animations have settled.
- `await Tester.Wait.ForStablePhysics()`: **AI Trigger:** Use in gameplay tests before checking positioning/collisions.

---

## 4. Interaction & Input

### Feature: Semantic Perform
- `await Tester.Interaction.Perform(intent, target)`: **AI Trigger:** The default way to interact. It handles spatial ranking and chooses the best input method (Mouse/Touch).
- **Intents:** "Click", "Tap", "Submit", "Drag", "Hover", "Type".

### Feature: Complex Input
- **Gamepad**: Use `Tester.Interaction.Gamepad` for console-like testing.
- **Gestures**: Use `Tester.Interaction.Gestures.Pinch` or `Twist` for mobile-specific mechanics.
- **Keyboard**: Use `Tester.Interaction.Keyboard.Type("text")` for input fields.

### Feature: Pre-Flight Validation
- `Tester.Interaction.PreFlight(intent, target)`: **AI Trigger:** Use before a destructive or complex action to ensure the target isn't occluded or logically blocked.

---

## 5. State & Navigation

### Feature: Causal State Tracking
**AI Trigger:** Use to verify that an action actually *did* something.
```csharp
var snapshot = Tester.State.Capture();
await Tester.Interaction.Perform("Click", "BuyButton");
string diff = Tester.State.GetDiff(snapshot, Tester.State.Capture());
// diff will contain: "Text 'Gold' changed '100'->'50'"
```

### Feature: Navigation Graph
**AI Trigger:** Use when the test needs to move to a different menu or screen.
- `await Tester.Navigation.Navigate("MainMenu", "Settings")`: The system finds the clicks needed to get there.
- **Component:** `RealPlayState`: Attach to UI panels to define state names for the graph.

---

## 6. Perception & Vision (For VLMs)

### Feature: Semantic DOM & Visual IDs
**AI Trigger:** Use when you (the AI) need to "see" the screen.
- `Tester.Perception.DumpHierarchyJson()`: Returns a JSON tree where every object has a `VisualID` (e.g., `#A1`).
- `Tester.Screenshot.CaptureWithAnnotations(out labels)`: Generates a screenshot where objects are labeled with their `#A1` IDs.
- **Usage:** Match the `#A1` in the JSON to the `#A1` in the image to verify spatial layout.

---

## 7. Performance & Health

### Feature: Technical Debt Monitoring
- `Tester.Performance.StartMonitoring()`: **AI Trigger:** Use in "Smoke Tests" or "Soak Tests" to catch memory leaks or FPS drops.
- `Tester.Monitoring.StartVisualHealthCheck()`: **AI Trigger:** Use to detect "Pink Shaders" or missing assets during play.

---

## 8. Assertions

### Feature: Ironclad Assertions
**AI Trigger:** Use for all pass/fail checks.
- `Tester.Assert.IsTrue(condition, "Reason")`
- `Tester.Assert.ScreenElementVisible("Target")`
- `Tester.Assert.NoUnexpectedErrors()`: **MANDATORY** at the end of every test to catch silent errors.

---

## 9. Comprehensive Example (Master Pattern)

```csharp
[PlayModeTest(Scene = "StoreScene")]
public class StorePurchaseTest : RealPlayTest {
    protected override async Task Run() {
        // 1. Setup Environment
        Tester.Performance.StartMonitoring();
        await Tester.Wait.ForSteadyState();

        // 2. Capture Initial State
        var before = Tester.State.Capture();

        // 3. Perform Validated Action
        var pre = await Tester.Interaction.PreFlight("Click", "BuySword");
        Tester.Assert.IsTrue(pre.Success, $"Cannot buy: {pre.BlockReason}");
        
        await Tester.Interaction.Perform("Click", "BuySword");

        // 4. Verify Consequence
        await Tester.Wait.ForSteadyState();
        var diff = Tester.State.GetDiff(before, Tester.State.Capture());
        Tester.Assert.Contains(diff, "Gold changed", "Gold should decrease");

        // 5. Final Cleanup/Audit
        Tester.Assert.NoUnexpectedErrors();
    }
}
```
