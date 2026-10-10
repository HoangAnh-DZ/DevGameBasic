using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace BlockCrush.UI
{
    public sealed class SettingsPanelController : MonoBehaviour
    {
        [SerializeField] CanvasGroup panel;
        [SerializeField] Image soundToggle, bgmToggle, vibrationToggle;
        [SerializeField] Sprite toggleOn, toggleOff;
        [SerializeField] Button replayButton, backHomeButton;
        [SerializeField] MoreSettingsController more;
        [SerializeField] SceneNavigationController navigation;
        [SerializeField] GameAudioService audioService;
        BlockGameManager game;
        public bool IsOpen => panel.gameObject.activeSelf;
        public CanvasGroup Panel => panel;
        public void Configure(CanvasGroup popup, Image sound, Image bgm, Image vibration, Sprite on, Sprite off,
            Button replay, Button home, MoreSettingsController details, SceneNavigationController nav, GameAudioService audio)
        {
            panel = popup; soundToggle = sound; bgmToggle = bgm; vibrationToggle = vibration; toggleOn = on; toggleOff = off;
            replayButton = replay; backHomeButton = home; more = details; navigation = nav; audioService = audio;
        }
        void Start() { game = FindFirstObjectByType<BlockGameManager>(); Refresh(); }
        public void Open()
        {
            game = FindFirstObjectByType<BlockGameManager>(); if (game) game.PauseGame();
            Refresh(); StopAllCoroutines(); panel.gameObject.SetActive(true); panel.transform.SetAsLastSibling();
            audioService.Feedback(); StartCoroutine(Appear());
        }
        IEnumerator Appear()
        {
            panel.alpha = 0;
            for (float t = 0; t < .10f; t += Time.unscaledDeltaTime) { panel.alpha = Mathf.Clamp01(t / .10f); yield return null; }
            panel.alpha = 1;
        }
        public void Close()
        { StopAllCoroutines(); panel.gameObject.SetActive(false); if (game) game.ResumeGame(); audioService.Feedback(); }
        public void ToggleSound() { GamePreferences.Set(GamePreferences.SoundKey, !GamePreferences.Sound); Changed(); }
        public void ToggleBgm() { GamePreferences.Set(GamePreferences.BgmKey, !GamePreferences.Bgm); Changed(); }
        public void ToggleVibration() { GamePreferences.Set(GamePreferences.VibrationKey, !GamePreferences.Vibration); Changed(); }
        void Changed() { audioService.ApplyPreferences(); Refresh(); audioService.Feedback(); }
        public void Refresh()
        {
            soundToggle.sprite = GamePreferences.Sound ? toggleOn : toggleOff;
            bgmToggle.sprite = GamePreferences.Bgm ? toggleOn : toggleOff;
            vibrationToggle.sprite = GamePreferences.Vibration ? toggleOn : toggleOff;
            replayButton.interactable = game;
            var adventure = FindFirstObjectByType<AdventurePanelController>();
            backHomeButton.interactable = game || (adventure && adventure.IsOpen);
        }
        public void Replay() { if (!game) return; Close(); game.RestartGame(); }
        public void BackHome()
        {
            if (game) { navigation.Home(); return; }
            var adventure = FindFirstObjectByType<AdventurePanelController>();
            if (adventure && adventure.IsOpen) adventure.BackHome();
        }
        public void OpenMore() { StopAllCoroutines(); panel.gameObject.SetActive(false); more.Open(); }
    }
}
