# Changelog

## [2.1.0] - 2026-01-10
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
    - `Wait.ForSteadyState` now intelligently bypasses ambient motion.
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
... (rest of the file)
