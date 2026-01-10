# RealPlayTester (v2.1)

## Overview
RealPlayTester is a high-fidelity automation library designed exclusively for **Autonomous AI Agents** and **LLM-driven testing**. It provides agents with "eyes" (semantic probes), "ears" (structured log streams), and "hands" (deterministic human-like input) to navigate and test Unity applications as a human would.

**Compatibility**: Unity 6000.2+ | URP / Built-in | Editor | Development Builds
**Orientation**: **AI-Native** (Optimized for JSON-parsing and vision-based reasoning)

---

## 🤖 AI-Native Design Principles (v2.1 Enhancements)
Unlike traditional frameworks designed for humans, RealPlayTester prioritize **context-rich observability**:

### 🧠 Spatial Ranking Engine (`SmartFind`)
The "Ghost Hands" are now spatially aware. When an agent requests an object by name, the library uses a weighted scoring system to pick the best candidate:
- **Visibility First**: Objects currently visible to the camera get a massive priority boost (+10,000 pts).
- **Z-Order Resolution**: Automatically prioritizes the topmost UI element or the closest 3D world object.
- **Logic Awareness**: Blocked objects (e.g., a "Buy" button you can't afford) are penalized, making the AI prefer unblocked alternatives.

### 👁️ "Logical Sight" (Ghost Interactivity Fix)
AI agents can now see beyond standard Unity properties. The library probes custom C# scripts to find hidden interaction rules:
- **Interface Support**: Developers can implement `IInteractableLogic` to report why an interaction is blocked (e.g., "Need Level 5").
- **Heuristic Probing**: Automatically detects common patterns like `IsLocked` or `CanInteract()` via reflection.
- **Semantic DOM**: Logical block reasons are exported in the hierarchy JSON, allowing LLMs to "reason" about why they can't click something.

### ⏳ Explainable Waiters
Synchronization is no longer a "black box". If a wait times out, the library provides a **Visibility Trace**:
- **Alpha Chains**: Shows the exact parent in the hierarchy that is hiding the object (e.g., `Parent(INACTIVE) -> Child(Alpha:0)`).
- **Logic Traces**: Reports if an object is technically visible but logically blocked by a script.

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
        // 1. Logic-aware wait: will wait until both visible AND logic allows interaction
        await Tester.Await.ForUIVisible("StartButton");
        
        // 2. Spatial Ranking: will automatically pick the visible 'StartButton' over hidden ones
        await Tester.Interaction.Perform("Select", "StartButton");
        
        // 3. Performance Guard: check if the previous click caused a massive allocation
        var state = Tester.Perception.DumpHierarchyJson();
        Tester.Assert.IsNotNull(state, "Scene state should be accessible");
    }
}
```

---

## 🏗️ Core AI API (v2.1)

### 🕹️ Tester.Interaction
| Method | Description |
|--------|-------------|
| `Perform(intent, target)` | High-level intent (Select, Submit, Cancel) with logical block detection. |
| `Mouse.MoveToCenter(GO)` | Precision move to object screen center. |
| `Gestures.*` | Human-like multi-touch (Pinch, Twist, Swipe). |

### 👁️ Tester.Perception
| Method | Description |
|--------|-------------|
| `DumpHierarchyJson()` | Export scene as minified, LLM-optimized JSON including logic states. |
| `ProbeScreen(Vector2)` | Get semantic and logical info for a specific pixel. |
| `GetActionableElements()` | JSON list of targets that are both technically and logically clickable. |

### ⏳ Tester.Await
| Method | Description |
|--------|-------------|
| `ForSteadyState()` | Wait for physics/anim stability, ignoring ambient motion. |
| `ForUIVisible(name)` | Wait for target to be visible, scaled, and logically unblocked. |

---

## Installation
1. Copy the `RealPlayTester/` folder into your project's `Assets/`.
2. No configuration required.

## Version History
See [CHANGELOG.md](CHANGELOG.md) for full version history.