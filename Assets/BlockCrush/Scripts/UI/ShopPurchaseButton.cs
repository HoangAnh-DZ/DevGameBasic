using UnityEngine;

namespace BlockCrush.UI
{
    public sealed class ShopPurchaseButton : MonoBehaviour
    {
        [SerializeField]
        ShopController controller;
        [SerializeField]
        int index;
        public void Configure(ShopController owner, int offer)
        {
            controller = owner;
            index = offer;
        }

        public void Buy() => controller.Buy(index);
    }
}
