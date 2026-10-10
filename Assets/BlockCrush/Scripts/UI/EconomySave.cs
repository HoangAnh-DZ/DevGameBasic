using System;
using UnityEngine;

namespace BlockCrush.UI
{
    [Serializable]
    public sealed class EconomySave
    {
#if UNITY_EDITOR
        // Editor purchases never become a shipping player's paid inventory.
        public const string Key = "BlockCrush.Economy.EditorDemo.v1";
#else
        public const string Key = "BlockCrush.Economy.v1";
#endif
        public int coins, bombs, boosters, reverses, demoSequence;
        public bool removeAds;
        public long dailyDay = -1, wheelDay = -1;
        public int dailyPlays, dailyClaimMask, wheelId;
        public static EconomySave Load() => Load(DateTime.UtcNow);
        public static EconomySave Load(DateTime now)
        {
            EconomySave state;
            try { state = JsonUtility.FromJson<EconomySave>(PlayerPrefs.GetString(Key, "{}")) ?? new EconomySave(); }
            catch (ArgumentException) { state = new EconomySave(); }
            state.coins = Mathf.Clamp(state.coins, 0, 999999999);
            state.bombs = Mathf.Clamp(state.bombs, 0, 999999);
            state.boosters = Mathf.Clamp(state.boosters, 0, 999999);
            state.reverses = Mathf.Clamp(state.reverses, 0, 999999);
            long day = LuckyWheelSave.Day(now);
            if (day > state.dailyDay)
            { state.dailyDay = day; state.dailyPlays = 0; state.dailyClaimMask = 0; state.Save(); }
            return state;
        }
        static int Add(int value, int amount, int maximum) => (int)Math.Min(maximum, (long)value + Math.Max(0, amount));
        void AddReward(WheelRewardKind kind, int amount)
        {
            switch (kind)
            {
                case WheelRewardKind.Coins: coins = Add(coins, amount, 999999999); break;
                case WheelRewardKind.Bomb: bombs = Add(bombs, amount, 999999); break;
                case WheelRewardKind.Booster: boosters = Add(boosters, amount, 999999); break;
                case WheelRewardKind.Reverse: reverses = Add(reverses, amount, 999999); break;
            }
        }
        public static bool GrantWheel(WheelReceipt receipt, long day)
        {
            if (receipt == null || receipt.id <= 0 || receipt.amount <= 0 || !Enum.IsDefined(typeof(WheelRewardKind), receipt.kind)) return false;
            var state = Load();
            if (day < state.wheelDay || (day == state.wheelDay && receipt.id <= state.wheelId)) return false;
            state.AddReward(receipt.kind, receipt.amount); state.wheelDay = day; state.wheelId = receipt.id;
            // Grant + idempotency marker share one durable JSON write. Recovery can safely retry.
            state.Save(); return true;
        }
        public static void RecordAdventureRound() => RecordAdventureRound(DateTime.UtcNow);
        public static void RecordAdventureRound(DateTime now)
        {
            var state = Load(now); state.dailyPlays = Add(state.dailyPlays, 1, 100000); state.Save();
        }
        public bool Claimed(int index) => index >= 0 && index < 5 && (dailyClaimMask & (1 << index)) != 0;
        public int ClaimedCount { get { int count = 0; for (int i = 0; i < 5; i++) if (Claimed(i)) count++; return count; } }
        public static bool ClaimDaily(int index, DailyTaskCatalog catalog, DateTime now)
        {
            if (!catalog || !catalog.Valid || index < 0 || index >= catalog.goals.Length) return false;
            var state = Load(now);
            if (state.dailyDay != LuckyWheelSave.Day(now) || state.Claimed(index) || state.dailyPlays < catalog.goals[index]) return false;
            state.dailyClaimMask |= 1 << index; state.AddReward(WheelRewardKind.Coins, catalog.coinReward); state.Save(); return true;
        }
        public static bool PurchaseDemo(ShopOffer offer)
        {
#if UNITY_EDITOR
            if (offer == null || !offer.Valid) return false;
            var state = Load(); if (offer.removeAds && state.removeAds) return false;
            state.AddReward(WheelRewardKind.Coins, offer.coins); state.AddReward(WheelRewardKind.Bomb, offer.bombs);
            state.AddReward(WheelRewardKind.Booster, offer.boosters); state.AddReward(WheelRewardKind.Reverse, offer.reverses);
            state.removeAds |= offer.removeAds; state.demoSequence++; state.Save(); return true;
#else
            return false;
#endif
        }
        public TimeSpan Remaining(DateTime now)
        {
            long day = Math.Max(dailyDay, LuckyWheelSave.Day(now));
            return TimeSpan.FromTicks(Math.Max(0, Math.Min(DateTime.MaxValue.Ticks, (day + 1) * TimeSpan.TicksPerDay) - now.ToUniversalTime().Ticks));
        }
        public void Save() { PlayerPrefs.SetString(Key, JsonUtility.ToJson(this)); PlayerPrefs.Save(); }
    }
}
