using System;
using UnityEngine;
using UnityEngine.UI;

namespace BlockCrush.UI
{
    public sealed class DailyTasksController : MonoBehaviour
    {
        [SerializeField]
        GameObject panel;
        [SerializeField]
        DailyTaskCatalog catalog;
        [SerializeField]
        Button[] claims;
        [SerializeField]
        Image[] fills;
        [SerializeField]
        GameObject[] checks;
        [SerializeField]
        Text[] progress;
        [SerializeField]
        Text countdown, completion;
        [SerializeField]
        Image totalFill;
        [SerializeField]
        RectTransform chestLid;
        [SerializeField]
        Sprite ready, waiting;
        [SerializeField]
        RewardPopupController rewards;
        [SerializeField]
        GameAudioService audioService;
        [SerializeField]
        AudioClip rewardClip;
        EconomySave state;
        int displayedRevision;
        long displayedDay;
        float nextCountdownUpdate;
        public bool IsOpen => panel && panel.activeSelf;
        public DailyTaskCatalog Catalog => catalog;

        public void Configure(GameObject view, DailyTaskCatalog config, Button[] buttons, Image[] bars, GameObject[] done, Text[] labels, Text time, Text claimed, Image total, RectTransform lid, Sprite enabledSprite, Sprite disabledSprite, RewardPopupController popup, GameAudioService audio, AudioClip sound)
        {
            panel = view;
            catalog = config;
            claims = buttons;
            fills = bars;
            checks = done;
            progress = labels;
            countdown = time;
            completion = claimed;
            totalFill = total;
            chestLid = lid;
            ready = enabledSprite;
            waiting = disabledSprite;
            rewards = popup;
            audioService = audio;
            rewardClip = sound;
        }

        public void Open()
        {
            if (IsOpen)
                return;
            panel.SetActive(true);
            transform.SetAsLastSibling();
            audioService.Feedback();
            Refresh();
        }

        public void Close()
        {
            if (!IsOpen || rewards.IsOpen)
                return;
            panel.SetActive(false);
            audioService.Feedback();
        }

        public void Claim(int index)
        {
            if (!IsOpen || rewards.IsOpen || !EconomySave.ClaimDaily(index, catalog, DateTime.UtcNow))
                return;
            audioService.PlaySound(rewardClip);
            Refresh();
            rewards.ShowReward(WheelRewardKind.Coins, catalog.coinReward);
        }

        void Update()
        {
            if (!IsOpen)
                return;
            if (state == null || displayedRevision != EconomySave.Revision)
                Refresh();
            if (state == null || Time.unscaledTime < nextCountdownUpdate)
                return;
            var now = DateTime.UtcNow;
            // Re-evaluate claims at midnight and on clock rollback, as in the saved rules.
            if (LuckyWheelSave.Day(now) != displayedDay)
                Refresh(now);
            else
                RefreshCountdown(now);
        }

        public void Refresh() => Refresh(DateTime.UtcNow);
        void Refresh(DateTime now)
        {
            if (!catalog || !catalog.Valid)
                return;
            state = EconomySave.Load(now);
            displayedRevision = EconomySave.Revision;
            displayedDay = LuckyWheelSave.Day(now);
            for (int i = 0; i < claims.Length; i++)
            {
                bool done = state.Claimed(i);
                bool can = state.dailyDay == displayedDay && state.dailyPlays >= catalog.goals[i] && !done;
                claims[i].gameObject.SetActive(!done);
                claims[i].interactable = can;
                claims[i].image.sprite = can ? ready : waiting;
                checks[i].SetActive(done);
                fills[i].fillAmount = Mathf.Clamp01((float)state.dailyPlays / catalog.goals[i]);
                progress[i].text = Mathf.Min(state.dailyPlays, catalog.goals[i]) + "/" + catalog.goals[i];
            }

            int claimed = state.ClaimedCount;
            completion.text = claimed + "/5";
            totalFill.fillAmount = claimed / 5f;
            chestLid.anchoredPosition = new Vector2(988, claimed == 5 ? -412 : -458);
            RefreshCountdown(now);
        }

        void RefreshCountdown(DateTime now)
        {
            countdown.text = CountdownText.Format(state.Remaining(now));
            nextCountdownUpdate = Time.unscaledTime + 1;
        }
    }
}
