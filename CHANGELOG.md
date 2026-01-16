# Changelog

## [2.4.9] - 2026-01-16
### Autonomous Testing
### Added
- **Chaos Monkey Mode**: Introduced `Tester.Advanced.StartChaosMonkey(duration)` for automated endurance testing. The monkey performs random valid interactions (clicks, moves, drags) on all discoverable and interactable UI and world objects to uncover edge-case crashes and state leaks.
- **Monkey Control API**: Added `Tester.Advanced.StopChaosMonkey()` for manual termination of the autonomous agent.

## [2.4.8] - 2026-01-16
### Performance & Optimization
### Fixed
- **Redundant Canvas Updates (O016/O020)**: Refactored `SemanticDOMDumper` to use throttled updates. The UI layout is now updated exactly once at the start of a scan, eliminating hundreds of redundant calls during deep hierarchy traversal.
- **Batchmode Pruning (M007)**: Ensured that logical objects (those without renderers or RectTransforms) are no longer pruned in batchmode, preserving the AI's "vision" of non-visual scene state.

## [2.4.7] - 2026-01-16
### Reliability & Extreme Scale
### Added
- **Deep Hierarchy Support (M001/M002)**: Increased maximum hierarchy depth to **1024 levels** and improved serialization to prevent StackOverflow while maintaining performance.

### Fixed
- **Wait Scaled Performance (M019)**: Optimized the unscaled-time fallback in `WaitScaled` to use 100% real-time speed when paused, preventing automation from becoming unacceptably slow.
- **Occlusion Detection Precision**: Improved raycast reliability in batchmode tests by ensuring correct camera context and disabling batchmode-only visibility overrides during verification.

## [2.4.6] - 2026-01-16
### Chaos-Proof Stabilization & AI-Native Lab
### Added
- **GPU Simulation Engine**: Implemented high-scale cellular automata on GPU via Compute Shaders with bitmask-based rules.
- **Chaos Mutation Mode**: Dynamic real-time ruleset evolution with integrated visual feedback (background pulse).
- **Selection & Clipboard Tools**: World-space pattern selection and clipboard support for advanced lab experimentation.
- **Enhanced Verification Suite**: Added comprehensive performance stress tests for 1000x1000 grids and deep hierarchies.

### Fixed
- **Wait Scaled Stability (M019)**: Implemented full unscaled-time fallback in `Wait.Seconds` to prevent infinite loops when `timeScale` is zero.
- **Deep Hierarchy Support (M001/M002)**: Increased maximum hierarchy depth to 512 levels and optimized serialization for massive trees.
- **Atomic State Capture (O030)**: `StateTracker` now forces a cache refresh before capture, ensuring immediate spawns/destructions are visible in diffs.
- **Environment Consistency (O029)**: `HardReset` now correctly restores all framework settings, including `AutoCreateEventSystem`.
- **Method Length Compliance (O028)**: Refactored `RealPlaySettings.Initialize` to satisfy strict linter standards.

## [2.4.5] - 2026-01-16
### CI/CD Reliability & Input Mapping
### Added
- **Deterministic Batchmode Physics (M015)**: Implemented a fixed-time accumulator in the library heartbeat to ensure consistent physics behavior across variable frame rates in CI.
- **Expanded Legacy Input (M016)**: Added full alphanumeric and common symbol mapping to `LegacyInputFallback` for projects without the New Input System.

### Fixed
- **Optimized Discovery Cache (O001)**: Implemented a dirty-flag pattern for `SceneCache` to eliminate redundant hierarchy scans during rapid scene transitions.
- **Corrected Alpha Math (O025)**: Updated `PanelStateMonitor` to use multiplicative composition for nested CanvasGroups, improving visibility reporting accuracy.

## [2.4.4] - 2026-01-15
### Stability Refinements
### Fixed
- **Scene Switch Safety (C003)**: Added validation to `Perform` to gracefully abort interactions if the scene changes during execution.
- **Virtual Device Isolation (M003)**: Implemented `ThreadStatic` backing fields for virtual input devices to prevent state contamination in parallel test environments.

## [2.4.3] - 2026-01-15
### Maintenance & Hotfixes
### Fixed
- **Invisible Blocker Bypass (M010)**: Occlusion detection now correctly ignores UI elements with alpha < 0.01.
- **TMP Base Type Support**: Refactored text discovery to use `TMP_Text` base classes, significantly improving search reliability for various MeshPro variants.

## [2.4.2] - 2026-01-15
### Refinement & Perception Precision
### Added
- **2D Occlusion Support**: Implemented `Physics2D.GetRayIntersection` in `RealPlayOcclusionRaycaster`. The tester now correctly identifies blockers with 2D colliders, improving reliability in 2D and hybrid projects.

### Fixed
- **Discovery Performance**: Optimized `SmartFind` by eliminating redundant `Canvas.ForceUpdateCanvases()` calls. Updates are now orchestrated once per discovery scan rather than once per candidate, significantly reducing CPU spikes during object finding.
- **Compilation Stability**: Resolved a missing namespace issue in `WaitUI.cs` related to `SceneCache`.
- **Test Integrity**: Updated `PerceptionFeatureTests` to correctly handle batchmode visibility overrides, ensuring raycasting logic is verified even in headless CI environments.

## [2.4.1] - 2026-01-15
### Professionalization & Robustness (100 Issue Resolution)
### Added
- **Failure Bundles v3**: Every test failure now generates a timestamped bundle containing truncated tail-logs, annotated screenshots, and a semantic hierarchy dump.
- **Thread-Safe Logging**: Completely refactored `LogInterceptor` and `TestRunContext` to safely capture logs and breadcrumbs from background threads using robust locking and thread-safe collections.
- **Occlusion-Aware Interactions**: Implemented physical occlusion checks. The library now correctly detects when a target is blocked by UI or 3D geometry.
- **Spatial Ranking Engine (`SmartFind`)**: Replaced simple first-match search with a weighted ranking system based on visibility, UI priority, and depth.
- **Inactive Object Awareness**: Optimized discovery of inactive objects, enabling robust "Wait Until Appears" scenarios.

### Fixed
- **CI/CD Physics Flaw**: Corrected a critical issue where physics simulation advanced during pauses in headless batchmode.
- **Hanging Interactions**: Resolved hangs during game pauses by switching to unscaled real-time waits and optimizing `EventSystem` updates.
- **Native Hardware Simulation**: Eliminated "fake" inputs in `TextInput` and `LegacyInput`, switching to raw hardware event simulation.
- **Panel Interactivity Consistency**: Centralized logic for checking UI interactability across all modules into `PanelStateMonitor`.
- **Memory & Performance**: Optimized O(N) scene scans using `SceneCache` and tiered candidate pruning, reducing overhead to sub-10ms.

### Changed
- **Architecture**: Decoupled perception from interaction logic to improve testability and reduce circular dependencies.
- **Diagnostic Compliance**: Refactored core classes to satisfy strict project-wide code quality and linter standards.

## [2.4.0] - 2026-01-15
### Hardening & Optimization
### Added
- **Hierarchy Optimization Engine**: Introduced `HierarchyHelpers` to intelligently group repetitive sibling objects (tiles, grid cells, list items) in both JSON and Text dumps, significantly reducing token usage for AI agents.
- **Enhanced Perception Facade**: Exposed `PreferredMode` and `CollapseRepetitiveSiblings` in `Tester.Settings` for easier runtime configuration of agent "vision".

### Fixed
- **Wait Stability**: Implemented unscaled time fallback in `Wait.Seconds` when `Time.timeScale` is extremely low (< 0.01) to prevent automation hangs during slow-motion or paused states.
- **Namespace Ambiguity**: Renamed internal `Text` interaction class to `TextInput` to resolve naming collisions with `UnityEngine.UI.Text`.
- **Input System Integration**: `RealInputUtility` now correctly prioritizes `InputSystemUIInputModule` when the new Input System is active, preventing `InvalidOperationException` during EventSystem initialization.
- **System Consolidation**: Merged `StateDiffer` logic into `StateTracker` and removed redundant files to simplify the perception pipeline.

### Changed
- **Stable State Keys**: `StateTracker` now uses unique hierarchy paths (e.g., `Parent/Child:0`) instead of heuristic roles for state identification, ensuring consistent diffing even when heuristic roles change.
- **Linter Compliance**: Refactored core perception and state tracking methods to satisfy strict 40-line method limits.

## [2.3.0] - 2026-01-14
### Persistence, Disambiguation & Robustness
### Added
- **Navigation Persistence**: Introduced `ExportToJson`, `ImportFromJson`, `SaveToFile`, and `LoadFromFile` to the Navigation API, allowing AI agents to retain learned interaction maps across sessions.
- **Auto-Discovery Learning Mode**: Added `RealPlaySettings.EnableNavigationLearning`. When active, the library automatically records transitions between game states during manual or automated play.
- **Semantic Disambiguation (`RealPlayState`)**: New component that allows developers to explicitly tag GameObjects with state names, overriding heuristics for more reliable state identification in complex UI or 3D environments.
- **High-Precision Causal Deltas**: Causal summaries now include specific "from -> to" values for Text, Toggles, Sliders, and Transforms (e.g., "Player moved (0,0,0)->(10,0,5)").
- **Multi-Point Occlusion Engine**: Refactored visibility checks to use a 5-point surface raycast (Center + 4 Corners), significantly reducing flakiness in batchmode and complex UI overlaps.
- **Manual Semantic DOM Parser**: Added `SemanticDOMParser` to bypass Unity's `JsonUtility` depth limits, enabling structured verification of deep scene hierarchies in unit tests.

### Fixed
- **Sorting Order Integrity**: `ManualRaycast` now correctly prioritizes hits based on `Canvas.sortingOrder`, fixing a critical bug in headless occlusion detection.
- **Recursion Depth Warnings**: Removed `[Serializable]` from core DOM nodes and switched to manual serialization to silence Unity console warnings in deep trees.
- **DDOL Cleanup**: Enhanced `CleanupReports` to recursively delete all old failure artifacts and subdirectories on initialization.

### Changed
- **Unified JSON Pipeline**: Consolidated all hierarchy requests to use the high-fidelity `DumpSemanticDOM` through the `Tester.Perception` facade.
- **Linter Compliance**: Refactored several core classes into partial files to maintain strict line and member limits.

## [2.2.0] - 2026-01-14
### Logical Sight & Spatial Priority Engine
### Added
- **"Logical Sight" (Ghost Interactivity Resolution)**:
    - Introduced `IInteractableLogic` interface for exposing deep game rules to the tester.
    - Implemented a **Heuristic Reflection Probe** that automatically identifies logical blocks like `bool IsLocked` or `bool CanInteract()` in any custom script.
    - Added logical state reporting to the Semantic DOM and AI timeline (e.g., `[LOGIC BLOCKED: Not enough gold]`).
- **Spatial Ranking Engine (`SmartFind`)**:
    - Replaced simple first-match search with a **Weighted Ranking System**.
    - Candidates are now scored based on:
        - **Visibility**: +10,000 pts boost for non-occluded objects.
        - **UI Priority**: +50,000 pts boost for UI elements over world objects.
        - **Sorting Order**: Priority based on Canvas `sortingOrder`.
        - **World Depth**: Prioritizes objects closer to the camera.
        - **Logic Status**: -20,000 pts penalty for logically blocked objects.
- **"Idle Motion" Stability Heuristics**:
    - `Wait.ForSteadyState` now intelligently ignores ambient motion (wind, breathing idles, VFX) using tags (`[Ambient]`) and velocity thresholds.
    - **Ambient Filtering**: Automatically ignores objects tagged with `[Ambient]`, `vfx_`, or `Particle`.
    - **Velocity Thresholds**: Added `RealPlaySettings.IdleVelocityEpsilon` to ignore micro-physics jitter.
    - **Animation Awareness**: Looping animations (idle/breathing) no longer block progress; only active **transitions** cause a wait.
- **Explainable Wait Traces**:
    - Timeouts now provide a detailed "Alpha Chain" and hierarchy trace explaining *why* an object is hidden (e.g., `Parent(INACTIVE) -> Child(Alpha:0)`).
- **Performance Profiler**:
    - Real-time background monitoring for **GC.Alloc violations** and **Frame-time spikes**.
    - Automatically records violations as breadcrumbs in the test context.
- **Robust Hierarchy Cache**:
    - Refactored `SceneCache` to use recursive scene-root traversal, ensuring transient objects created during unit tests are always discovered.

### Fixed
- **Input System Redundancy**: Refactored `InputSystemShim` into a clean facade, delegating to `InputSimulator` and `VirtualDeviceManager` to prevent log overflow.
- **Batchmode UI Accuracy**: Fixed coordinate calculation for `ScreenSpaceOverlay` canvases in headless environments.
- **Deterministic Selection**: Added stable InstanceID tie-breaking to all object discovery calls.

### Changed
- **Package Metadata**: Bumped version to 2.1.0.
- **Linter Compliance**: Split large feature sets into specialized partial classes (`Interaction.Perform`, `Interaction.Gestures`, etc.) to maintain strict file length limits.

## [2.0.0] - 2026-01-04
### AI-Native Facade & Black Box Recorder
### Added
- **Nested API Facade**: Re-architected the `Tester` class into specialized, discoverable nested categories:
    - `Tester.Interaction`: (Mouse, Keyboard, Touch) for deterministic input.
    - `Tester.Perception`: (Find, Probe, DescribeRegion, DumpHierarchy) for scene understanding.
    - `Tester.State`: (Capture, CaptureObject, GetDiff) for causal consequence analysis.
    - `Tester.Await`: (Seconds, Frames, Until, Step) for flexible synchronization.
    - `Tester.Advanced`: (Drag, Scroll, ClickButtonWithText) for complex flows.
    - `Tester.Utility`: (GetScreenCenter, Raycast, PerformAndVerify) for common helpers.
- **AI-Native JSON Perception**: Introduced `Tester.Perception.DumpHierarchyJson()` for coordinate-aware, LLM-optimized scene trees.
- **Black Box Recorder**:
    - **Unified Diagnostic Stream**: Structured log categories (`[INPUT]`, `[BLOCKER]`, `[STEP]`) with real-time raycast occlusion detection.
    - **Failure Bundle v2**: Automated capture of state snapshots, hierarchy JSON, and categorized session logs on failure.
- **State Tracking**: `StateTracker` API with serializable snapshots and semantic diffing to verify the impact of agent actions.
- **Parameter Consolidation**: Introduced `PinchParams` and `ScreenPosition` structs to satisfy strict linter limits and improve API consistency.

### Changed
- **Diagnostic Compliance**: 100% adherence to project-wide code quality standards:
    - Max 40 lines per method.
    - Max 300 lines per file.
    - Max 20 public members per class (achieved via modular partial classes).
- **Modernized APIs**: Use of modern Unity performance APIs (`FindFirstObjectByType`, `FindObjectsByType`).

### Fixed
- **Suite Stability**: All primary verification tests stabilized and passing (49/49).
- **Baseline Management**: Restored auto-saving of missing baselines in `Capture.CompareToBaseline`.

## [1.8.1] - Visual Cursor Fixes
### Fixed
- **Visual Cursor Visibility**: Enforced "Always-on-Top" rendering for the visual pointer by automatically claiming the maximum sorting order (32767) and maintaining the last sibling position in the hierarchy every frame.
- **Input Interference**: Removed redundant `GraphicRaycaster` from the pointer canvas to prevent the virtual cursor from blocking UI interactions.

## [1.8.0] - 2026-01-01
### Added
- **Automated Pipeline Enhancements**:
    - **Headless Optimization**: Added `Tester.Settings.ForceBatchmodeVisibility` to bypass UI animation delays and alpha/interactability checks in batchmode.
    - **Unified Input Device Ownership**: Virtual Keyboard and Mouse are now explicitly "claimed" via `MakeCurrent()` during initialization to ensure consistency in headless environments.
    - **Internal Input Heartbeat**: Added `Tester.Settings.EnableInputHeartbeat` to force `InputSystem.Update()` every frame during test runs.
    - **CLI Log Routing**: Added `-realplay-stdout` command-line argument to mirror all internal logs directly to the standard Unity log output.
    - **Blocker-Bypass Strategy**: Drag and Click interactions now automatically skip UI elements without event handlers (like "Background" images) and handle event bubbling for complex components like `ScrollView` and `Slider`.
    - **Robust Device Discovery**: Fixed reflection-based device creation in batchmode, resolving the "Late bound operations" error during virtual device setup.

## [1.7.0] - 2025-12-31

### Added
- **Built-in Visual Pointer**: Added `RealPlay_VisualPointer` component that renders a virtual red cursor for all simulated inputs. Configurable via `Tester.Settings.ShowVisualPointer`.
- **Realistic UI Demo Mode**: Pressing `P` in Play Mode now triggers a full simulation of common UI interactions (Button, Toggle, Input, Slider, Dropdown, Scroll).
- **Smooth Movement & Continuity**: 
  - Cursor position is now persistent between actions. No more "teleporting" from (0,0).
  - `Tester.MouseMoveToCenter(GameObject, float)` calculates exact screen centers for both UI and World elements.
  - Added `Tester.GetScreenCenter(GameObject)` public API for coordinate calculation.
- **URP Compatibility**: Library now automatically adds `UniversalAdditionalCameraData` to the Main Camera if URP is detected, resolving common console warnings.
- **Auto-Scene Setup**: Added `BuildSettingsHelper` to automatically ensure the required verification scene is added to Unity's Build Settings.

### Fixed
- **SmartFind Robustness**:
  - Now ignores prefabs and assets, prioritizing active scene objects.
  - Uses `Resources.FindObjectsOfTypeAll` to find inactive scene objects reliably.
  - Silenced partial match warnings to reduce console noise.
  - Specialized "Scroll" query logic: prioritizes `ScrollRect` over `Scrollbar` children.
- **Targeting Accuracy**:
  - Fixed `GetScreenCenter` for `ScreenSpaceOverlay` UI to be pixel-perfect.
  - Added defensive screen clamping to all simulated movements.
  - Improved ScrollView interaction: targets list items and nudges left to avoid scrollbars.

### Changed
- **AI-Agent Awareness**: Documentation updated to clarify that this package is optimized for autonomous AI agents to probe and control the game state.
- **Internal Cleanup**: Relocated all internal library verification tests and sample assets outside of the package folder into a centralized `Assets/Tests/` hierarchy. The package now only contains core tools and logic, keeping it "lean" for distribution.

---

## [1.6.1] - 2025-12-30

### Fixed
- **Input Simulation Critical Fix**: Fixed `InputSystemShim` resetting device state on every event. Now caches and merges state, allowing complex interactions like "Drag" (Move-While-Holding) or multi-button presses to work correctly with New Input System.
- **SmartFind Performance**: Optimized object search from double-pass full-scene recursion to a single-pass hybrid scan, significantly improving performance in large scenes.

## [1.6.0] - 2025-12-30

### Added
- **Public Cancellation API**: `RealPlayExecutionContext` is now public, enabling advanced cancellation control for background tasks and custom test flows.
- **Enhanced Wait.Seconds**: Added optional `CancellationToken` parameter to `Wait.Seconds()` with automatic token combining logic, allowing explicit cancellation control alongside context-based cancellation.
- **Human-like Input Simulation**: 
  - `InputSystemShim` now supports realistic mouse movement and click simulation via `QueueStateEvent` when New Input System is active.
  - `SimulateMouseMove(Vector2)` and `SimulateMouseClick()` added to `InputHelpers` for low-level input injection.
  - `Touch.Tap()` and `Touch.Swipe()` now use realistic input sequences: hover → delay → press → delay → release.
- **SmartFind Enhancements**:
  - Multi-scene search support: finds objects across all loaded scenes.
  - Better error messages with scene information when objects aren't found.
  - Depth-limited search to prevent infinite loops in complex hierarchies.
- **Test Lifecycle Verification**: New `LifeCycleVerificationTests` demonstrating proper `CancellationTokenSource` usage patterns for background tasks.

### Fixed
- **Scroll Logic**: `Scroll.UntilVisible()` now uses intersection (`Rect.Overlaps`) instead of strict containment, properly handling elements larger than the viewport.
- **Scroll Infinite Loop**: Added safety checks and improved heuristics to prevent infinite scrolling when target is unreachable.
- **TestRunner Token Management**: Explicitly cancel per-test `CancellationTokenSource` in `finally` blocks to ensure background tasks are properly cleaned up between tests.
- **Touch Input Reliability**: `Touch.Tap()` and `Touch.Swipe()` now use `ExecuteEvents.ExecuteHierarchy()` to properly bubble events up to parent handlers (e.g., Button components on parents of Text/Image children).
- **Assert Occlusion Detection**: `Assert.IsVisible()` now performs raycast checks to detect physical occlusion (objects behind walls), not just frustum visibility.

### Changed
- **Input Architecture**: Prioritized New Input System simulation over legacy `ExecuteEvents` when available, providing more realistic input handling that triggers hovers, animations, and physics exactly as player input would.
- **VisualTreeLogger Performance**: Optimized hierarchy dump output format for better AI analysis and token efficiency.

### Tests
- **100% Pass Rate**: All 22 verification tests passing, including new tests for:
  - `AsyncVerificationTests`: Timeout handling, Wait.Until, Wait.UntilWithDiagnostics
  - `ScrollVerificationTests`: Large elements, normal elements, nested scroll views
  - `LifeCycleVerificationTests`: Proper cancellation patterns, background task cleanup

---

## [1.5.1] - 2025-12-25

### Fixed
- **Input System Compatibility**: Updated verification scenes to use `InputSystemUIInputModule`, preventing crashes in projects with the New Input System active.
- **Batchmode Visibility**: `Assert.IsVisible` now bypasses the `renderer.isVisible` check when running in `-batchmode` (headless), as Unity cameras do not render in this mode.
- **Async Test Stability**: Replaced `WaitForEndOfFrame` with `yield return null` in several verification tests to ensure compatibility with batchmode execution.
- **Namespace Ambiguity**: Resolved conflicts between `UnityEngine.Assertions.AssertionException` and `NUnit.Framework.AssertionException` in test scripts.

### Changed
- **API Accessibility**: `RealPlayEnvironment` and `RealPlayLog` classes are now `public` to allow verification from external test assemblies.
- **Assembly Definitions**: Updated `Project.VerificationTests.asmdef` to include required references for `InputSystem`, `TestRunner`, and `NUnit`.

---

## [1.5.0] - 2025-12-25

### Added
- **Continuous Monitoring**:
  - `Tester.Monitoring.StartVisualHealthCheck()`: Background coroutine to detect "Pink" shaders and null materials during active gameplay.
- **Event Tracking System**:
  - `Tester.Events.Record(string)`: Track game events, callbacks, and visual feedback triggers.
  - `Tester.Assert.EventFired(string, int)`: Verify event chain occurrences.
- **Interaction Patterns**:
  - `Tester.PerformAndVerify(interaction, predicate)`: Atomic interaction-validation gate for reliable user flow testing.
- **Improved Integration**:
  - `VisualFeedbackCorrect()` hook now integrates with `EventTracker`.

---

## [1.4.0] - 2025-12-25

### Added
- **Region-Based Screenshots**:
  - `Tester.Screenshot.CaptureAndCompareRegion(string, Rect)`: Compare specific screen regions.
  - `Tester.Assert.VisualStateMatches(string, Rect)`: Assertion wrapper for region comparison.
- **Visual Validation**:
  - `Tester.Assert.NoMissingMaterials()`: Scan scene for null materials or "Pink" error shaders on Renderers and UI.
- **Asset Performance**:
  - `Tester.Assert.TextureWithinLimits(string, int, int)`: Verify texture assets do not exceed dimension budgets.

---

## [1.3.0] - 2025-12-25

### Added
- **Visual Assertions**:
  - `Tester.Assert.IsVisible(GameObject)`: Check visibility of GameObjects, Renderers, and UI elements.
  - `Tester.Assert.HasSprite(Component, Sprite)`: Verify sprite assignments on SpriteRenderers and Images.
  - `Tester.Assert.ScreenElementVisible(string)`: Verify visibility of elements by name or tag.
  - `Tester.Assert.VisualStateMatches(string)`: Compare current screen against a baseline image.
- **Asset Validation**:
  - `Tester.Assert.AssetLoaded<T>(string)`: Verify assets can be loaded from Resources.
  - `Tester.Assert.SceneConfigurationValid()`: Ensure no missing scripts on active GameObjects.
- **Screenshot System**:
  - `Tester.Screenshot.CaptureAndCompare(string)`: Capture and compare screenshots with auto-baseline generation.
  - New `Screenshot` core class for handling capture, loading, and comparison logic.
- **Game State Validation**:
  - `Tester.Assert.GameStateMatches(Action)`: Execute custom logic for complex state validation.

### Changed
- **API Structure**: Introduced nested `Tester.Assert` and `Tester.Screenshot` classes for better organization (existing methods preserved for compatibility).

---

## [1.2.1] - 2025-12-20

### Added
- **TestRunContextTracker**: automatic context tracking and snapshot output to `TestReports/current-test-context.json` and `.md`.
- **TestRunner integration**: failure bundles now created automatically on timeouts and exceptions.

### Changed
- **Wait.Step** now updates the active test context action.
- **Input System reference** added to RealPlayTester asmdef to resolve InputSystem types when enabled.

### Tests
- Added EditMode and PlayMode tests for TestRunContextTracker snapshots and Wait.Step updates.

---

## [1.2.0] - 2025-12-20

### Added
- **Diagnostics Infrastructure**:
  - `TestRunContext` class for tracking test execution context with JSON/Markdown export
  - `FailureBundleWriter` for collecting comprehensive failure artifacts
  - `TestLog` API with GameLogger/EventAggregator integration (falls back to Debug.Log)
- **Enhanced Wait Utilities**:
  - `Wait.UntilWithDiagnostics(predicate, timeout, context)` with enhanced timeout messages
  - `Wait.Step(label)` for tracking test progress
- **Input Stability**:
  - `SimulatedInputGuard` class with input system readiness checks
  - `InputSystemReady()` helper to detect input availability
  - `IsPointerOverUI()` helper for UI overlap detection
- **UI Panel Monitoring**:
  - `PanelStateMonitor` class for structured panel state validation
  - Panel state info: IsVisible, Alpha, Interactable, ActiveInHierarchy

### Changed
- Failure bundles now include: diagnostics.json, diagnostics.md, logs, screenshots, hierarchy dumps
- All diagnostic features route through TestLog for centralized logging

### Tests
- Added `TestRunContextTests` (EditMode) for serialization validation
- Added `TestLogTests` (EditMode) for logging API validation
- Added `FailureBundleWriterTests` (PlayMode) for bundle generation verification
- Added `PanelStateMonitorTests` (PlayMode) for panel state detection accuracy

---

## [1.1.3] - 2025-12-19

### Changed
- **Performance**: Optimized `VisualTreeLogger.DumpHierarchy` to group repetitive "boring" sibling objects (like tiles) to save tokens and improve readability for AI analysis.

---

## [1.1.2] - 2025-12-13

### Fixed
- **Deprecation Warnings**: Replaced all deprecated Unity `Object.FindObjectOfType` and `Object.FindObjectsOfType` calls with modern equivalents:
  - `FindObjectOfType<T>()` → `FindFirstObjectByType<T>()` (Files: `Wait.cs`, `TextInput.cs`)
  - `FindObjectOfType<T>(bool)` → `FindFirstObjectByType<T>(FindObjectsInactive.Include)` (Files: `ClickActions.cs`, `InputHelpers.cs`)
  - `FindObjectsOfType<T>(bool)` → `FindObjectsByType<T>(FindObjectsSortMode.None)` (Files: `ClickActions.cs`, `Wait.cs`)
  - `FindObjectsOfType(Type)` → `FindObjectsByType(Type, FindObjectsSortMode.None)` (File: `InputHelpers.cs`)
- **CS0414 Warning**: Suppressed unused field warning for sample `requiredScore` field using pragma (File: `SampleCompleteLevel1.cs`).
- **CS1998 Warning**: Added `await Task.Yield()` to async test method to ensure proper async execution (File: `ImprovementTests.cs`).

### Changed
- **Performance**: Using `FindObjectsSortMode.None` provides faster object lookups since results don't need to be sorted by InstanceID.

---

## [1.1.0] - 2025-12-13

### Added
- **Feature**: Configurable report output path via `TestRunner.ReportOutputPath` (File: `Runtime/Core/TestRunner.cs`).
- **Feature**: Report events `TestRunner.OnReportGenerated` and `TestRunner.OnAllTestsCompleted` for CI/CD integrations (File: `Runtime/Core/TestRunner.cs`).
- **Feature**: Custom report handler via `TestRunner.CustomReportHandler` and `ITestReportHandler` interface (File: `Runtime/Core/ITestReportHandler.cs`).
- **File**: New `ITestReportHandler.cs` interface for advanced report customization.

---

## [1.0.2] - 2025-12-11

### Fixed (Critical - Compilation)
- **Async Syntax**: Fixed invalid return statements in `Click.WorldObject` async method (File: `Runtime/Input/ClickActions.cs`).
- **Variable Name**: Fixed undefined variable `pointer` → `data` in `Touch.SimulateTouch` (File: `Runtime/Input/Touch.cs`).
- **ExecuteEvents**: Corrected `initializePotentialDragHandler` → `initializePotentialDrag` (File: `Runtime/Input/Touch.cs`).

---

## [1.0.1] - 2025-12-10

### Fixed (Critical)
- **Compilation**: `ClickActions.WorldObject` marked `async` (File: `Runtime/Input/ClickActions.cs`).
- **Compilation**: `Scroll.UntilVisible` fixed missing variables and broken logic (File: `Runtime/Input/Scroll.cs`).
- **Compilation**: `InputSystemShim` fixed syntax error in dictionary initializer (File: `Runtime/Input/InputSystemShim.cs`).
- **Thread Safety**: `RealPlayExecutionContext.Token` is now `AsyncLocal<CancellationToken>` (File: `Runtime/Core/RealPlayTesterHost.cs`).

### Fixed (Logic)
- **Async Safety**: `TestRunner` handles exceptions in fire-and-forget calls via `RunAllAsyncSafe` (File: `Runtime/Core/TestRunner.cs`).
- **DevTools**: `Breakpoint` avoids infinite loop by using unscaled time (File: `Runtime/Utilities/DevTools.cs`).
- **Capture**: `Capture.Screenshot` is now synchronous using `Texture2D.ReadPixels` (File: `Runtime/Assert/Capture.cs`).
- **Touch**: `SimulateTouch` triggers `IDragHandler` events (File: `Runtime/Input/Touch.cs`).
- **Input**: `TextInput` respects `characterLimit` (File: `Runtime/Input/TextInput.cs`).

### Added
- **Feature**: Horizontal scrolling support in `Scroll.UntilVisible` (File: `Runtime/Input/Scroll.cs`).
- **Feature**: TMP (TextMeshPro) support in `Wait.ForInteractable<T>` via reflection (File: `Runtime/Await/Wait.cs`).
- **Data**: ~50 missing key mappings added to `InputSystemShim` (File: `Runtime/Input/InputSystemShim.cs`).
- **Safety**: `VisualTreeLogger` depth limit set to 15 (File: `Runtime/Utilities/VisualTreeLogger.cs`).
- **Safety**: `Drag.FromTo` applies `ClampToScreen` (File: `Runtime/Input/ClickActions.cs`).

### Changed
- **UX**: Test Hotkey changed from `F12` to **`F9`** (File: `Runtime/Core/TestRunner.cs`, `README.md`).
- **State**: `LogAssert` clears expected patterns on `StartListening` (File: `Runtime/Assert/LogAssert.cs`).
- **Threading**: `Assert` ensures overlay creation runs on Main Thread (File: `Runtime/Assert/Assert.cs`).
