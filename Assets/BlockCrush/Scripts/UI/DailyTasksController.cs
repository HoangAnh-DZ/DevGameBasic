using System;
using UnityEngine;
using UnityEngine.UI;

namespace BlockCrush.UI
{
    public sealed class DailyTasksController : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] DailyTaskCatalog catalog;
        [SerializeField] Button[] claims;
        [SerializeField] Image[] fills;
        [SerializeField] GameObject[] checks;
        [SerializeField] Text[] progress;
        [SerializeField] Text countdown, completion;
        [SerializeField] Image totalFill;
        [SerializeField] RectTransform chestLid;
        [SerializeField] Sprite ready, waiting;
        [SerializeField] RewardPopupController rewards;
        [SerializeField] GameAudioService audioService;
        [SerializeField] AudioClip rewardClip;
        float next;
        public bool IsOpen => panel && panel.activeSelf;
        public DailyTaskCatalog Catalog => catalog;
        public void Configure(GameObject view, DailyTaskCatalog config, Button[] buttons, Image[] bars, GameObject[] done,
            Text[] labels, Text time, Text claimed, Image total, RectTransform lid, Sprite enabledSprite, Sprite disabledSprite,
            RewardPopupController popup, GameAudioService audio, AudioClip sound)
        { panel=view; catalog=config; claims=buttons; fills=bars; checks=done; progress=labels; countdown=time; completion=claimed; totalFill=total; chestLid=lid; ready=enabledSprite; waiting=disabledSprite; rewards=popup; audioService=audio; rewardClip=sound; }
        public void Open() { if(IsOpen) return; panel.SetActive(true); transform.SetAsLastSibling(); audioService.Feedback(); Refresh(); }
        public void Close() { if(!IsOpen || rewards.IsOpen) return; panel.SetActive(false); audioService.Feedback(); }
        public void Claim(int index)
        {
            if(!IsOpen || rewards.IsOpen || !EconomySave.ClaimDaily(index,catalog,DateTime.UtcNow)) return;
            audioService.PlaySound(rewardClip); Refresh(); rewards.ShowReward(WheelRewardKind.Coins,catalog.coinReward);
        }
        void Update() { if(IsOpen && Time.unscaledTime>=next) { next=Time.unscaledTime+.25f; Refresh(); } }
        public void Refresh()
        {
            if(!catalog || !catalog.Valid) return; var state=EconomySave.Load();
            for(int i=0;i<claims.Length;i++)
            {
                bool done=state.Claimed(i), can=state.dailyDay==LuckyWheelSave.Day(DateTime.UtcNow)&&state.dailyPlays>=catalog.goals[i]&&!done;
                claims[i].gameObject.SetActive(!done); claims[i].interactable=can; claims[i].GetComponent<Image>().sprite=can?ready:waiting;
                checks[i].SetActive(done); fills[i].fillAmount=Mathf.Clamp01((float)state.dailyPlays/catalog.goals[i]);
                progress[i].text=Mathf.Min(state.dailyPlays,catalog.goals[i])+"/"+catalog.goals[i];
            }
            completion.text=state.ClaimedCount+"/5"; totalFill.fillAmount=state.ClaimedCount/5f;
            chestLid.anchoredPosition=new Vector2(988,state.ClaimedCount==5?-412:-458);
            var left=state.Remaining(DateTime.UtcNow);
            countdown.text=left.TotalHours>99?"99:59:59":string.Format("{0:00}:{1:00}:{2:00}",(int)left.TotalHours,left.Minutes,left.Seconds);
        }
    }
}
