# RealPlayTester (v2.4.2) - Manual for AI Agents

RealPlayTester is an **AI-Native Automation Framework** for Unity. It is designed to be used by LLM-based agents (e.g., Claude 3.5, GPT-4o) to navigate, observe, and test games.

---

## 🤖 How to use this library (AI Instructions)

### 1. Observe the World (`Tester.Perception`)
**Your primary tool is `DumpHierarchyJson()`.** 
This returns a **Semantic DOM** (Document Object Model) of the Unity scene. Instead of raw coordinates, use this JSON tree to understand the spatial and logical relationship between objects.

*   **`Name`**: Unique or fuzzy identifier for finding objects.
*   **`Role`**: Semantic category (e.g., `Button`, `InputField`, `ValueSlider`).
*   **`ScreenRect`**: Pixel coordinates `(x, y, width, height)`. Use for vision-grounding.
*   **`Affordances`**: A list of valid actions (e.g., `["Click", "Type"]`). **Check this before acting.**
*   **`LogicalBlocked`**: If `true`, internal game logic (e.g., `isLocked`) prevents interaction. Read `BlockedReason`.
*   **`IsOccluded`**: If `true`, the object is hidden behind another UI element or 3D object.

### 2. Interact deterministically (`Tester.Interaction`)
**Prefer `Perform(intent, target)` over raw inputs.**
`Perform` captures the state before and after your action, returning a **Causal Reaction Summary**.

*   **Syntax**: `await Tester.Interaction.Perform("click", "StartButton");`
*   **Feedback**: Returns a string like: `"Action: Click 'StartButton' -> Result: MainMenu hidden, LoadingScreen appeared."` Use this to verify your action worked.
*   **Fuzzy Matching**: You don't need exact paths. `Find("Start")` will use spatial ranking to find the most relevant "Start" button (prioritizing visible, non-occluded, top-layer UI).

### 3. Handle Time and Physics (`Tester.Await`)
Unity is asynchronous. Never assume a screen transition is instant.

*   **`ForSteadyState()`**: Call this after any major action. It waits until physics, animations, and hierarchy changes stop.
*   **`ForUIVisible(name)`**: Specifically waits for an object to be active and have `alpha > 0`.
*   **`Seconds(n, unscaled: true)`**: Use `unscaled: true` to wait in real-time, even if the game is paused (`Time.timeScale = 0`).

### 4. Verify Success (`Tester.Assert`)
Use hardware-validated assertions.

*   **`Assert.IsVisible(go)`**: Performs a frustum and raycast check to ensure the object is actually rendered on screen.
*   **`Assert.ScreenElementVisible(name)`**: Combines fuzzy finding with visibility validation.

---

## 📄 API Reference Table

| Feature | Method | Recommended Agent Usage |
| :--- | :--- | :--- |
| **Observation** | `Perception.DumpHierarchyJson()` | Call this every "turn" to refresh your mental model of the scene. |
| **Validation** | `Interaction.PreFlight(intent, target)` | Call before `Perform` to check for occlusions or logic blocks without wasting a "turn". |
| **Action** | `Interaction.Perform(intent, target)` | Your main interaction tool. Interprets `click`, `drag`, `type`, `submit`. |
| **Wait** | `Await.ForSteadyState()` | Essential after clicking buttons that trigger loading or animations. |
| **Logic** | `Perception.IsOccluded(target)` | Use to debug why a click might be failing at a specific coordinate. |
| **State** | `State.GetDiff(before, after)` | Use to generate natural language explanations of what changed in the game. |

---

## 🛠️ Execution Context

### Play Mode Controls
*   **`F9`**: Discovery Mode. Forces the framework to find and run all `RealPlayTest` assets.
*   **`P`**: Interactive Demo. Runs a scripted sequence of common UI interactions.

### Command Line Interface (CI/CD)
```bash
Unity.exe -projectPath . -runRealTests -realplay-stdout --tags=Smoke
```
*   `-runRealTests`: Auto-exit with failure count as exit code.
*   `-realplay-stdout`: Pipes internal `RealPlayLog` entries to the shell for real-time AI monitoring.

---

## 💡 Pro-Tips for AI Agents
1.  **Don't "Teleport"**: The library simulates real hardware. If you click at `(0,0)` and then `(1000,1000)`, the cursor will move across the screen, potentially triggering `OnPointerEnter` events on objects in between.
2.  **Z-Order Priority**: `SmartFind` (used by `Perform`) automatically ranks UI elements above world objects. If a button is on top of a 3D chest, `Perform("click", "Button")` will correctly hit the button.
3.  **Alpha Transparency**: Objects with `alpha < 0.05` are considered "Invisible" by `ForUIVisible`.
4.  **Physics Simulations**: In batchmode (headless), the library automatically calls `Physics.Simulate()` during waits to ensure physics-based game logic progresses.

---

## JSON Node Example (Input for your Reasoning)
```json
{
  "Name": "HealthSlider",
  "Role": "ValueSlider",
  "Active": true,
  "Interactable": true,
  "ScreenRect": { "x": 50, "y": 50, "width": 200, "height": 20 },
  "Text": "85%",
  "ZDepth": 0.0,
  "IsOccluded": false,
  "LogicalBlocked": false,
  "Affordances": ["Drag", "Hover"],
  "Children": []
}
```

---
**License**: MIT
**Created by**: Daliys