# RealPlayTester (v2.4.2) - Technical Manual for AI Agents

RealPlayTester is an **AI-Native Automation Framework** for Unity. It provides LLM-based agents with structured sensory input (JSON), deterministic motor controls (Hardware-mimicking input), and causal verification tools.

---

## 👁️ Sensory Input (`Tester.Perception`)
Your primary way of "seeing" the world is through the **Semantic DOM**.

### `Perception.DumpHierarchyJson()`
Returns a tree of `SemanticNode` objects. **This is your primary observation loop.**

**JSON Schema:**
```json
{
  "Name": "Btn_Attack",         // Object identifier
  "Role": "Button",             // Semantic category (Button, InputField, Slider, etc.)
  "Active": true,               // Activation state
  "Interactable": true,         // Combined UI/Logic interaction state
  "ScreenRect": { "x": 0, "y": 0, "width": 100, "height": 50 }, // Pixel coordinates
  "Text": "ATTACK",             // Text content
  "ZDepth": 10.5,               // Distance from camera
  "IsOccluded": false,          // True if hidden behind another object
  "LogicalBlocked": false,      // True if a script prevents interaction
  "BlockedReason": "No Ammo",   // Why it's blocked (if LogicalBlocked is true)
  "Affordances": ["Click"],     // Valid actions for this object
  "Children": []                // Child nodes
}
```

### 🧠 Token Management (Context Optimization)
To prevent massive JSON dumps from overflowing your context window, use:
- `Tester.Settings.FilterDOMToViewport = true;` (Only see what's on screen).
- `Tester.Settings.CollapseRepetitiveSiblings = true;` (Groups 50+ tiles into one node).

---

## 🕹️ Motor Controls (`Tester.Interaction`)
Deterministic hardware-level simulation. **Prefer `Perform` for automatic causal reasoning.**

### `Interaction.Perform(intent, target)`
Executes an intent and returns a **Causal Reaction Summary**.
- **Supported Intents**: `click`, `drag`, `type`, `submit`, `hover`.
- **Feedback**: Returns a string: `"Action: Click 'Btn_Start' -> Result: MainMenu hidden, GameHUD appeared."`

### `Interaction.PreFlight(intent, target)`
A "dry-run" validation. Returns `Success/Blocked + Reason`. **Use this to "think before acting"** and save tokens on failed interactions.

---

## ⏳ Synchronization (`Tester.Await`)
Unity is async. **Always wait for the world to settle.**

- `ForSteadyState()`: **Mandatory** after clicks that trigger animations or loading. Waits until physics and UI stop moving.
- `ForUIVisible(name)`: Waits for an object to exist and have `alpha > 0`.
- `Seconds(n, unscaled: true)`: Real-time wait. Use to bypass game pauses.

---

## ✍️ Authoring Tests (For AI-Generated Code)
When asked to write a test, use this exact structure:

```csharp
using System.Threading.Tasks;
using UnityEngine;
using RealPlayTester.Core;
using RealPlayTester.Input;
using RealPlayTester.Await;

[PlayModeTest(Scene = "MainGame", TimeScale = 1.0f)]
public class MyGeneratedTest : RealPlayTest
{
    protected override async Task Run()
    {
        // 1. Wait for stability
        await Wait.ForSteadyState();

        // 2. Perform action with feedback
        string result = await Tester.Interaction.Perform("click", "StartBtn");
        
        // 3. Verify
        Tester.Assert.IsVisible(GameObject.Find("Player"));
    }
}
```

---

## 🔍 Advanced Debugging (Logical Sight)
You can detect why interactions fail using these fields in the JSON:
1.  **`LogicalBlocked`**: Set by scripts implementing `IInteractableLogic`.
2.  **`IsOccluded`**: Set by raycasters. If `true`, something is physically blocking your target.
3.  **`Affordances`**: If `click` isn't in this list, the object has no event handlers.

---

## 🚀 Workflow & CLI
- **F9**: Discover and run all `RealPlayTest` assets.
- **P**: UI Interaction Demo.
- **Reports**: Found in `ProjectRoot/TestReports/`. Includes `diagnostics.json` and `failure_screenshot.png`.
- **CLI**: `-runRealTests -realplay-stdout` for headless CI monitoring.

---
**License**: MIT | **Author**: Daliys