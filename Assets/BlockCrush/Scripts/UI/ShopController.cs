using UnityEngine;
using UnityEngine.UI;

namespace BlockCrush.UI
{
    public sealed class ShopController : MonoBehaviour
    {
        [SerializeField] GameObject panel, bundles, items;
        [SerializeField] ShopCatalog catalog;
        [SerializeField] RewardPopupController rewards;
        [SerializeField] GameAudioService audioService;
        [SerializeField] MoreSettingsController information;
        [SerializeField] AudioClip rewardClip;
        [SerializeField] Text status, ownedLabel, bundleTabLabel, itemTabLabel;
        [SerializeField] Button removeAdsButton;
        public bool IsOpen => panel && panel.activeSelf;
        public bool BundlesVisible => bundles.activeSelf;
        public ShopCatalog Catalog => catalog;
        public void Configure(GameObject view, GameObject bundleList, GameObject itemList, ShopCatalog config, RewardPopupController popup,
            GameAudioService audio, MoreSettingsController info, AudioClip sound, Text message, Button noAds, Text noAdsText, Text bundleTab, Text itemTab)
        { panel=view; bundles=bundleList; items=itemList; catalog=config; rewards=popup; audioService=audio; information=info; rewardClip=sound; status=message; removeAdsButton=noAds; ownedLabel=noAdsText; bundleTabLabel=bundleTab; itemTabLabel=itemTab; }
        public void Open()
        {
            if(IsOpen) return; panel.SetActive(true); transform.SetAsLastSibling(); ShowBundles();
            foreach(var list in new[]{bundles,items})
            {
                var scroll=list.GetComponent<ScrollRect>(); scroll.StopMovement(); scroll.verticalNormalizedPosition=1;
            }
            Refresh();
        }
        public void Close() { if(!IsOpen || rewards.IsOpen) return; panel.SetActive(false); audioService.Feedback(); }
        public void ShowBundles() { if(rewards && rewards.IsOpen) return; bundles.SetActive(true); items.SetActive(false); SelectTabs(true); audioService.Feedback(); }
        public void ShowItems() { if(rewards && rewards.IsOpen) return; bundles.SetActive(false); items.SetActive(true); SelectTabs(false); audioService.Feedback(); }
        void SelectTabs(bool first) { if(bundleTabLabel) bundleTabLabel.color=first?Color.yellow:Color.white; if(itemTabLabel) itemTabLabel.color=first?Color.white:Color.yellow; }
        public void Buy(int index)
        {
            if(!IsOpen || rewards.IsOpen || !catalog || !catalog.Valid || index<0 || index>=catalog.offers.Length) return;
            var offer=catalog.offers[index]; if(offer.removeAds && EconomySave.Load().removeAds) return;
            audioService.Feedback();
#if UNITY_EDITOR
            rewards.ConfirmDemo(offer,()=>CompleteDemo(index));
#else
            information.ShowInformation("Shop", "Real purchases require a connected store. Demo purchases are available in Unity Editor only.");
#endif
        }
        void CompleteDemo(int index)
        {
            if(!IsOpen || index<0 || index>=catalog.offers.Length) return;
            var offer=catalog.offers[index]; if(!EconomySave.PurchaseDemo(offer)) return;
            audioService.PlaySound(rewardClip); Refresh();
            rewards.ShowReward(offer.boosters>0&&!offer.bundle?WheelRewardKind.Booster:WheelRewardKind.Coins,
                offer.coins>0?offer.coins:offer.boosters, "DEMO · "+offer.Contents);
        }
        public void Refresh()
        {
            bool owned=EconomySave.Load().removeAds; removeAdsButton.interactable=!owned;
            ownedLabel.text=owned?"Owned":catalog.offers[5].price;
#if UNITY_EDITOR
            status.text="DEMO • No real payment";
#else
            status.text="Store not connected";
#endif
        }
    }
}
