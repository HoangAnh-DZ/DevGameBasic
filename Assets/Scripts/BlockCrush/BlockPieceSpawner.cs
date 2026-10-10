using System.Collections.Generic;
using UnityEngine;

namespace BlockCrush
{
    public sealed class BlockPieceSpawner : MonoBehaviour
    {
        [SerializeField]
        RectTransform[] slots;
        [SerializeField]
        Sprite[] blockSprites;
        [SerializeField]
        BlockGameManager manager;
        readonly BlockPiece[] pieces = new BlockPiece[3];
        public IReadOnlyList<BlockPiece> Pieces => pieces;
        public Sprite[] BlockSprites => blockSprites;
        public RectTransform[] Slots => slots;

        public int RemainingCount
        {
            get
            {
                int n = 0;
                foreach (var p in pieces)
                    if (p)
                        n++;
                return n;
            }
        }

        public int SetsSpawned { get; private set; }

        public void Configure(RectTransform[] pieceSlots, Sprite[] sprites, BlockGameManager game)
        {
            slots = pieceSlots;
            blockSprites = sprites;
            manager = game;
        }

        BlockShapeData RandomShape(HashSet<string> chosen)
        {
            int total = 0;
            foreach (var shape in BlockShapeData.Database)
                if (!chosen.Contains(shape.id))
                    total += shape.weight;
            int value = Random.Range(0, total);
            foreach (var shape in BlockShapeData.Database)
            {
                if (chosen.Contains(shape.id))
                    continue;
                value -= shape.weight;
                if (value < 0)
                    return shape;
            }

            return BlockShapeData.Database[0];
        }

        public void SpawnSet()
        {
            ClearPieces();
            SetsSpawned++;
            var chosen = new HashSet<string>();
            int previousColor = -1;
            for (int i = 0; i < 3; i++)
            {
                var shape = manager.Rules == null ? RandomShape(chosen) : manager.Rules.SelectShape(chosen, manager.Board);
                chosen.Add(shape.id);
                int color = Random.Range(0, blockSprites.Length);
                if (blockSprites.Length > 1 && color == previousColor)
                    color = (color + 1) % blockSprites.Length;
                previousColor = color;
                SpawnPiece(i, shape, blockSprites[color]);
            }
        }

        public BlockPiece SpawnPiece(int slot, BlockShapeData shape, Sprite sprite)
        {
            if (pieces[slot])
            {
                pieces[slot].CancelDrag();
                pieces[slot].gameObject.SetActive(false);
                Destroy(pieces[slot].gameObject);
            }

            var go = new GameObject("Piece_" + shape.id, typeof(RectTransform), typeof(CanvasGroup), typeof(BlockPiece));
            go.transform.SetParent(slots[slot], false);
            float unit = Mathf.Min(manager.Board.CellSize * .56f, slots[slot].rect.width / 5.2f);
            var piece = go.GetComponent<BlockPiece>();
            piece.Initialize(shape, sprite, slot, manager, unit);
            pieces[slot] = piece;
            return piece;
        }

        public bool Contains(BlockPiece piece) => piece && pieces[piece.SlotIndex] == piece;
        public void MarkUsed(BlockPiece piece)
        {
            if (!Contains(piece))
                return;
            pieces[piece.SlotIndex] = null;
            piece.gameObject.SetActive(false);
            Destroy(piece.gameObject);
        }

        public void ClearPieces()
        {
            for (int i = 0; i < pieces.Length; i++)
            {
                if (pieces[i])
                {
                    pieces[i].CancelDrag();
                    pieces[i].gameObject.SetActive(false);
                    Destroy(pieces[i].gameObject);
                }

                pieces[i] = null;
            }
        }
    }
}
