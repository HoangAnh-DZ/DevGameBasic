using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BlockCrush
{
    [RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
    public sealed class BlockPiece : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, ICancelHandler
    {
        public BlockShapeData Shape { get; private set; }
        public Sprite BlockSprite { get; private set; }
        public int SlotIndex { get; private set; }
        public bool IsDragging { get; private set; }
        public int DragEvents { get; private set; }

        BlockGameManager manager;
        RectTransform rect;
        Transform originalParent;
        Vector2 originalPosition;
        Vector2 originalSize;
        Vector3 originalScale;
        CanvasGroup group;
        int pointerId;
        Vector2 lift;
        public void Initialize(BlockShapeData shape, Sprite sprite, int index, BlockGameManager game, float unitSize)
        {
            Shape = shape;
            BlockSprite = sprite;
            SlotIndex = index;
            manager = game;
            rect = (RectTransform)transform;
            group = GetComponent<CanvasGroup>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(shape.Width, shape.Height) * unitSize;
            var hit = gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            hit.raycastTarget = true;
            foreach (var p in shape.cells)
            {
                var child = new GameObject("Block_" + p.x + "_" + p.y, typeof(RectTransform), typeof(Image));
                var rt = (RectTransform)child.transform;
                rt.SetParent(rect, false);
                rt.anchorMin = new Vector2(p.x / (float)shape.Width, p.y / (float)shape.Height);
                rt.anchorMax = new Vector2((p.x + 1f) / shape.Width, (p.y + 1f) / shape.Height);
                rt.offsetMin = Vector2.one;
                rt.offsetMax = -Vector2.one;
                var image = child.GetComponent<Image>();
                image.sprite = sprite;
                image.raycastTarget = false;
            }
        }

        public void OnBeginDrag(PointerEventData data)
        {
            if (!manager.CanInteract || IsDragging)
                return;
            pointerId = data.pointerId;
            IsDragging = true;
            DragEvents++;
            originalParent = rect.parent;
            originalPosition = rect.anchoredPosition;
            originalSize = rect.sizeDelta;
            originalScale = rect.localScale;
            rect.SetParent(manager.DragLayer, false);
            rect.SetAsLastSibling();
            rect.localScale = Vector3.one;
            rect.sizeDelta = new Vector2(Shape.Width, Shape.Height) * manager.Board.CellSize;
            group.blocksRaycasts = false;
            bool touch = data is UnityEngine.InputSystem.UI.ExtendedPointerEventData extended && extended.pointerType == UnityEngine.InputSystem.UI.UIPointerType.Touch;
            lift = touch ? Vector2.up * manager.Board.CellSize * .85f : Vector2.zero;
            MoveToPointer(data);
            UpdatePreview();
        }

        void MoveToPointer(PointerEventData data)
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(manager.DragLayer, data.position, data.pressEventCamera, out var point))
                rect.anchoredPosition = point + lift;
        }

        void UpdatePreview()
        {
            if (manager.Board.TryGetAnchor(Shape, rect.position, out var anchor))
                manager.Board.ShowPreview(Shape, anchor, BlockSprite);
            else
                manager.Board.ClearPreview();
        }

        public void OnDrag(PointerEventData data)
        {
            if (!IsDragging || data.pointerId != pointerId)
                return;
            if (!manager.CanInteract)
            {
                CancelDrag();
                return;
            }

            DragEvents++;
            MoveToPointer(data);
            UpdatePreview();
        }

        public void OnEndDrag(PointerEventData data)
        {
            if (!IsDragging || data.pointerId != pointerId)
                return;
            MoveToPointer(data);
            manager.Board.ClearPreview();
            Vector2Int anchor = default;
            bool valid = manager.CanInteract && manager.Board.TryGetAnchor(Shape, rect.position, out anchor);
            IsDragging = false;
            if (valid && manager.TryPlacePiece(this, anchor))
                return;
            if (manager.CanInteract)
                manager.Feedback?.InvalidPlacement();
            ReturnToSlot();
        }

        public void OnCancel(BaseEventData data) => CancelDrag();
        public void CancelDrag()
        {
            if (!IsDragging)
                return;
            IsDragging = false;
            manager.Board.ClearPreview();
            ReturnToSlot();
        }

        void ReturnToSlot()
        {
            rect.SetParent(originalParent, false);
            rect.anchoredPosition = originalPosition;
            rect.sizeDelta = originalSize;
            rect.localScale = originalScale;
            group.blocksRaycasts = true;
        }

        void OnApplicationFocus(bool focused)
        {
            if (!focused)
                CancelDrag();
        }

        void OnApplicationPause(bool paused)
        {
            if (paused)
                CancelDrag();
        }

        void OnDisable()
        {
            if (manager && IsDragging)
                CancelDrag();
        }
    }
}
