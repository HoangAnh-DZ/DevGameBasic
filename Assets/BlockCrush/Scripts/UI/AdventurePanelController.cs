using UnityEngine;
using UnityEngine.UI;

namespace BlockCrush.UI
{
    public sealed class AdventurePanelController : MonoBehaviour
    {
        [SerializeField] GameObject homeDesign;
        [SerializeField] SettingsPanelController settings;
        [SerializeField] SceneNavigationController navigation;
        [SerializeField] GameAudioService audioService;
        [SerializeField] AdventureLevelCatalog catalog;
        [SerializeField] Button[] levelButtons;
        [SerializeField] Image[] badges;
        [SerializeField] Sprite unlockedBadge, lockedBadge;
        [SerializeField] Text playLabel;
        [SerializeField] ScrollRect levelScroll;
        [SerializeField] Text[] completionLabels;
        public bool IsOpen => gameObject.activeSelf;
        public int SelectedLevel { get; private set; }
        public AdventureLevelCatalog Catalog => catalog;

        public void Configure(GameObject home, SettingsPanelController popup, SceneNavigationController nav,
            GameAudioService audio, AdventureLevelCatalog levels, Button[] buttons, Image[] icons,
            Sprite unlocked, Sprite locked, Text label, ScrollRect scroll, Text[] states)
        {
            homeDesign = home; settings = popup; navigation = nav; audioService = audio; catalog = levels;
            levelButtons = buttons; badges = icons; unlockedBadge = unlocked; lockedBadge = locked; playLabel = label;
            levelScroll = scroll; completionLabels = states;
        }

        public void Open()
        {
            AdventureProgress.ReturnToMap = false;
            homeDesign.SetActive(false); gameObject.SetActive(true); transform.SetAsLastSibling();
            settings.transform.SetAsLastSibling(); SelectedLevel = AdventureProgress.Unlocked(catalog); Refresh(); audioService.Feedback();
            Canvas.ForceUpdateCanvases(); levelScroll.verticalNormalizedPosition = Mathf.Clamp01((SelectedLevel - 1f) / Mathf.Max(1, catalog.Count - 3));
        }
        public void SelectLevel1() => Select(1);
        public void SelectLevel2() => Select(2);
        public void SelectLevel3() => Select(3);
        public void Select(int level)
        {
            if (level < 1 || level > AdventureProgress.Unlocked(catalog)) return;
            SelectedLevel = level; Refresh(); audioService.Feedback();
        }
        void Refresh()
        {
            for (int i = 0; i < levelButtons.Length; i++)
            {
                bool unlocked = i + 1 <= AdventureProgress.Unlocked(catalog);
                levelButtons[i].interactable = unlocked; badges[i].sprite = unlocked ? unlockedBadge : lockedBadge;
                badges[i].GetComponent<Button>().interactable = unlocked;
                completionLabels[i].text = AdventureProgress.IsComplete(i + 1) ? "✓  Best " + AdventureProgress.Best(i + 1) : "";
            }
            playLabel.text = "▶ Level " + SelectedLevel;
        }
        public void PlaySelected()
        {
            if (AdventureProgress.Begin(SelectedLevel, catalog)) navigation.AdventureGameplay();
        }
        public void BackHome()
        {
            if (settings.IsOpen) settings.Close();
            AdventureProgress.ReturnToMap = false; gameObject.SetActive(false); homeDesign.SetActive(true); audioService.Feedback();
        }
    }
}
