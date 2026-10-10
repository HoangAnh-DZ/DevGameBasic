using System;
using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BlockCrush.UI.Editor
{
    public static partial class BlockCrushMenuVerification
    {
        public static string StartWheel()
        { Start(); stack.Clear(); reportPath="Captures/Figma/LuckyWheelVerification.md"; stack.Push(VerifyWheel()); return Status; }
        static IEnumerator VerifyWheel()
        {
            PlayerPrefs.DeleteKey(EconomySave.Key);
            PlayerPrefs.DeleteKey(LuckyWheelSave.Key);
            GamePreferences.Set(GamePreferences.SoundKey,true); Find<GameAudioService>().ApplyPreferences();
            var wheel=Find<LuckyWheelController>(); var config=wheel.Catalog;
            Check(config.Valid && config.TotalWeight==100,"Eight configured Figma sectors, explicit editable probabilities");
            var date=new DateTime(2026,10,10,23,59,59,DateTimeKind.Utc);
            int cumulative=0;
            for(int slot=0;slot<8;slot++)
            {
                double unit=(cumulative+config.rewards[slot].weight*.5)/config.TotalWeight;
                Check(config.Select(unit)==slot,"Weighted interval maps to visible sector "+slot);
                PlayerPrefs.DeleteKey(LuckyWheelSave.Key); PlayerPrefs.DeleteKey(EconomySave.Key); var state=LuckyWheelSave.Load();
                Check(state.Available(date) && state.Begin(config,date,unit),"Reserve daily spin at sector "+slot);
                var restored=LuckyWheelSave.Load();
                Check(restored.pending.slot==slot && restored.pending.amount==config.rewards[slot].amount && restored.pending.kind==config.rewards[slot].kind,"Saved immutable Figma reward "+slot);
                Check(!restored.Begin(config,date,unit) && !restored.Available(date),"Pending prevents duplicate reservation "+slot);
                Check(restored.Record() && !restored.Record() && restored.recordedCount==1,"Receipt recorded exactly once "+slot);
                restored=LuckyWheelSave.Load(); Check(restored.pending==null && restored.last.slot==slot && restored.recordedCount==1,"Empty inline JSON class normalizes on reload "+slot);
                Check(!restored.Available(date.AddDays(-1)) && !restored.Available(date) && restored.Available(date.AddSeconds(1)),"UTC midnight and clock rollback guard "+slot);
                Check(restored.Remaining(date)==TimeSpan.FromSeconds(1),"UTC countdown exact at boundary "+slot);
                wheel.Open(); yield return Frames(3);
                Check(Find<RewardPopupController>().IsOpen && Find<RewardPopupController>().Amount==restored.last.amount.ToString(),"Unacknowledged saved outcome opens reward modal "+slot);
                yield return VerifyWheelRewardBounds("Saved sector "+slot);
                Check(Mathf.Abs(Mathf.DeltaAngle(wheel.Disc.localEulerAngles.z,slot*45))<.01f,"Saved reward lands at sector "+slot);
                var marker=wheel.Disc.Find("RewardSlot"+slot);
                var direction=marker.position-wheel.Disc.position;
                Check(direction.y>0 && Mathf.Abs(Mathf.Atan2(direction.x,direction.y)*Mathf.Rad2Deg)<22.5f,"Actual sector label sits under top pointer "+slot);
                yield return DismissReward(); wheel.Close();
                cumulative+=config.rewards[slot].weight;
            }
            PlayerPrefs.DeleteKey(LuckyWheelSave.Key);
            PlayerPrefs.DeleteKey(EconomySave.Key);
            foreach(var size in new[]{new Vector2Int(1080,1920),new Vector2Int(1440,2960)})
            {
                yield return Resolution(size.x,size.y); yield return Click(Button("WheelButton")); wheel=Find<LuckyWheelController>();
                Check(wheel.IsOpen && !Find<MoreSettingsController>().InformationOpen,"Home wheel opens real popup "+size);
                Check(Mathf.Abs(Mathf.DeltaAngle(wheel.Disc.localEulerAngles.z,0))<.01f,"Fresh wheel opens with Figma's top purple sector "+size);
                ValidateScene(); var design=(RectTransform)GameObject.Find("WheelDesign").transform;
                var frame=(RectTransform)design.Find("Panel_7_16502");
                yield return Fits(frame,Screen.safeArea,"Wheel panel respects natural safe area "+size);
                ValidatePopupOverlay(design.parent.parent,"Wheel "+size);
                Check(Vector2.Distance(CommerceScreenRect(frame).center,Screen.safeArea.center)<1,"Wheel panel centers on safe area "+size);
                Check(wheel.Disc.Find("Disc_7_31109").GetComponent<Image>().sprite.texture.width==1156,"Original transparent Figma disc, not a screen screenshot");
                Check(wheel.Disc.GetComponentsInChildren<Text>().Count(t=>t.name=="Amount")==8,"All eight amount labels visible on rotating sectors");
                Check(wheel.Disc.GetComponentsInChildren<Image>().Count()==9,"Disc and all eight reward icons assigned");
                Check(UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length==3,"Wheel reuses two SFX voices and separate BGM");
                yield return Capture("LuckyWheel_Unity_"+size.x+"x"+size.y);
                var safe=design.parent.GetComponent<SafeAreaController>(); var notch=new Rect(0,100,size.x,size.y-220);
                safe.enabled=false; safe.Apply(notch); yield return Fits(frame,notch,"Wheel fits notch and gesture inset "+size);
                safe.enabled=true; safe.Apply(Screen.safeArea); yield return Frames(4);
                var receipt=LuckyWheelSave.Load();
                Check(receipt.Begin(config,DateTime.UtcNow,.015),"Reserve coin popup layout fixture "+size);
                wheel.Close(); wheel.Open(); yield return Frames(5);
                yield return VerifyWheelRewardBounds("Coin reward "+size);
                yield return Capture("LuckyWheel_CoinReward_Unity_"+size.x+"x"+size.y);
                safe.enabled=false; safe.Apply(notch); yield return Frames(5);
                yield return VerifyWheelRewardBounds("Reward respects wheel notch bounds "+size);
                safe.enabled=true; safe.Apply(Screen.safeArea); yield return Frames(4);
                if(size.x==1080)
                {
                    yield return Resolution(1440,2960);
                    yield return VerifyWheelRewardBounds("Open reward follows live screen resize");
                    yield return Resolution(1080,1920);
                }
                yield return DismissReward();
                yield return Click(Button("LuckyCloseButton")); Check(!wheel.IsOpen,"Close returns to Home "+size);
                PlayerPrefs.DeleteKey(LuckyWheelSave.Key); PlayerPrefs.DeleteKey(EconomySave.Key);
            }
            yield return Resolution(1080,1920); yield return Click(Button("WheelButton")); wheel=Find<LuckyWheelController>();
            var audio=Find<GameAudioService>(); int clicks=audio.Played(GameSfx.ButtonClick); float original=wheel.Disc.localEulerAngles.z;
            yield return Click(Button("LuckySpinButton"));
            Check(wheel.IsSpinning && wheel.State.pending!=null && audio.Played(GameSfx.ButtonClick)==clicks+1,"Actual Spin click animates and durably reserves one outcome");
            int sequence=wheel.State.sequence, selected=wheel.State.pending.slot;
            for(int i=0;i<20;i++) { wheel.Spin(); wheel.Close(); wheel.Open(); }
            Check(wheel.IsOpen && wheel.IsSpinning && wheel.State.sequence==sequence && !Button("LuckyCloseButton").interactable && !Button("LuckySpinButton").interactable,"Spam Spin/Open/Close cannot replace outcome or grant a second turn");
            yield return Seconds(.95f);
            Check(Mathf.Abs(Mathf.DeltaAngle(wheel.Disc.localEulerAngles.z,original))>.2f,"Wheel physically rotates over player frames");
            while(wheel.IsSpinning && wheel.TickSounds==0) yield return null;
            Check(wheel.TickSounds>0 && audio.SoundSource.clip.name=="WheelTick","Audible lightweight ticks on existing source");
            GamePreferences.Set(GamePreferences.SoundKey,false); audio.ApplyPreferences(); int ticks=wheel.TickSounds;
            yield return Seconds(.35f); Check(wheel.TickSounds==ticks && !audio.SoundSource.isPlaying,"Sound OFF suppresses moving wheel ticks immediately");
            GamePreferences.Set(GamePreferences.BgmKey,false); GamePreferences.Set(GamePreferences.SoundKey,true); audio.ApplyPreferences();
            Check(audio.MusicSource.mute && !audio.SoundSource.mute,"Wheel sound respects independent BGM toggle");
            while(wheel.IsSpinning) yield return null;
            Check(Mathf.Abs(Mathf.DeltaAngle(wheel.Disc.localEulerAngles.z,selected*45))<.01f,"Final wheel angle points at selected visible sector");
            Check(wheel.State.last.slot==selected && wheel.State.recordedCount==1 && wheel.State.pending==null,"Animated outcome matches exactly one durable receipt");
            var selectedTransform=wheel.Disc.Find("RewardSlot"+selected); yield return Seconds(.3f);
            Check(selectedTransform.localScale.x>1 && wheel.StopSounds==1 && wheel.RewardSounds==1,"Winner highlight, stop cue and cheerful receipt cue play once");
            Check(Find<RewardPopupController>().IsOpen && Find<RewardPopupController>().Amount==wheel.State.last.amount.ToString(),"Visible You get modal matches selected reward amount");
            yield return VerifyWheelRewardBounds("Animated spin result");
            var wallet=EconomySave.Load(); var item=wheel.State.last;
            int credited=item.kind==WheelRewardKind.Coins?wallet.coins:item.kind==WheelRewardKind.Bomb?wallet.bombs:item.kind==WheelRewardKind.Booster?wallet.boosters:wallet.reverses;
            Check(credited==item.amount && !EconomySave.GrantWheel(item,wheel.State.usedDay),"Spin credits exact wallet/inventory amount once");
            Check(!Button("LuckySpinButton").interactable && GameObject.Find("DailyTimer"),"Used spin switches to Figma grey countdown state");
            wheel.Spin(); Check(wheel.State.sequence==sequence && wheel.State.recordedCount==1,"Post-spin repeated callback cannot grant another receipt");
            yield return Capture("LuckyWheel_Result_Unity_1080x1920");
            yield return DismissReward();
            yield return Click(Button("LuckyCloseButton")); yield return Click(Button("WheelButton"));
            Check(wheel.State.sequence==sequence && wheel.State.recordedCount==1 && !Button("LuckySpinButton").interactable,"Close/reopen preserves consumed daily spin");
            yield return Click(Button("LuckyCloseButton"));
            // Genuine scene unload during an in-flight spin, followed by restored Home UI.
            PlayerPrefs.DeleteKey(LuckyWheelSave.Key); PlayerPrefs.DeleteKey(EconomySave.Key); yield return Click(Button("WheelButton")); yield return Click(Button("LuckySpinButton"));
            selected=wheel.State.pending.slot; SceneManager.LoadSceneAsync("Home"); yield return Loaded("Home");
            Check(LuckyWheelSave.Load().pending!=null,"Scene interruption preserves reservation before landing");
            yield return Click(Button("WheelButton")); wheel=Find<LuckyWheelController>();
            Check(wheel.State.pending==null && wheel.State.last.slot==selected && wheel.State.recordedCount==1 && !wheel.IsSpinning,"Recreated controller recovers reserved outcome once");
            Check(Mathf.Abs(Mathf.DeltaAngle(wheel.Disc.localEulerAngles.z,selected*45))<.01f,"Recovery lands same visible sector");
            yield return DismissReward();
            yield return Click(Button("LuckyCloseButton")); yield return Click(Button("WheelButton"));
            Check(wheel.State.recordedCount==1,"Recovered receipt cannot be duplicated by reopening");
            GamePreferences.Set(GamePreferences.SoundKey,false); Find<GameAudioService>().ApplyPreferences();
            PlayerPrefs.DeleteKey(LuckyWheelSave.Key); PlayerPrefs.DeleteKey(EconomySave.Key); wheel.Close(); wheel.Open(); wheel.Spin();
            int stop=wheel.StopSounds, reward=wheel.RewardSounds; while(wheel.IsSpinning) yield return null; yield return Seconds(.3f);
            Check(wheel.StopSounds==stop && wheel.RewardSounds==reward,"Sound OFF suppresses stop and reward cues too");
            yield return DismissReward(); wheel.Close();
            if(hadWheelSave) PlayerPrefs.SetString(LuckyWheelSave.Key,wheelSave); else PlayerPrefs.DeleteKey(LuckyWheelSave.Key);
            PlayerPrefs.Save(); GamePreferences.Set(GamePreferences.SoundKey,true); Find<GameAudioService>().ApplyPreferences();
        }
        static IEnumerator DismissReward()
        {
            var popup=Find<RewardPopupController>(); if(popup && popup.IsOpen) yield return Click(Button("RewardOKButton"));
        }
        static IEnumerator VerifyWheelRewardBounds(string label)
        {
            var host=(RectTransform)GameObject.Find("Panel_7_16502").transform;
            var frame=(RectTransform)GameObject.Find("WhiteFrame_7_810").transform;
            for(int i=0;i<3;i++)
            {
                yield return null;
                var outer=CommerceScreenRect(host); var inner=CommerceScreenRect(frame);
                var inset=new Rect(outer.xMin+outer.width*.045f,outer.yMin+outer.height*.045f,outer.width*.91f,outer.height*.91f);
                Check(inner.xMin>=inset.xMin-1 && inner.xMax<=inset.xMax+1 && inner.yMin>=inset.yMin-1 && inner.yMax<=inset.yMax+1,
                    "Reward white frame is wholly inside wheel with a visible margin: "+label+" frame "+i);
                Check(Vector2.Distance(inner.center,outer.center)<1,"Reward is centered in wheel panel: "+label+" frame "+i);
            }
            CommerceTouchTarget(Button("RewardOKButton"));
        }
    }
}
