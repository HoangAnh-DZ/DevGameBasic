# Code optimization verification — 2026-10-10

Project: Unity 6000.0.68f1, `D:/Unity/GameBlock`.

## Changes

- Wallet and Daily views reload JSON when the saved economy revision changes or the view opens. The revision advances after the existing save transaction completes.
- Daily/Wheel share countdown formatting and update once per second, with UTC rollover and clock rollback handled by the existing eligibility rules.
- Layout components skip unchanged geometry; contained rewards still follow their host during movement, live resizing and safe-area changes.
- Adventure HUD skips unchanged text/progress writes. Board preview reuses its buffer and skips repeated highlights after placement validation.
- Editor UI builders share geometry helpers, refuse unsaved scenes/Play Mode, release temporary prefab contents reliably and reconnect gameplay feedback to replaced SettingsUI audio.
- C# formatting is consistent with `.editorconfig`. Catalog validation gives clear errors for invalid Adventure configuration.
- Player compilation exposed a pre-existing Windows error at `Handheld.Vibrate()`. Its compilation is now limited to Editor/Android/iOS; the original mobile vibration condition is preserved.

## Verification

| Check | Result |
| --- | --- |
| Full menu/gameplay/economy regression | PASS — 2,885 checks |
| Classic real Mouse/Touchscreen input regression | PASS — 65 checks |
| Optimization/cache/layout regression | PASS — 43 checks |
| Editor rebuild safeguards/audio reconnection in disposable scenes | PASS — 6 checks |
| Unity Editor compilation | PASS |
| StandaloneWindows64 Player script compilation, without `UNITY_EDITOR` | PASS — 26 assemblies including Assembly-CSharp |
| C# syntax, both Editor and Player branches | PASS — 55 scripts |
| Formatting-only scripts | PASS — 33 scripts retain identical tokens |
| Final Console | 0 errors, 0 warnings |

The 2,993 runtime checks include all eight wheel outcomes, interruption recovery and acknowledgement, duplicate reward protection, Daily eligibility and claims, Shop demo purchases/cancellation, all ten Adventure levels, final-move completion, progression, score/combo/line clearing, settings/audio, popup callbacks, navigation, invalid/cancelled drops and restart. Layout verification covers 1080×1920 and 1440×2960, live changes and simulated safe areas.

Measured optimization checks confirm no repeated wallet JSON loading or layout applications in stable idle views, no repeated Adventure HUD writes when values are unchanged, and **zero managed bytes allocated by 1,000 same-anchor preview calls**. Changed anchors and occupancy still invalidate the preview. These measurements do not claim an FPS improvement or zero allocations across the whole game.

The six Editor checks confirm that a dirty scene blocks rebuilding; an existing gameplay presenter initially references its SettingsUI audio; replacing SettingsUI and reconfiguring feedback reconnects the new audio; required references remain valid; the Combo font and initial hidden state remain correct; and clean Home is restored after disposing the temporary scenes. Production scenes/prefabs were not rebuilt during these checks.

## Preserved behavior and data

Gameplay formulas, shape randomness, Adventure targets/move budgets, wheel identities/weights/animation, Daily milestones/rewards, Shop contents/prices, receipt identities, persistence keys/schema and UI assets are unchanged. The source token comparison also confirms that Editor/Android/iOS audio branches keep their original statements.

All 27 original preference/progression keys and the exact original wallet/wheel JSON were compared after testing and restored. Home is open in Edit Mode without unsaved changes; `runInBackground` is restored. No scene, prefab, catalog, package or ProjectSettings changes are included. Existing unrelated template deletions are excluded from the optimization commit.

This validation compiles Windows Player scripts but does not create a full executable/APK or replace testing on an Android/iOS device. Fresh Daily, Shop and contained reward captures were visually inspected in addition to automated layout checks. Manual gameplay testing remains the next step before pushing.

Detailed reports: [full regression](MenuVerification.md), [Classic regression](../BlockCrush/Verification.md), [optimization regression](OptimizationVerification.md).
