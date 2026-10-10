using System;
using UnityEngine;

namespace BlockCrush.UI
{
    public enum WheelRewardKind { Coins, Bomb, Booster, Reverse }
    [Serializable]
    public sealed class WheelReward
    {
        public WheelRewardKind kind;
        [Min(1)] public int amount = 1;
        [Min(0)] public int weight = 1;
        public string Label => amount + " " + kind;
    }
    [CreateAssetMenu(menuName = "Block Crush/Lucky Wheel")]
    public sealed class LuckyWheelCatalog : ScriptableObject
    {
        // Clockwise from the top purple sector; artwork and outcome share this order.
        public WheelReward[] rewards = {
            new WheelReward { kind=WheelRewardKind.Coins, amount=1000, weight=2 },
            new WheelReward { kind=WheelRewardKind.Coins, amount=300, weight=10 },
            new WheelReward { kind=WheelRewardKind.Coins, amount=100, weight=24 },
            new WheelReward { kind=WheelRewardKind.Bomb, amount=2, weight=6 },
            new WheelReward { kind=WheelRewardKind.Coins, amount=50, weight=35 },
            new WheelReward { kind=WheelRewardKind.Coins, amount=150, weight=15 },
            new WheelReward { kind=WheelRewardKind.Booster, amount=2, weight=4 },
            new WheelReward { kind=WheelRewardKind.Reverse, amount=2, weight=4 }
        };
        [Range(3,8)] public float duration = 5.2f;
        [Range(4,8)] public int turns = 6;
        public bool Valid => rewards != null && rewards.Length == 8 && Array.TrueForAll(rewards, r => r != null && r.amount > 0 && r.weight >= 0) && TotalWeight > 0;
        public int TotalWeight { get { int total=0; if(rewards!=null) foreach(var r in rewards) if(r!=null) total+=Mathf.Clamp(r.weight,0,100000); return total; } }
        public int Select(double unit)
        {
            if(!Valid) throw new InvalidOperationException("Lucky Wheel requires eight valid rewards.");
            double pick = Math.Max(0,Math.Min(.999999999,unit))*TotalWeight;
            for(int i=0;i<rewards.Length;i++) { pick-=Mathf.Clamp(rewards[i].weight,0,100000); if(pick<0) return i; }
            return rewards.Length-1;
        }
    }
}
