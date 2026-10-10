using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BlockCrush.UI.Editor
{
    // No board fixtures: every simulated turn uses one of the three available pieces at a legal anchor.
    public static class AdventureSimulation
    {
        public sealed class Sample
        {
            public int[] scores;
            public bool noFit;
        }

        public static string Status = "Not started";
        static readonly Dictionary<string, ulong[]> placements = new Dictionary<string, ulong[]>();
        static readonly ulong[] rows = new ulong[8], columns = new ulong[8];
        static readonly List<Sample>[, ] samples = new List<Sample>[10, 2];
        static AdventureLevelCatalog catalog;
        static int level, strategy, seed, count, seedOffset;
        static bool calibrate;
        public static string Start(int runs = 200, bool adjust = false, int firstSeed = 0)
        {
            if (Status == "Running")
                throw new InvalidOperationException("Simulation already running.");
            catalog = AssetDatabase.LoadAssetAtPath<AdventureLevelCatalog>("Assets/BlockCrush/Data/AdventureLevels.asset");
            if (catalog.Count != 10)
                throw new InvalidOperationException("Ten configured levels required.");
            BuildMasks();
            for (int l = 0; l < 10; l++)
                for (int s = 0; s < 2; s++)
                    samples[l, s] = new List<Sample>();
            count = runs;
            seedOffset = firstSeed;
            level = strategy = seed = 0;
            calibrate = adjust;
            Status = "Running";
            EditorApplication.update += Tick;
            return Status;
        }

        static void Tick()
        {
            try
            {
                // Bounded batches keep the Editor responsive and avoid MCP timeouts.
                double deadline = EditorApplication.timeSinceStartup + .08;
                do
                {
                    samples[level, strategy].Add(Run(catalog.Get(level + 1), seedOffset + seed, strategy == 1));
                    if (++seed == count)
                    {
                        seed = 0;
                        if (++strategy == 2)
                        {
                            strategy = 0;
                            level++;
                        }
                    }
                }
                while (level < 10 && EditorApplication.timeSinceStartup < deadline);
                if (level == 10)
                {
                    EditorApplication.update -= Tick;
                    Finish();
                }
            }
            catch (Exception error)
            {
                EditorApplication.update -= Tick;
                Status = "FAIL: " + error;
                Debug.LogError(Status);
            }
        }

        static void BuildMasks()
        {
            if (placements.Count > 0)
                return;
            for (int i = 0; i < 8; i++)
            {
                rows[i] = 255UL << (i * 8);
                for (int j = 0; j < 8; j++)
                    columns[i] |= 1UL << (j * 8 + i);
            }

            foreach (var shape in BlockShapeData.Database)
            {
                var masks = new List<ulong>();
                for (int y = 0; y <= 8 - shape.Height; y++)
                    for (int x = 0; x <= 8 - shape.Width; x++)
                    {
                        ulong mask = 0;
                        foreach (var p in shape.cells)
                            mask |= 1UL << ((p.y + y) * 8 + p.x + x);
                        masks.Add(mask);
                    }

                placements[shape.id] = masks.ToArray();
            }
        }

        static int Bits(ulong value)
        {
            int n = 0;
            while (value != 0)
            {
                value &= value - 1;
                n++;
            }

            return n;
        }

        static bool Fits(ulong board, BlockShapeData shape)
        {
            foreach (var mask in placements[shape.id])
                if ((mask & board) == 0)
                    return true;
            return false;
        }

        public static ulong Clear(ulong board, out int lines)
        {
            ulong clear = 0;
            lines = 0;
            for (int i = 0; i < 8; i++)
            {
                if ((board & rows[i]) == rows[i])
                {
                    clear |= rows[i];
                    lines++;
                }

                if ((board & columns[i]) == columns[i])
                {
                    clear |= columns[i];
                    lines++;
                }
            }

            return board & ~clear;
        }

        static double Value(ulong board, int lines, int cells, bool lineStrategy)
        {
            double result = lines * (lineStrategy ? 140 : 35) + cells * .3 - Bits(board) * .4;
            for (int i = 0; i < 8; i++)
            {
                int r = Bits(board & rows[i]), c = Bits(board & columns[i]);
                result += (r * r + c * c) * (lineStrategy ? .8 : .2);
            }

            // Favor compact edges; penalize single-cell cavities and disconnected vacancies.
            int holes = 0, contacts = 0;
            for (int y = 0; y < 8; y++)
                for (int x = 0; x < 8; x++)
                {
                    ulong bit = 1UL << (y * 8 + x);
                    if ((board & bit) != 0)
                        continue;
                    int neighbors = 0;
                    if (x == 0 || (board & (bit >> 1)) != 0)
                        neighbors++;
                    if (x == 7 || (board & (bit << 1)) != 0)
                        neighbors++;
                    if (y == 0 || (board & (bit >> 8)) != 0)
                        neighbors++;
                    if (y == 7 || (board & (bit << 8)) != 0)
                        neighbors++;
                    if (neighbors == 4)
                        holes++;
                    contacts += neighbors;
                }

            return result - holes * (lineStrategy ? 12 : 2) + contacts * .25;
        }

        public static Sample Run(AdventureLevelCatalog.Level configuration, int randomSeed, bool lineStrategy)
        {
            BuildMasks();
            var random = new System.Random(randomSeed);
            ulong board = 0;
            var pieces = new BlockShapeData[3];
            var scores = new List<int>();
            int score = 0, combo = 0;
            for (int move = 0; move < configuration.maxMoves; move++)
            {
                if (pieces.All(p => p == null))
                {
                    var chosen = new HashSet<string>();
                    for (int i = 0; i < 3; i++)
                    {
                        pieces[i] = AdventurePieceGenerator.Pick(configuration, chosen, p => Fits(board, p), random.Next);
                        chosen.Add(pieces[i].id);
                    }
                }

                int slot = -1, bestLines = 0;
                ulong bestBoard = 0;
                double best = double.NegativeInfinity;
                for (int i = 0; i < 3; i++)
                    if (pieces[i] != null)
                        foreach (var mask in placements[pieces[i].id])
                        {
                            if ((mask & board) != 0)
                                continue;
                            int lines;
                            ulong next = Clear(board | mask, out lines);
                            double value = Value(next, lines, pieces[i].cells.Length, lineStrategy);
                            // Strict deterministic tie-break uses slot and bottom-left mask enumeration.
                            if (value > best)
                            {
                                best = value;
                                slot = i;
                                bestBoard = next;
                                bestLines = lines;
                            }
                        }

                if (slot < 0)
                    return new Sample
                    {
                        scores = scores.ToArray(),
                        noFit = true
                    };
                score += AdventureScoreSystem.Calculate(pieces[slot].cells.Length, bestLines, combo, out combo);
                pieces[slot] = null;
                board = bestBoard;
                scores.Add(score);
            }

            return new Sample
            {
                scores = scores.ToArray(),
                noFit = false
            };
        }

        static void Finish()
        {
            if (calibrate)
            {
                var levels = new AdventureLevelCatalog.Level[10];
                int previous = 0;
                for (int l = 0; l < 10; l++)
                {
                    var cfg = catalog.Get(l + 1);
                    double desired = .9 - (l / 2) * .1;
                    var ordered = samples[l, 1].Select(s => s.scores.Length == 0 ? 0 : s.scores.Last()).OrderBy(n => n).ToArray();
                    int target = ordered[Math.Min(ordered.Length - 1, (int)(ordered.Length * (1 - desired)))];
                    // Smooth monotonic targets; retain given move budgets and shape categories.
                    target = Math.Max(previous + 5, (int)(Math.Round(target / 5.0) * 5));
                    previous = target;
                    levels[l] = new AdventureLevelCatalog.Level(target, cfg.maxMoves, cfg.smallWeight, cfg.mediumWeight, cfg.complexWeight, cfg.largeWeight);
                }

                catalog.SetLevels(levels);
                EditorUtility.SetDirty(catalog);
                AssetDatabase.SaveAssets();
            }

            var text = new System.Text.StringBuilder("Adventure balance simulation\n");
            text.AppendLine("Seeds per level/strategy: " + count + "; seeds " + seedOffset + ".." + (seedOffset + count - 1) + ". Strategies: compact basic / prioritize lines and avoid cavities. No boosters, retries or board fixtures. Shared generator and scoring; legal 8x8 mask placement, three distinct shapes, refill only when all used.\n");
            text.AppendLine("Level,Target,Moves,Strategy,WinRate,AvgScore,AvgMoves,NoFitLoss,MoveLoss");
            for (int l = 0; l < 10; l++)
                for (int s = 0; s < 2; s++)
                {
                    int target = catalog.Target(l + 1), wins = 0, nofit = 0;
                    double sumScore = 0, sumMoves = 0;
                    foreach (var run in samples[l, s])
                    {
                        int at = Array.FindIndex(run.scores, n => n >= target);
                        bool won = at >= 0;
                        if (won)
                            wins++;
                        else if (run.noFit)
                            nofit++;
                        sumScore += won ? run.scores[at] : run.scores.LastOrDefault();
                        sumMoves += won ? at + 1 : run.scores.Length;
                    }

                    text.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0},{1},{2},{3},{4:F1}%,{5:F1},{6:F1},{7},{8}", l + 1, target, catalog.MaxMoves(l + 1), s == 0 ? "Basic" : "Line", 100.0 * wins / count, sumScore / count, sumMoves / count, nofit, count - wins - nofit));
                }

            text.AppendLine("Bot win rates describe these deterministic heuristics, not measured human difficulty. Calibration uses full legal-play score distributions; metrics stop at the first target-crossing move. Independent seeds are required after tuning.");
            Directory.CreateDirectory("Captures/Figma");
            File.WriteAllText("Captures/Figma/AdventureBalance.csv", text.ToString());
            Status = "PASS: " + (count * 20) + " simulated games";
            Debug.Log("[AdventureSimulation] " + Status);
        }
    }
}
