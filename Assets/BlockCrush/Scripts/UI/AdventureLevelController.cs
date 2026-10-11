using UnityEngine;
using UnityEngine.UI;

namespace BlockCrush.UI
{
    [DefaultExecutionOrder(100)]
    public sealed class AdventureLevelController : MonoBehaviour, IBlockGameRules
    {
        [SerializeField]
        BlockGameManager game;
        [SerializeField]
        AdventureLevelCatalog catalog;
        [SerializeField]
        SceneNavigationController navigation;
        [SerializeField]
        MoreSettingsController information;
        [SerializeField]
        Text levelLabel, progressLabel, bestLabel, targetLabel;
        [SerializeField]
        Image progressFill;
        [SerializeField]
        GameObject tutorial, completion;
        [SerializeField]
        Text completionTitle, completionScore, nextLabel;
        [SerializeField]
        Text movesLabel;
        [SerializeField]
        Button nextButton;
        public int Level { get; private set; }
        public int Target => catalog.Target(Level);
        public bool Completed { get; private set; }
        public bool Lost { get; private set; }
        public int MovesUsed { get; private set; }
        public int MovesRemaining => Mathf.Max(0, catalog.MaxMoves(Level) - MovesUsed);
        public AdventureLevelCatalog.Level BoosterSettings => catalog.Get(Level);
        public string FailureReason { get; private set; }
        public string BestScoreKey => AdventureProgress.BestKey(Level);
        public bool TutorialVisible => tutorial.activeSelf;

        float fillWidth;
        bool dailyRoundRecorded;
        bool hudInitialized;
        int displayedLevel, displayedScore, displayedBest, displayedTarget, displayedMoves;
#if UNITY_EDITOR
        public int HudUpdates { get; private set; }

#endif
        public void Configure(BlockGameManager manager, AdventureLevelCatalog levels, SceneNavigationController nav, MoreSettingsController details, Text level, Text progress, Text best, Text target, Image fill, GameObject hand, GameObject win, Text title, Text score, Button next, Text nextText, Text moves)
        {
            game = manager;
            catalog = levels;
            navigation = nav;
            information = details;
            levelLabel = level;
            progressLabel = progress;
            bestLabel = best;
            targetLabel = target;
            progressFill = fill;
            tutorial = hand;
            completion = win;
            completionTitle = title;
            completionScore = score;
            nextButton = next;
            nextLabel = nextText;
            movesLabel = moves;
        }

        void Awake()
        {
            Level = Mathf.Clamp(AdventureProgress.SelectedLevel, 1, AdventureProgress.Unlocked(catalog));
            fillWidth = progressFill.rectTransform.sizeDelta.x;
            game.Rules = this;
        }

        public void Restarted()
        {
            Completed = Lost = false;
            MovesUsed = 0;
            FailureReason = "";
            completion.SetActive(false);
            dailyRoundRecorded = false;
            tutorial.SetActive(Level == 1 && PlayerPrefs.GetInt(AdventureProgress.TutorialKey, 0) == 0);
            Refresh();
        }

        void LateUpdate()
        {
            if (Completed || Lost)
            {
                if (!game.IsPaused)
                    game.PauseGame();
                return;
            }

            Refresh();
            if (game.Score > 0 && tutorial.activeSelf)
            {
                tutorial.SetActive(false);
                PlayerPrefs.SetInt(AdventureProgress.TutorialKey, 1);
                PlayerPrefs.Save();
            }
        }

        public int PlacementScore(int cells, int lines, int previousCombo, out int combo)
        {
            MovesUsed++;
            if (tutorial.activeSelf)
            {
                tutorial.SetActive(false);
                PlayerPrefs.SetInt(AdventureProgress.TutorialKey, 1);
                PlayerPrefs.Save();
            }

            return AdventureScoreSystem.Calculate(cells, lines, previousCombo, out combo);
        }

        public bool EndTurn(int score, bool noFit)
        {
            if (Completed || Lost)
                return true;
            Refresh();
            if (score >= Target)
            {
                RecordDailyRound();
                FinishLevel();
                return true;
            }

            if (MovesRemaining == 0 || noFit)
            {
                RecordDailyRound();
                Lost = true;
                FailureReason = noFit ? "No space" : "Out of moves";
                tutorial.SetActive(false);
                game.PauseGame();
                game.ShowGameOver();
                return true;
            }

            return false;
        }

        void RecordDailyRound()
        {
            // Count a finished win/loss with an accepted move, never scene visits, invalid drags or quits.
            if (dailyRoundRecorded || MovesUsed == 0)
                return;
            dailyRoundRecorded = true;
            EconomySave.RecordAdventureRound();
        }

        public BlockShapeData SelectShape(System.Collections.Generic.HashSet<string> chosen, BlockBoard board) => AdventurePieceGenerator.Pick(catalog.Get(Level), chosen, board.CanShapeFitAnywhere, count => Random.Range(0, count));
        void Refresh()
        {
            int score = game.Score, best = game.BestScore, target = Target, moves = MovesRemaining;
            if (hudInitialized && displayedLevel == Level && displayedScore == score && displayedBest == best && displayedTarget == target && displayedMoves == moves)
                return;
            levelLabel.text = "Level " + Level.ToString("00");
            progressLabel.text = Mathf.Min(score, target).ToString();
            bestLabel.text = best.ToString();
            targetLabel.text = target.ToString();
            movesLabel.text = "Moves " + moves;
            var size = progressFill.rectTransform.sizeDelta;
            size.x = fillWidth * Mathf.Clamp01((float)score / target);
            progressFill.rectTransform.sizeDelta = size;
            displayedLevel = Level;
            displayedScore = score;
            displayedBest = best;
            displayedTarget = target;
            displayedMoves = moves;
            hudInitialized = true;
#if UNITY_EDITOR
            HudUpdates++;
#endif
        }

        void FinishLevel()
        {
            Completed = true;
            tutorial.SetActive(false);
            game.GameOverPanel.SetActive(false);
            game.PauseGame();
            game.Feedback?.Completed();
            AdventureProgress.Complete(Level, catalog);
            completionTitle.text = "Level " + Level + " Complete";
            completionScore.text = game.Score.ToString();
            nextLabel.text = Level < catalog.Count ? "▶ Level " + (Level + 1) : "Levels";
            completion.SetActive(true);
            completion.transform.SetAsLastSibling();
            navigation.transform.SetAsLastSibling();
        }

        public void NextLevel()
        {
            if (!Completed)
                return;
            if (Level == catalog.Count)
            {
                navigation.Home();
                return;
            }

            if (AdventureProgress.Begin(Level + 1, catalog))
                navigation.AdventureGameplay();
        }

        // Compatibility for scenes created before the dedicated booster builder.
        public void Booster() => GetComponent<GameplayBoosterController>()?.ToggleBomb();
        void OnDestroy()
        {
            if (game)
                game.Rules = null;
        }
    }
}
