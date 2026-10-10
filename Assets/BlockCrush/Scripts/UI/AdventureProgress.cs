using UnityEngine;

namespace BlockCrush.UI
{
    public static class AdventureProgress
    {
        public const string CompletedKey = "BlockCrush.Adventure.Completed";
        public const string TutorialKey = "BlockCrush.Adventure.TutorialDone";
        public const string UnlockedKey = "BlockCrush.Adventure.HighestUnlocked";
        public static string BestKey(int level) => "BlockCrush.Adventure.Level" + level + ".Best";
        public static string CompleteKey(int level) => "BlockCrush.Adventure.Level" + level + ".Complete";
        public static int SelectedLevel { get; private set; } = 1;
        public static bool ReturnToMap { get; set; }
        public static int Completed => Mathf.Max(0, PlayerPrefs.GetInt(CompletedKey, 0));
        public static int Unlocked(AdventureLevelCatalog catalog)
        {
            // Migrate the existing three-level sequential save without deleting its keys.
            int old = Mathf.Min(Completed, catalog.Count);
            bool changed = false;
            for (int i = 1; i <= old; i++) if (!PlayerPrefs.HasKey(CompleteKey(i))) { PlayerPrefs.SetInt(CompleteKey(i), 1); changed = true; }
            int unlocked = Mathf.Clamp(Mathf.Max(old + 1, PlayerPrefs.GetInt(UnlockedKey, 1)), 1, catalog.Count);
            if (!PlayerPrefs.HasKey(UnlockedKey) || PlayerPrefs.GetInt(UnlockedKey) != unlocked) { PlayerPrefs.SetInt(UnlockedKey, unlocked); changed = true; }
            if (changed) PlayerPrefs.Save(); return unlocked;
        }
        public static bool IsComplete(int level) => PlayerPrefs.GetInt(CompleteKey(level), level <= Completed ? 1 : 0) != 0;
        public static int Best(int level) => PlayerPrefs.GetInt(BestKey(level), 0);
        public static bool Begin(int level, AdventureLevelCatalog catalog)
        {
            if (level < 1 || level > Unlocked(catalog)) return false;
            SelectedLevel = level; ReturnToMap = false; return true;
        }
        public static void Complete(int level, AdventureLevelCatalog catalog)
        {
            if (level != SelectedLevel || level > Unlocked(catalog) || level > catalog.Count) return;
            PlayerPrefs.SetInt(CompletedKey, Mathf.Max(Completed, level));
            PlayerPrefs.SetInt(CompleteKey(level), 1);
            PlayerPrefs.SetInt(UnlockedKey, Mathf.Min(catalog.Count, Mathf.Max(PlayerPrefs.GetInt(UnlockedKey, 1), level + 1))); PlayerPrefs.Save();
        }
    }
}
