using UnityEngine;

namespace BlockCrush.UI
{
    [ExecuteAlways, RequireComponent(typeof(RectTransform))]
    public sealed class FigmaLayoutFitter : MonoBehaviour
    {
        [SerializeField]
        Vector2 designSize;
        [SerializeField, Range(.1f, 1)]
        float availableSpace = 1;
        [SerializeField]
        Vector2 contentCenterOffset;
        RectTransform containment;
        float containmentSpace = 1;
        readonly Vector3[] corners = new Vector3[4];
        bool applying;
        bool hasLayout, hadContainment;
        RectTransform rect, lastParent;
        FigmaLayoutFitter hostFitter;
        Transform containmentParent;
        Rect lastParentRect, lastContainmentRect;
        Matrix4x4 lastContainmentMatrix, lastParentMatrix;
        Vector2 lastDesignSize, lastCenterOffset;
        float lastSpace;
#if UNITY_EDITOR
        public int LayoutApplications { get; private set; }

#endif
        public void Configure(Vector2 size, float margin = 1, Vector2? centerOffset = null)
        {
            designSize = size;
            availableSpace = margin;
            contentCenterOffset = centerOffset ?? Vector2.zero;
            Apply();
        }

        public void FitInside(RectTransform bounds, float margin = .9f)
        {
            containment = bounds;
            hostFitter = null;
            containmentParent = null;
            containmentSpace = Mathf.Clamp(margin, .1f, 1);
            Apply();
        }

        void OnEnable() => Apply();
        void OnRectTransformDimensionsChange()
        {
            if (isActiveAndEnabled)
                Apply();
        }

        void LateUpdate() => UpdateLayout();
        public void Apply()
        {
            if (applying)
                return;
            hasLayout = false;
            UpdateLayout();
        }

        void UpdateLayout()
        {
            if (!rect)
                rect = (RectTransform)transform;
            var parent = rect.parent as RectTransform;
            if (applying || !parent || designSize.x <= 0 || designSize.y <= 0)
                return;
            applying = true;
            try
            {
                bool contained = containment && containment.gameObject.activeInHierarchy;
                if (contained)
                {
                    // The wheel's own fitter may run later in the frame after a screen resize.
                    if (containmentParent != containment.parent || !hostFitter)
                    {
                        containmentParent = containment.parent;
                        hostFitter = containment.GetComponentInParent<FigmaLayoutFitter>();
                    }

                    if (hostFitter && hostFitter != this)
                        hostFitter.UpdateLayout();
                }

                var parentRect = parent.rect;
                var containmentRect = contained ? containment.rect : default;
                var containmentMatrix = contained ? containment.localToWorldMatrix : default;
                var parentMatrix = contained ? parent.worldToLocalMatrix : default;
                float space = contained ? containmentSpace : availableSpace;
                if (hasLayout && parent == lastParent && parentRect == lastParentRect && contained == hadContainment && containmentRect == lastContainmentRect && containmentMatrix == lastContainmentMatrix && parentMatrix == lastParentMatrix && designSize == lastDesignSize && contentCenterOffset == lastCenterOffset && space == lastSpace)
                    return;
                var bounds = parentRect;
                if (contained)
                {
                    containment.GetWorldCorners(corners);
                    var min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
                    var max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
                    foreach (var corner in corners)
                    {
                        Vector2 point = parent.InverseTransformPoint(corner);
                        min = Vector2.Min(min, point);
                        max = Vector2.Max(max, point);
                    }

                    bounds = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
                }

                rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * .5f;
                rect.sizeDelta = designSize;
                float scale = Mathf.Min(bounds.width / designSize.x, bounds.height / designSize.y) * space;
                rect.localScale = Vector3.one * Mathf.Max(.01f, scale);
                rect.anchoredPosition = bounds.center - parentRect.center - contentCenterOffset * rect.localScale.x;
                lastParent = parent;
                lastParentRect = parentRect;
                hadContainment = contained;
                lastContainmentRect = containmentRect;
                lastContainmentMatrix = containmentMatrix;
                lastParentMatrix = parentMatrix;
                lastDesignSize = designSize;
                lastCenterOffset = contentCenterOffset;
                lastSpace = space;
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
    }
}
