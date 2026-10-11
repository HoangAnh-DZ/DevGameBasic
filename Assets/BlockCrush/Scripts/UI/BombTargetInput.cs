using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BlockCrush.UI
{
    public sealed class BombTargetInput : MonoBehaviour, IPointerDownHandler, IPointerMoveHandler, IPointerClickHandler, IPointerExitHandler
    {
        [SerializeField]
        GameplayBoosterController controller;
        [SerializeField]
        Image surface;
        [SerializeField]
        Image[] highlights;
        int pointer = int.MinValue;
        float flashAge = -1;
        public bool IsFlashing => gameObject.activeSelf && flashAge >= 0;

        public void Configure(GameplayBoosterController owner, Image image, Image[] cells)
        {
            controller = owner;
            surface = image;
            highlights = cells;
        }

        public void ShowSelection()
        {
            flashAge = -1;
            pointer = int.MinValue;
            surface.raycastTarget = true;
            surface.color = new Color(0, 0, 0, .12f);
            HideHighlights();
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }

        public void Hide()
        {
            flashAge = -1;
            pointer = int.MinValue;
            gameObject.SetActive(false);
        }

        public void PlayBlast(Vector2Int center)
        {
            surface.raycastTarget = false;
            surface.color = Color.clear;
            flashAge = 0;
            pointer = int.MinValue;
            gameObject.SetActive(true);
            ShowArea(center, new Color(1, .8f, .2f, .65f));
        }

        void Update()
        {
            if (!IsFlashing)
                return;
            flashAge += Time.unscaledDeltaTime;
            float alpha = .65f * (1 - Mathf.Clamp01(flashAge / .22f));
            foreach (var highlight in highlights)
                if (highlight.enabled)
                    highlight.color = new Color(1, .8f, .2f, alpha);
            if (flashAge >= .22f)
                Hide();
        }

        public bool TryGetCell(Vector2 screenPoint, Camera camera, out Vector2Int cell)
        {
            cell = default;
            var board = controller.Board;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(board.Rect, screenPoint, camera, out var local) || !board.Rect.rect.Contains(local))
                return false;
            cell = new Vector2Int(Mathf.FloorToInt((local.x - board.Rect.rect.xMin) / board.CellSize), Mathf.FloorToInt((local.y - board.Rect.rect.yMin) / board.CellSize));
            return cell.x >= 0 && cell.y >= 0 && cell.x < BlockBoard.Size && cell.y < BlockBoard.Size;
        }

        void Preview(PointerEventData data)
        {
            if (controller.BombArmed && TryGetCell(data.position, data.pressEventCamera, out var cell))
                ShowArea(cell, new Color(1, .8f, .2f, .32f));
            else
                HideHighlights();
        }

        void ShowArea(Vector2Int center, Color color)
        {
            int index = 0;
            for (int row = center.y - 1; row <= center.y + 1; row++)
                for (int col = center.x - 1; col <= center.x + 1; col++)
                {
                    var image = highlights[index++];
                    image.enabled = col >= 0 && row >= 0 && col < BlockBoard.Size && row < BlockBoard.Size;
                    if (!image.enabled)
                        continue;
                    var rect = image.rectTransform;
                    rect.anchorMin = new Vector2(col / (float)BlockBoard.Size, row / (float)BlockBoard.Size);
                    rect.anchorMax = new Vector2((col + 1f) / BlockBoard.Size, (row + 1f) / BlockBoard.Size);
                    rect.offsetMin = Vector2.one;
                    rect.offsetMax = -Vector2.one;
                    image.color = color;
                }
        }

        void HideHighlights()
        {
            foreach (var image in highlights)
                image.enabled = false;
        }

        public void OnPointerDown(PointerEventData data)
        {
            if (controller.BombArmed && data.button == PointerEventData.InputButton.Left && pointer == int.MinValue)
            {
                pointer = data.pointerId;
                Preview(data);
            }
        }

        public void OnPointerMove(PointerEventData data)
        {
            if (pointer == int.MinValue || pointer == data.pointerId)
                Preview(data);
        }

        public void OnPointerClick(PointerEventData data)
        {
            if (data.button != PointerEventData.InputButton.Left || pointer != data.pointerId)
                return;
            pointer = int.MinValue;
            if (controller.BombArmed && TryGetCell(data.position, data.pressEventCamera, out var cell))
                controller.TryUseBomb(cell);
        }

        public void OnPointerExit(PointerEventData data)
        {
            if (controller.BombArmed && (pointer == int.MinValue || pointer == data.pointerId))
            {
                pointer = int.MinValue;
                HideHighlights();
            }
        }
    }
}
