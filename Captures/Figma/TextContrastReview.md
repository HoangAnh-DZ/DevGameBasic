# Text contrast review — 2026-10-10

Reviewed Home, Adventure map, Classic, Adventure gameplay, win/loss, Settings/More Settings/information, Lucky Wheel, rewards, Daily and both Shop tabs. Applied color/edge changes to 125 labels across the saved scenes and Settings prefab. Original sprite textures, gameplay rules, prices and rewards are preserved.

`ReadableText.Ink` (#24123D) replaces white/yellow lettering on white, lavender and beige surfaces: reward/Shop wallets, all item counters, bundle contents, item amounts, Daily reward values and task titles. Bright wheel sectors use the same dark ink; purple sectors keep white with a solid dark outline. Wheel's silver used-spin timer also uses dark ink. Green buttons use #102114 with a thin white edge. Shop's selected tab has a bullet marker rather than yellow lettering. Shared reward, mixed-fill progress, Adventure HUD and Combo labels have a dark edge. Home Adventure/Classic and available Settings button labels are tinted dark; disabled Settings actions keep white on their dark disabled artwork.

Builders apply the same styles when recreating views. `Block Crush > Improve Text Contrast` updates existing scenes/prefab without recreating their objects or callbacks.

The design reference is [W3C contrast guidance](https://www.w3.org/WAI/WCAG22/Understanding/contrast-minimum.html): 4.5:1 for ordinary text and 3:1 for large text. These spot measurements compare the assigned foreground with one representative opaque pixel of the original surface PNG using sRGB relative luminance. Gradients, borders, disabled states, baked artwork, antialiasing and device density require separate visual review; this is not a complete accessibility certification.

| Surface | Sample RGB | Previous foreground ratio | New foreground ratio |
| --- | --- | ---: | ---: |
| Inventory pill (including reward wallet) | 242, 236, 255 | Yellow 1.04:1 | Dark ink 14.82:1 |
| Bundle contents | 225, 181, 242 | White 1.74:1 | Dark ink 9.84:1 |
| Item contents | 224, 178, 242 | White 1.78:1 | Dark ink 9.62:1 |
| Daily coin tile | 238, 200, 145 | White 1.58:1 | Dark ink 10.83:1 |
| Grey Claim | 171, 172, 177 | White 2.27:1 | Button ink 7.42:1 |
| Green purchase button | 1, 195, 36 | White 2.38:1 | Button ink 7.07:1 |

White against the solid #140C24 text edge is 18.96:1. The edge keeps small progress text distinct as its bar changes from brown/blue to green. The foreground remains bright on dark surfaces.

Fresh Unity runtime evidence: [reward wallet](LuckyWheel_CoinReward_Unity_1080x1920.png), [wheel sectors](LuckyWheel_Unity_1080x1920.png), [Daily progress/claimed state](DailyTasks_Claimed_Unity_1080x1920.png), [Bundles](ShopBundles_Unity_1080x1920.png), [Items](ShopItems_Unity_1080x1920.png), [Home](Home_Unity_1080x1920.png), [Adventure completion](AdventureComplete_Unity_1080x1920.png). Portrait captures also cover 1440×2960.

Validation: **2,885 complete runtime checks PASS** in [MenuVerification.md](MenuVerification.md), including the latest Wheel/Commerce regression fixtures, both portrait resolutions, simulated safe areas, actual mouse/button callbacks, animated rewards, claim/purchase/reload protection, all ten Adventure levels, Settings and Classic navigation. No final Console errors/warnings. Independent comparisons confirmed preferences/progression/Best, wheel JSON and wallet JSON were restored exactly. The Adventure balance asset SHA256 remains F861BA299D1C7CB214CA0483AF194E4373BC610847F74389F8E1B4794439FA64. Verification used Unity MCP and file tools without Computer Use.
