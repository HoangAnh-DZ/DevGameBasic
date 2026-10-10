using System.Collections.Generic;

namespace BlockCrush
{
    // Optional rules for another game mode. Null retains every Classic rule.
    public interface IBlockGameRules
    {
        string BestScoreKey { get; }

        void Restarted();
        int PlacementScore(int cells, int lines, int previousCombo, out int combo);
        bool EndTurn(int score, bool noFit);
        BlockShapeData SelectShape(HashSet<string> chosen, BlockBoard board);
    }
}
