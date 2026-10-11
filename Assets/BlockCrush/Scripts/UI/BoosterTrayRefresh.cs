using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlockCrush.UI
{
    // Refresh only plans changes; normal tray generation remains in the existing spawner.
    internal static class BoosterTrayRefresh
    {
        internal readonly struct Replacement
        {
            public readonly int Slot;
            public readonly BlockShapeData Shape;
            public readonly Sprite Sprite;
            public Replacement(int slot, BlockShapeData shape, Sprite sprite)
            {
                Slot = slot;
                Shape = shape;
                Sprite = sprite;
            }
        }

        public static bool TryPlan(BlockPieceSpawner spawner, BlockBoard board, AdventureLevelCatalog.Level settings, Func<int, int> next, out List<Replacement> plan)
        {
            plan = new List<Replacement>(3);
            var fitting = new List<BlockShapeData>();
            foreach (var shape in BlockShapeData.Database)
                if (board.CanShapeFitAnywhere(shape))
                    fitting.Add(shape);
            if (fitting.Count == 0)
                return false;
            var chosen = new HashSet<string>();
            bool changed = false;
            for (int slot = 0; slot < spawner.Pieces.Count; slot++)
            {
                var piece = spawner.Pieces[slot];
                if (!piece)
                    continue; // Never restore an already used slot or refill the set early.
                if (!piece.BlockSprite || piece.IsDragging)
                    return false;
                var candidates = new List<BlockShapeData>();
                foreach (var shape in fitting)
                    if (shape.id != piece.Shape.id && !chosen.Contains(shape.id))
                        candidates.Add(shape);
                if (candidates.Count == 0)
                    foreach (var shape in fitting)
                        if (shape.id != piece.Shape.id)
                            candidates.Add(shape);
                if (candidates.Count == 0)
                    candidates.AddRange(fitting);
                int total = 0;
                foreach (var shape in candidates)
                    total += Weight(shape, settings);
                int value = next(total);
                BlockShapeData selected = candidates[candidates.Count - 1];
                foreach (var shape in candidates)
                {
                    value -= Weight(shape, settings);
                    if (value < 0)
                    {
                        selected = shape;
                        break;
                    }
                }

                chosen.Add(selected.id);
                changed |= selected.id != piece.Shape.id;
                plan.Add(new Replacement(slot, selected, piece.BlockSprite));
            }

            return changed && plan.Count > 0;
        }

        static int Weight(BlockShapeData shape, AdventureLevelCatalog.Level settings) => settings == null ? shape.weight : settings.Weight(AdventurePieceGenerator.Category(shape));
    }
}
