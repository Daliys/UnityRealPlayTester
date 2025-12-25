# Changelog

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