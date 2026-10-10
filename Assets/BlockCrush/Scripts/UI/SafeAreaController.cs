using UnityEngine;

namespace BlockCrush.UI
{
    [ExecuteAlways, RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaController : MonoBehaviour
    {
        RectTransform rect;
        void OnEnable() { rect = (RectTransform)transform; Apply(Screen.safeArea); }
        void LateUpdate() => Apply(Screen.safeArea);
        public void Apply(Rect safe)
        {
            if (!rect) rect = (RectTransform)transform;
            if (Screen.width <= 0 || Screen.height <= 0) return;
            rect.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            rect.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
