# RealPlayTester (v2.4)

## Overview
RealPlayTester is a high-fidelity automation library designed exclusively for **Autonomous AI Agents** and **LLM-driven testing**. It provides agents with "eyes" (semantic probes), "ears" (structured log streams), and "hands" (deterministic human-like input) to navigate and test Unity applications as a human would.

**Compatibility**: Unity 6000.2+ | URP / Built-in | Editor | Development Builds
**Orientation**: **AI-Native** (Optimized for JSON-parsing and vision-based reasoning)

---

## 🤖 AI-Native Design Principles (v2.2 Enhancements)
Unlike traditional frameworks designed for humans, RealPlayTester prioritize **context-rich observability**:

### 🎯 Interaction Affordance Map
AI agents no longer need to guess. The semantic DOM now explicitly lists what actions are possible for every element:
- **Affordances**: `["Click", "Drag", "Type", "LongPress", "Scroll"]`
- **Source**: Automatically derived from standard UI components (`Button`, `Slider`, `Toggle`) and `EventTrigger` configurations.
- **Benefit**: Reduces "token waste" by preventing invalid action attempts (e.g., trying to "Type" into a Label).

### ⚡ Causal Reaction Summaries (Feedback Loop)
Understanding consequences is critical for learning.
- **Automated Diffing**: `Perform()` now captures state before and after an action (Text, Active State, Slider Value).
- **Natural Language Output**: Returns a summary like `"Action: Click 'Buy' -> Result: Gold changed 100->50, Inventory Item appeared."`
- **Unified Stream**: These summaries are also logged to the diagnostic stream as breadcrumbs.

### 🖼️ Visual Annotation Overlays (Vision Helpers)
To help Vision Models (GPT-4o, Claude 3.5) "see" the UI structure:
- **Diagnostic Screenshots**: `Tester.Screenshot.CaptureWithAnnotations` generates an image with bright bounding boxes and labels (`#A1`, `#B2`) baked in.
- **Grounding**: The library returns a mapping of `Label -> GameObject Name`, bridging pixels to code.

### 🧭 Navigation Graph
- **Waypoints**: Register high-level paths: `Tester.Navigation.RegisterPath("MainMenu", "Settings", async () => await Click("SettingsBtn"))`.
- **Teleport**: `Tester.Navigation.Navigate("MainMenu", "Audio")` replays the recorded interaction sequence to ensure valid state transitions.

### 🧟 "Ghost" Input Validation (Pre-Flight)
- **Think Before Acting**: `Tester.Interaction.PreFlight(intent, target)` simulates the action without performing it.
- **Checks**: Verifies hierarchy active state, UI interactability, occlusion, and custom logic (`IAffordanceValidator`).

---

## Feature Highlights

### 📦 Black Box Recorder & Diagnostics
- **Unified Stream**: Automatic categorization of logs into `Input`, `Test`, `Step`, `Game`, `Library`, and `Heartbeat`.
- **Performance Profiler**: Real-time background monitoring for **GC.Alloc violations** and **Frame-time spikes**.
- **Failure Bundles v2**: On failure, a bundle is exported with `hierarchy.json`, `diagnostics.md`, screenshots, and session logs.

### 🚀 CI/CD & Headless Robustness
- **"Idle Motion" Bypass**: `Wait.ForSteadyState` intelligently ignores ambient motion (wind, breathing idles, VFX) using tags (`[Ambient]`) and velocity thresholds.
- **Bypass Heuristics**: Automatically forces UI interactability and visibility in batchmode.
- **Input Heartbeat**: Forced event processing ensures zero-lag simulation.

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
        // 1. Ghost Check: Can I actually click this?
        var check = await Tester.Interaction.PreFlight("Click", "StartButton");
        if (!check.Success) Debug.LogWarning($"Blocked: {check.BlockReason}");

        // 2. Perform with Causal Feedback
        string result = await Tester.Interaction.Perform("Click", "StartButton");
        // Output: "Action: Click 'StartButton' -> Result: MenuPanel disappeared, GameHUD appeared."
        
        // 3. Vision Helper
        Tester.Screenshot.CaptureWithAnnotations("Diagnose_MainMenu");
    }
}
```

---

## 🏗️ Core AI API (v2.2)

### 🕹️ Tester.Interaction
| Method | Description |
|--------|-------------|
| `Perform(intent, target)` | Executes intent & returns Causal Summary string. |
| `PreFlight(intent, target)` | dry-run validation returning Success/Blocked + Reason. |
| `Mouse.MoveToCenter(GO)` | Precision move to object screen center. |

### 👁️ Tester.Perception
| Method | Description |
|--------|-------------|
| `DumpHierarchyJson()` | Export scene as minified, LLM-optimized JSON including logic & affordances. |
| `ProbeScreen(Vector2)` | Get semantic and logical info for a specific pixel. |

### 🧭 Tester.Navigation
| Method | Description |
|--------|-------------|
| `RegisterPath(from, to, action)` | Define how to get from A to B. |
| `Navigate(from, to)` | Replay interactions to transition state. |

### 📸 Tester.Screenshot
| Method | Description |
|--------|-------------|
| `CaptureWithAnnotations()` | Get texture with baked #A1 labels for Vision Models. |

---

## Installation
1. Copy the `RealPlayTester/` folder into your project's `Assets/`.
2. No configuration required.

## Version History
See [CHANGELOG.md](CHANGELOG.md) for full version history.