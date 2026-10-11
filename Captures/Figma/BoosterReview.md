# Gameplay booster implementation and verification

Project: Unity 6000.0.68f1, `D:/Unity/GameBlock`. Both saved gameplay scenes use all three existing Figma icons. Work and tests ran in the background through Unity MCP without computer use.

## Chosen rules

| Icon / inventory | Effect in Classic and Adventure | Successful cost |
| --- | --- | --- |
| Bomb / `bombs` | Select a cell; clear occupied cells in the surrounding 3×3 area, clipped at board edges. Tap Bomb again to cancel. | One bomb; no score, combo or move change. |
| Circular arrows / `reverses` | Replace only remaining tray pieces with different shapes that fit the board. Preserve colors, used slots and normal set/refill count. | One reverse; no board, score, combo or move change. |
| +1 / `boosters` | Convert the largest waiting piece to Single 1×1 in its existing slot/color. | One booster; no immediate score or extra move. Normal placement still scores and consumes an Adventure move. |

Bomb and tray refresh are established options in this genre: [DoonDookStudio's BlockUp Puzzle](https://doondook.studio/game/block-up-html5-puzzle-game/) lists Bomb and Refresh among its boosters. The exact 3×3 effect and +1 single-block conversion are design decisions for this project, chosen to make all three useful in both modes while preserving the existing scoring and Adventure move budgets.

No free items are granted. Players use existing Lucky Wheel/Shop inventory. The Shop remains the authorized Editor demo. Empty/invalid bomb areas, cancellation, already-single trays and impossible/no-effect refreshes cost nothing. Failed refreshes preserve the random generator state. The controller rejects dragging, clear animations, settings/information modals, transitions and terminal games. Same-frame duplicate callbacks spend once. A stale bomb selection rechecks saved inventory before clearing cells. Secondary pointers cannot take ownership of the active bomb pointer.

## Scope preserved

Original scoring, combos, targets, move budgets, tray refill rules, daily milestones, Shop grants/prices and Lucky Wheel grants/weights are unchanged. Saved board/spawner/level-rule components retain their original configuration. Home, Settings prefab, data assets, Packages and ProjectSettings were not edited.

The core manager gains only a restart notification after its existing refill; the optional controller uses it to clear targeting/presentation. Adventure exposes its existing level weights and redirects the obsolete availability callback. The wallet gains a consumption method while retaining its original JSON schema and grant/claim APIs. Other changes are booster-specific runtime/editor components, two gameplay scenes and verification/documentation.

Classic initially added a toolbar below the tray and moved the drag instruction above the board. The subsequent [gameplay layout update](GameplayLayoutReview.md) lifts the Classic board and toolbar and removes that template caption; the linked captures now show the updated layout. Adventure preserves button artwork placement and expands transparent hit regions. `GameplayBoosterLayout` scales waiting pieces within their slots so long shapes cannot cover any booster hit region. The original drag code continues to use board-cell dimensions. The layout fixture explicitly includes Vertical5, Horizontal5 and Square3x3. Both modes pass a minimum 48 logical units at a normalized 360-unit viewport width at 1080×1920 and 1440×2960, including simulated top notches and bottom gesture insets. This is a measured layout check, not physical-device accessibility certification. Bomb selection follows the board after resizing.

## Final validation

| Check | Result |
| --- | --- |
| [Full menu/gameplay regression](MenuVerification.md), including 10 levels, wheel, Daily, Shop, audio and both booster modes | **3,289 PASS** |
| [Isolated booster fixtures](BoosterVerification.md), saved callbacks, real mouse/touch, consumption, no-effect/duplicate/stale actions, pause/restart/terminal guards and layout | **332 PASS** |
| [Classic core mouse/touch regression](../BlockCrush/Verification.md) | **65 PASS** |
| [Wallet/cache/HUD/layout regression](OptimizationVerification.md) | **43 PASS** |
| Repeated booster builder in two preview scenes | Object counts stable; one controller/target per scene; each of three buttons has exactly one booster callback; active Home preserved. |
| Editor compilation / Console | PASS; final Console has **0 errors and 0 warnings**. |
| Windows Player script compilation | PASS; **26 assemblies**, including runtime boosters; Editor-only tests excluded. |
| Formatting check | All 12 changed/new C# files retain identical tokens before/after normalization in Editor and Player branches. |
| Saved data comparison | **27 preference/progression keys and raw wallet/wheel JSON exactly restored** to the original snapshot. |
| Git whitespace check | PASS. |

Home's normal UTC rollover occurred on first entering Play before the fixture snapshot: the prior day's Daily progress reset through existing code. The original pre-run wallet was restored explicitly afterward, then compared byte-for-byte along with progression and wheel records. Test purchases/booster consumption do not remain in the user's save. Final Editor is idle in Home, outside Play Mode, with background execution restored to its original setting.

## Visual evidence

- Classic toolbar: [1080×1920](Classic_Boosters_Unity_1080x1920.png), [1440×2960](Classic_Boosters_Unity_1440x2960.png).
- Adventure toolbar: [1080×1920](Adventure_Boosters_Unity_1080x1920.png), [1440×2960](Adventure_Boosters_Unity_1440x2960.png).
- Bomb selection and guidance: [Classic](Classic_BombTarget_Unity_1080x1920.png), [Adventure](Adventure_BombTarget_Unity_1080x1920.png).

Runtime checks and captures are from the Unity Editor. Player validation compiles Windows scripts; it is not a full standalone/mobile build or a test on an Android/iOS device. Generated regression captures for unrelated screens were retained locally under ignored `Logs/BoosterCaptures`; only the six new booster captures are added to this change.

To reproduce, open Home and enter Play, then call `BlockCrush.UI.Editor.BlockCrushMenuVerification.StartBoosters()` or `Start()` for the full regression. Do not run suites concurrently. For manual inventory testing, open Home → Shop, confirm an Editor demo bundle, then start Classic or Adventure. To reconnect saved scenes, exit Play and use **Block Crush > Update Gameplay Boosters** with all open scenes saved.
