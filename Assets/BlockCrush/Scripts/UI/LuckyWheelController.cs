using System;
using UnityEngine;
using UnityEngine.UI;

namespace BlockCrush.UI
{
    public sealed class LuckyWheelController : MonoBehaviour
    {
        [SerializeField]
        GameObject panel;
        [SerializeField]
        RectTransform disc;
        [SerializeField]
        RectTransform rewardBounds;
        [SerializeField]
        RectTransform[] slots;
        [SerializeField]
        Image[] tokens;
        [SerializeField]
        Button spinButton, closeButton;
        [SerializeField]
        GameObject spinLabel, timerView;
        [SerializeField]
        Text countdown, result, availability;
        [SerializeField]
        LuckyWheelCatalog catalog;
        [SerializeField]
        GameAudioService audioService;
        [SerializeField]
        AudioClip tick, stopped, recorded;
        [SerializeField]
        RewardPopupController rewards;
        LuckyWheelSave save;
        readonly System.Random random = new System.Random();
        float elapsed, startAngle, distance, tickAt, celebration = -1;
        int boundary;
        bool rewardPlayed;
        long displayedDay;
        float nextCountdownUpdate;
        public bool IsOpen => panel && panel.activeSelf;
        public bool IsSpinning { get; private set; }
        public LuckyWheelSave State => save;
        public RectTransform Disc => disc;
        public LuckyWheelCatalog Catalog => catalog;

        public void ConfigureRewards(RewardPopupController view, RectTransform bounds)
        {
            rewards = view;
            rewardBounds = bounds;
        }

#if UNITY_EDITOR
        public int TickSounds { get; private set; }
        public int StopSounds { get; private set; }
        public int RewardSounds { get; private set; }

#endif
        public void Configure(GameObject view, RectTransform wheel, RectTransform[] rewardSlots, Image[] spinTokens, Button spin, Button close, GameObject label, GameObject timer, Text time, Text message, Text status, LuckyWheelCatalog config, GameAudioService audio, AudioClip tickClip, AudioClip stopClip, AudioClip rewardClip)
        {
            panel = view;
            disc = wheel;
            slots = rewardSlots;
            tokens = spinTokens;
            spinButton = spin;
            closeButton = close;
            spinLabel = label;
            timerView = timer;
            countdown = time;
            result = message;
            availability = status;
            catalog = config;
            audioService = audio;
            tick = tickClip;
            stopped = stopClip;
            recorded = rewardClip;
        }

        public void Open()
        {
            if (IsOpen)
                return;
            IsSpinning = false;
            save = LuckyWheelSave.Load();
            panel.SetActive(true);
            transform.SetAsLastSibling();
            audioService.Feedback();
            celebration = -1;
            rewardPlayed = false;
            if (save.pending != null)
            {
                Land(save.pending.slot);
                save.Record();
                ShowReceipt(false);
            }
            else if (save.last != null)
            {
                Land(save.last.slot);
                EconomySave.GrantWheel(save.last, save.usedDay);
                if (save.acknowledgedId != save.last.id)
                    ShowReceipt(false);
            }
            else
            {
                Land(0);
                result.text = "";
            }

            Refresh();
        }

        public void Close()
        {
            if (!IsOpen || IsSpinning || (rewards && rewards.IsOpen))
                return;
            panel.SetActive(false);
            celebration = -1;
            ResetSlots();
            audioService.Feedback();
        }

        public void Spin()
        {
            if (!IsOpen || IsSpinning || (rewards && rewards.IsOpen) || !catalog || !catalog.Valid)
                return;
            // Re-read durable state: duplicate listeners/controllers cannot reserve a second spin.
            save = LuckyWheelSave.Load();
            if (!save.Begin(catalog, DateTime.UtcNow, random.NextDouble()))
            {
                Refresh();
                return;
            }

            IsSpinning = true;
            elapsed = 0;
            startAngle = disc.localEulerAngles.z;
            float target = save.pending.slot * 45;
            distance = -360 * catalog.turns - Mathf.Repeat(startAngle - target, 360);
            boundary = 0;
            tickAt = 0;
            celebration = -1;
            rewardPlayed = false;
            ResetSlots();
            result.text = "";
            audioService.Feedback();
            Refresh();
        }

        void Update()
        {
            if (!IsOpen)
                return;
            if (IsSpinning)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / catalog.duration);
                float eased = t * t * t * (10 + t * (-15 + 6 * t));
                float travel = distance * eased;
                disc.localRotation = Quaternion.Euler(0, 0, startAngle + travel);
                int crossed = Mathf.FloorToInt(Mathf.Abs(travel) / 45);
                if (crossed != boundary && Time.unscaledTime >= tickAt && t < .985f)
                {
                    boundary = crossed;
                    tickAt = Time.unscaledTime + .08f;
                    Cue(tick, 0);
                }

                if (t >= 1)
                {
                    Land(save.pending.slot);
                    IsSpinning = false;
                    save.Record();
                    ShowReceipt(true);
                    Cue(stopped, 1);
                    Refresh();
                }
            }

            if (celebration >= 0)
            {
                celebration += Time.unscaledDeltaTime;
                if (!rewardPlayed && celebration >= .2f)
                {
                    rewardPlayed = true;
                    Cue(recorded, 2);
                }

                if (save.last != null && save.last.slot >= 0 && save.last.slot < slots.Length)
                    slots[save.last.slot].localScale = Vector3.one * (1 + .14f * Mathf.Sin(Mathf.Clamp01(celebration / .9f) * Mathf.PI));
                if (celebration >= .9f)
                {
                    ResetSlots();
                    celebration = -1;
                }
            }

            if (Time.unscaledTime >= nextCountdownUpdate)
            {
                var now = DateTime.UtcNow;
                if (LuckyWheelSave.Day(now) != displayedDay)
                    Refresh();
                else
                    RefreshCountdown(now);
            }
        }

        void ShowReceipt(bool animate)
        {
            result.text = save.last == null ? "" : "Recorded: " + save.last.Label;
            if (save.last != null && rewards)
                rewards.ShowReward(save.last.kind, save.last.amount, "", save.Acknowledge, rewardBounds);
            celebration = animate ? 0 : -1;
        }

        void Land(int slot)
        {
            disc.localRotation = Quaternion.Euler(0, 0, Mathf.Clamp(slot, 0, 7) * 45);
            ResetSlots();
        }

        void ResetSlots()
        {
            foreach (var slot in slots)
                if (slot)
                    slot.localScale = Vector3.one;
        }

        void Refresh()
        {
            var now = DateTime.UtcNow;
            displayedDay = LuckyWheelSave.Day(now);
            bool available = save != null && save.Available(now) && catalog && catalog.Valid;
            spinButton.interactable = available && !IsSpinning;
            closeButton.interactable = !IsSpinning;
            // Figma's grey clock button is the used-spin state, not an additional paid/ad spin.
            spinLabel.SetActive(available || IsSpinning);
            timerView.SetActive(!available && !IsSpinning);
            spinButton.image.enabled = available || IsSpinning;
            RefreshCountdown(now);
            for (int i = 0; i < tokens.Length; i++)
                tokens[i].color = new Color(1, 1, 1, i == 0 && available ? 1 : .3f);
            availability.text = IsSpinning ? "Spinning…" : available ? "1 free spin • UTC daily" : "Next free spin • UTC daily";
        }

        void RefreshCountdown(DateTime now)
        {
            countdown.text = CountdownText.Format(save == null ? TimeSpan.Zero : save.Remaining(now));
            nextCountdownUpdate = Time.unscaledTime + 1;
        }

        void Cue(AudioClip clip, int type)
        {
            if (!GamePreferences.Sound || !clip)
                return;
            audioService.PlaySound(clip);
#if UNITY_EDITOR
            if (type == 0)
                TickSounds++;
            else if (type == 1)
                StopSounds++;
            else
                RewardSounds++;
#endif
        }

        void OnDisable()
        {
            // The reservation survives scene unload, application close and interruption.
            IsSpinning = false;
            celebration = -1;
        }
    }
}
