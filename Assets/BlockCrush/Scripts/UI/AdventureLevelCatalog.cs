using UnityEngine;

namespace BlockCrush.UI
{
    [CreateAssetMenu(menuName = "Block Crush/Adventure Levels")]
    public sealed class AdventureLevelCatalog : ScriptableObject
    {
        [System.Serializable]
        public sealed class Level
        {
            public int targetScore, maxMoves;
            // Per-shape weights: small, simple medium, complex, large rectangles.
            public int smallWeight, mediumWeight, complexWeight, largeWeight;
            public Level(int target, int moves, int small, int medium, int complex, int large)
            {
                targetScore = target;
                maxMoves = moves;
                smallWeight = small;
                mediumWeight = medium;
                complexWeight = complex;
                largeWeight = large;
            }

            public int Weight(int category) => Mathf.Max(1, category == 0 ? smallWeight : category == 1 ? mediumWeight : category == 2 ? complexWeight : largeWeight);
        }

        [SerializeField]
        Level[] levels = Defaults();
        [SerializeField]
        Sprite[] previews;
        public int Count => levels == null ? 0 : levels.Length;
        public bool Valid => ValidLevels(levels);

        public Level Get(int level)
        {
            if (Count == 0)
                throw new System.InvalidOperationException("Adventure requires at least one configured level.");
            var configuration = levels[Mathf.Clamp(level - 1, 0, Count - 1)];
            if (configuration == null || configuration.targetScore <= 0 || configuration.maxMoves <= 0)
                throw new System.InvalidOperationException("Adventure levels require a positive target and move budget.");
            return configuration;
        }

        public int Target(int level) => Get(level).targetScore;
        public int MaxMoves(int level) => Get(level).maxMoves;
        public Sprite Preview(int level)
        {
            if (previews == null || previews.Length == 0)
                throw new System.InvalidOperationException("Adventure thumbnails have not been configured.");
            return previews[(level - 1) % previews.Length];
        }

        public void Configure(Sprite[] thumbnails)
        {
            previews = thumbnails;
        }

        public void SetLevels(Level[] configuration)
        {
            if (!ValidLevels(configuration))
                throw new System.ArgumentException("Adventure levels require positive targets and move budgets.", nameof(configuration));
            levels = configuration;
        }

        static bool ValidLevels(Level[] configuration) => configuration != null && configuration.Length > 0 && System.Array.TrueForAll(configuration, level => level != null && level.targetScore > 0 && level.maxMoves > 0);
        public static Level[] Defaults()
        {
            int[] scores =
            {
                100,
                160,
                230,
                310,
                400,
                500,
                610,
                730,
                860,
                1000
            };
            var data = new Level[10];
            for (int i = 0; i < 10; i++)
                data[i] = new Level(scores[i], 22 + 4 * i, Mathf.Max(6, 12 - i), 8, Mathf.Min(7, 1 + i), Mathf.Min(2, 1 + i / 3));
            return data;
        }
    }
}
