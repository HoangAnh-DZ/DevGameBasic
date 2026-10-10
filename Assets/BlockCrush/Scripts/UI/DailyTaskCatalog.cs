using UnityEngine;

namespace BlockCrush.UI
{
    [CreateAssetMenu(menuName = "Block Crush/Daily Tasks")]
    public sealed class DailyTaskCatalog : ScriptableObject
    {
        public int[] goals =
        {
            2,
            4,
            6,
            8,
            10
        };
        [Min(1)]
        public int coinReward = 100;
        public bool Valid
        {
            get
            {
                if (goals == null || goals.Length != 5 || coinReward <= 0)
                    return false;
                int previous = 0;
                foreach (int goal in goals)
                {
                    if (goal <= previous)
                        return false;
                    previous = goal;
                }

                return true;
            }
        }
    }
}
