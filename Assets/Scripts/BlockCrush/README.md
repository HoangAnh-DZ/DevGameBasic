# Block Crush gameplay

Open `Assets/Scenes/BlockCrushGameplay.unity` and press Play. All scene references are already assigned.

The board has 64 serialized prefab instances. Coordinates use `(column, row)` with row zero at the bottom. Shape coordinates are relative to their lower-left bounding box. Mouse and touch use the Input System UI module and EventSystem drag handlers. Touch lifts the piece above the finger; placement uses RectTransform coordinates and board cell size.

All blocks in one piece share one imported texture through a persistent Sprite reference asset in `Assets/Art/BlockCrushSpriteReferences`. Source PNGs and their import metadata in `Assets/UI` are unchanged. The background, grid and settings icon also reuse existing artwork.

The tray generates three distinct weighted random shapes. Used slots remain empty until all three are consumed. Larger shapes have lower weights. Full rows and columns are collected before clearing; their intersecting cells are deduplicated. Clear feedback lasts 0.16 seconds, with placement locked during the animation.

Placement awards one point per block. A simultaneous clear of `n` lines awards `100 * n * n + 25 * (combo - 1) * n`. Clearing increments combo; a placement without a clear resets it. Best score uses PlayerPrefs key `BlockCrush.BestScore`.

Game over requires every remaining piece to fail at every possible board anchor. Restart clears all transient game state while retaining best score. Settings now opens the shared Figma popup and pauses interaction and line-clear feedback; closing it resumes the same game.

Game Over now uses the current Figma artwork and displays the actual final score. Home returns to the startup scene; the yellow Play button restarts the session. Best Score continues to persist, with its Game Over text hidden because the design does not contain that field. Use `BlockCrush.UI.Editor.BlockCrushScreenBuilder.BuildGameOver()` to reapply this screen after intentionally rebuilding the gameplay scene.

The overlay Canvas uses a 1080x1920 reference and match 0.5. Layout respects Screen.safeArea, keeps the board square and covers the screen with the original background. Home is first in Build Settings, followed by this gameplay scene; portrait orientation is configured. Menu details are documented in `Assets/BlockCrush/Scripts/UI/README.md`.

Verification: enter Play Mode, then use `Block Crush > Verify Current Scene in Play Mode`. The checks send synthetic Mouse and Touchscreen state events through the real InputSystemUIInputModule and EventSystem, exercise actual scene components and verify both 1080x1920 and 1440x2960. Verification restores the previous best score, random state and input settings. Results and screenshots are written to `Captures/BlockCrush`.

`Block Crush > Build Gameplay Scene` rebuilds the generated scene with all references. This requires a saved current scene and being outside Play Mode.
