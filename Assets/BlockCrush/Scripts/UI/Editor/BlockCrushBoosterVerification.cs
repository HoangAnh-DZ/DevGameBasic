using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

namespace BlockCrush.UI.Editor
{
    public static partial class BlockCrushMenuVerification
    {
        public static string StartBoosters()
        {
            Start();
            stack.Clear();
            reportPath = "Captures/Figma/BoosterVerification.md";
            stack.Push(VerifyBoosters());
            return Status;
        }

        static void BoosterWallet(int bombs = 5, int singles = 5, int refreshes = 5)
        {
            var wallet = EconomySave.Load();
            wallet.bombs = bombs;
            wallet.boosters = singles;
            wallet.reverses = refreshes;
            wallet.Save();
        }

        static int Moves(BlockGameManager game) => game.Rules is AdventureLevelController level ? level.MovesUsed : -1;
        static void BoosterPreservesTurn(BlockGameManager game, int score, int best, int combo, int moves, string label)
        {
            Check(game.Score == score && game.BestScore == best && game.Combo == combo && Moves(game) == moves, label + " preserves score, best, combo and Adventure moves");
        }

        static IEnumerator TapBoard(BlockBoard board, Vector2Int cell)
        {
            var point = RectTransformUtility.WorldToScreenPoint(null, board.PlacementCenter(BlockShapeData.Database[0], cell));
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
            yield return Frames(2);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point }.WithButton(MouseButton.Left));
            yield return Frames(2);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
            yield return Frames(8);
        }

        static void VerifyBoosterLayout(BlockGameManager game, Button[] buttons, bool adventure, string label)
        {
            foreach (var button in buttons)
            {
                var bounds = CommerceScreenRect((RectTransform)button.transform);
                var size = bounds.size * (360f / Screen.width);
                Check(size.x >= 48 && size.y >= 48, label + " touch region at least 48 units: " + button.name);
                Check(bounds.xMin >= -1 && bounds.xMax <= Screen.width + 1 && bounds.yMin >= -1 && bounds.yMax <= Screen.height + 1, label + " touch region remains on screen: " + button.name);
            }

            foreach (var piece in game.Spawner.Pieces)
            {
                if (!piece)
                    continue;
                var pieceBounds = CommerceScreenRect((RectTransform)piece.transform);
                var slotBounds = CommerceScreenRect((RectTransform)piece.transform.parent);
                Check(pieceBounds.xMin >= slotBounds.xMin - 1 && pieceBounds.xMax <= slotBounds.xMax + 1 && pieceBounds.yMin >= slotBounds.yMin - 1 && pieceBounds.yMax <= slotBounds.yMax + 1, label + " waiting piece stays inside its slot: " + piece.Shape.id);
                Check(buttons.All(b => !CommerceScreenRect((RectTransform)b.transform).Overlaps(pieceBounds)), label + " waiting piece does not cover any booster touch region: " + piece.Shape.id);
            }

            if (adventure)
                return;
            var guide = CommerceScreenRect(GameObject.Find("DragHint").GetComponent<RectTransform>());
            var board = CommerceScreenRect(game.Board.Rect);
            Check(guide.yMin > board.yMax, label + " drag instruction sits above board without covering cells or boosters");
            var tray = CommerceScreenRect(game.Spawner.Slots[0].parent as RectTransform);
            Check(buttons.All(b => CommerceScreenRect((RectTransform)b.transform).yMax < tray.yMin), label + " Classic tray stays above booster toolbar");
        }

        static IEnumerator VerifyBoosters()
        {
            var touch = InputSystem.AddDevice<Touchscreen>("BoosterVerificationTouch");
            try
            {
                foreach (bool adventure in new[]
                {
                    false,
                    true
                }

                )
                {
                    string mode = adventure ? "Adventure" : "Classic";
                    BoosterWallet();
                    if (adventure)
                    {
                        var catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<AdventureLevelCatalog>("Assets/BlockCrush/Data/AdventureLevels.asset");
                        AdventureProgress.Begin(1, catalog);
                        Find<SceneNavigationController>().AdventureGameplay();
                    }
                    else
                        Find<SceneNavigationController>().Gameplay();
                    yield return Loaded(adventure ? "BlockCrushAdventure" : "BlockCrushGameplay");
                    yield return Resolution(1080, 1920);
                    ValidateScene();
                    var game = Find<BlockGameManager>();
                    var boosters = Find<GameplayBoosterController>();
                    var board = game.Board;
                    var spawner = game.Spawner;
                    var sprite = spawner.BlockSprites[0];
                    string prefix = mode;
                    var bomb = Button(prefix + "BombButton");
                    var refresh = Button(prefix + "ReverseButton");
                    var single = Button(prefix + "ThirdBoosterButton");
                    spawner.SpawnPiece(0, BlockShapeData.Database.First(s => s.Width == 1 && s.Height == 5), sprite);
                    spawner.SpawnPiece(1, BlockShapeData.Database.First(s => s.Width == 5 && s.Height == 1), sprite);
                    spawner.SpawnPiece(2, BlockShapeData.Database.First(s => s.id == "Square3x3"), sprite);
                    yield return Frames(3);
                    var buttons = new[]
                    {
                        bomb,
                        refresh,
                        single
                    };
                    VerifyBoosterLayout(game, buttons, adventure, mode + " 1080x1920");
                    yield return Capture(mode + "_Boosters_Unity_1080x1920");
                    foreach (var button in new[]
                    {
                        bomb,
                        refresh,
                        single
                    }

                    )
                    {
                        Check(button.onClick.GetPersistentEventCount() > 0, mode + " saved booster callback: " + button.name);
                        var size = CommerceScreenRect((RectTransform)button.transform).size * (360f / Screen.width);
                        Check(size.x >= 48 && size.y >= 48, mode + " booster touch region at least 48 units: " + button.name);
                    }

                    yield return Frames(4);
                    Check(GameObject.Find(prefix + "BombButtonAvailability").GetComponent<Text>().text == "5" && GameObject.Find(prefix + "ReverseButtonAvailability").GetComponent<Text>().text == "5" && GameObject.Find(prefix + "ThirdBoosterButtonAvailability").GetComponent<Text>().text == "5", mode + " all three counters read persisted inventory");
                    int loads = EconomySave.LoadCount;
                    yield return Frames(20);
                    Check(EconomySave.LoadCount == loads, mode + " idle booster HUD does not poll wallet JSON");
                    int score = game.Score, best = game.BestScore, combo = game.Combo, moves = Moves(game);
                    string before = PlayerPrefs.GetString(EconomySave.Key);
                    yield return Click(bomb);
                    Check(!boosters.BombArmed && before == PlayerPrefs.GetString(EconomySave.Key), mode + " empty board bomb has no cost");
                    board.SetOccupied(3, 3, sprite);
                    board.SetOccupied(0, 0, sprite);
                    yield return Click(bomb);
                    Check(boosters.BombArmed && game.IsPaused && !game.CanInteract && board.PreviewCount == 0, mode + " bomb targeting blocks drag/placement without consuming inventory");
                    Check(EconomySave.Load().bombs == 5 && !boosters.TryUseBomb(new Vector2Int(-1, 3)), mode + " invalid bomb target costs nothing");
                    yield return TapBoard(board, new Vector2Int(7, 7));
                    Check(boosters.BombArmed && EconomySave.Load().bombs == 5 && board.OccupiedCount == 2, mode + " empty bomb area remains selectable without spending");
                    yield return Click(bomb);
                    Check(!boosters.BombArmed && game.CanInteract && EconomySave.Load().bombs == 5, mode + " cancel restores interaction with no cost");
                    // A stale selection must not spend an item removed by another saved view.
                    yield return Click(bomb);
                    BoosterWallet(0);
                    Check(!boosters.TryUseBomb(new Vector2Int(3, 3)) && !boosters.BombArmed && board.OccupiedCount == 2 && EconomySave.Load().bombs == 0, mode + " stale selected bomb rechecks saved inventory before affecting board");
                    yield return Click(InformationClose());
                    BoosterWallet();
                    for (int row = 2; row <= 4; row++)
                        for (int col = 2; col <= 4; col++)
                            board.SetOccupied(col, row, sprite);
                    yield return Click(bomb);
                    var pointerPoint = RectTransformUtility.WorldToScreenPoint(null, board.PlacementCenter(BlockShapeData.Database[0], new Vector2Int(3, 3)));
                    var firstPointer = new PointerEventData(EventSystem.current)
                    {
                        pointerId = 701,
                        position = pointerPoint,
                        button = PointerEventData.InputButton.Left
                    };
                    var otherPointer = new PointerEventData(EventSystem.current)
                    {
                        pointerId = 702,
                        position = pointerPoint,
                        button = PointerEventData.InputButton.Left
                    };
                    boosters.Target.OnPointerDown(firstPointer);
                    boosters.Target.OnPointerDown(otherPointer);
                    boosters.Target.OnPointerExit(otherPointer);
                    boosters.Target.OnPointerClick(otherPointer);
                    Check(boosters.BombArmed && EconomySave.Load().bombs == 5, mode + " secondary pointer cannot steal or spend the selected bomb");
                    boosters.Target.OnPointerExit(firstPointer);
                    InputSystem.QueueStateEvent(mouse, new MouseState { position = pointerPoint });
                    yield return Frames(3);
                    yield return Capture(mode + "_BombTarget_Unity_1080x1920");
                    yield return TapBoard(board, new Vector2Int(3, 3));
                    Check(!boosters.BombArmed && game.CanInteract && board.OccupiedCount == 1 && board.IsOccupied(0, 0), mode + " real mouse bomb clears exactly 3x3 and preserves other cells");
                    Check(EconomySave.Load().bombs == 4 && !boosters.TryUseBomb(new Vector2Int(3, 3)), mode + " bomb receipt cannot be spent again by repeated callbacks");
                    BoosterPreservesTurn(game, score, best, combo, moves, mode + " bomb");
                    yield return Seconds(.3f);
                    Check(!boosters.Target.gameObject.activeSelf, mode + " explosion presentation ends cleanly");
                    for (int row = 0; row <= 1; row++)
                        for (int col = 0; col <= 1; col++)
                            board.SetOccupied(col, row, sprite);
                    yield return Click(bomb);
                    var touchPoint = RectTransformUtility.WorldToScreenPoint(null, board.PlacementCenter(BlockShapeData.Database[0], Vector2Int.zero));
                    InputSystem.QueueStateEvent(touch, new TouchState { touchId = 91, position = touchPoint, phase = UnityEngine.InputSystem.TouchPhase.Began });
                    yield return Frames(3);
                    InputSystem.QueueStateEvent(touch, new TouchState { touchId = 91, position = touchPoint, phase = UnityEngine.InputSystem.TouchPhase.Ended });
                    yield return Frames(10);
                    Check(!boosters.BombArmed && board.OccupiedCount == 0 && EconomySave.Load().bombs == 3, mode + " real touch bomb clips safely at the board corner");
                    yield return Seconds(.3f);
                    board.SetOccupied(3, 3, sprite);
                    yield return Click(bomb);
                    yield return Click(Button(adventure ? "AdventureSettingsButton" : "Settings"));
                    Check(!boosters.BombArmed && game.IsPaused && EconomySave.Load().bombs == 3, mode + " Settings cancels targeting and retains its pause");
                    before = PlayerPrefs.GetString(EconomySave.Key);
                    Check(!boosters.TryUseSingleBlock() && !boosters.TryRefreshTray() && !boosters.TryUseBomb(new Vector2Int(3, 3)) && before == PlayerPrefs.GetString(EconomySave.Key), mode + " modal pause rejects every booster without spending");
                    yield return Click(InSettings("CloseButton"));
                    Check(game.CanInteract, mode + " Settings close resumes normal interaction");
                    board.ResetBoard();
                    var large = BlockShapeData.Database.First(s => s.id == "Square3x3");
                    var line = BlockShapeData.Database.First(s => s.id == "Horizontal3");
                    var oldLargest = spawner.SpawnPiece(0, large, sprite);
                    spawner.SpawnPiece(1, line, sprite);
                    spawner.SpawnPiece(2, BlockShapeData.Database[0], sprite);
                    yield return Frames(3);
                    int sets = spawner.SetsSpawned;
                    yield return Click(single);
                    Check(spawner.Pieces[0].Shape.id == "Single" && spawner.Pieces[1].Shape.id == "Horizontal3" && spawner.RemainingCount == 3 && spawner.SetsSpawned == sets && board.OccupiedCount == 0, mode + " +1 converts only the largest waiting block and preserves tray/refill state");
                    Check(EconomySave.Load().boosters == 4 && !game.TryPlacePiece(oldLargest, new Vector2Int(0, 0)), mode + " +1 spends one item and rejects stale replaced-piece callbacks");
                    BoosterPreservesTurn(game, score, best, combo, moves, mode + " +1");
                    Check(boosters.TryUseSingleBlock() && !boosters.TryUseSingleBlock() && EconomySave.Load().boosters == 3, mode + " duplicate +1 invocation in the same frame spends once");
                    yield return Frames(3);
                    before = PlayerPrefs.GetString(EconomySave.Key);
                    Check(!boosters.TryUseSingleBlock() && before == PlayerPrefs.GetString(EconomySave.Key), mode + " already-single tray has no +1 cost");
                    var singlePiece = spawner.Pieces[0];
                    Check(game.TryPlacePiece(singlePiece, new Vector2Int(3, 3)), mode + " converted single remains normally playable");
                    yield return Frames(3);
                    Check(game.Score == score + 1 && (moves < 0 || Moves(game) == moves + 1), mode + " placing converted single uses the unchanged scoring/move rule");
                    var first = spawner.SpawnPiece(1, line, sprite);
                    var second = spawner.SpawnPiece(2, large, sprite);
                    var oldIds = new[]
                    {
                        first.Shape.id,
                        second.Shape.id
                    };
                    int occupied = board.OccupiedCount;
                    score = game.Score;
                    combo = game.Combo;
                    best = game.BestScore;
                    moves = Moves(game);
                    yield return Click(refresh);
                    Check(!spawner.Pieces[0] && spawner.RemainingCount == 2 && spawner.SetsSpawned == sets && spawner.Pieces.Where(p => p).All(p => board.CanShapeFitAnywhere(p.Shape)) && (spawner.Pieces[1].Shape.id != oldIds[0] || spawner.Pieces[2].Shape.id != oldIds[1]), mode + " Refresh replaces only unused slots with different fitting shapes");
                    Check(EconomySave.Load().reverses == 4 && board.OccupiedCount == occupied && !game.TryPlacePiece(first, new Vector2Int(0, 0)) && !game.TryPlacePiece(second, new Vector2Int(0, 0)), mode + " Refresh has one durable cost and invalidates old tray callbacks");
                    BoosterPreservesTurn(game, score, best, combo, moves, mode + " Refresh");
                    yield return Frames(2);
                    Check(boosters.TryRefreshTray() && !boosters.TryRefreshTray() && EconomySave.Load().reverses == 3, mode + " duplicate Refresh callbacks in one frame spend once");
                    board.ResetBoard();
                    for (int row = 0; row < BlockBoard.Size; row++)
                        for (int col = 0; col < BlockBoard.Size; col++)
                            if (col != 7 || row != 7)
                                board.SetOccupied(col, row, sprite);
                    spawner.SpawnPiece(1, BlockShapeData.Database[0], sprite);
                    spawner.SpawnPiece(2, BlockShapeData.Database[0], sprite);
                    yield return Frames(3);
                    before = PlayerPrefs.GetString(EconomySave.Key);
                    var rng = JsonUtility.ToJson(UnityEngine.Random.state);
                    Check(!boosters.TryRefreshTray() && before == PlayerPrefs.GetString(EconomySave.Key) && rng == JsonUtility.ToJson(UnityEngine.Random.state), mode + " impossible-to-change Refresh preserves inventory and future random state");
                    board.SetOccupied(7, 7, sprite);
                    Check(!boosters.TryRefreshTray() && before == PlayerPrefs.GetString(EconomySave.Key), mode + " no-fit Refresh has no cost");
                    game.RestartGame();
                    yield return Frames(4);
                    board.SetOccupied(3, 3, sprite);
                    yield return Click(bomb);
                    game.RestartGame();
                    yield return Frames(4);
                    Check(!boosters.BombArmed && !boosters.Target.gameObject.activeSelf && game.CanInteract && EconomySave.Load().bombs == 3, mode + " restart cancels targeting without resetting saved inventory");
                    for (int i = 0; i < 7; i++)
                        board.SetOccupied(i, 7, sprite);
                    var clearPiece = spawner.SpawnPiece(0, BlockShapeData.Database[0], sprite);
                    Check(game.TryPlacePiece(clearPiece, new Vector2Int(7, 7)) && game.IsBusy, mode + " row animation fixture starts");
                    before = PlayerPrefs.GetString(EconomySave.Key);
                    boosters.ToggleBomb();
                    Check(!boosters.BombArmed && !boosters.TryUseSingleBlock() && !boosters.TryRefreshTray() && before == PlayerPrefs.GetString(EconomySave.Key), mode + " line animation rejects every booster without spending");
                    while (game.IsBusy)
                        yield return null;
                    game.RestartGame();
                    var dragged = spawner.Pieces[0];
                    var dragData = new PointerEventData(EventSystem.current)
                    {
                        pointerId = 703,
                        position = RectTransformUtility.WorldToScreenPoint(null, dragged.transform.position)
                    };
                    dragged.OnBeginDrag(dragData);
                    before = PlayerPrefs.GetString(EconomySave.Key);
                    boosters.ToggleBomb();
                    Check(dragged.IsDragging && !boosters.BombArmed && !boosters.TryUseSingleBlock() && !boosters.TryRefreshTray() && before == PlayerPrefs.GetString(EconomySave.Key), mode + " active drag rejects all boosters without losing a piece or spending");
                    dragged.CancelDrag();
                    yield return Frames(2);
                    BoosterWallet(0, 0, 0);
                    yield return Frames(3);
                    before = PlayerPrefs.GetString(EconomySave.Key);
                    foreach (var button in new[]
                    {
                        bomb,
                        refresh,
                        single
                    }

                    )
                    {
                        yield return Click(button);
                        Check(Find<MoreSettingsController>().InformationOpen && before == PlayerPrefs.GetString(EconomySave.Key), mode + " empty inventory explains how to collect without granting or spending: " + button.name);
                        yield return Click(InformationClose());
                    }

                    Check(!EconomySave.TryConsumeBooster(WheelRewardKind.Coins) && !EconomySave.TryConsumeBooster((WheelRewardKind)99) && before == PlayerPrefs.GetString(EconomySave.Key), mode + " invalid consumption kinds do not change persistence");
                    BoosterWallet(2, 2, 2);
                    yield return Frames(3);
                    Check(GameObject.Find(prefix + "ThirdBoosterButtonAvailability").GetComponent<Text>().text == "2", mode + " external saved inventory refreshes HUD next frame");
                    yield return Resolution(1440, 2960);
                    VerifyBoosterLayout(game, buttons, adventure, mode + " 1440x2960");
                    var safe = (adventure ? game.Spawner.Slots[0].parent.parent.parent : game.Spawner.Slots[0].parent.parent) as RectTransform;
                    var minimum = safe.anchorMin;
                    var maximum = safe.anchorMax;
                    try
                    {
                        safe.anchorMin = new Vector2(0, 120f / Screen.height);
                        safe.anchorMax = new Vector2(1, (Screen.height - 180f) / Screen.height);
                        Canvas.ForceUpdateCanvases();
                        if (adventure)
                            safe.GetComponentInChildren<FigmaLayoutFitter>().Apply();
                        else
                            Find<GameplayBoosterLayout>().SendMessage("LateUpdate");
                        Canvas.ForceUpdateCanvases();
                        VerifyBoosterLayout(game, buttons, adventure, mode + " simulated notches/gesture area");
                        var resizedPoint = RectTransformUtility.WorldToScreenPoint(null, board.PlacementCenter(BlockShapeData.Database[0], new Vector2Int(6, 5)));
                        Check(boosters.Target.TryGetCell(resizedPoint, null, out var resizedCell) && resizedCell == new Vector2Int(6, 5), mode + " bomb coordinates follow resized safe-area board");
                    }
                    finally
                    {
                        safe.anchorMin = minimum;
                        safe.anchorMax = maximum;
                        Canvas.ForceUpdateCanvases();
                        if (adventure)
                            safe.GetComponentInChildren<FigmaLayoutFitter>().Apply();
                    }

                    foreach (var button in new[]
                    {
                        bomb,
                        refresh,
                        single
                    }

                    )
                    {
                        var size = CommerceScreenRect((RectTransform)button.transform).size * (360f / Screen.width);
                        Check(size.x >= 48 && size.y >= 48, mode + " tall-screen booster touch region: " + button.name);
                    }

                    yield return Capture(mode + "_Boosters_Unity_1440x2960");
                    if (adventure)
                        ((AdventureLevelController)game.Rules).EndTurn(((AdventureLevelController)game.Rules).Target, false);
                    else
                        game.ShowGameOver();
                    before = PlayerPrefs.GetString(EconomySave.Key);
                    boosters.ToggleBomb();
                    Check(!boosters.BombArmed && !boosters.TryUseSingleBlock() && !boosters.TryRefreshTray() && before == PlayerPrefs.GetString(EconomySave.Key), mode + " terminal game rejects all boosters without spending");
                    Find<SceneNavigationController>().CompleteHome();
                    yield return Loaded("Home");
                    Check(EconomySave.Load().bombs == 2 && EconomySave.Load().boosters == 2 && EconomySave.Load().reverses == 2, mode + " inventory persists through scene reload");
                }
            }
            finally
            {
                if (touch.added)
                    InputSystem.RemoveDevice(touch);
            }

            yield return Resolution(1080, 1920);
        }
    }
}
