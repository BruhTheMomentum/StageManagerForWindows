# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run

```bash
dotnet run --project StageManager          # Run in Debug mode
dotnet build                               # Build only
dotnet publish StageManager/StageManager.csproj --configuration Release --runtime win-x64 --self-contained true -p:PublishSingleFile=true  # Release build
```

No test projects exist. Verify changes by building and running manually.

## Architecture

macOS Stage Manager clone for Windows. Groups windows by process into "scenes", showing one scene at a time while hiding others via Win32 opacity tricks.

```
MainWindow.xaml.cs          UI + sidebar + global mouse hooks (SharpHook)
    ↓ SwitchSceneCommand
SceneManager.cs             Orchestration: scene switching, window grouping, desktop toggle
    ↓ events
WindowsManager.cs           Window tracking via WinEventHook, mouse hooks, focus detection
    ↓
OpacityWindowStrategy.cs    Hides windows by setting alpha=0 + WS_EX_TRANSPARENT (keeps DWM thumbnails live)
```

**Key flow**: Click sidebar scene → animation plays (SceneTransitionAnimator) → SceneManager.SwitchTo() hides other windows (alpha→0) and shows target windows (alpha→255 instant) → sidebar updates via CurrentSceneSelectionChanged event.

## Key Design Decisions

- **OpacityWindowStrategy** over minimize: windows stay at alpha=0 so DWM can still render live thumbnails in the sidebar. The `IWindowStrategy` interface allows swapping strategies.
- **[Conditional("DEBUG")]** on `Log` class: all logging compiles away in Release. Log output goes to `stagemanager.log` next to the exe via `TextWriterTraceListener`.
- **Scene grouping by process**: `Scene.Key` is the process filename. All windows from the same process belong to one scene.
- **Reentrancy protection**: `SceneManager.SwitchTo` has a reentrancy guard (`_reentrancyLockSceneId`) because focus events can trigger recursive switches. The animation code checks `IsCurrentScene()` before doing destructive work (hiding windows, collapsing sidebar items).

## P/Invoke Organization

Win32 APIs are in `Native/PInvoke/` as partial classes on `Win32`:
- `Win32.cs` — constants, enums, core functions
- `Win32.Window.cs` — SetWindowPos, window positioning
- `Win32.Long.cs` — Get/SetWindowLong, extended styles (WS_EX)
- `Win32.WinEvent.cs` — SetWinEventHook, event constants

DWM thumbnail APIs are in `Native/Interop/NativeMethods.cs`.

## Animation System (WIP)

`Animations/SceneTransitionAnimator.cs` uses a separate transparent topmost WPF window (`TransitionOverlayWindow`) as an overlay. Placeholder rectangles animate from sidebar position to window position (incoming) and vice versa (outgoing). Duration: 300ms, PowerEase EaseOut.

The overlay has `WS_EX_TOOLWINDOW | WS_EX_TRANSPARENT` so it doesn't appear in Alt-Tab or intercept clicks.

## CI/CD

Pipeline (`.github/workflows/dotnet-desktop.yml`) only triggers on `v*` tags. Regular pushes to main don't trigger builds. Includes CodeQL analysis and Gitleaks secret scanning.

## Target Framework

.NET 10.0 (WPF + WinForms enabled). SDK version pinned in `global.json` to `10.0.100-rc.2.25502.107`.
