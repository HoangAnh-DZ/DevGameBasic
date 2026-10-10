using System;
using UnityEngine;

namespace BlockCrush.UI
{
    [Serializable]
    public sealed class WheelReceipt
    {
        public int id, slot, amount;
        public WheelRewardKind kind;
        public string Label => amount + " " + kind;
    }
    [Serializable]
    public sealed class LuckyWheelSave
    {
        public const string Key = "BlockCrush.LuckyWheel.v1";
        public long usedDay = -1;
        public int sequence, recordedCount, acknowledgedId;
        public WheelReceipt pending, last;
        public static long Day(DateTime utc) => utc.ToUniversalTime().Ticks / TimeSpan.TicksPerDay;
        public static LuckyWheelSave Load()
        {
            var json=PlayerPrefs.GetString(Key,"");
            if(string.IsNullOrEmpty(json)) return new LuckyWheelSave();
            try
            {
                var state=JsonUtility.FromJson<LuckyWheelSave>(json) ?? new LuckyWheelSave();
                // Unity serializes null inline classes as an empty object. Zero IDs are empty slots.
                if(state.pending!=null && state.pending.id<=0) state.pending=null;
                if(state.last!=null && state.last.id<=0) state.last=null;
                return state;
            }
            catch(ArgumentException) { return new LuckyWheelSave { usedDay=Day(DateTime.UtcNow) }; }
        }
        public bool Available(DateTime utc) => pending == null && Day(utc) > usedDay;
        public TimeSpan Remaining(DateTime utc)
        {
            long day=Math.Max(Day(utc),usedDay);
            long ticks=Math.Min(DateTime.MaxValue.Ticks,(day+1)*TimeSpan.TicksPerDay);
            return TimeSpan.FromTicks(Math.Max(0,ticks-utc.ToUniversalTime().Ticks));
        }
        public bool Begin(LuckyWheelCatalog catalog, DateTime utc, double random)
        {
            if(!catalog || !catalog.Valid || !Available(utc)) return false;
            int slot=catalog.Select(random); var reward=catalog.rewards[slot];
            // A single durable record reserves the day and immutable outcome before animation.
            pending=new WheelReceipt { id=++sequence, slot=slot, kind=reward.kind, amount=reward.amount };
            usedDay=Day(utc); Save(); return true;
        }
        public bool Record()
        {
            if(pending==null) return false;
            EconomySave.GrantWheel(pending,usedDay);
            if(last==null || last.id!=pending.id) { last=pending; recordedCount++; }
            pending=null; Save(); return true;
        }
        public void Acknowledge() { if(last==null) return; acknowledgedId=last.id; Save(); }
        public void Save() { PlayerPrefs.SetString(Key,JsonUtility.ToJson(this)); PlayerPrefs.Save(); }
    }
}
