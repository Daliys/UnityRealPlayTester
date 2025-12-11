# RealPlayTester

## Overview
A zero-config Unity package for writing automated playtests using real human-like inputs (EventSystem + raycasts). Tests are authored as async/await C# ScriptableObjects.

**Compatibility**: Unity 6000.2+ | Editor | Development Builds (PC, Mobile, Console)
**License**: MIT

---

## Installation
1. Copy the `RealPlayTester/` folder into your project's `Assets/`.
2. No configuration required.

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
        await Click.ScreenPercent(0.5f, 0.5f);
        await Wait.Seconds(1f);
        Assert.IsTrue(true, "Success");
    }
}
```
Place test assets in `Resources/RealPlayTests/` for runtime discovery.

---

## Running Tests
| Trigger | Description |
|---------|-------------|
| `F9` | Run all tests (Editor + Dev Builds) |
| `-runRealTests` | CLI: Run all, then quit with exit code = failure count |
| `--tags=smoke,ui` | CLI: Filter by tags |
| Menu: `RealPlayTester/Run All (F9)` | Editor menu item |

---

## API Reference

### Click
| Method | Description |
|--------|-------------|
| `Click.ScreenPercent(float x, float y)` | Click at screen position (0-1 range). |
| `Click.ScreenPixels(float x, float y)` | Click at pixel position. |
| `Click.WorldPosition(Vector3, Camera?)` | Click at world position. |
| `Click.WorldObject(GameObject, Camera?)` | Click on a GameObject. **Auto-scrolls if in ScrollRect.** |
| `Click.ButtonWithText(string)` | Find and click a Button by label text. **Auto-scrolls if needed.** |
| `Click.ObjectNamed(string)` | Find and click a GameObject by name. |
| `Click.Component<T>()` | Find and click the first component of type T. |
| `Click.RightClick(Vector2)` | Right-click at position. |
| `Click.MiddleClick(Vector2)` | Middle-click at position. |
| `Click.DoubleClick(Vector2, float?)` | Double-click at position. |
| `Click.Hold(Vector2, float)` | Click and hold at position. |
| `Click.RaycastFromCamera(Camera, Vector2)` | Raycast and return hit result. |

> **Note**: World interactions are automatically clamped to screen bounds to prevent "No Target" errors.

### Press (Keyboard)
| Method | Description |
|--------|-------------|
| `Press.Key(KeyCode, float)` | Press and hold key for duration. |
| `Press.KeyDown(KeyCode)` | Press key down (no release). |
| `Press.KeyUp(KeyCode)` | Release key. |

### Drag
| Method | Description |
|--------|-------------|
| `Drag.FromTo(Vector2, Vector2, float)` | Drag from start to end over duration. |

### Wait
| Method | Description |
|--------|-------------|
| `Wait.Seconds(float, bool unscaled=false)` | Wait for seconds (scaled or unscaled time). |
| `Wait.Frames(int)` | Wait for N frames. |
| `Wait.Until(Func<bool>, float? timeout)` | Wait until predicate is true. |
| `Wait.While(Func<bool>, float? timeout)` | Wait while predicate is true. |
| `Wait.SceneLoaded(string, float? timeout)` | Wait for scene to load. |
| `Wait.ForObject(string, float? timeout)` | Wait for GameObject by name to exist. |
| `Wait.ForComponent<T>(float? timeout)` | Wait for component to exist. |
| `Wait.ForUIVisible(string, float? timeout)` | Wait for named UI to be active and interactable (checks parent CanvasGroups). |
| `Wait.ForInteractable<T>(string text?, float? timeout)` | Wait for component T to be fully visible, enabled, and interactable. **Supports TMP.** |
| `Wait.ForAnimationState(Animator, string, float?)` | Wait for Animator state. |
| `Wait.ForAudioComplete(AudioSource, float?)` | Wait for audio to stop playing. |
| `Wait.ForLoadingComplete(string?, float?)` | Wait for loading screen to disappear. |

### Assert
| Method | Description |
|--------|-------------|
| `Assert.IsTrue(bool, string?)` | Fail if false. |
| `Assert.IsFalse(bool, string?)` | Fail if true. |
| `Assert.AreEqual<T>(T, T, string?)` | Fail if not equal. |
| `Assert.IsNull(object, string?)` | Fail if not null. |
| `Assert.IsNotNull(object, string?)` | Fail if null. |
| `Assert.Greater<T>(T, T, string?)` | Fail if value <= threshold. |
| `Assert.Less<T>(T, T, string?)` | Fail if value >= threshold. |
| `Assert.InRange<T>(T, T, T, string?)` | Fail if value outside range. |
| `Assert.Contains(string, string, string?)` | Fail if substring not found. |
| `Assert.Throws<T>(Action, string?)` | Fail if exception T not thrown. |
| `Assert.Fail(string?)` | Immediately fail. |

**On Failure**: Screenshot saved to `persistentDataPath/RealPlayTester/Failures/<timestamp>.png`, `Time.timeScale = 0`, red overlay shown.

### Text
| Method | Description |
|--------|-------------|
| `Text.Type(string, float delay=0.05f)` | Type into currently focused input field. |
| `Text.TypeIntoField(string fieldName, string, float?)` | Find field by name and type text. |

### Touch
| Method | Description |
|--------|-------------|
| `Touch.Tap(Vector2, float duration=0.1f)` | Simulate tap. |
| `Touch.Swipe(Vector2, Vector2, float duration=0.3f)` | Simulate swipe. |
| `Touch.Pinch(Vector2 center, float start, float end, float dur)` | Simulate pinch. |
| `Touch.LongPress(Vector2, float duration=1f)` | Simulate long press. |

### Scroll
| Method | Description |
|--------|-------------|
| `Scroll.ToBottom(ScrollRect, float duration=0.5f)` | Scroll to bottom. |
| `Scroll.UntilVisible(ScrollRect, RectTransform, float timeout=5f)` | Scroll until element is visible (Vertical & Horizontal). |
| `Scroll.EnsureVisible(GameObject)` | Helper to find parent ScrollRect and scroll target into view. |

### Capture
| Method | Description |
|--------|-------------|
| `Capture.Screenshot(string? name)` | Take screenshot, return path. |
| `Capture.CompareToBaseline(string path, float threshold=0.95f)` | Visual regression test. |
| `Capture.StartRecording()` | (Stub) Start video recording. |
| `Capture.StopRecording(string? path)` | (Stub) Stop video recording. |

### Debug / DevTools
| Method | Description |
|--------|-------------|
| `DevTools.Breakpoint(KeyCode resumeKey=Space)` | Pause test until key pressed. |
| `DevTools.ShowClickMarker(Vector2, float dur=0.5f)` | Visual marker at position. |
| `DevTools.SetSlowMotion(float timeScale=0.25f)` | Set Time.timeScale. |
| `DevTools.Inspect<T>(string name, T value)` | Log value to console. |
| `Tester.ResetCursor()` | Safely center the cursor. |

---

## AI & Debugging Tools

These tools help AI agents diagnose test failures autonomously.

| Method | Description |
|--------|-------------|
| `Tester.DumpHierarchy()` | Returns string dump of scene hierarchy (auto-appended to failures). |
| `Tester.FindObject(string fuzzyName)` | Fuzzy find: exact → case-insensitive → contains match. |
| `Tester.ProbeScreen(Vector2 pos)` | Returns string describing what UI/World is under pixel. |
| `Tester.AssertNoLogErrors()` | Fail if any unexpected `Debug.LogError` occurred. |
| `Tester.ExpectLog(string regex)` | Mark expected error pattern (ignored by AssertNoLogErrors). |

**Hierarchy Dump Format**:
```
[+] Canvas
  [+] Panel_Login
    [+] Button_Submit (Button) [Text: "Login"]
    [-] LoadingSpinner
```

---

## Runner Attributes
| Attribute | Description |
|-----------|-------------|
| `[TestTag("smoke")]` | Tag tests for filtering with `--tags=smoke`. |
| `[TestData("easy", "hard")]` | Parameterized test data. |
| `[TestData("PropertyName", values...)]` | Data injected into property. |

---

## Tester Facade
All API methods are also available via `Tester.*`:
- `Tester.ClickScreenPercent(...)`, `Tester.WaitSeconds(...)`, `Tester.AssertTrue(...)`, etc.

---

## Behavior & Guarantees
- **Real Inputs**: Uses `EventSystem`, `ExecuteEvents`, `PointerEventData`. Never calls `.onClick.Invoke()`.
- **Auto-Scroll & Clamp**: Automatically scrolls targets into view and clamps clicks to screen valid area.
- **Input System**: Supports both Legacy Input Manager (limited) and new Input System (full).
- **Dev-Build Guard**: Package is auto-disabled in non-development builds.
- **TearDown Safety**: `TearDown()` is always called, even on test failure.
- **No Dependencies**: Self-contained, no external packages required.

---

## File Structure
```
RealPlayTester/
├── Runtime/
│   ├── Core/        # Tester, TestRunner, RealPlayTesterHost, TestBase
│   ├── Input/       # Click, Press, Drag, Text, Touch, Scroll, SmartFind
│   ├── Await/       # Wait
│   ├── Assert/      # Assert, Capture, LogAssert
│   └── Utilities/   # DevTools, VisualTreeLogger, InteractionProbe
├── Tests/           # Sample tests
├── Resources/       # Sample test assets
├── README.md
└── package.json
```
