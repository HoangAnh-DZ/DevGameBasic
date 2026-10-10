using System;
using System.Collections.Generic;

namespace BlockCrush.UI
{
    public static class AdventurePieceGenerator
    {
        public static int Category(BlockShapeData shape)
        {
            if (shape.cells.Length <= 3)
                return 0;
            if (shape.Width == 1 || shape.Height == 1 || shape.id == "Square2x2")
                return 1;
            return shape.cells.Length <= 5 ? 2 : 3;
        }

        public static BlockShapeData Pick(AdventureLevelCatalog.Level settings, HashSet<string> chosen, Func<BlockShapeData, bool> fits, Func<int, int> next)
        {
            int total = 0;
            foreach (var shape in BlockShapeData.Database)
                if (!chosen.Contains(shape.id) && fits(shape))
                    total += settings.Weight(Category(shape));
            // A board with no fitting shape loses; no reroll/search loop is used.
            bool anyFit = total > 0;
            if (!anyFit)
                foreach (var shape in BlockShapeData.Database)
                    if (!chosen.Contains(shape.id))
                        total += settings.Weight(Category(shape));
            int value = next(total);
            foreach (var shape in BlockShapeData.Database)
                if (!chosen.Contains(shape.id) && (!anyFit || fits(shape)))
                {
                    value -= settings.Weight(Category(shape));
                    if (value < 0)
                        return shape;
                }

            throw new InvalidOperationException("Adventure shape weights must be positive.");
        }
    }
}
