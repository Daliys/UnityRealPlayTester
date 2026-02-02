# RealPlayTester (v2.9.2)
Autonomous, AI-Native Automated Playtesting for Unity

RealPlayTester is a lightweight, high-fidelity automation library designed for modern AI-native development. It allows you to write stable, human-like automated tests using a simple `async/await` API that interacts with Unity objects semantically.

## 🚀 Key Features

### 🛡️ Security & Resilience (New in v2.9.0)
Built for the chaos of production development. The library is now hardened against:
- **Concurrency Attacks**: Strict `EnsureMainThread()` guards prevent background threads from crashing Unity.
- **Input Flooding**: Throttled event queues (default 1000 events/frame) prevent buffer overflows during fuzzing.
- **State Injection**: Auto-healing singletons and clamped configuration values protect against reflection-based sabotage.
- **WebGL Support**: Fully compatible with single-threaded Wasm builds.

### 🐒 Autonomous Chaos Monkey
Uncover edge-case crashes automatically. The Chaos Monkey discovers all interactables in the scene and performs random valid actions (clicks, moves, drags) while respecting logical game rules.
```csharp
await Tester.Advanced.StartChaosMonkey(durationSeconds: 60f);
```

### 👁️ AI Vision-Language Mapping
Bridge the gap between vision and structured data. RealPlayTester can generate **Annotated Vision Bundles** where screenshots are automatically labeled with unique IDs (`#A1`, `#A2`) that match the `VisualID` field in the **Semantic DOM (JSON)**. This is specifically optimized for Vision-Language Models like Gemini Pro Vision or GPT-4o.

### 🧠 Semantic DOM (JSON)
Export the entire scene as an AI-optimized structure. Includes:
- **Spatial Rects**: Pixel-perfect screen coordinates.
- **Affordance Hints**: Valid interactions for each object.
- **Command Snippets**: Ready-to-use API calls for autonomous agents.
- **Occlusion Detection**: Identifies if objects are hidden by other UI or world geometry.

### ⚡ Performance Optimized
- **SceneCache**: Sub-ms object discovery using a dirty-flagged hierarchy cache.
- **Throttled Updates**: Eliminates redundant `Canvas.ForceUpdateCanvases()` calls.
- **Scale-Aware**: Supports hierarchies up to 1024 levels deep.

## 📦 Quick Start

1. Import the package.
2. Ensure an `EventSystem` is present (or let the library auto-create one).
3. Start writing tests:

```csharp
[PlayModeTest(Scene = "Menu")]
public class MainFlowTest : RealPlayTest {
    protected override async Task Run() {
        await Tester.Wait.ForObject("PlayButton");
        await Tester.Interaction.Perform("Click", "PlayButton");
        
        await Tester.Await.ForSteadyState();
        Tester.Assert.ScreenElementVisible("GameHUD");
    }
}
```

## 🛠️ Performance & Scalability
- **Hierarchy Scanning**: Optimized for 1000+ objects.
- **Deterministic Physics**: Fixed-time accumulator for stable batchmode/CI execution.
- **Memory Safe**: Automatic resource cleanup and object pooling for high-frequency interactions.

---
*Developed by Daliys. MIT License.*