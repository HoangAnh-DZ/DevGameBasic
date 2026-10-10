using UnityEngine;
using UnityEngine.UI;

namespace BlockCrush.UI
{
    public sealed class WalletHud : MonoBehaviour
    {
        [SerializeField] Text coins, bombs, boosters, reverses;
        float next;
        public bool Valid => coins && ((!bombs && !boosters && !reverses) || (bombs && boosters && reverses));
        public void Configure(Text money, Text bomb, Text booster, Text reverse) { coins=money; bombs=bomb; boosters=booster; reverses=reverse; }
        void OnEnable() => Refresh();
        void Update() { if(Time.unscaledTime>=next) { next=Time.unscaledTime+.25f; Refresh(); } }
        public void Refresh()
        {
            if(!coins && !bombs && !boosters && !reverses) return;
            var state=EconomySave.Load();
            if(coins) coins.text=state.coins.ToString("N0"); if(bombs) bombs.text=state.bombs.ToString();
            if(boosters) boosters.text=state.boosters.ToString(); if(reverses) reverses.text=state.reverses.ToString();
        }
    }
}
