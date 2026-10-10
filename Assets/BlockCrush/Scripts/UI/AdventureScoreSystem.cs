using System;

namespace BlockCrush.UI
{
    public static class AdventureScoreSystem
    {
        public static int Calculate(int placedCells, int lines, int previousCombo, out int combo)
        {
            if (placedCells < 0 || lines < 0 || previousCombo < 0)
                throw new ArgumentOutOfRangeException();
            combo = lines > 0 ? previousCombo + 1 : 0;
            int bonus = lines == 0 ? 0 : lines == 1 ? 10 : lines == 2 ? 25 : lines == 3 ? 45 : 70;
            return placedCells + bonus + (lines > 0 ? Math.Min(25, previousCombo * 5) : 0);
        }
    }
}
