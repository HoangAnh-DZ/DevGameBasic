using UnityEngine;

namespace BlockCrush.UI
{
    [ExecuteAlways, RequireComponent(typeof(RectTransform))]
    public sealed class FigmaLayoutFitter : MonoBehaviour
    {
        [SerializeField] Vector2 designSize;
        [SerializeField, Range(.1f, 1)] float availableSpace = 1;
        RectTransform containment;
        float containmentSpace = 1;
        readonly Vector3[] corners = new Vector3[4];
        bool applying;
        public void Configure(Vector2 size, float margin = 1) { designSize = size; availableSpace = margin; Apply(); }
        public void FitInside(RectTransform bounds, float margin = .9f)
        { containment = bounds; containmentSpace = Mathf.Clamp(margin, .1f, 1); Apply(); }
        void OnEnable() => Apply();
        void OnRectTransformDimensionsChange() { if (isActiveAndEnabled) Apply(); }
        void LateUpdate() => Apply();
        public void Apply()
        {
            var rect = (RectTransform)transform;
            var parent = rect.parent as RectTransform;
            if (applying || !parent || designSize.x <= 0 || designSize.y <= 0) return;
            applying = true;
            try
            {
                rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * .5f;
                rect.sizeDelta = designSize;
                var bounds = parent.rect;
                float space = availableSpace;
                if (containment && containment.gameObject.activeInHierarchy)
                {
                    // The wheel's own fitter may run later in the frame after a screen resize.
                    var hostFitter = containment.GetComponentInParent<FigmaLayoutFitter>();
                    if (hostFitter && hostFitter != this) hostFitter.Apply();
                    containment.GetWorldCorners(corners);
                    var min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
                    var max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
                    foreach (var corner in corners)
                    {
                        Vector2 point = parent.InverseTransformPoint(corner);
                        min = Vector2.Min(min, point); max = Vector2.Max(max, point);
                    }
                    bounds = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
                    space = containmentSpace;
                }
                rect.anchoredPosition = bounds.center - parent.rect.center;
                float scale = Mathf.Min(bounds.width / designSize.x, bounds.height / designSize.y) * space;
                rect.localScale = Vector3.one * Mathf.Max(.01f, scale);
            }
            finally { applying = false; }
        }
    }
}
