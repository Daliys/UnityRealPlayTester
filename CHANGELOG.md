# Changelog

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
- **Test**: Added `Runtime/Tests/ProductionVerificationTest.cs` for runtime verification.

### Changed
- **UX**: Test Hotkey changed from `F12` to **`F9`** (File: `Runtime/Core/TestRunner.cs`, `README.md`).
- **State**: `LogAssert` clears expected patterns on `StartListening` (File: `Runtime/Assert/LogAssert.cs`).
- **Threading**: `Assert` ensures overlay creation runs on Main Thread (File: `Runtime/Assert/Assert.cs`).
