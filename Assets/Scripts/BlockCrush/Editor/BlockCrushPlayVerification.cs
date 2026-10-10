using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace BlockCrush.Editor
{
    // Integration checks run against the saved scene in Play Mode. No test code is included in player builds.
    public static class BlockCrushPlayVerification
    {
        public static string Status = "Not started";
        static readonly List<string> results = new List<string>();
        static readonly Stack<IEnumerator> stack = new Stack<IEnumerator>();
        static Mouse testMouse;
        static Touchscreen testTouch;
        static BlockGameManager game;
        static Sprite sprite;
        static int lastFrame;
        static double deadline;
        static bool hadBest;
        static int savedBest;
        static UnityEngine.Random.State randomState;
        static bool savedRunInBackground;
        static InputSettings.BackgroundBehavior savedBackgroundBehavior;
        static InputSettings.EditorInputBehaviorInPlayMode savedEditorInputBehavior;
        const string ReportPath = "Captures/BlockCrush/Verification.md";

        [MenuItem("Block Crush/Verify Current Scene in Play Mode")]
        static void VerifyFromMenu() { Start(); }

        public static string Start()
        {
            if (!EditorApplication.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
            if (stack.Count > 0) throw new InvalidOperationException("Verification already running.");
            game = UnityEngine.Object.FindFirstObjectByType<BlockGameManager>();
            if (!game) throw new InvalidOperationException("Gameplay scene is not loaded.");
            savedBest = PlayerPrefs.GetInt(BlockGameManager.BestScoreKey, 0);
            hadBest = PlayerPrefs.HasKey(BlockGameManager.BestScoreKey);
            randomState = UnityEngine.Random.state;
            savedRunInBackground = Application.runInBackground;
            savedBackgroundBehavior = InputSystem.settings.backgroundBehavior;
            savedEditorInputBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            Application.runInBackground = true;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            sprite = game.Spawner.BlockSprites[0];
            testMouse = InputSystem.AddDevice<Mouse>("BlockCrushVerificationMouse");
            testTouch = InputSystem.AddDevice<Touchscreen>("BlockCrushVerificationTouch");
            BlockCrushSceneBuilder.SetGameViewSize(1080, 1920);
            results.Clear(); Status = "Running"; lastFrame = -1; deadline = EditorApplication.timeSinceStartup + 90;
            stack.Push(Run()); EditorApplication.update += Tick;
            return Status;
        }

        static void Tick()
        {
            EditorApplication.QueuePlayerLoopUpdate();
            try
            {
                if (!EditorApplication.isPlaying) throw new Exception("Play Mode ended during verification.");
                if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Verification timed out.");
                if (Time.frameCount == lastFrame) return;
                lastFrame = Time.frameCount;
                while (stack.Count > 0)
                {
                    var top = stack.Peek();
                    if (!top.MoveNext()) { stack.Pop(); continue; }
                    if (top.Current is IEnumerator child) { stack.Push(child); continue; }
                    return;
                }
                Finish(true, null);
            }
            catch (Exception error) { Finish(false, error.ToString()); }
        }

        static void Finish(bool success, string error)
        {
            EditorApplication.update -= Tick; stack.Clear();
            if (testMouse != null && testMouse.added) InputSystem.RemoveDevice(testMouse);
            if (testTouch != null && testTouch.added) InputSystem.RemoveDevice(testTouch);
            testMouse = null; testTouch = null;
            if (hadBest) PlayerPrefs.SetInt(BlockGameManager.BestScoreKey, savedBest); else PlayerPrefs.DeleteKey(BlockGameManager.BestScoreKey);
            PlayerPrefs.Save(); UnityEngine.Random.state = randomState;
            if (game) game.RestartGame();
            InputSystem.settings.backgroundBehavior = savedBackgroundBehavior;
            InputSystem.settings.editorInputBehaviorInPlayMode = savedEditorInputBehavior;
            Application.runInBackground = savedRunInBackground;
            Status = success ? "PASS: " + results.Count + " checks" : "FAIL: " + error;
            Directory.CreateDirectory("Captures/BlockCrush");
            File.WriteAllText(ReportPath, "Block Crush Play Mode Verification\n\n" + Status + "\n\n" + string.Join("\n", results) +
                "\n\nInput: synthetic Mouse/Touchscreen state events → InputSystemUIInputModule → EventSystem → actual scene handlers.\n" +
                "Original best score restored after verification.\n");
            Debug.Log("[BlockCrushVerification] " + Status);
        }

        static void Check(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
            results.Add("PASS: " + message);
        }
        static BlockShapeData Shape(string name) => BlockShapeData.Database.First(s => s.id == name);
        static IEnumerator Frames(int count = 3) { for (int i = 0; i < count; i++) yield return null; }
        static IEnumerator Settled() { while (game.IsBusy) yield return null; yield return Frames(); }
        static Vector2 ToScreen(Vector3 world) => RectTransformUtility.WorldToScreenPoint(null, world);
        static Vector2 Destination(BlockPiece piece, Vector2Int anchor) => ToScreen(game.Board.PlacementCenter(piece.Shape, anchor));
        static void MouseEvent(Vector2 point, bool down)
        { InputSystem.QueueStateEvent(testMouse, new MouseState { position = point }.WithButton(MouseButton.Left, down)); }

        static IEnumerator BeginMouse(BlockPiece piece, Vector2 destination)
        {
            var start = ToScreen(piece.transform.position);
            MouseEvent(start, false); yield return Frames();
            var data = new PointerEventData(EventSystem.current) { position = start };
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(data, hits);
            Check(hits.Any(h => h.gameObject == piece.gameObject), "Piece is reachable by GraphicRaycaster");
            MouseEvent(start, true); yield return Frames();
            MouseEvent(start + new Vector2(20, 25), true); yield return Frames();
            MouseEvent(destination, true); yield return Frames();
            Check(piece.IsDragging && piece.DragEvents > 1, "Mouse state events invoke begin/drag handlers");
        }
        static IEnumerator ReleaseMouse(Vector2 point)
        { MouseEvent(point, false); yield return Frames(); yield return Settled(); }

        static void SetSingles()
        {
            game.RestartGame();
            for (int i = 0; i < 3; i++) game.Spawner.SpawnPiece(i, Shape("Single"), sprite);
        }

        static IEnumerator Run()
        {
            yield return Frames(6); game.RestartGame(); yield return Frames();
            Check(ScreenResolution(1080, 1920), "Game View renders exactly 1080x1920");
            Check(game.Board.CellCount == 64 && game.Board.OccupiedCount == 0, "Start: 8x8 board / 64 empty cells");
            Check(game.Spawner.RemainingCount == 3, "Start: exactly 3 pieces");
            Check(game.Spawner.Pieces.Select(p => p.Shape.id).Distinct().Count() == 3, "Initial shapes are distinct");
            Check(game.Spawner.BlockSprites.Length == 9 && game.Spawner.BlockSprites.All(s => s && s.texture &&
                AssetDatabase.GetAssetPath(s.texture).StartsWith("Assets/UI/arrow 3D/")), "All nine block colors reference existing textures");
            Check(game.Spawner.Pieces.All(p => p.GetComponentsInChildren<Image>().Where(i => i.transform != p.transform)
                .All(i => i.sprite == p.BlockSprite)), "Each runtime shape uses one consistent block sprite");
            Check(BlockShapeData.Database.Count >= 25 && BlockShapeData.Database.All(s => s.cells.Distinct().Count() == s.cells.Length),
                "Shape database includes lines, squares, rectangles and L/T/S/Z rotations without duplicate cells");
            ValidateResponsive();

            SetSingles(); yield return Frames();
            var piece = game.Spawner.SpawnPiece(0, Shape("Horizontal3"), sprite); yield return Frames();
            var target = Destination(piece, new Vector2Int(2, 2)); int sets = game.Spawner.SetsSpawned;
            yield return BeginMouse(piece, target);
            Check(game.Board.PreviewCount == 3 && game.Board.OccupiedCount == 0, "Valid drag preview is visible without changing board state");
            Check(game.Board.GetPreviewCells(piece.Shape, new Vector2Int(2, 2)).All(c => c.IsPreview), "Preview highlights all destination cells");
            yield return ReleaseMouse(target);
            Check(game.Board.OccupiedCount == 3 && game.Board.IsOccupied(2, 2) && game.Board.IsOccupied(4, 2), "Valid mouse drop places shape at the selected anchor");
            Check(game.Board.CellAt(3, 2).CurrentSprite == sprite && game.Score == 3 && game.ScoreText.text == "3", "Placement preserves color and updates score / UI");
            Check(game.Spawner.RemainingCount == 2 && game.Spawner.SetsSpawned == sets && game.Spawner.Pieces[0] == null, "Used slot stays empty; no immediate refill");
            Check(game.Board.PreviewCount == 0, "Drop clears preview");

            piece = game.Spawner.Pieces[1]; var parent = piece.transform.parent;
            target = Destination(piece, new Vector2Int(3, 2));
            yield return BeginMouse(piece, target);
            Check(game.Board.PreviewCount == 0, "Overlap placement has no valid preview");
            yield return ReleaseMouse(target);
            Check(piece && piece.transform.parent == parent && ((RectTransform)piece.transform).anchoredPosition == Vector2.zero &&
                game.Board.OccupiedCount == 3 && game.Score == 3, "Overlap drop returns to the original slot without changing board or score");

            yield return BeginMouse(piece, new Vector2(Screen.width * .02f, Screen.height * .98f));
            Check(game.Board.PreviewCount == 0, "Dragging outside the board clears preview");
            yield return ReleaseMouse(new Vector2(Screen.width * .02f, Screen.height * .98f));
            Check(piece.transform.parent == parent && game.Score == 3, "Outside drop preserves the piece and score");

            target = Destination(piece, new Vector2Int(0, 0)); yield return BeginMouse(piece, target);
            ExecuteEvents.Execute(piece.gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.cancelHandler);
            yield return ReleaseMouse(target);
            Check(!piece.IsDragging && game.Board.PreviewCount == 0 && piece.transform.parent == parent, "Cancel restores the piece and clears preview");

            Check(game.TryPlacePiece(game.Spawner.Pieces[1], new Vector2Int(0, 0)), "Second remaining piece places successfully");
            Check(game.Spawner.RemainingCount == 1 && game.Spawner.SetsSpawned == sets, "Second use still does not refill");
            Check(game.TryPlacePiece(game.Spawner.Pieces[2], new Vector2Int(7, 7)), "Third remaining piece places successfully");
            yield return Frames();
            Check(game.Spawner.RemainingCount == 3 && game.Spawner.SetsSpawned == sets + 1, "Consuming all three refills exactly one set of three");

            SetSingles(); yield return Frames();
            for (int x = 0; x < 7; x++) game.Board.SetOccupied(x, 0, sprite);
            Check(game.TryPlacePiece(game.Spawner.Pieces[0], new Vector2Int(7, 0)), "Row completion accepts the final block");
            Check(game.IsBusy && !game.CanInteract, "Line feedback locks placement until clear finishes");
            yield return Settled();
            Check(game.Board.OccupiedCount == 0 && game.Score == 101 && game.Combo == 1, "Full row clears and awards line score / combo");
            for (int y = 0; y < 7; y++) game.Board.SetOccupied(0, y, sprite);
            game.TryPlacePiece(game.Spawner.Pieces[1], new Vector2Int(0, 7)); yield return Settled();
            Check(game.Board.OccupiedCount == 0 && game.Score == 227 && game.Combo == 2, "Full column clears and consecutive clear increments combo / bonus");
            game.TryPlacePiece(game.Spawner.Pieces[2], new Vector2Int(3, 3)); yield return Frames();
            Check(game.Combo == 0, "Placement without a line resets combo");
            Check(PlayerPrefs.GetInt(BlockGameManager.BestScoreKey, 0) >= game.Score && game.BestScore >= game.Score, "Best score updates in PlayerPrefs");

            SetSingles(); yield return Frames();
            for (int x = 0; x < 8; x++) if (x != 3) game.Board.SetOccupied(x, 4, sprite);
            for (int y = 0; y < 8; y++) if (y != 4) game.Board.SetOccupied(3, y, sprite);
            game.TryPlacePiece(game.Spawner.Pieces[0], new Vector2Int(3, 4));
            var full = game.Board.CheckFullLines();
            Check(full.Count == 2 && full.cells.Count == 15, "Simultaneous row + column identifies 15 unique cells");
            yield return Settled();
            Check(game.Board.OccupiedCount == 0 && game.Score == 401, "Simultaneous row + column clears once and awards multi-line bonus");

            SetSingles(); yield return Frames();
            var square = game.Spawner.SpawnPiece(0, Shape("Square2x2"), sprite);
            for (int x = 0; x < 8; x++) if (x != 2 && x != 3) { game.Board.SetOccupied(x, 2, sprite); game.Board.SetOccupied(x, 3, sprite); }
            for (int y = 0; y < 8; y++) if (y != 2 && y != 3) game.Board.SetOccupied(2, y, sprite);
            game.TryPlacePiece(square, new Vector2Int(2, 2));
            full = game.Board.CheckFullLines();
            Check(full.Count == 3 && full.cells.Count == 22, "Two rows + one column collect 22 unique cells");
            yield return Settled();
            Check(game.Board.OccupiedCount == 0 && game.Score == 904, "Three-line clear clears every selected cell and awards larger bonus");

            SetSingles(); yield return Frames();
            var horizontal = game.Spawner.SpawnPiece(0, Shape("Horizontal2"), sprite);
            for (int y = 0; y < 8; y++) if (y != 3)
            { game.Board.SetOccupied(2, y, sprite); game.Board.SetOccupied(3, y, sprite); }
            game.TryPlacePiece(horizontal, new Vector2Int(2, 3));
            full = game.Board.CheckFullLines();
            Check(full.rows.Count == 0 && full.columns.Count == 2 && full.cells.Count == 16, "Two columns are detected together");
            yield return Settled();
            Check(game.Board.OccupiedCount == 0 && game.Score == 402, "Multiple columns clear together with correct bonus");

            SetSingles(); yield return Frames();
            for (int x = 0; x < 8; x++) for (int y = 0; y < 8; y++) if ((x + y) % 2 == 0) game.Board.SetOccupied(x, y, sprite);
            for (int i = 0; i < 3; i++) game.Spawner.SpawnPiece(i, Shape(i == 1 ? "Vertical2" : "Horizontal2"), sprite);
            Check(game.Board.OccupiedCount == 32 && game.CheckGameOver() && game.GameOverPanel.activeSelf, "Game over detects no fitting piece even with 32 empty cells");
            Check(!game.TryPlacePiece(game.Spawner.Pieces[0], Vector2Int.zero), "Game over blocks further placement");
            yield return Frames();
            ScreenCapture.CaptureScreenshot("Captures/BlockCrush/GameOver_1080x1920.png"); yield return Frames(6);
            var restartPoint = ToScreen(game.RestartButton.transform.position);
            var restartHits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = restartPoint }, restartHits);
            Check(restartHits.Count > 0 && restartHits[0].gameObject == game.RestartButton.gameObject, "Restart button receives the topmost UI raycast");
            MouseEvent(restartPoint, false); yield return Frames();
            MouseEvent(restartPoint, true); yield return Frames();
            MouseEvent(restartPoint, false); yield return Frames();
            Check(!game.IsGameOver && !game.IsBusy && !game.GameOverPanel.activeSelf && game.Board.OccupiedCount == 0 &&
                game.Score == 0 && game.Combo == 0 && game.Spawner.RemainingCount == 3, "Actual mouse click on Restart resets board, visuals, score, combo, tray and game state");

            SetSingles(); yield return Frames();
            for (int x = 0; x < 8; x++) for (int y = 0; y < 8; y++) if (x != 7 || y != 7) game.Board.SetOccupied(x, y, sprite);
            game.Spawner.SpawnPiece(0, Shape("Square3x3"), sprite); game.Spawner.SpawnPiece(1, Shape("Horizontal5"), sprite);
            Check(!game.CheckGameOver() && game.Board.CanShapeFitAnywhere(Shape("Single")), "Nearly full board continues when one remaining piece can fit");
            Check(!game.Board.IsValidPlacement(Shape("Horizontal3"), new Vector2Int(7, 0)) &&
                !game.Board.IsValidPlacement(Shape("Single"), new Vector2Int(-1, 0)), "Placement rejects overflow and negative anchors");

            SetSingles(); yield return Frames();
            piece = game.Spawner.Pieces[0]; var screenStart = ToScreen(piece.transform.position);
            InputSystem.QueueStateEvent(testTouch, new TouchState { touchId = 1, phase = UnityEngine.InputSystem.TouchPhase.Began, position = screenStart });
            yield return Frames();
            InputSystem.QueueStateEvent(testTouch, new TouchState { touchId = 1, phase = UnityEngine.InputSystem.TouchPhase.Moved, position = screenStart + new Vector2(30, 30) });
            yield return Frames();
            var destination = Destination(piece, new Vector2Int(5, 5));
            destination.y -= game.Board.CellSize * ((Canvas)game.DragLayer.GetComponentInParent<Canvas>()).scaleFactor * .85f;
            InputSystem.QueueStateEvent(testTouch, new TouchState { touchId = 1, phase = UnityEngine.InputSystem.TouchPhase.Moved, position = destination });
            yield return Frames();
            Check(piece.IsDragging && game.Board.PreviewCount == 1, "Touch state events drive drag and valid preview with finger offset");
            InputSystem.QueueStateEvent(testTouch, new TouchState { touchId = 1, phase = UnityEngine.InputSystem.TouchPhase.Ended, position = destination });
            yield return Frames();
            Check(game.Board.IsOccupied(5, 5) && game.Score == 1, "Touch release places the piece correctly");

            BlockCrushSceneBuilder.SetGameViewSize(1440, 2960); yield return Frames(8);
            game.RestartGame(); yield return Frames();
            Check(ScreenResolution(1440, 2960), "Game View renders exactly 1440x2960"); ValidateResponsive();
            piece = game.Spawner.SpawnPiece(0, Shape("Horizontal5"), sprite); yield return Frames();
            target = Destination(piece, new Vector2Int(3, 7)); yield return BeginMouse(piece, target);
            Check(game.Board.PreviewCount == 5, "Responsive drag maps five-block shape onto the top-right board edge");
            yield return ReleaseMouse(target);
            Check(game.Board.IsOccupied(3, 7) && game.Board.IsOccupied(7, 7) && game.Score == 5, "Responsive drop preserves board coordinates on 1440x2960");
            game.RestartGame(); yield return Frames();
            if (hadBest) PlayerPrefs.SetInt(BlockGameManager.BestScoreKey, savedBest); else PlayerPrefs.DeleteKey(BlockGameManager.BestScoreKey);
            game.RestartGame(); yield return Frames();
            ScreenCapture.CaptureScreenshot("Captures/BlockCrush/Gameplay_1440x2960.png"); yield return Frames(6);
            BlockCrushSceneBuilder.SetGameViewSize(1080, 1920); yield return Frames(8);
            game.RestartGame(); yield return Frames();
            ScreenCapture.CaptureScreenshot("Captures/BlockCrush/Gameplay_1080x1920.png"); yield return Frames(6);
        }

        static bool ScreenResolution(int width, int height) => UnityEngine.Screen.width == width && UnityEngine.Screen.height == height;
        static void ValidateResponsive()
        {
            Canvas.ForceUpdateCanvases();
            var board = game.Board.Rect; var corners = new Vector3[4]; board.GetWorldCorners(corners);
            var bottom = ToScreen(corners[0]); var top = ToScreen(corners[2]);
            Check(Mathf.Abs((top.x - bottom.x) - (top.y - bottom.y)) < 1 && bottom.x > 0 && top.x < UnityEngine.Screen.width,
                "Board is square and contained at " + UnityEngine.Screen.width + "x" + UnityEngine.Screen.height);
            foreach (var slot in game.Spawner.Slots)
            {
                slot.GetWorldCorners(corners); var center = ToScreen(slot.position);
                Check(center.y > 0 && center.y < bottom.y && center.x > 0 && center.x < UnityEngine.Screen.width,
                    "Tray slot remains below board and within screen: " + slot.name);
            }
            var scaler = board.GetComponentInParent<CanvasScaler>();
            Check(scaler.referenceResolution == new Vector2(1080, 1920) && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize,
                "Canvas uses the requested portrait reference resolution and scaling mode");
        }
    }
}
