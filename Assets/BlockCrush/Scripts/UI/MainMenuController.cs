using UnityEngine;

namespace BlockCrush.UI
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] SceneNavigationController navigation;
        [SerializeField] SettingsPanelController settings;
        [SerializeField] MoreSettingsController more;
        [SerializeField] AdventurePanelController adventure;
        [SerializeField] LuckyWheelController wheel;
        [SerializeField] DailyTasksController daily;
        [SerializeField] ShopController shop;
        public void Configure(SceneNavigationController nav, SettingsPanelController panel, MoreSettingsController details)
        { navigation = nav; settings = panel; more = details; }
        public void Play() => navigation.Gameplay();
        public void OpenSettings() => settings.Open();
        public void ConfigureAdventure(AdventurePanelController view) => adventure = view;
        public void ConfigureWheel(LuckyWheelController view) => wheel = view;
        public void ConfigureCommerce(DailyTasksController tasks, ShopController store) { daily=tasks; shop=store; }
        void Start() { if (AdventureProgress.ReturnToMap) adventure.Open(); }
        public void Adventure() => adventure.Open();
        public void Shop() => shop.Open();
        public void Challenge() => more.ShowInformation("Challenges", "Challenges are not available in this build.");
        public void Wheel() => wheel.Open();
        public void Calendar() => daily.Open();
    }
}
