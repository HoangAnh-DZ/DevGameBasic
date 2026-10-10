using UnityEngine;

namespace BlockCrush.UI
{
    // Keep the original artwork proportions; only the scroll window changes with screen height.
    [ExecuteAlways, RequireComponent(typeof(RectTransform))]
    public sealed class ShopResponsiveLayout : MonoBehaviour
    {
        const float Width = 1432;
        [SerializeField]
        RectTransform bundleViewport, itemViewport, bundleTab, itemTab, notice;
        bool applying;
        bool hasLayout;
        RectTransform lastParent;
        Rect lastParentRect;
#if UNITY_EDITOR
        public int LayoutApplications { get; private set; }

#endif
        public void Configure(RectTransform bundles, RectTransform items, RectTransform first, RectTransform second, RectTransform status)
        {
            bundleViewport = bundles;
            itemViewport = items;
            bundleTab = first;
            itemTab = second;
            notice = status;
            Apply();
        }

        void OnEnable() => Apply();
        void OnRectTransformDimensionsChange()
        {
            if (isActiveAndEnabled)
                Apply();
        }

        void LateUpdate()
        {
            var parent = transform.parent as RectTransform;
            if (!hasLayout || parent != lastParent || (parent && parent.rect != lastParentRect))
                Apply();
        }

        public void Apply()
        {
            var rect = (RectTransform)transform;
            var parent = rect.parent as RectTransform;
            if (applying || !parent || !bundleViewport || !itemViewport || !bundleTab || !itemTab || !notice || parent.rect.width <= 0 || parent.rect.height <= 0)
                return;
            applying = true;
            try
            {
                float scale = parent.rect.width / Width, height = parent.rect.height / scale;
                rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * .5f;
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = new Vector2(Width, height);
                rect.localScale = Vector3.one * scale;
                Place(bundleViewport, 0, 544, Width, Mathf.Max(1, height - 980));
                Place(itemViewport, 0, 544, Width, Mathf.Max(1, height - 980));
                Place(bundleTab, 92, height - 388, 600, 236);
                Place(itemTab, 740, height - 388, 600, 236);
                Place(notice, 92, height - 100, 1248, 56);
                lastParent = parent;
                lastParentRect = parent.rect;
                hasLayout = true;
#if UNITY_EDITOR
                LayoutApplications++;
#endif
            }
            finally
            {
                applying = false;
            }
        }

        static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }
    }
}
