using UnityEngine;
using UnityEngine.UI;

namespace BlockCrush
{
    [RequireComponent(typeof(RectTransform), typeof(Image))]
    public sealed class BlockCell : MonoBehaviour
    {
        [SerializeField] int row;
        [SerializeField] int column;
        [SerializeField] Image blockImage;
        public int Row => row;
        public int Column => column;
        public bool Occupied { get; private set; }
        public bool IsPreview { get; private set; }
        public Sprite CurrentSprite => Occupied ? blockImage.sprite : null;

        public void Configure(int col, int r)
        {
            column = col; row = r; blockImage = GetComponent<Image>();
            blockImage.raycastTarget = false; ResetCell();
        }

        public void SetBlock(Sprite sprite)
        {
            Occupied = true; IsPreview = false;
            blockImage.sprite = sprite; blockImage.color = Color.white; blockImage.enabled = true;
        }

        public void Preview(Sprite sprite)
        {
            if (Occupied) return;
            IsPreview = true; blockImage.sprite = sprite;
            blockImage.color = new Color(1, 1, 1, .48f); blockImage.enabled = true;
        }

        public void ClearPreview() { if (IsPreview) ResetCell(); }
        public void ClearFeedback(float amount)
        {
            blockImage.color = new Color(1, 1, 1, 1 - amount);
            transform.localScale = Vector3.one * (1 - amount * .18f);
        }

        public void ResetCell()
        {
            if (!blockImage) blockImage = GetComponent<Image>();
            Occupied = false; IsPreview = false;
            blockImage.enabled = false; blockImage.sprite = null;
            blockImage.color = Color.white; transform.localScale = Vector3.one;
        }
    }
}
