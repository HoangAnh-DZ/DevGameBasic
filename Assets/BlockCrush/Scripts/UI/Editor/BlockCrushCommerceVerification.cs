using System;
using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BlockCrush.UI.Editor
{
    public static partial class BlockCrushMenuVerification
    {
        public static string StartCommerce()
        { Start(); stack.Clear(); reportPath="Captures/Figma/CommerceVerification.md"; stack.Push(VerifyCommerce()); return Status; }
        static IEnumerator VerifyCommerce()
        {
            PlayerPrefs.DeleteKey(EconomySave.Key); PlayerPrefs.DeleteKey(LuckyWheelSave.Key);
            GamePreferences.Set(GamePreferences.SoundKey,true); Find<GameAudioService>().ApplyPreferences();
            yield return Loaded("Home");
            var daily=Find<DailyTasksController>(); var shop=Find<ShopController>(); var popup=Find<RewardPopupController>();
            Check(daily && shop && popup && daily.Catalog.Valid && shop.Catalog.Valid,"Saved Home has actual Daily, Shop, reward views and valid catalogs");
            // A missing optional Cancel reference previously prevented all wheel/Daily rewards.
            var cancelField=typeof(RewardPopupController).GetField("cancel",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            var savedCancel=cancelField.GetValue(popup); int acknowledgements=0, originalCoins=EconomySave.Load().coins;
            try
            {
                cancelField.SetValue(popup,null);
                popup.ShowReward(WheelRewardKind.Coins,100,"",()=>acknowledgements++);
                Check(popup.IsOpen && popup.Amount=="100","Reward opens when optional Cancel reference is missing");
                popup.Confirm(); popup.Confirm();
                Check(acknowledgements==1 && EconomySave.Load().coins==originalCoins,"Missing Cancel does not duplicate acknowledgement or alter the wallet");
            }
            finally { cancelField.SetValue(popup,savedCancel); }
            Check(daily.Catalog.goals.SequenceEqual(new[]{2,4,6,8,10}) && daily.Catalog.coinReward==100,"Five balanced milestones with Figma 100-coin reward");
            var now=DateTime.UtcNow; var fixture=new EconomySave { dailyDay=LuckyWheelSave.Day(now),dailyPlays=1 };
            fixture.Save(); Check(!EconomySave.ClaimDaily(0,daily.Catalog,now),"Incomplete task cannot claim");
            fixture.dailyPlays=10; fixture.Save();
            for(int i=0;i<5;i++) Check(EconomySave.ClaimDaily(i,daily.Catalog,now) && !EconomySave.ClaimDaily(i,daily.Catalog,now),"Task "+i+" grant is atomic and rejects repeated claim");
            Check(EconomySave.Load().coins==500 && EconomySave.Load().ClaimedCount==5,"Five rewards persist exactly 500 coins");
            Check(EconomySave.Load(now.AddDays(-1)).ClaimedCount==5 && !EconomySave.ClaimDaily(0,daily.Catalog,now.AddDays(-1)),"Clock rollback cannot repeat old claims");
            var nextDay=EconomySave.Load(now.Date.AddDays(1)); Check(nextDay.dailyPlays==0 && nextDay.ClaimedCount==0 && nextDay.coins==500,"UTC midnight resets daily progress and preserves wallet");
            var bad=ShopOfferFixture(); bad.coins=-1; Check(!EconomySave.PurchaseDemo(bad),"Invalid purchase configuration cannot grant inventory");
            PlayerPrefs.DeleteKey(EconomySave.Key);
            foreach(var size in new[]{new Vector2Int(1080,1920),new Vector2Int(1440,2960)})
            {
                yield return Resolution(size.x,size.y); yield return Click(Button("CalendarButton"));
                Check(daily.IsOpen && !Find<MoreSettingsController>().InformationOpen,"Calendar opens real Daily Tasks "+size);
                ValidateScene(); var design=(RectTransform)GameObject.Find("DailyDesign").transform;
                var frame=(RectTransform)design.Find("DailyFrame_7_16542");
                yield return Fits(frame,Screen.safeArea,"Daily panel safe area "+size);
                ValidatePopupOverlay(design.parent.parent,"Daily "+size);
                var dailyBounds=CommerceScreenRect(frame);
                ValidateDailyLayout(Screen.safeArea);
                Check(!Button("DailyClaimButton0").interactable,"Waiting Daily Claim visibly disabled");
                yield return Capture("DailyTasks_Unity_"+size.x+"x"+size.y);
                var inset=new Rect(60,100,size.x-120,size.y-240); var safe=design.parent.GetComponent<SafeAreaController>();
                safe.enabled=false; safe.Apply(inset); yield return Fits(frame,inset,"Daily fits notch + gestures "+size); ValidateDailyLayout(inset); safe.enabled=true; safe.Apply(Screen.safeArea); yield return Frames(5);
                yield return Click(Button("DailyCloseButton")); Check(!daily.IsOpen,"Daily Close restores Home "+size);
                yield return Click(Button("WheelButton"));
                var wheelFrame=(RectTransform)GameObject.Find("Panel_7_16502").transform;
                var wheelBounds=CommerceScreenRect(wheelFrame);
                Check(Vector2.Distance(dailyBounds.center,wheelBounds.center)<1 && Mathf.Abs(dailyBounds.width-wheelBounds.width)<1 && Mathf.Abs(dailyBounds.height-wheelBounds.height)<1,
                    "Daily and Wheel panels have matching center and dimensions "+size);
                ValidatePopupOverlay(wheelFrame.parent.parent.parent,"Wheel "+size);
                yield return Click(Button("LuckyCloseButton"));
                yield return Click(Button("ShopButton")); Check(shop.IsOpen && shop.BundlesVisible,"Shop opens Bundle tab "+size);
                Check(GameObject.Find("BundleList").GetComponent<ScrollRect>().verticalNormalizedPosition>.95f,"Shop reopen starts at first offer "+size);
                design=(RectTransform)GameObject.Find("ShopDesign").transform; ValidateScene();
                yield return Fits(design,Screen.safeArea,"Shop safe area "+size); yield return Capture("ShopBundles_Unity_"+size.x+"x"+size.y);
                ValidateShopLayout();
                var footer=GameObject.Find("ShopStatus").GetComponent<Text>();
                Check(footer.cachedTextGenerator.characterCountVisible==footer.text.Length,"Full demo notice generates visible glyphs "+size);
                yield return Fits(footer.rectTransform,Screen.safeArea,"Demo notice stays inside screen "+size);
                yield return Click(Button("ShopBuyButton0"));
                var modal=(RectTransform)GameObject.Find("RewardDesign").transform;
                yield return Fits(modal,Screen.safeArea,"Confirmation/reward modal fits safe area "+size);
                yield return Capture("ShopConfirm_Unity_"+size.x+"x"+size.y);
                yield return Click(Button("RewardCancelButton"));
                // Real InputSystem mouse wheel, routed through ScrollRect, exposes the fifth bundle.
                var viewport=GameObject.Find("BundleList").GetComponent<ScrollRect>();
                var center=RectTransformUtility.WorldToScreenPoint(null,viewport.transform.TransformPoint(new Vector3(716,-1100,0)));
                InputSystem.QueueStateEvent(mouse,new MouseState{position=center,scroll=new Vector2(0,-100)}); yield return Frames(12);
                Check(viewport.verticalNormalizedPosition<.05f,"Actual scroll reaches Special Bundle "+size);
                yield return Capture("ShopBundles_Scrolled_Unity_"+size.x+"x"+size.y);
                yield return Click(Button("ShopItemsTab")); Check(!shop.BundlesVisible && GameObject.Find("ShopBuyButton10"),"Items tab exposes all six Figma offers "+size);
                yield return Capture("ShopItems_Unity_"+size.x+"x"+size.y);
                safe=design.parent.GetComponent<SafeAreaController>(); safe.enabled=false; safe.Apply(inset); yield return Fits(design,inset,"Shop fits notch + gestures "+size); ValidateShopLayout(); safe.enabled=true; safe.Apply(Screen.safeArea); yield return Frames(5);
                yield return Click(Button("ShopCloseButton")); Check(!shop.IsOpen,"Shop Close restores Home "+size);
            }
            yield return Resolution(1080,1920);
            // Genuine Adventure runs exercise the new hook through existing accepted placements and rules.
            foreach(var key in keys.Where(k=>k.StartsWith("BlockCrush.Adventure."))) PlayerPrefs.DeleteKey(key);
            PlayerPrefs.DeleteKey(EconomySave.Key); yield return Click(Button("AdventureButton")); yield return Click(Button("AdventurePlayButton")); yield return Loaded("BlockCrushAdventure");
            var game=Find<BlockGameManager>(); var level=Find<AdventureLevelController>();
            Check(EconomySave.Load().dailyPlays==0,"Entering Adventure alone does not increase daily plays");
            var piece=game.Spawner.SpawnPiece(0,BlockShapeData.Database[0],game.Spawner.BlockSprites[0]);
            Check(!game.TryPlacePiece(piece,new Vector2Int(-1,-1)) && EconomySave.Load().dailyPlays==0,"Invalid drop cannot increase Daily progress");
            yield return CompleteAdventureTarget();
            Check(EconomySave.Load().dailyPlays==1,"Actual completed Adventure credits one daily play");
            for(int i=0;i<10;i++) level.EndTurn(game.Score,true);
            Check(EconomySave.Load().dailyPlays==1,"Repeated completion callbacks do not count multiple rounds");
            game.RestartGame(); yield return Frames(3);
            int max=level.MovesRemaining;
            for(int turn=0;turn<max;turn++) { game.Board.ResetBoard(); piece=game.Spawner.SpawnPiece(0,BlockShapeData.Database[0],game.Spawner.BlockSprites[0]); Check(game.TryPlacePiece(piece,new Vector2Int(3,3)),"Accepted placement in Daily loss fixture "+turn); }
            Check(level.Lost && EconomySave.Load().dailyPlays==2,"Actual lost Adventure credits one play too");
            for(int i=0;i<10;i++) level.EndTurn(game.Score,true); Check(EconomySave.Load().dailyPlays==2,"Loss callback spam cannot farm daily progress");
            Find<SceneNavigationController>().CompleteHome(); yield return Loaded("Home"); daily=Find<DailyTasksController>(); shop=Find<ShopController>(); popup=Find<RewardPopupController>();
            yield return Click(Button("CalendarButton"));
            Check(Button("DailyClaimButton0").interactable && !Button("DailyClaimButton1").interactable,"Two real rounds unlock first Daily claim only");
            var audio=Find<GameAudioService>(); GamePreferences.Set(GamePreferences.SoundKey,false); audio.ApplyPreferences();
            yield return Click(Button("DailyClaimButton0"));
            Check(popup.IsOpen && popup.Amount=="100" && EconomySave.Load().coins==100 && !audio.SoundSource.isPlaying,"Daily claim opens 100 reward modal, credits wallet and honors Sound OFF");
            for(int i=0;i<10;i++) daily.Claim(0); Check(EconomySave.Load().coins==100,"UI claim spam does not duplicate reward");
            yield return Capture("DailyReward_Unity_1080x1920"); yield return DismissReward();
            Check(GameObject.Find("DailyCompletedCheck0") && !GameObject.Find("DailyClaimButton0"),"Claimed row replaces button with Figma green check");
            yield return Capture("DailyTasks_Claimed_Unity_1080x1920"); yield return Click(Button("DailyCloseButton"));
            yield return Click(Button("ShopButton"));
            int walletBefore=EconomySave.Load().coins; yield return Click(Button("ShopBuyButton0"));
            Check(popup.IsConfirmation && EconomySave.Load().coins==walletBefore,"Price opens explicit demo confirmation without awarding yet");
            yield return Click(Button("RewardCancelButton")); Check(!popup.IsOpen && EconomySave.Load().coins==walletBefore,"Cancel demo purchase grants nothing");
            yield return Click(Button("ShopBuyButton0")); yield return Click(Button("RewardOKButton"));
            var state=EconomySave.Load(); Check(popup.IsOpen && !popup.IsConfirmation && state.coins==walletBefore+4000 && state.bombs==10 && state.boosters==10 && state.reverses==10,"Confirmed demo bundle grants exactly all displayed contents");
            int purchases=state.demoSequence; for(int i=0;i<10;i++) shop.Buy(0);
            Check(EconomySave.Load().demoSequence==purchases,"Purchase callback blocked while reward modal is visible");
            yield return Capture("ShopDemoReward_Unity_1080x1920"); yield return DismissReward();
            yield return Click(Button("ShopItemsTab")); yield return Click(Button("ShopBuyButton5")); yield return Click(Button("RewardOKButton")); yield return DismissReward();
            Check(EconomySave.Load().removeAds && !Button("ShopBuyButton5").interactable && Button("ShopBuyButton5").transform.Find("Price").GetComponent<Text>().text=="Owned","Remove Ads demo entitlement persists and cannot be repurchased");
            yield return Click(Button("ShopBuyButton6")); yield return Click(Button("RewardOKButton")); yield return DismissReward();
            Check(EconomySave.Load().boosters==11,"Individual booster purchase adds exactly one");
            for(int index=7;index<11;index++)
            {
                int before=EconomySave.Load().coins;
                if(index>=9) { GameObject.Find("ItemList").GetComponent<ScrollRect>().verticalNormalizedPosition=0; yield return Frames(8); }
                yield return Click(Button("ShopBuyButton"+index)); yield return Click(Button("RewardOKButton")); yield return DismissReward();
                Check(EconomySave.Load().coins==before+shop.Catalog.offers[index].coins,"Demo coin offer "+index+" adds exact displayed amount");
            }
            var saved=PlayerPrefs.GetString(EconomySave.Key); yield return Click(Button("ShopCloseButton"));
            SceneManager.LoadSceneAsync("Home"); yield return Loaded("Home");
            daily=Find<DailyTasksController>(); shop=Find<ShopController>(); popup=Find<RewardPopupController>();
            Check(PlayerPrefs.GetString(EconomySave.Key)==saved,"Wallet, demo entitlement and claimed Daily reward survive actual scene reload");
            yield return Click(Button("CalendarButton")); Check(GameObject.Find("DailyCompletedCheck0"),"Daily check survives reopen/reload"); yield return Click(Button("DailyCloseButton"));
            // Completed chest state uses the five genuine claim flags, rather than Figma example progress.
            state=EconomySave.Load(); state.dailyPlays=10; state.Save(); yield return Click(Button("CalendarButton"));
            for(int i=1;i<5;i++) { yield return Click(Button("DailyClaimButton"+i)); yield return DismissReward(); }
            Check(EconomySave.Load().ClaimedCount==5 && GameObject.Find("DailyClaimedCount").GetComponent<Text>().text=="5/5","Five claimed milestones fill chest progress");
            yield return Capture("DailyTasks_AllClaimed_Unity_1080x1920"); yield return Click(Button("DailyCloseButton"));
            yield return Click(Button("CalendarButton")); yield return CommerceEscape();
            Check(!daily.IsOpen,"Escape closes Daily and restores Home");
            yield return Click(Button("ShopButton")); int beforeCancel=EconomySave.Load().demoSequence;
            yield return Click(Button("ShopBuyButton0")); yield return CommerceEscape();
            Check(!popup.IsOpen && shop.IsOpen && EconomySave.Load().demoSequence==beforeCancel,"Escape cancels demo confirmation without purchase");
            yield return CommerceEscape(); Check(!shop.IsOpen,"Escape closes Shop after cancellation");
            var wheel=Find<LuckyWheelController>(); var spin=LuckyWheelSave.Load();
            Check(spin.Begin(wheel.Catalog,DateTime.UtcNow,.015),"Recovery fixture reserves actual top coin sector");
            int beforeWheel=EconomySave.Load().coins; yield return Click(Button("WheelButton"));
            Check(popup.IsOpen && popup.Amount=="1000" && EconomySave.Load().coins==beforeWheel+1000,"Recovered coin spin shows exact coin artwork/amount and balance");
            yield return Capture("LuckyWheel_CoinReward_Unity_1080x1920");
            yield return CommerceEscape(); Check(!popup.IsOpen && wheel.IsOpen && LuckyWheelSave.Load().acknowledgedId==spin.pending.id,"Escape acknowledges reward without closing wheel or granting again");
            yield return CommerceEscape(); Check(!wheel.IsOpen,"Escape then returns from wheel to Home");
            Check(UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length==3,"Daily/Shop reuse bounded SFX and independent BGM sources");
            GamePreferences.Set(GamePreferences.SoundKey,true); Find<GameAudioService>().ApplyPreferences();
        }
        static IEnumerator CommerceEscape()
        {
            InputSystem.QueueStateEvent(navigationKeyboard,new KeyboardState(Key.Escape)); yield return Frames(3);
            InputSystem.QueueStateEvent(navigationKeyboard,new KeyboardState()); yield return Frames(8);
        }
        static Rect CommerceScreenRect(RectTransform rect)
        {
            var corners=new Vector3[4]; rect.GetWorldCorners(corners);
            var a=RectTransformUtility.WorldToScreenPoint(null,corners[0]);
            var b=RectTransformUtility.WorldToScreenPoint(null,corners[2]);
            return Rect.MinMaxRect(a.x,a.y,b.x,b.y);
        }
        static void ValidatePopupOverlay(Transform popup,string label)
        {
            var blocker=popup.Find("ModalInputBlocker").GetComponent<Image>();
            Check(!blocker.sprite && blocker.raycastTarget && !blocker.GetComponent<AspectRatioFitter>() &&
                blocker.color.r==0 && blocker.color.g==0 && blocker.color.b==0 && Mathf.Abs(blocker.color.a-.65f)<.001f,
                "Plain translucent black backdrop blocks input: "+label);
            var bounds=CommerceScreenRect(blocker.rectTransform);
            Check(Mathf.Abs(bounds.xMin)<1 && Mathf.Abs(bounds.yMin)<1 && Mathf.Abs(bounds.width-Screen.width)<1 && Mathf.Abs(bounds.height-Screen.height)<1,
                "Backdrop covers display including unsafe edges: "+label);
            Check(Find<MainMenuController>().gameObject.activeInHierarchy && GameObject.Find("HomeDesign_4_280"),"Home stays visible behind popup: "+label);
            var hits=new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
            UnityEngine.EventSystems.EventSystem.current.RaycastAll(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){position=new Vector2(8,8)},hits);
            Check(hits.Count>0 && hits[0].gameObject==blocker.gameObject,"Outside-panel input hits backdrop instead of Home: "+label);
        }
        static void CommerceTouchTarget(Button button)
        {
            var bounds=CommerceScreenRect((RectTransform)button.transform);
            // A 360-unit mobile viewport at 3x/4x pixel scale, including simulated insets.
            float unit=360f/Screen.width;
            Check(bounds.width*unit>=48 && bounds.height*unit>=48,"48-unit minimum mobile touch region: "+button.name);
        }
        static void ValidateDailyLayout(Rect safe)
        {
            var design=GameObject.Find("DailyDesign").transform;
            var frame=CommerceScreenRect((RectTransform)design.Find("DailyFrame_7_16542"));
            Check(frame.width>=safe.width*.85f,"Daily panel occupies at least 85% of available width");
            Check(Mathf.Abs(frame.center.y-safe.center.y)<safe.height*.025f,"Daily panel is vertically centered with balanced outer margins");
            CommerceTouchTarget(Button("DailyCloseButton"));
            for(int i=0;i<5;i++)
            {
                CommerceTouchTarget(Button("DailyClaimButton"+i));
                var title=design.Find("DailyTask"+i+"/TaskTitle").GetComponent<Text>();
                Check(title.preferredWidth<=title.rectTransform.rect.width+1 && title.preferredHeight<=title.rectTransform.rect.height+1,"Daily task title fits without colliding with Claim: "+i);
                if(i>0)
                {
                    var previous=CommerceScreenRect((RectTransform)design.Find("DailyTask"+(i-1)));
                    var current=CommerceScreenRect((RectTransform)design.Find("DailyTask"+i));
                    Check((previous.yMin-current.yMax)*360f/Screen.width>=8,"Daily cards have at least eight mobile units of separation: "+i);
                }
            }
            var last=CommerceScreenRect((RectTransform)design.Find("DailyTask4"));
            var rule=CommerceScreenRect((RectTransform)design.Find("DailyRule"));
            Check((last.yMin-rule.yMax)*360f/Screen.width>=8,"Daily reset caption has space below the last card");
            Check((rule.yMin-frame.yMin)*360f/Screen.width>=12,"Daily reset caption clears the decorative bottom border");
        }
        static void ValidateShopLayout()
        {
            var first=Button("ShopBundlesTab"); var second=Button("ShopItemsTab");
            CommerceTouchTarget(first); CommerceTouchTarget(second); CommerceTouchTarget(Button("ShopCloseButton"));
            foreach(var tab in new[]{first,second})
            {
                var rect=(RectTransform)tab.transform; var image=tab.GetComponent<Image>();
                Check(image.preserveAspect && Mathf.Abs(rect.rect.width/rect.rect.height-image.sprite.rect.width/image.sprite.rect.height)<.02f,"Shop tab preserves rounded artwork proportions: "+tab.name);
                var label=tab.transform.Find("Label").GetComponent<Text>();
                Check(label.preferredWidth<rect.rect.width && label.preferredHeight<rect.rect.height,"Shop tab label has comfortable padding: "+tab.name);
            }
            var a=CommerceScreenRect((RectTransform)first.transform); var b=CommerceScreenRect((RectTransform)second.transform);
            Check((b.xMin-a.xMax)*360f/Screen.width>=8,"Shop tabs have at least eight mobile units between hit regions");
            var viewport=GameObject.Find(Find<ShopController>().BundlesVisible?"BundleList":"ItemList").GetComponent<RectTransform>();
            Check((CommerceScreenRect(viewport).yMin-a.yMax)*360f/Screen.width>=8,"Scrollable Shop offers cannot overlap the bottom navigation");
            foreach(var button in viewport.GetComponentsInChildren<Button>()) CommerceTouchTarget(button);
        }
        static ShopOffer ShopOfferFixture() => new ShopOffer { id="invalid",title="invalid",price="$0",coins=1 };
    }
}
