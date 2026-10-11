# Gameplay layout and caption cleanup — 2026-10-11

Classic's booster toolbar now has breathing room above the bottom safe-area edge. Its offset is 9% of the safe-area width, equivalent to 32.4 logical units at a 360-unit width. The board center moves from 52% to 55.5% of the safe-area height. The board's size formula is unchanged. The tray's upper anchor moves to 27.5%, and its lower anchor accounts for the raised toolbar plus padding. Waiting pieces still fit their slots, including Vertical5, Horizontal5 and Square3x3.

The template “Drag blocks to the grid” caption is empty and inactive in both gameplay scenes. Shop's development footer is also empty and inactive. Purchase confirmation uses “Confirm purchase” and “Confirm”; reward details show the product contents without a “DEMO” prefix. Scores, inventory counters, objectives, remaining moves and useful booster instructions remain visible.

These changes affect presentation and the corresponding scene builders. Board occupancy, placement rules, scoring, Adventure turn limits, booster effects, item consumption and wallet serialization are unchanged. Shop still uses the previously authorized Editor demo purchases; no payment or advertising SDK has been added, and purchases remain blocked in Player builds.

The Editor resolution helper now resizes an existing Game View without calling `Focus()` or opening a tab. Background verification uses Unity MCP and queued player updates. The physical Mouse is temporarily disabled only within Unity's Input System to keep synthetic test input separate, then restored; Windows mouse use is unaffected.

## Visual evidence

- Classic: [1080×1920](Classic_Boosters_Unity_1080x1920.png), [1440×2960](Classic_Boosters_Unity_1440x2960.png), [bomb targeting](Classic_BombTarget_Unity_1080x1920.png).
- Adventure: [1080×1920](Adventure_Boosters_Unity_1080x1920.png), [1440×2960](Adventure_Boosters_Unity_1440x2960.png), [bomb targeting](Adventure_BombTarget_Unity_1080x1920.png).
- Shop: [Bundles](ShopBundles_Unity_1080x1920.png), [Items](ShopItems_Unity_1080x1920.png), [confirmation](ShopConfirm_Unity_1080x1920.png), [reward](ShopDemoReward_Unity_1080x1920.png).

The layout fixtures measure at least 48 logical units for booster touch regions, at least 30 logical units between Classic controls and the safe-area bottom, no board/tray overlap, and no waiting piece over a booster touch region. Both portrait resolutions include simulated top notches and bottom gesture insets. These are Editor checks; no Android/iOS device run is claimed.

## Verification

| Check | Result |
| --- | --- |
| [Full menu/game regression](MenuVerification.md), including all ten Adventure levels and both booster modes | **3302 PASS** |
| [Isolated booster checks](BoosterVerification.md), including the new spacing/hidden-caption assertions | **344 PASS** |
| [Daily/Shop checks](CommerceVerification.md), including hidden footer and confirmation/reward text | **839 PASS** |
| [Core Classic mouse/touch regression](../BlockCrush/Verification.md) | **65 PASS** |
| [Cache/HUD/layout checks](OptimizationVerification.md) | **43 PASS** |
| Scene builder repeated twice on each preview gameplay scene | Stable object counts; one controller, one target, one callback per booster |
| Windows Player script compile | **26 assemblies PASS** |
| Final Unity Console | **0 errors / 0 warnings** |
| Original player data after verification | All **27** preference/progression keys and exact wheel/wallet JSON match the pre-test snapshot |

The final full regression ran with Unity unfocused through MCP. Earlier test attempts stopped on synthetic clicks; their diagnostics are retained locally in ignored Logs. After removing the resolution helper's focus calls and isolating test input, the complete run passed without changing any gameplay controller or rules.
