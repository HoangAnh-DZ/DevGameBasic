using UnityEngine;

namespace BlockCrush.UI
{
    public sealed class AdventureLevelButton : MonoBehaviour
    {
        [SerializeField] AdventurePanelController map;
        [SerializeField] int level;
        public void Configure(AdventurePanelController controller, int number) { map = controller; level = number; }
        public void Select() => map.Select(level);
    }
}
