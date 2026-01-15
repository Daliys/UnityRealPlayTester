# RealPlayTester (v2.4.2)

## Overview
RealPlayTester is a high-fidelity automation library designed exclusively for **Autonomous AI Agents** and **LLM-driven testing**. It provides agents with "eyes" (semantic probes), "ears" (structured log streams), and "hands" (deterministic human-like input) to navigate and test Unity applications as a human would.

**Compatibility**: Unity 6000.2+ | URP / Built-in | Editor | Development Builds
**Orientation**: **AI-Native** (Optimized for JSON-parsing and vision-based reasoning)

---

## 🤖 AI-Native Design Principles
Unlike traditional frameworks designed for humans, RealPlayTester prioritizes **context-rich observability**:

### 🎯 Interaction Affordance Map
The semantic DOM explicitly lists possible actions for every element, reducing token waste:
- **Affordances**: `["Click", "Drag", "Type", "LongPress", "Scroll"]`
- **Logic Blocks**: Detects script-level blocks like `bool IsLocked` via reflection.

### ⚡ Causal Reaction Summaries
`Perform()` captures state before and after an action, returning a natural language summary:
- `"Action: Click 'Buy' -> Result: Gold changed 100->50, Inventory Item appeared."`

### 🖼️ Visual Annotation Overlays
`Tester.Screenshot.CaptureWithAnnotations` generates images with bright bounding boxes and labels (`#A1`, `#B2`) baked in for Vision Models (GPT-4o, Claude 3.5).

### 🧭 Navigation Graph
Register paths and allow agents to replay complex sequences:
- `Tester.Navigation.Navigate("MainMenu", "Audio")`

---

## 🏗️ Core AI API Reference

### 🕹️ Tester.Interaction
Deterministic input simulation mimicking hardware events.

| Method | Description |
|--------|-------------|
| `Perform(intent, target)` | High-level action. Returns causal summary. Intent: `click`, `drag`, `type`, `submit`. |
| `PreFlight(intent, target)` | Dry-run validation. Returns `Success/Blocked + Reason`. Checks visibility, occlusion, and logic. |
| `Mouse.Click(Vector2)` | Simulates hardware mouse click at pixel coordinates. |
| `Mouse.Drag(from, to, duration)` | Precision drag simulation. |
| `Keyboard.Type(text)` | High-speed character injection into focused input fields. |
| `Keyboard.Press(KeyCode)` | Simulates physical key press. |
| `Touch.Tap(Vector2)` | Simulates mobile touch event. |
| `Gamepad.Click(GamepadButton)` | Simulates controller input. |

### 👁️ Tester.Perception
The agent's "eyes" into the scene hierarchy and physics.

| Method | Description |
|--------|-------------|
| `DumpHierarchyJson()` | **Primary Tool.** Returns minified JSON of the entire scene (Semantic DOM). |
| `Find(fuzzyName)` | Uses fuzzy matching and spatial ranking to find the best GameObject match. |
| `Probe(Vector2)` | Returns semantic info for the object at a specific pixel. |
| `IsOccluded(GameObject)` | Raycasts to check if object is blocked by UI or World geometry. |
| `DescribeRegion(Rect)` | Lists all interactable elements within a screen area. |

### ⏳ Tester.Await
Advanced synchronization helpers.

| Method | Description |
|--------|-------------|
| `ForSteadyState()` | Waits until physics, animations, and hierarchy stop changing (ignores ambient motion). |
| `ForUIVisible(name)` | Robust wait for UI elements, including those in inactive hierarchies. |
| `Until(predicate)` | Custom polling wait. |
| `Seconds(n, unscaled:true)` | Real-time wait (immune to `Time.timeScale = 0`). |

### 📊 Tester.State & Assert
Verification and consequence analysis.

| Method | Description |
|--------|-------------|
| `Capture()` | Takes a serializable snapshot of the entire game state. |
| `GetDiff(before, after)` | Returns human-readable delta between two state snapshots. |
| `Assert.IsVisible(GO)` | Hardware-verified visibility check. |
| `Assert.ScreenElementVisible(name)` | Visual grounding assertion. |

---

## 📄 Semantic DOM JSON Schema
`DumpHierarchyJson()` returns a tree of `SemanticNode` objects:

```json
{
  "Name": "Button_Start",
  "Role": "Button",
  "Active": true,
  "Interactable": true,
  "ScreenRect": { "x": 100, "y": 200, "width": 50, "height": 30 },
  "Text": "START GAME",
  "IsOccluded": false,
  "LogicalBlocked": false,
  "Affordances": ["Click", "Hover"],
  "Children": []
}
```

---

## 🛠️ Execution & Workflow

### Hotkeys (Play Mode)
- **`F9`**: Run all discovered `RealPlayTest` assets in the project.
- **`P`**: Trigger internal UI Interaction Demo.

### CLI Arguments
- `-runRealTests`: Executes all tests and quits with exit code = failure count.
- `--tags=Smoke,Core`: Filter tests by assigned tags.
- `-realplay-stdout`: Redirects all internal library logs to standard output.

---

## 💡 AI Best Practices for Agents
1. **Prefer `Perform()` over raw `Click()`**: It provides the causal feedback loop necessary for reasoning about the game's response.
2. **Use `PreFlight()` before acting**: Save tokens and time by checking if an object is occluded or logically disabled before attempting interaction.
3. **Wait for Steady State**: Always call `await Tester.Await.ForSteadyState()` after loading scenes or opening complex panels to ensure input hits valid targets.
4. **Leverage Affordances**: Use the `Affordances` list in the JSON dump to determine valid actions rather than guessing.
5. **Fuzzy Search**: `Tester.Perception.Find("Start")` is highly optimized to resolve Z-order ambiguity and pick the most relevant "Start" button.

---

## Installation
1. Copy the `Assets/UnityRealPlayTester/` folder into your project.
2. Ensure the `RealPlayTester.asmdef` is referenced by your test assemblies.
3. No additional configuration required.

## License
MIT - Created by Daliys.
