using UnityEngine;
using UnityEngine.UI;

namespace BlockCrush.UI
{
    public sealed class BlockGameFeedback : MonoBehaviour, IBlockGameFeedback
    {
        [SerializeField]
        BlockGameManager game;
        [SerializeField]
        GameAudioService audioService;
        [SerializeField]
        RectTransform root;
        [SerializeField]
        CanvasGroup group;
        [SerializeField]
        Text label;
        int pendingCombo;
        float delay, age;
        bool visible, terminal, paused;
        public bool Visible => visible;
        public Text Label => label;
        public RectTransform Root => root;

        public void Configure(BlockGameManager manager, GameAudioService audio, RectTransform rect, CanvasGroup canvasGroup, Text text)
        {
            game = manager;
            audioService = audio;
            root = rect;
            group = canvasGroup;
            label = text;
        }

        void Awake()
        {
            game.Feedback = this;
            Hide();
        }

        public void Restarted()
        {
            terminal = false;
            paused = false;
            Hide();
            audioService.ResetGameplay();
        }

        void Hide()
        {
            visible = false;
            pendingCombo = 0;
            age = 0;
            group.alpha = 0;
            label.text = "";
        }

        public void Placed(int lines, int combo)
        {
            if (terminal)
                return;
            audioService.Play(GameSfx.BlockPlace);
            if (lines == 0)
            {
                Hide();
                return;
            }

            audioService.Play(lines == 1 ? GameSfx.LineClear : GameSfx.MultipleLineClear);
            // Actual combo state is calculated once by the manager, never by this presenter.
            if (combo >= 2)
            {
                Hide();
                pendingCombo = combo;
                delay = .055f;
            }
        }

        public void InvalidPlacement()
        {
            if (!terminal)
                audioService.Play(GameSfx.InvalidPlacement);
        }

        public void Completed()
        {
            if (terminal)
                return;
            terminal = true;
            Hide();
            audioService.Play(GameSfx.LevelComplete);
        }

        public void Failed()
        {
            if (terminal)
                return;
            terminal = true;
            Hide();
            audioService.Play(GameSfx.GameOver);
        }

        void Update()
        {
            if (paused != game.IsPaused)
            {
                paused = game.IsPaused;
                audioService.PauseGameplay(paused);
            }

            if (terminal || paused)
                return;
            float dt = Time.unscaledDeltaTime;
            if (pendingCombo > 0)
            {
                delay -= dt;
                if (delay <= 0)
                {
                    int combo = pendingCombo;
                    pendingCombo = 0;
                    age = 0;
                    visible = true;
                    // Figma 4:70 / 4:69: Lilita One, green +2 / gold +4, shadow (0,10), 40% black.
                    label.text = "COMBO+" + combo;
                    label.fontSize = combo < 4 ? 80 : 88;
                    label.color = combo < 4 ? new Color32(133, 219, 12, 255) : new Color32(241, 193, 30, 255);
                    audioService.Play(GameSfx.Combo, combo);
                }
            }

            if (!visible)
                return;
            age += dt;
            float unit = ((RectTransform)root.parent).rect.width / 1338f;
            float pop = age < .12f ? Mathf.Lerp(.82f, 1.2f, age / .12f) : age < .23f ? Mathf.Lerp(1.2f, 1, (age - .12f) / .11f) : 1;
            root.localScale = Vector3.one * unit * pop;
            root.anchoredPosition = new Vector2(0, -110 * unit);
            group.alpha = age < .1f ? age / .1f : age < .65f ? 1 : Mathf.Clamp01((.85f - age) / .2f);
            if (age >= .85f)
                Hide();
        }

        void OnDestroy()
        {
            if (game && ReferenceEquals(game.Feedback, this))
                game.Feedback = null;
        }
    }
}
