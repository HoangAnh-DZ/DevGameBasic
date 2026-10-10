using System.Collections.Generic;
using UnityEngine;

namespace BlockCrush
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class BlockBoard : MonoBehaviour
    {
        public const int Size = 8;
        [SerializeField] BlockCell cellPrefab;
        [SerializeField] BlockCell[] cells = new BlockCell[0];
        readonly bool[,] occupied = new bool[Size, Size];
        readonly List<BlockCell> preview = new List<BlockCell>();
        public RectTransform Rect => (RectTransform)transform;
        public float CellSize => Rect.rect.width / Size;
        public int CellCount => cells.Length;
        public int PreviewCount => preview.Count;
        public int OccupiedCount { get { int count = 0; foreach (bool value in occupied) if (value) count++; return count; } }
        public BlockCell CellAt(int col, int row) => cells[row * Size + col];
        public bool IsOccupied(int col, int row) => occupied[col, row];

        void Awake() { if (cells.Length != Size * Size) GenerateBoard(); ResetBoard(); }
        public void SetCellPrefab(BlockCell prefab) { cellPrefab = prefab; }

        public void GenerateBoard()
        {
            if (cells.Length == Size * Size) return;
            cells = new BlockCell[Size * Size];
            for (int row = 0; row < Size; row++) for (int col = 0; col < Size; col++)
            {
                BlockCell cell = cellPrefab ? Instantiate(cellPrefab, transform) :
                    new GameObject("Cell", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(BlockCell)).GetComponent<BlockCell>();
                cell.transform.SetParent(transform, false); cell.name = $"Cell_{row}_{col}";
                var rt = (RectTransform)cell.transform;
                rt.anchorMin = new Vector2(col / (float)Size, row / (float)Size);
                rt.anchorMax = new Vector2((col + 1f) / Size, (row + 1f) / Size);
                rt.offsetMin = Vector2.one; rt.offsetMax = -Vector2.one;
                cell.Configure(col, row); cells[row * Size + col] = cell;
            }
        }

        public bool IsValidPlacement(BlockShapeData shape, Vector2Int anchor)
        {
            if (shape == null || shape.cells == null || shape.cells.Length == 0) return false;
            foreach (var offset in shape.cells)
            {
                var p = anchor + offset;
                if (p.x < 0 || p.y < 0 || p.x >= Size || p.y >= Size || occupied[p.x, p.y]) return false;
            }
            return true;
        }

        public bool TryGetAnchor(BlockShapeData shape, Vector3 pieceCenter, out Vector2Int anchor)
        {
            var local = (Vector2)Rect.InverseTransformPoint(pieceCenter);
            anchor = new Vector2Int(Mathf.RoundToInt((local.x - Rect.rect.xMin) / CellSize - shape.Width * .5f),
                Mathf.RoundToInt((local.y - Rect.rect.yMin) / CellSize - shape.Height * .5f));
            return Rect.rect.Contains(local) && IsValidPlacement(shape, anchor);
        }

        public List<BlockCell> GetPreviewCells(BlockShapeData shape, Vector2Int anchor)
        {
            var result = new List<BlockCell>();
            if (IsValidPlacement(shape, anchor)) foreach (var p in shape.cells) result.Add(CellAt(anchor.x + p.x, anchor.y + p.y));
            return result;
        }

        public void ShowPreview(BlockShapeData shape, Vector2Int anchor, Sprite sprite)
        {
            ClearPreview(); preview.AddRange(GetPreviewCells(shape, anchor));
            foreach (var cell in preview) cell.Preview(sprite);
        }

        public void ClearPreview() { foreach (var cell in preview) cell.ClearPreview(); preview.Clear(); }

        public bool PlacePiece(BlockShapeData shape, Vector2Int anchor, Sprite sprite)
        {
            if (!sprite || !IsValidPlacement(shape, anchor)) return false;
            ClearPreview();
            foreach (var p in shape.cells) SetOccupied(anchor.x + p.x, anchor.y + p.y, sprite);
            return true;
        }

        public void SetOccupied(int col, int row, Sprite sprite)
        {
            occupied[col, row] = sprite != null;
            if (sprite) CellAt(col, row).SetBlock(sprite); else CellAt(col, row).ResetCell();
        }

        public sealed class FullLines
        {
            public readonly List<int> rows = new List<int>();
            public readonly List<int> columns = new List<int>();
            public readonly HashSet<Vector2Int> cells = new HashSet<Vector2Int>();
            public int Count => rows.Count + columns.Count;
        }

        public FullLines CheckFullLines()
        {
            var result = new FullLines();
            for (int i = 0; i < Size; i++)
            {
                bool row = true, col = true;
                for (int j = 0; j < Size; j++) { row &= occupied[j, i]; col &= occupied[i, j]; }
                if (row) { result.rows.Add(i); for (int j = 0; j < Size; j++) result.cells.Add(new Vector2Int(j, i)); }
                if (col) { result.columns.Add(i); for (int j = 0; j < Size; j++) result.cells.Add(new Vector2Int(i, j)); }
            }
            return result;
        }

        public int ClearLines(FullLines lines)
        {
            foreach (var p in lines.cells) SetOccupied(p.x, p.y, null);
            return lines.cells.Count;
        }

        public bool CanShapeFitAnywhere(BlockShapeData shape)
        {
            for (int row = 0; row < Size; row++) for (int col = 0; col < Size; col++)
                if (IsValidPlacement(shape, new Vector2Int(col, row))) return true;
            return false;
        }

        public Vector3 PlacementCenter(BlockShapeData shape, Vector2Int anchor) => Rect.TransformPoint(new Vector3(
            Rect.rect.xMin + (anchor.x + shape.Width * .5f) * CellSize,
            Rect.rect.yMin + (anchor.y + shape.Height * .5f) * CellSize));

        public void ResetBoard()
        {
            ClearPreview(); System.Array.Clear(occupied, 0, occupied.Length);
            foreach (var cell in cells) if (cell) cell.ResetCell();
        }
    }
}
