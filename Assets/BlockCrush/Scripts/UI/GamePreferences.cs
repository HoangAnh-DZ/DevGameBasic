using UnityEngine;

namespace BlockCrush.UI
{
    public static class GamePreferences
    {
        public const string SoundKey = "BlockCrush_Sound", BgmKey = "BlockCrush_BGM", VibrationKey = "BlockCrush_Vibration";
        public static bool Sound => PlayerPrefs.GetInt(SoundKey, 1) != 0;
        public static bool Bgm => PlayerPrefs.GetInt(BgmKey, 1) != 0;
        public static bool Vibration => PlayerPrefs.GetInt(VibrationKey, 1) != 0;

        public static void Set(string key, bool enabled)
        {
            PlayerPrefs.SetInt(key, enabled ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
