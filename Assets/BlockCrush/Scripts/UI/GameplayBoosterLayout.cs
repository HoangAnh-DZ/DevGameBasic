using UnityEngine;

namespace BlockCrush.UI
{
    // Visual room for booster controls; board size, placement coordinates and rules are untouched.
    [ExecuteAlways, DefaultExecutionOrder(200)]
    public sealed class GameplayBoosterLayout : MonoBehaviour
    {
        [SerializeField]
        RectTransform toolbar, tray;
        [SerializeField]
        BlockPieceSpawner spawner;
        [SerializeField]
        bool reserveFooter = true;
        public void Configure(RectTransform bar, RectTransform pieces, BlockPieceSpawner source, bool footer = true)
        {
            toolbar = bar;
            tray = pieces;
            spawner = source;
            reserveFooter = footer;
        }

        void LateUpdate()
        {
            if (!toolbar || !tray || !spawner)
                return;
            var safe = tray.parent as RectTransform;
            if (!safe || safe.rect.height <= 0)
                return;
            if (reserveFooter)
            {
                var minimum = tray.anchorMin;
                minimum.y = Mathf.Clamp((toolbar.rect.height + 24) / safe.rect.height, .08f, Mathf.Max(.08f, tray.anchorMax.y - .02f));
                if (tray.anchorMin != minimum)
                    tray.anchorMin = minimum;
            }

            foreach (var piece in spawner.Pieces)
            {
                if (!piece || piece.IsDragging)
                    continue;
                var rect = (RectTransform)piece.transform;
                var slot = rect.parent as RectTransform;
                if (!slot || rect.sizeDelta.x <= 0 || rect.sizeDelta.y <= 0)
                    continue;
                float scale = Mathf.Min(1, Mathf.Max(.1f, slot.rect.height - 16) / rect.sizeDelta.y, Mathf.Max(.1f, slot.rect.width - 16) / rect.sizeDelta.x);
                var desired = Vector3.one * scale;
                if (rect.localScale != desired)
                    rect.localScale = desired;
            }
        }
    }
}
