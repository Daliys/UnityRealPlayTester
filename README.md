# RealPlayTester

Drop-in, zero-config package for writing real human-like playtests in 3–8 lines of async/await C#. Works in Unity 6000.2+ (editor + development builds on PC/mobile/console). Inputs are injected via real pointer/keyboard events (EventSystem + raycasts), never direct invokes.

## Quickstart
1. Drop the `RealPlayTester/` folder into your project.
2. Create a test asset:
```csharp
[CreateAssetMenu(menuName = "RealPlay Tests/Complete Level 1")]
public class CompleteLevel1 : RealPlayTest
{
    [SerializeField] private GameObject enemyBoss;
    [SerializeField] private GameObject player;

    protected override async Task Run()
    {
        await Click.ScreenPercent(0.5f, 0.8f);  // Click start button
        await Wait.Seconds(3f);                  // Wait for level load
        await Press.Key(KeyCode.W, 15f);         // Move forward
        await Click.WorldObject(enemyBoss);      // Attack boss
        await Wait.Until(() => player.activeInHierarchy && !enemyBoss.activeInHierarchy);
        Assert.IsTrue(GameManager.Score > 50000, "Score too low");
    }
}
```
3. Place test assets under `Resources/RealPlayTests` (ensures discovery in builds).
4. Run tests: press **F12** in editor or dev build, or launch with `-runRealTests` command-line argument (exit code = failure count).

## Public API
- Base: `public abstract class RealPlayTest : TestBase { protected abstract Task Run(); }`
- **Click**: `ScreenPercent(x, y)`, `ScreenPixels(x, y)`, `WorldPosition(pos, cam)`, `WorldObject(go, cam)`, `RaycastFromCamera(cam, pos)`
- **Press**: `Key(key, duration)`, `KeyDown(key)`, `KeyUp(key)`
- **Drag**: `FromTo(start, end, duration)`
- **Wait**: `Seconds(t, unscaled)`, `Frames(n)`, `Until(predicate, timeout)`, `SceneLoaded(name, timeout)`
- **Assert**: `IsTrue(cond, msg)`, `AreEqual(a, b, msg)`, `Fail(msg)`
- **Tester**: Static facade with all methods above (e.g., `Tester.ClickScreenPercent`, `Tester.WaitSeconds`).

## Behavior & Guarantees
- **Real inputs only**: EventSystem + raycasts + `ExecuteEvents`. No `.onClick.Invoke`.
- **Input system support**: Legacy Input Manager (limited) and new Input System (full support).
- **Dev-build guard**: Auto-disabled in non-development player builds.
- **Failures**: Screenshot to `persistentDataPath/RealPlayTester/Failures/<timestamp>.png`, red overlay, `Time.timeScale = 0`, then throw.
- **One-folder**: No external dependencies; no scene/prefab setup required.

## Running
- **F12** (editor + dev builds) runs all `RealPlayTest` assets sequentially.
- **CLI**: `-runRealTests` runs and quits; exit code = failure count.
- **Editor menu**: `RealPlayTester/Run All (F12)`.

## Performance Notes
- Uses pooled `PointerEventData` to reduce GC allocations.
- Scaled time waiting by default; handles `timeScale = 0` gracefully.
- KeyCode→InputSystem.Key mapping is cached at startup.

## Notes
- Keep tests short and focused; prefer Resources placement for builds.
- Package follows Allman braces, <500 lines per file. XML docs on all public APIs.
