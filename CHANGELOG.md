# Changelog

## [1.8.1] - 2026-01-01
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