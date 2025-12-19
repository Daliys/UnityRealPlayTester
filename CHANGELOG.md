# Changelog

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
