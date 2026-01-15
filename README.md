# RealPlayTester (v2.4.2) - Technical Manual for AI Agents

RealPlayTester is an **AI-Native Automation Framework** for Unity. It provides LLM-based agents with structured sensory input (JSON), deterministic motor controls (Hardware-mimicking input), and causal verification tools.

---

## 👁️ Sensory Input (`Tester.Perception`)
Your primary way of "seeing" the world is through the **Semantic DOM**.

### `Perception.DumpHierarchyJson()`
Returns a tree of `SemanticNode` objects representing the current scene state. Use this to find targets and understand their affordances.

**JSON Schema:**
```json
{
  "Name": "Btn_Attack",         // Object identifier
  "Role": "Button",             // Semantic category (Button, InputField, Slider, etc.)
  "Active": true,               // Activation state
  "Interactable": true,         // Combined UI/Logic interaction state
  "ScreenRect": { "x": 0, "y": 0, "width": 100, "height": 50 }, // Pixel coordinates
  "Text": "ATTACK",             // Text content (from Text/TMP components)
  "ZDepth": 10.5,               // Distance from camera
  "IsOccluded": false,          // True if hidden behind another object
  "LogicalBlocked": false,      // True if a script prevents interaction (e.g., isLocked)
  "Affordances": ["Click"],     // Valid actions for this object
  "Children": []                // Child nodes in hierarchy
}
```

### Perception Helpers:
- `Find(fuzzyName)`: Spatial fuzzy search. Ranks results by visibility, sorting order, and depth.
- `Probe(Vector2)`: Returns semantic metadata for the object at specific pixel coordinates.
- `DescribeRegion(Rect)`: Lists all interactable objects within a specific screen area.
- `IsOccluded(GameObject)`: Performs a physical raycast check to see if an object is blocked.

---

## 🕹️ Motor Controls (`Tester.Interaction`)
Deterministic hardware-level simulation. Prefer `Perform` for automatic feedback loops.

### `Interaction.Perform(intent, target)`
Executes a high-level intent (`click`, `drag`, `type`, `submit`) and returns a **Causal Reaction Summary**.
- **Example**: `await Perform("click", "Btn_Start")` -> `"Action: Click 'Btn_Start' -> Result: MainMenu hidden, GamePanel appeared."`

### Direct Inputs:
- `Mouse.Click(Vector2)` / `Mouse.Drag(from, to, duration)`
- `Keyboard.Type(text)` / `Keyboard.Press(KeyCode)` / `Keyboard.Held(KeyCode)`
- `Touch.Tap(Vector2)` / `Touch.Swipe(from, to)` / `Touch.Pinch(center, start, end)`
- `Gamepad.Click(GamepadButton)` / `Gamepad.MoveStick(leftStick, Vector2)`

---

## ⏳ Synchronization (`Tester.Await`)
Unity is async. Use these to wait for the world to catch up to your actions.

- `ForSteadyState()`: **Essential.** Waits until physics, animations, and hierarchy stop changing. Ignores ambient motion (wind, breathing).
- `ForUIVisible(name)`: Waits for an object to be active and have alpha > 0.
- `Seconds(n, unscaled: true)`: Real-time wait. Use `unscaled: true` to bypass `Time.timeScale = 0` (paused game).
- `SceneLoaded(name)` / `ForLoadingComplete()`: Use when transitioning between levels.
- `ForStablePhysics()`: Waits until all rigidbodies come to rest.

---

## ⚖️ Judgment & Verification (`Tester.Assert`)
Hardware-validated assertions for verifying game state.

- `IsVisible(GO)`: Performs both frustum and occlusion raycast checks.
- `ScreenElementVisible(name)`: Verifies presence and visibility of an object by name.
- `AreEqual(expected, actual)` / `IsTrue(cond)`: Standard logical assertions.
- `StateMatches(stateObject, baselineJson)`: Deep object state verification.
- `NoUnexpectedErrors()`: Fails the test if any `LogType.Error` or `Exception` was caught.

---

## 🧭 Strategy & Memory (`Tester.State`, `Tester.Navigation`)
- `State.Capture()`: Takes a full-scene state snapshot.
- `State.GetDiff(before, after)`: Explains what changed between two snapshots in natural language.
- `Navigation.Navigate(from, to)`: Replays a learned path between two game states.
- `Navigation.SaveToFile(path)`: Persists learned navigation maps for future sessions.

---

## 🛠️ Diagnostics & Performance
- `Screenshot.CaptureWithAnnotations()`: Generates a texture with bright boxes and IDs (`#A1`, `#B2`) for Vision LLMs.
- `Performance.StartMonitoring()`: Tracks FPS spikes and `GC.Alloc` violations.
- `Utility.TestStep(msg)` / `RecordBreadcrumb(msg)`: Injects logical steps into the diagnostic timeline.
- `Utility.Debug.Breakpoint()`: Pauses execution and waits for `Space` key (useful for human inspection).

---

## ⚙️ Settings (`Tester.Settings`)
Adjust these to tune your "Vision" and "Hands":
- `PreferredMode`: `Mouse`, `Touch`, or `Gamepad`.
- `FilterDOMToViewport`: If true, `DumpHierarchyJson` only includes objects the camera can see.
- `CollapseRepetitiveSiblings`: If true, groups 50+ similar tiles into one "CollapsedGroup" node to save tokens.
- `AutoSimulatePhysics`: If true, `Physics.Simulate()` runs during `Await` calls in batchmode.

---

## 🚀 Execution & CLI
- **F9**: Run all play tests.
- **P**: Interactive UI Demo.
- CLI: `-runRealTests` (batchmode runner), `-realplay-stdout` (stream logs to console).

---
**License**: MIT | **Author**: Daliys
