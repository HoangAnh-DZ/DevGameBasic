using UnityEngine;

namespace BlockCrush.UI
{
    [ExecuteAlways, RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaController : MonoBehaviour
    {
        RectTransform rect;
        Rect appliedSafeArea;
        int appliedWidth, appliedHeight;
#if UNITY_EDITOR
        public int LayoutApplications { get; private set; }

#endif
        void OnEnable()
        {
            rect = (RectTransform)transform;
            Apply(Screen.safeArea);
        }

        void LateUpdate()
        {
            var safe = Screen.safeArea;
            if (appliedWidth != Screen.width || appliedHeight != Screen.height || appliedSafeArea != safe)
                Apply(safe);
        }

        public void Apply(Rect safe)
        {
            if (!rect)
                rect = (RectTransform)transform;
            if (Screen.width <= 0 || Screen.height <= 0)
                return;
            rect.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            rect.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            appliedSafeArea = safe;
            appliedWidth = Screen.width;
            appliedHeight = Screen.height;
#if UNITY_EDITOR
            LayoutApplications++;
#endif
        }
    }
}
