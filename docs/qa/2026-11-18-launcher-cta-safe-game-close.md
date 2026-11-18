# QA - Launcher CTA and safe game close (2026-11-18)

Scope: UI HomePage, HomeViewModel manual close, GameProcessCloseService,
GameExitWatchdog and GameLaunchService argument handoff. Original R29 package,
translation content, installer and game files were not modified.

## Visual changes

- Clear three-step flow: Chon game -> Cai Viet hoa -> Mo game.
- Play button fills the 378px action area, uses a calmer indigo gradient, icon
  and short contextual hint. All primary actions have 60px height.
- Resource badge moved next to the artwork title, not squeezed next to Play.
- Original background artwork and muted video are preserved.
- WPF screenshot: D:\WAHU_WORK\WAHU_V322_LAUNCHER_V3_SAFE_CLOSE_VISUAL_QA_20261118.png

## Original close risks

- Manual close killed all processes with matching names, without verifying
  executable paths or asking confirmation.
- Exit watchdog could kill a game client after only 5 seconds without UnrealWindow.
  The 2026-09-18 log records a forced termination; zombie status unverified.
- Exit watchdog killed the official wrapper/launcher if it remained alive
  5 seconds after game client exit.

## Revised behavior

- Manual close filters by executable path under the selected game directory.
- First Yes/No asks for graceful WM_CLOSE, waits up to 5 seconds.
- If still running, separate Yes/No warns about force stop and unsaved data.
- Force quit targets only verified game PID; Steam and launcher excluded.
- Watchdog follows only the exact expected client executable path. Unknown paths
  fail open. Missing-window grace increased to 30 seconds, and other visible
  windows prevent automatic termination. No automatic launcher close.

## Test evidence

- Full solution unit tests: 214/214 PASS (Core 69, Infrastructure 145).
- WPF App release build: PASS, 0 warnings, 0 errors.
- Isolated dummy executable test: PASS. Two same-named processes from different
  game directories; only selected one was eligible/force stopped. No-window
  process survived graceful close. Unrelated process remained alive.
- WPF UI 1440x820 window screenshot and accessibility tree: PASS. 378x60 CTA,
  no clipping, contextual note visible. Existing installer binding unchanged.
- Real Wuthering Waves game E2E: HOLD. Stored WIN3 game path invalid; did not
  run or stop the game. Actual in-game shutdown/save behavior unverified.

Public v3.0.22 ZIP remains unchanged; source QA is not a public release.