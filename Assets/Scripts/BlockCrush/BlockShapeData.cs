using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlockCrush
{
    [Serializable]
    public sealed class BlockShapeData
    {
        public string id;
        public Vector2Int[] cells;
        public int weight;
        public int Width { get; private set; }
        public int Height { get; private set; }

        public BlockShapeData(string name, int probability, params Vector2Int[] coordinates)
        {
            id = name;
            weight = probability;
            cells = coordinates;
            foreach (var p in cells)
            {
                Width = Mathf.Max(Width, p.x + 1);
                Height = Mathf.Max(Height, p.y + 1);
            }
        }

        static Vector2Int P(int x, int y) => new Vector2Int(x, y);
        static readonly List<BlockShapeData> database = BuildDatabase();
        public static IReadOnlyList<BlockShapeData> Database => database;

        static List<BlockShapeData> BuildDatabase()
        {
            var result = new List<BlockShapeData>
            {
                new BlockShapeData("Single", 8, P(0, 0))
            };
            for (int n = 2; n <= 5; n++)
            {
                var h = new Vector2Int[n];
                var v = new Vector2Int[n];
                for (int i = 0; i < n; i++)
                {
                    h[i] = P(i, 0);
                    v[i] = P(0, i);
                }

                result.Add(new BlockShapeData("Horizontal" + n, 12 - n * 2, h));
                result.Add(new BlockShapeData("Vertical" + n, 12 - n * 2, v));
            }

            AddRectangle(result, "Square2x2", 2, 2, 7);
            AddRectangle(result, "Square3x3", 3, 3, 1);
            AddRectangle(result, "Rectangle2x3", 2, 3, 2);
            AddRectangle(result, "Rectangle3x2", 3, 2, 2);
            AddRotations(result, "L3", 6, P(0, 0), P(1, 0), P(0, 1));
            AddRotations(result, "L5", 2, P(0, 0), P(1, 0), P(2, 0), P(0, 1), P(0, 2));
            AddRotations(result, "T", 4, P(0, 1), P(1, 1), P(2, 1), P(1, 0));
            AddRotations(result, "S", 3, P(0, 0), P(1, 0), P(1, 1), P(2, 1));
            AddRotations(result, "Z", 3, P(0, 1), P(1, 1), P(1, 0), P(2, 0));
            result.Add(new BlockShapeData("Corner", 3, P(0, 0), P(1, 0), P(2, 0), P(0, 1)));
            return result;
        }

        static void AddRectangle(List<BlockShapeData> list, string id, int w, int h, int weight)
        {
            var cells = new List<Vector2Int>();
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    cells.Add(P(x, y));
            list.Add(new BlockShapeData(id, weight, cells.ToArray()));
        }

        static void AddRotations(List<BlockShapeData> list, string id, int weight, params Vector2Int[] initial)
        {
            var cells = initial;
            var seen = new HashSet<string>();
            for (int r = 0; r < 4; r++)
            {
                var ordered = (Vector2Int[])cells.Clone();
                Array.Sort(ordered, (a, b) => a.y == b.y ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
                var key = string.Join(";", Array.ConvertAll(ordered, p => p.x + "," + p.y));
                if (seen.Add(key))
                    list.Add(new BlockShapeData(id + "_" + r * 90, weight, cells));
                int minX = int.MaxValue, minY = int.MaxValue;
                var rotated = new Vector2Int[cells.Length];
                for (int i = 0; i < cells.Length; i++)
                {
                    rotated[i] = P(-cells[i].y, cells[i].x);
                    minX = Mathf.Min(minX, rotated[i].x);
                    minY = Mathf.Min(minY, rotated[i].y);
                }

                for (int i = 0; i < rotated.Length; i++)
                    rotated[i] -= P(minX, minY);
                cells = rotated;
            }
        }
    }
}
