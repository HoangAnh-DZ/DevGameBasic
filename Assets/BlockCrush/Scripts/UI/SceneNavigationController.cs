using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BlockCrush.UI
{
    public sealed class SceneNavigationController : MonoBehaviour
    {
        [SerializeField] CanvasGroup transition;
        [SerializeField] GameAudioService audioService;
        public bool IsTransitioning { get; private set; }
        Coroutine fadeIn;
        public void Configure(CanvasGroup overlay, GameAudioService audio) { transition = overlay; audioService = audio; }
        void Start() { transition.gameObject.SetActive(true); fadeIn = StartCoroutine(FadeIn()); }
        IEnumerator FadeIn()
        {
            transition.blocksRaycasts = true;
            for (float t = 0; t < .16f; t += Time.unscaledDeltaTime) { transition.alpha = 1 - t / .16f; yield return null; }
            transition.alpha = 0; transition.blocksRaycasts = false;
        }
        public void Gameplay() { AdventureProgress.ReturnToMap = false; Navigate("BlockCrushGameplay"); }
        public void AdventureGameplay() => Navigate("BlockCrushAdventure");
        public void CompleteHome()
        {
            AdventureProgress.ReturnToMap = false; PlayerPrefs.Save(); Navigate("Home");
        }
        public void Home()
        {
            AdventureProgress.ReturnToMap = FindFirstObjectByType<AdventureLevelController>();
            Navigate("Home");
        }
        void Navigate(string scene)
        {
            if (IsTransitioning) return;
            IsTransitioning = true; audioService.Feedback();
            var game = FindFirstObjectByType<BlockGameManager>(); if (game) game.PauseGame();
            StartCoroutine(Load(scene));
        }
        IEnumerator Load(string scene)
        {
            if (fadeIn != null) StopCoroutine(fadeIn);
            transition.transform.SetAsLastSibling(); transition.blocksRaycasts = true;
            for (float t = 0; t < .16f; t += Time.unscaledDeltaTime) { transition.alpha = Mathf.Clamp01(t / .16f); yield return null; }
            transition.alpha = 1;
            yield return SceneManager.LoadSceneAsync(scene, LoadSceneMode.Single);
        }
        void Update()
        {
            if (IsTransitioning || UnityEngine.InputSystem.Keyboard.current?.escapeKey.wasPressedThisFrame != true) return;
            var more = GetComponent<MoreSettingsController>();
            if (more.InformationOpen) { more.CloseInformation(); return; }
            var reward = FindFirstObjectByType<RewardPopupController>();
            if(reward && reward.IsOpen) { reward.Back(); return; }
            var shop = FindFirstObjectByType<ShopController>();
            if(shop && shop.IsOpen) { shop.Close(); return; }
            var daily = FindFirstObjectByType<DailyTasksController>();
            if(daily && daily.IsOpen) { daily.Close(); return; }
            var wheel = FindFirstObjectByType<LuckyWheelController>();
            if (wheel && wheel.IsOpen) { wheel.Close(); return; }
            if (more.IsOpen) more.Close();
            else if (GetComponent<SettingsPanelController>().IsOpen) GetComponent<SettingsPanelController>().Close();
            else
            {
                var adventure = FindFirstObjectByType<AdventurePanelController>();
                if (adventure && adventure.IsOpen) adventure.BackHome();
                else if (FindFirstObjectByType<AdventureLevelController>()) Home();
            }
        }
    }
}
