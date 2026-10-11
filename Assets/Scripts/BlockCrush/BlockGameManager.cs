using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace BlockCrush
{
    public sealed class BlockGameManager : MonoBehaviour
    {
        public const string BestScoreKey = "BlockCrush.BestScore";
        [SerializeField]
        BlockBoard board;
        [SerializeField]
        BlockPieceSpawner spawner;
        [SerializeField]
        RectTransform dragLayer;
        [SerializeField]
        Text scoreText, bestText, comboText, finalScoreText, finalBestText;
        [SerializeField]
        GameObject gameOverPanel;
        [SerializeField]
        Button restartButton;
        [SerializeField]
        float clearDuration = .16f;
        public BlockBoard Board => board;
        public BlockPieceSpawner Spawner => spawner;
        public RectTransform DragLayer => dragLayer;
        public int Score { get; private set; }
        public int BestScore { get; private set; }
        public int Combo { get; private set; }
        public bool IsGameOver { get; private set; }
        public bool IsBusy { get; private set; }
        public bool IsPaused { get; private set; }
        public bool CanInteract => !IsGameOver && !IsBusy && !IsPaused;
        public Text ScoreText => scoreText;
        public GameObject GameOverPanel => gameOverPanel;
        public Button RestartButton => restartButton;
        public IBlockGameRules Rules { get; set; }
        public IBlockGameFeedback Feedback { get; set; }

        public event System.Action SessionRestarted;
        string ActiveBestKey => Rules == null ? BestScoreKey : Rules.BestScoreKey;

        public void Configure(BlockBoard b, BlockPieceSpawner s, RectTransform drag, Text score, Text best, Text combo, GameObject panel, Text finalScore, Text finalBest, Button restart)
        {
            board = b;
            spawner = s;
            dragLayer = drag;
            scoreText = score;
            bestText = best;
            comboText = combo;
            gameOverPanel = panel;
            finalScoreText = finalScore;
            finalBestText = finalBest;
            restartButton = restart;
        }

        void Start()
        {
            BestScore = PlayerPrefs.GetInt(ActiveBestKey, 0);
            restartButton.onClick.AddListener(RestartGame);
            RestartGame();
        }

        public void RestartGame()
        {
            IsPaused = false;
            StopAllCoroutines();
            spawner.ClearPieces();
            board.ResetBoard();
            BestScore = PlayerPrefs.GetInt(ActiveBestKey, 0);
            Score = 0;
            Combo = 0;
            IsBusy = false;
            IsGameOver = false;
            Feedback?.Restarted();
            Rules?.Restarted();
            gameOverPanel.SetActive(false);
            UpdateUI();
            Canvas.ForceUpdateCanvases();
            spawner.SpawnSet();
            SessionRestarted?.Invoke();
            CheckGameOver();
        }

        public void PauseGame()
        {
            IsPaused = true;
            foreach (var piece in spawner.Pieces)
                if (piece)
                    piece.CancelDrag();
            board.ClearPreview();
        }

        public void ResumeGame() => IsPaused = false;
        public bool TryPlacePiece(BlockPiece piece, Vector2Int anchor)
        {
            if (!CanInteract || !spawner.Contains(piece) || !board.PlacePiece(piece.Shape, anchor, piece.BlockSprite))
                return false;
            int cellsPlaced = piece.Shape.cells.Length;
            spawner.MarkUsed(piece);
            var lines = board.CheckFullLines();
            if (Rules != null)
            {
                int combo;
                Score += Rules.PlacementScore(cellsPlaced, lines.Count, Combo, out combo);
                Combo = combo;
            }
            else if (lines.Count > 0)
            {
                Score += cellsPlaced;
                Combo++;
                Score += 100 * lines.Count * lines.Count + 25 * (Combo - 1) * lines.Count;
            }
            else
            {
                Score += cellsPlaced;
                Combo = 0;
            }

            if (Score > BestScore)
            {
                BestScore = Score;
                PlayerPrefs.SetInt(ActiveBestKey, BestScore);
            }

            UpdateUI();
            Feedback?.Placed(lines.Count, Combo);
            if (lines.Count > 0)
            {
                IsBusy = true;
                StartCoroutine(AnimateClear(lines));
            }
            else
                FinishTurn();
            return true;
        }

        IEnumerator AnimateClear(BlockBoard.FullLines lines)
        {
            float elapsed = 0;
            while (elapsed < clearDuration)
            {
                if (IsPaused)
                {
                    yield return null;
                    continue;
                }

                elapsed += Time.unscaledDeltaTime;
                foreach (var p in lines.cells)
                    board.CellAt(p.x, p.y).ClearFeedback(Mathf.Clamp01(elapsed / clearDuration));
                yield return null;
            }

            board.ClearLines(lines);
            IsBusy = false;
            FinishTurn();
        }

        void FinishTurn()
        {
            // Win and move exhaustion are resolved before refill/no-fit, including the final move.
            if (Rules != null && Rules.EndTurn(Score, false))
                return;
            if (spawner.RemainingCount == 0)
                spawner.SpawnSet();
            CheckGameOver();
        }

        public bool CheckGameOver()
        {
            if (Rules != null && !IsBusy && Rules.EndTurn(Score, false))
                return true;
            if (IsBusy || spawner.RemainingCount == 0)
                return false;
            foreach (var piece in spawner.Pieces)
                if (piece && board.CanShapeFitAnywhere(piece.Shape))
                    return false;
            if (Rules != null)
                return Rules.EndTurn(Score, true);
            ShowGameOver();
            return true;
        }

        public void ShowGameOver()
        {
            bool first = !IsGameOver;
            IsGameOver = true;
            board.ClearPreview();
            foreach (var piece in spawner.Pieces)
                if (piece)
                    piece.CancelDrag();
            finalScoreText.text = Score.ToString();
            finalBestText.text = "Best  " + BestScore;
            gameOverPanel.SetActive(true);
            gameOverPanel.transform.SetAsLastSibling();
            if (first)
                Feedback?.Failed();
            PlayerPrefs.Save();
        }

        void UpdateUI()
        {
            scoreText.text = Score.ToString();
            bestText.text = "BEST  " + BestScore;
            comboText.text = Combo > 0 ? "COMBO ×" + Combo : "";
        }

        void OnApplicationPause(bool paused)
        {
            if (paused)
                PlayerPrefs.Save();
        }

        void OnDisable()
        {
            PlayerPrefs.Save();
        }
    }
}
