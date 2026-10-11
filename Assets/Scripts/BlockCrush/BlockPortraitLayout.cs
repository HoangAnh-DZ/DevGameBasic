using UnityEngine;
using UnityEngine.UI;

namespace BlockCrush
{
    [ExecuteAlways]
    public sealed class BlockPortraitLayout : MonoBehaviour
    {
        [SerializeField]
        RectTransform safeArea, hud, boardArea, tray;
        [SerializeField]
        Image background;
        int lastWidth, lastHeight;
        Rect lastSafeArea;
        bool hasLayout;
        Rect lastParentRect;
#if UNITY_EDITOR
        public int LayoutApplications { get; private set; }

#endif
        public void Configure(RectTransform safe, RectTransform top, RectTransform board, RectTransform pieces, Image bg)
        {
            safeArea = safe;
            hud = top;
            boardArea = board;
            tray = pieces;
            background = bg;
        }

        void OnEnable()
        {
            Apply();
        }

        void LateUpdate()
        {
            if (!safeArea)
                return;
            var parent = safeArea.parent as RectTransform;
            if (!hasLayout || lastWidth != Screen.width || lastHeight != Screen.height || lastSafeArea != Screen.safeArea || (parent && parent.rect != lastParentRect))
                Apply();
        }

        public void Apply()
        {
            if (!safeArea || Screen.width == 0 || Screen.height == 0)
                return;
            Rect safe = Screen.safeArea;
            safeArea.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            safeArea.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
            safeArea.offsetMin = safeArea.offsetMax = Vector2.zero;
            if (lastWidth != Screen.width || lastHeight != Screen.height || lastSafeArea != safe)
            {
                lastWidth = Screen.width;
                lastHeight = Screen.height;
                lastSafeArea = safe;
                Canvas.ForceUpdateCanvases();
            }

            var size = safeArea.rect.size;
            float side = Mathf.Min(size.x * .89f, size.y * .53f);
            boardArea.anchorMin = boardArea.anchorMax = new Vector2(.5f, .555f);
            boardArea.sizeDelta = Vector2.one * side;
            boardArea.anchoredPosition = Vector2.zero;
            hud.anchorMin = new Vector2(0, 1);
            hud.anchorMax = Vector2.one;
            hud.pivot = new Vector2(.5f, 1);
            hud.sizeDelta = new Vector2(-size.x * .1f, 320);
            hud.anchoredPosition = new Vector2(0, -40);
            tray.anchorMin = new Vector2(.04f, .08f);
            tray.anchorMax = new Vector2(.96f, .275f);
            tray.offsetMin = tray.offsetMax = Vector2.zero;
            // Cover the screen while retaining the imported background's aspect ratio.
            if (background && background.sprite)
            {
                var canvasRect = (RectTransform)background.transform.parent;
                var spriteSize = background.sprite.rect.size;
                float scale = Mathf.Max(canvasRect.rect.width / spriteSize.x, canvasRect.rect.height / spriteSize.y);
                background.rectTransform.sizeDelta = spriteSize * scale;
            }

            var parent = safeArea.parent as RectTransform;
            lastParentRect = parent ? parent.rect : default;
            hasLayout = true;
#if UNITY_EDITOR
            LayoutApplications++;
#endif
        }
    }
}
