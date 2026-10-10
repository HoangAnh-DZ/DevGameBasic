# Adventure 10 levels — 2026-10-10

Expanded the existing Figma map into a ten-card scroll view with fixed title/Play controls. Three original Figma preview illustrations are reused. The purple gameplay HUD now displays actual score, configured target, moves remaining and the level's own Best.

Adventure scoring is centralized in `AdventureScoreSystem.Calculate`: placed cells + line bonus 10/25/45/70, plus consecutive-clear steps of 5 capped at 25. Valid placement alone consumes a move. Existing board clearing deduplicates intersecting cells/lines; score is applied before animation once. Optional `IBlockGameRules` leaves the Classic branch unchanged.

`AdventureLevels.asset` contains ten targets, budgets 22/26/30/34/38/42/46/50/54/58 and four shape-family weights. New Adventure trays use bounded weighted selection from fitting shapes when available, without replacement, and refill only when all three pieces are used. Classic keeps its original random shape/color generation. No booster rules/inventory were provided; the existing availability controls consume nothing.

Win resolves after clearing and takes precedence on the final allowed move. Lose occurs on exhausted moves or when none of the remaining pieces fits. Existing completion/Game Over artwork supplies Next/Map and Retry. Restart resets the session but retains saved unlocks. Per-level Best/completion and highest unlocked are saved under Adventure keys. The legacy three-level sequential save migrates without deletion or overwriting Classic.

The initial supplied targets reached 0% measured wins from level 5 in the first simulation; targets and later-level shape weights were tuned. Initial targets remain in `AdventureLevelCatalog.Defaults()` and the initial 3,000-game metrics in `AdventureBalance_Initial.csv`. Final metrics are `AdventureBalance.csv`, with two deterministic legal-placement heuristics. Both use shared scoring/generation, an 8×8 board and the three-piece refill rule, without fixtures, boosters or retries. Runtime fixtures separately test score equations, final-move ordering, lose/retry and ten-level unlocks. Bot win rates are not human playtest measurements.

Figma MCP was checked but repeatedly returned an authentication request after acceptance. The map/gameplay were verified in the logged-in browser using nodes `7:147` / `7:1657`, and existing exported assets were reused. Sample 368 is target artwork content, replaced by configuration. A small Moves label and completion/Best states are the required dynamic additions. The hand is tutorial guidance only. Dynamic text uses the available bold runtime font; Montserrat's font file was not supplied.

Saved files: Home and Adventure scenes, existing core manager/spawner with optional mode rules, Adventure catalog/controller/progress/map/button scripts, screen builder, UI integration verification and simulation. Scene GUIDs/build order and shared Settings prefab are retained. No external assets/packages, manual Inspector setup, sub-agents, commits or pushes.

Final test counts/results are in `CompletionChecklist.md`, `MenuVerification.md`, `AdventureBalance.csv` and `../BlockCrush/Verification.md`.
