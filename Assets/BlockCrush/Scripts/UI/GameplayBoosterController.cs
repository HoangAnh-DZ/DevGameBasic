using UnityEngine;
using UnityEngine.UI;

namespace BlockCrush.UI
{
    [DefaultExecutionOrder(150)]
    public sealed class GameplayBoosterController : MonoBehaviour
    {
        [SerializeField]
        BlockGameManager game;
        [SerializeField]
        SettingsPanelController settings;
        [SerializeField]
        MoreSettingsController information;
        [SerializeField]
        SceneNavigationController navigation;
        [SerializeField]
        GameAudioService audioService;
        [SerializeField]
        Button bombButton, refreshButton, singleBlockButton;
        [SerializeField]
        Text bombCount, refreshCount, singleBlockCount, notice;
        [SerializeField]
        BombTargetInput target;
        EconomySave wallet;
        int displayedRevision = -1;
        int lastUseFrame = -1, lastToggleFrame = -1;
        bool applying;
        float noticeUntil;
        public bool BombArmed { get; private set; }
        public BlockBoard Board => game.Board;
        public BombTargetInput Target => target;
        public Text Notice => notice;

        AdventureLevelController Level => game.Rules as AdventureLevelController;

        bool Configured => game && settings && information && navigation && audioService && bombButton && refreshButton && singleBlockButton && bombCount && refreshCount && singleBlockCount && notice && target;

        bool Terminal => game.IsGameOver || (Level && (Level.Completed || Level.Lost));

        bool ModalOpen => settings.IsOpen || information.IsOpen || information.InformationOpen || navigation.IsTransitioning;

        bool OtherPauseOwner => settings.IsOpen || information.IsOpen || navigation.IsTransitioning || Terminal;

        bool Dragging
        {
            get
            {
                foreach (var piece in game.Spawner.Pieces)
                    if (piece && piece.IsDragging)
                        return true;
                return false;
            }
        }

        bool Ready => Configured && !applying && !BombArmed && !target.IsFlashing && game.CanInteract && !Terminal && !ModalOpen && !Dragging;

        bool SelectionReady => Configured && BombArmed && game.IsPaused && !applying && !game.IsBusy && !Terminal && !ModalOpen && !Dragging;

        public void Configure(BlockGameManager manager, SettingsPanelController settingsPanel, MoreSettingsController details, SceneNavigationController nav, GameAudioService audio, Button bomb, Button refresh, Button singleBlock, Text bombs, Text refreshes, Text singleBlocks, Text status, BombTargetInput input)
        {
            game = manager;
            settings = settingsPanel;
            information = details;
            navigation = nav;
            audioService = audio;
            bombButton = bomb;
            refreshButton = refresh;
            singleBlockButton = singleBlock;
            bombCount = bombs;
            refreshCount = refreshes;
            singleBlockCount = singleBlocks;
            notice = status;
            target = input;
            if (Application.isPlaying && isActiveAndEnabled)
                Subscribe();
        }

        void OnEnable()
        {
            if (Configured)
            {
                Subscribe();
                RefreshInventory();
            }
        }

        void Subscribe()
        {
            game.SessionRestarted -= ResetSession;
            game.SessionRestarted += ResetSession;
        }

        void Update()
        {
            if (!Configured)
                return;
            if (BombArmed && (!game.IsPaused || game.IsBusy || Terminal || ModalOpen))
                CancelBomb();
            if (wallet == null || displayedRevision != EconomySave.Revision)
                RefreshInventory();
            bool ready = Ready;
            bombButton.interactable = ready || SelectionReady;
            refreshButton.interactable = singleBlockButton.interactable = ready;
            if (!BombArmed && notice.text.Length > 0 && Time.unscaledTime >= noticeUntil)
            {
                notice.text = "";
                notice.transform.parent.gameObject.SetActive(false);
            }
        }

        public void RefreshInventory()
        {
            wallet = EconomySave.Load();
            displayedRevision = EconomySave.Revision;
            bombCount.text = Quantity(wallet.bombs);
            refreshCount.text = Quantity(wallet.reverses);
            singleBlockCount.text = Quantity(wallet.boosters);
        }

        static string Quantity(int count) => count > 999 ? "999+" : count.ToString();
        bool Has(WheelRewardKind kind)
        {
            RefreshInventory();
            int count = kind == WheelRewardKind.Bomb ? wallet.bombs : kind == WheelRewardKind.Reverse ? wallet.reverses : wallet.boosters;
            if (count > 0)
                return true;
            Missing(kind);
            return false;
        }

        void Missing(WheelRewardKind kind)
        {
            information.ShowInformation(kind == WheelRewardKind.Bomb ? "Bomb" : kind == WheelRewardKind.Reverse ? "Refresh" : "+1 Single Block", "No items available. Collect boosters from the Lucky Wheel or Shop on Home.");
        }

        void Status(string text)
        {
            notice.text = text;
            notice.transform.parent.gameObject.SetActive(true);
            noticeUntil = Time.unscaledTime + 3;
        }

        public void ToggleBomb()
        {
            if (!Configured || lastToggleFrame == Time.frameCount)
                return;
            lastToggleFrame = Time.frameCount;
            if (BombArmed)
            {
                CancelBomb();
                audioService.Feedback();
                return;
            }

            if (!Ready || lastUseFrame == Time.frameCount || !Has(WheelRewardKind.Bomb))
                return;
            if (Board.OccupiedCount == 0)
            {
                Status("Bomb: place some blocks first.");
                return;
            }

            BombArmed = true;
            game.PauseGame(); // Existing pause cancels drag and blocks placement during targeting.
            target.ShowSelection();
            Status("BOMB: tap an area to clear • tap Bomb again to cancel");
            audioService.Feedback();
        }

        public void CancelBomb()
        {
            if (!BombArmed)
                return;
            BombArmed = false;
            target.Hide();
            notice.text = "";
            notice.transform.parent.gameObject.SetActive(false);
            // Closing selection must never resume a Settings pause or a terminal level.
            if (game.IsPaused && !OtherPauseOwner)
                game.ResumeGame();
        }

        public bool TryUseBomb(Vector2Int center)
        {
            if (!SelectionReady || lastUseFrame == Time.frameCount || center.x < 0 || center.y < 0 || center.x >= BlockBoard.Size || center.y >= BlockBoard.Size)
                return false;
            int minX = Mathf.Max(0, center.x - 1), maxX = Mathf.Min(BlockBoard.Size - 1, center.x + 1);
            int minY = Mathf.Max(0, center.y - 1), maxY = Mathf.Min(BlockBoard.Size - 1, center.y + 1);
            int cleared = 0;
            for (int row = minY; row <= maxY; row++)
                for (int col = minX; col <= maxX; col++)
                    if (Board.IsOccupied(col, row))
                        cleared++;
            if (cleared == 0)
            {
                Status("Choose an area containing blocks • tap Bomb to cancel");
                return false;
            }

            applying = true;
            try
            {
                if (!EconomySave.TryConsumeBooster(WheelRewardKind.Bomb))
                {
                    CancelBomb();
                    Missing(WheelRewardKind.Bomb);
                    return false;
                }

                for (int row = minY; row <= maxY; row++)
                    for (int col = minX; col <= maxX; col++)
                        if (Board.IsOccupied(col, row))
                            Board.SetOccupied(col, row, null);
                lastUseFrame = Time.frameCount;
                CancelBomb();
                target.PlayBlast(center);
                Status("Cleared " + cleared + " blocks");
                audioService.Play(GameSfx.MultipleLineClear);
                RefreshInventory();
                return true;
            }
            finally
            {
                applying = false;
            }
        }

        public void UseSingleBlock() => TryUseSingleBlock();
        public bool TryUseSingleBlock()
        {
            if (!Ready || lastUseFrame == Time.frameCount || !Has(WheelRewardKind.Booster))
                return false;
            BlockPiece largest = null;
            foreach (var piece in game.Spawner.Pieces)
                if (piece && piece.Shape.cells.Length > 1 && (!largest || piece.Shape.cells.Length > largest.Shape.cells.Length))
                    largest = piece;
            var single = BlockShapeData.Database[0];
            if (!largest || !Board.CanShapeFitAnywhere(single))
            {
                Status("+1: no waiting block can be reduced.");
                return false;
            }

            applying = true;
            try
            {
                if (!EconomySave.TryConsumeBooster(WheelRewardKind.Booster))
                    return false;
                game.Spawner.SpawnPiece(largest.SlotIndex, single, largest.BlockSprite);
                lastUseFrame = Time.frameCount;
                Status("+1: largest block becomes 1×1");
                audioService.Feedback();
                RefreshInventory();
                return true;
            }
            finally
            {
                applying = false;
            }
        }

        public void RefreshTray() => TryRefreshTray();
        public bool TryRefreshTray()
        {
            if (!Ready || lastUseFrame == Time.frameCount || !Has(WheelRewardKind.Reverse))
                return false;
            var random = Random.state;
            if (!BoosterTrayRefresh.TryPlan(game.Spawner, Board, Level ? Level.BoosterSettings : null, total => Random.Range(0, total), out var plan))
            {
                Random.state = random;
                Status("Refresh: no different fitting blocks available.");
                return false;
            }

            applying = true;
            try
            {
                if (!EconomySave.TryConsumeBooster(WheelRewardKind.Reverse))
                {
                    Random.state = random;
                    return false;
                }

                Board.ClearPreview();
                foreach (var replacement in plan)
                    game.Spawner.SpawnPiece(replacement.Slot, replacement.Shape, replacement.Sprite);
                lastUseFrame = Time.frameCount;
                Status("Remaining blocks refreshed");
                audioService.Feedback();
                RefreshInventory();
                return true;
            }
            finally
            {
                applying = false;
            }
        }

        public void ResetSession()
        {
            if (!Configured)
                return;
            BombArmed = false;
            target.Hide();
            notice.text = "";
            notice.transform.parent.gameObject.SetActive(false);
            lastUseFrame = lastToggleFrame = -1;
        }

        void OnApplicationFocus(bool focused)
        {
            if (!focused && Configured)
                CancelBomb();
        }

        void OnApplicationPause(bool paused)
        {
            if (paused && Configured)
                CancelBomb();
        }

        void OnDisable()
        {
            if (Configured)
            {
                game.SessionRestarted -= ResetSession;
                CancelBomb();
                target.Hide();
            }
        }
    }
}
