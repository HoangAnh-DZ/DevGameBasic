using System;
using UnityEngine;
using UnityEngine.UI;

namespace BlockCrush.UI
{
    public sealed class RewardPopupController : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] Image artwork;
        [SerializeField] Sprite[] rewardIcons;
        [SerializeField] Text heading, amount, details, buttonLabel, coins;
        [SerializeField] Button cancel;
        [SerializeField] GameAudioService audioService;
        [SerializeField] FigmaLayoutFitter layout;
        Action callback;
        bool confirmation;
        float elapsed;
        public bool IsOpen => panel && panel.activeSelf;
        public bool IsConfirmation => IsOpen && confirmation;
        public string Amount => amount.text;
        public void Configure(GameObject view, Image icon, Sprite[] icons, Text title, Text value, Text detail,
            Text buttonText, Text balance, Button cancelButton, GameAudioService audio, FigmaLayoutFitter fitter)
        { panel=view; artwork=icon; rewardIcons=icons; heading=title; amount=value; details=detail; buttonLabel=buttonText; coins=balance; cancel=cancelButton; audioService=audio; layout=fitter; }
        public void ShowReward(WheelRewardKind kind, int quantity, string detail = "", Action acknowledged = null, RectTransform bounds = null)
        {
            confirmation=false; callback=acknowledged; heading.text="You get:"; buttonLabel.text="OK";
            artwork.sprite=rewardIcons[(int)kind]; amount.text=quantity > 0 ? quantity.ToString() : "";
            details.text=string.IsNullOrEmpty(detail) && kind!=WheelRewardKind.Coins ? kind.ToString() : detail;
            cancel.gameObject.SetActive(false); Show(bounds);
        }
        public void ConfirmDemo(ShopOffer offer, Action buy)
        {
            confirmation=true; callback=buy; heading.text="Demo purchase"; buttonLabel.text="Buy demo";
            artwork.sprite=rewardIcons[offer.boosters>0&&!offer.bundle?2:0];
            amount.text=offer.price; details.text=offer.title+"\n"+offer.Contents+"\nNo real payment";
            cancel.gameObject.SetActive(true); Show(null);
        }
        void Show(RectTransform bounds)
        {
            coins.text=EconomySave.Load().coins.ToString("N0"); panel.SetActive(true); transform.SetAsLastSibling(); elapsed=0;
            artwork.rectTransform.localScale=Vector3.one;
            layout.FitInside(bounds);
        }
        public void Confirm()
        {
            if(!IsOpen) return;
            var action=callback; callback=null; panel.SetActive(false); confirmation=false; audioService.Feedback(); action?.Invoke();
        }
        public void Back()
        {
            if(!IsOpen) return;
            if(confirmation) { callback=null; panel.SetActive(false); confirmation=false; audioService.Feedback(); }
            else Confirm();
        }
        void Update()
        {
            if(!IsOpen) return; elapsed+=Time.unscaledDeltaTime;
            artwork.rectTransform.localScale=Vector3.one*(1+.08f*Mathf.Sin(Mathf.Clamp01(elapsed/.35f)*Mathf.PI));
        }
    }
}
