using System;
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BlockCrush.UI.Editor
{
    public static class BlockCrushCommerceBuilder
    {
        const string Art="Assets/BlockCrush/Art/Figma/Commerce/";
        const string WheelArt="Assets/BlockCrush/Art/Figma/LuckyWheel/";
        static Font font;
        static GameAudioService audio;
        static RewardPopupController rewards;
        static Transform canvas;
        [MenuItem("Block Crush/Build Daily Tasks, Shop and Rewards")]
        public static string Build()
        {
            if(EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
            if(EditorSceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Save the current scene first.");
            foreach(var p in Directory.GetFiles(Art,"*.png"))
            {
                var path=p.Replace('\\','/'); AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
                var importer=(TextureImporter)AssetImporter.GetAtPath(path); importer.textureType=TextureImporterType.Sprite;
                importer.spriteImportMode=SpriteImportMode.Single; importer.mipmapEnabled=false; importer.alphaIsTransparency=true;
                importer.textureCompression=TextureImporterCompression.Uncompressed; importer.filterMode=FilterMode.Bilinear;
                importer.maxTextureSize=4096; importer.wrapMode=TextureWrapMode.Clamp; importer.SaveAndReimport();
            }
            font=AssetDatabase.LoadAssetAtPath<Font>("Assets/BlockCrush/Fonts/Montserrat-Bold.ttf");
            var daily=AssetDatabase.LoadAssetAtPath<DailyTaskCatalog>("Assets/BlockCrush/Data/DailyTasks.asset");
            if(!daily) { daily=ScriptableObject.CreateInstance<DailyTaskCatalog>(); AssetDatabase.CreateAsset(daily,"Assets/BlockCrush/Data/DailyTasks.asset"); }
            var shop=AssetDatabase.LoadAssetAtPath<ShopCatalog>("Assets/BlockCrush/Data/Shop.asset");
            if(!shop) { shop=ScriptableObject.CreateInstance<ShopCatalog>(); AssetDatabase.CreateAsset(shop,"Assets/BlockCrush/Data/Shop.asset"); }
            if(!daily.Valid || !shop.Valid) throw new Exception("Daily/Shop configuration invalid.");
            var scene=EditorSceneManager.OpenScene(BlockCrushMenuBuilder.HomePath);
            canvas=UnityEngine.Object.FindFirstObjectByType<Canvas>().transform; audio=UnityEngine.Object.FindFirstObjectByType<GameAudioService>();
            foreach(string name in new[]{"RewardFlow","DailyTasksFlow","ShopFlow"}) { var old=canvas.Find(name); if(old) UnityEngine.Object.DestroyImmediate(old.gameObject); }
            rewards=BuildReward(); var tasks=BuildDaily(daily); var store=BuildShop(shop);
            UnityEngine.Object.FindFirstObjectByType<MainMenuController>().ConfigureCommerce(tasks,store);
            var wheel=UnityEngine.Object.FindFirstObjectByType<LuckyWheelController>(); if(!wheel) throw new Exception("Build Lucky Wheel first.");
            var design=wheel.transform.Find("LuckyWheelPopup/SafeArea/WheelDesign");
            wheel.ConfigureRewards(rewards,(RectTransform)design.Find("Panel_7_16502"));
            foreach(string name in new[]{"IntegrationStatus","ExtraSpinStatus"}) { var old=design.Find(name); if(old) UnityEngine.Object.DestroyImmediate(old.gameObject); }
            design.Find("WheelResult").gameObject.SetActive(false);
            var oldWallet=design.Find("WheelWallet"); if(oldWallet) UnityEngine.Object.DestroyImmediate(oldWallet.gameObject);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            return "Home saved: Figma reward modal, Daily 2/4/6/8/10 milestones, two Shop tabs and Editor demo purchases.";
        }
        static RewardPopupController BuildReward()
        {
            var flow=Flow("RewardFlow"); var controller=flow.gameObject.AddComponent<RewardPopupController>();
            var popup=Rect("RewardPopup",flow); Stretch(popup); Solid("ModalInputBlocker",popup,new Color(.0343f,.0343f,.1346f,.9f));
            var design=Design(popup,"RewardDesign",1014,1624);
            Picture("WhiteFrame_7_810",design,"RewardFrame",0,0,1014,1624);
            var money=Wallet(design,654,89,274,92);
            var title=Label("RewardHeading",design,"You get:",60,180,346,654,105);
            var icon=PathPicture("RewardArtwork_7_943",design,WheelArt+"CoinPile.png",332,494,403,305); icon.preserveAspect=true;
            var amount=Label("RewardAmount",design,"",96,140,790,734,117); amount.color=new Color(1,.93349f,.20192f);
            amount.resizeTextForBestFit=true; amount.resizeTextMinSize=54; amount.resizeTextMaxSize=96;
            var details=Label("RewardDetails",design,"",36,100,935,814,250); details.horizontalOverflow=HorizontalWrapMode.Wrap;
            var ok=PathButton("RewardOKButton",design,Art+"RewardOK.png",299,1257.5f,449,236,controller.Confirm);
            CenterArtwork(ok.GetComponent<Image>());
            var buttonText=Label("RewardOKLabel",ok.transform,"OK",72,0,0,449,236);
            var cancel=PathButton("RewardCancelButton",design,WheelArt+"Close.png",862,204,80,75,controller.Back);
            controller.Configure(popup.gameObject,icon,new[]{Sprite(WheelArt+"CoinPile.png"),Icon("Icon_AdventureBomb"),Icon("Icon_AdventureBooster"),Icon("Icon_AdventureReverse")},
                title,amount,details,buttonText,money,cancel,audio,design.GetComponent<FigmaLayoutFitter>());
            popup.gameObject.SetActive(false); return controller;
        }
        static DailyTasksController BuildDaily(DailyTaskCatalog catalog)
        {
            var flow=Flow("DailyTasksFlow"); var controller=flow.gameObject.AddComponent<DailyTasksController>();
            var popup=Rect("DailyTasksPopup",flow); Stretch(popup); Background(popup);
            // Trim unused Figma canvas space so the actual panel fills the portrait safe area.
            var d=Design(popup,"DailyDesign",1432,2560);
            Picture("DailyFrame_7_16542",d,"DailyPanel",68,164,1272,2298);
            Picture("Calendar_7_16557",d,"DailyCalendar",134,131,270,302);
            Picture("DailyTitle_7_31084",d,"DailyTitle",468,238,571,114);
            CloseButton("DailyCloseButton",d,1135,251,controller.Close);
            PathPicture("Clock_7_16556",d,WheelArt+"Clock.png",512,440,88,103);
            var countdown=Label("DailyCountdown",d,"",64,590,430,435,105); countdown.color=new Color(1,.8784f,.1294f);
            Picture("DailyTrack_7_16547",d,"DailyTrack",306,566.293f,852.28f,95.444f);
            var total=Picture("DailyTotalFill_7_16552",d,"DailyFill",322.52f,582.82f,805,54.777f); Fill(total);
            var completed=Label("DailyClaimedCount",d,"0/5",48,479,573,520,85);
            Picture("ChestBase_7_31151",d,"ChestBase",992,515,230,199);
            var lid=Picture("ChestLid_7_31153",d,"ChestLid",988,458,242,166).rectTransform;
            var claims=new Button[5]; var fills=new Image[5]; var checks=new GameObject[5]; var progress=new Text[5];
            for(int i=0;i<5;i++)
            {
                float y=747+318*i; var row=Rect("DailyTask"+i,d); Place(row,139,y,1143,281);
                Picture("TaskCard",row,"DailyRow",0,0,1143,281);
                Picture("CoinTile_7_31037",row,"DailyCoinTile",56,37,207,207);
                PathPicture("Coin",row,WheelArt+"Coin50.png",88,49,143,144);
                Label("Reward",row,catalog.coinReward.ToString(),60,103,162,112,73);
                var goal=Label("TaskTitle",row,"Finish "+catalog.goals[i]+" rounds",58,305,43,500,76);
                goal.color=new Color(.196f,.0314f,.6784f); goal.alignment=TextAnchor.MiddleLeft;
                Picture("TaskProgressTrack",row,"TaskTrack",305,139,480,72);
                fills[i]=Picture("TaskProgressFill",row,"TaskFill",309,144,472,61); Fill(fills[i]);
                progress[i]=Label("TaskProgress",row,"",42,309,144,472,61);
                claims[i]=PathButton("DailyClaimButton"+i,row,Art+"ClaimGrey.png",812,23,304,236,null);
                CenterArtwork(claims[i].GetComponent<Image>());
                var handler=claims[i].gameObject.AddComponent<DailyTaskClaimButton>(); handler.Configure(controller,i);
                UnityEventTools.AddPersistentListener(claims[i].onClick,handler.Claim);
                Label("Claim",claims[i].transform,"Claim",60,0,0,304,236);
                checks[i]=PathPicture("DailyCompletedCheck"+i,row,WheelArt+"Video.png",904,74,164,134).gameObject;
                checks[i].SetActive(false);
            }
            Label("DailyRule",d,"Adventure rounds • Resets 00:00 UTC",42,160,2344,1112,56);
            controller.Configure(popup.gameObject,catalog,claims,fills,checks,progress,countdown,completed,total,lid,
                Sprite(Art+"ClaimGreen.png"),Sprite(Art+"ClaimGrey.png"),rewards,audio,RewardSound());
            popup.gameObject.SetActive(false); return controller;
        }
        static ShopController BuildShop(ShopCatalog catalog)
        {
            var flow=Flow("ShopFlow"); var controller=flow.gameObject.AddComponent<ShopController>();
            var popup=Rect("ShopPopup",flow); Stretch(popup); Background(popup);
            var d=Design(popup,"ShopDesign",1432,2934);
            UnityEngine.Object.DestroyImmediate(d.GetComponent<FigmaLayoutFitter>());
            Picture("Awning_7_31094",d,"Awning",83,0,1256,349);
            Picture("ShopTitle_7_31096",d,"ShopTitle",544,129,385,124);
            var bomb=Inventory(d,"Bomb","Icon_AdventureBomb",69,372,136,134,79,396,245,110,237);
            var booster=Inventory(d,"Booster","Icon_AdventureBooster",347,392,114,109,375,396,233,105,507);
            var reverse=Inventory(d,"Reverse","Icon_AdventureReverse",625,385,115,115,658,396,223,100,776);
            var coins=Wallet(d,898,387,318,109);
            d.gameObject.AddComponent<WalletHud>().Configure(coins,bomb,booster,reverse);
            CloseButton("ShopCloseButton",d,1269,397,controller.Close);
            var bundleList=ScrollList(d,"BundleList",2582); var itemList=ScrollList(d,"ItemList",2380);
            string[] art={"StarterCoins","ExtraCoins","UltraCoins","ValueCoins","SpecialChest"};
            for(int i=0;i<5;i++)
            {
                float y=i==4?2085:523*i; var row=Rect("BundleOffer"+i,bundleList); Place(row,0,y,1432,482);
                Picture("BundleFrame_7_16558",row,"BundleCard",46,0,1331,482);
                Picture("BundleContents_7_30974",row,"BundleContents",101,112,695,311);
                Picture("BundleRibbon_7_31021",row,"BundleRibbon",394,-1,597,101);
                var title=Label("BundleTitle",row,catalog.offers[i].title,50,421,12,550,70); title.color=new Color(.451f,.09f,.02f);
                PathPicture("Booster",row,BlockCrushMenuBuilder.Art+"Icons/Icon_AdventureBooster.png",139,139,110,106);
                PathPicture("Reverse",row,BlockCrushMenuBuilder.Art+"Icons/Icon_AdventureReverse.png",448,139,106,106);
                PathPicture("Bomb",row,BlockCrushMenuBuilder.Art+"Icons/Icon_AdventureBomb.png",139,280,120,119);
                PathPicture("Coin",row,WheelArt+"Coin50.png",448,284,115,115);
                Label("Boosters",row,"x"+catalog.offers[i].boosters,60,264,154,120,80);
                Label("Reverses",row,"x"+catalog.offers[i].reverses,60,581,154,120,80);
                Label("Bombs",row,"x"+catalog.offers[i].bombs,60,264,294,120,80);
                Label("Coins",row,"x"+catalog.offers[i].coins,60,581,289,210,80);
                if(i==4) { Picture("SpecialRays_7_16569",row,"SpecialCoins",851,8,495,446); Picture("SpecialChest_7_16644",row,"SpecialChest",962,61,294.827f,393); }
                else { var coinsArt=Picture("BundleArt",row,art[i],i==3?955:917,i==1?33:99,i==1?391:i==3?349:385,i==1?391:i==3?349:385); coinsArt.preserveAspect=true; }
                Purchase(row,controller,i,866,263,443,174,catalog.offers[i].price);
                if(i==3) Picture("Sale_7_30973",row,"Sale",1202,-34,209,213);
            }
            Button noAds=null; Text noAdsText=null;
            for(int i=0;i<6;i++)
            {
                float y=393.5f*i; var row=Rect("ItemOffer"+i,itemList); Place(row,0,y,1432,355);
                Picture("ItemFrame_7_16559",row,"ItemCard",41,-6.5f,1336,355);
                Picture("ItemContents_7_30981",row,"ItemContents",224,91,596,160);
                if(i==0)
                {
                    Picture("RemoveAds_7_31150",row,"NoAds",131,65,204,199);
                    Label("RemoveAdsLabel",row,"REMOVE ADS",60,359,128,432,86);
                }
                else if(i==1)
                {
                    PathPicture("Booster",row,BlockCrushMenuBuilder.Art+"Icons/Icon_AdventureBooster.png",148,82.5f,176,168);
                    Label("Amount",row,"X1",84,491,110.5f,200,112);
                }
                else
                {
                    PathPicture("Coin",row,WheelArt+(i==2?"CoinPile":"Coin1000")+".png",119,79,264,220);
                    Label("Amount",row,catalog.offers[i+5].coins.ToString(),84,437,110,330,112);
                }
                var purchase=Purchase(row,controller,i+5,866,86,443,174,catalog.offers[i+5].price);
                if(i==0) { noAds=purchase; noAdsText=purchase.transform.Find("Price").GetComponent<Text>(); }
            }
            var bundleTab=PathButton("ShopBundlesTab",d,Art+"BuyGreen.png",92,2546,600,236,controller.ShowBundles);
            bundleTab.GetComponent<Image>().preserveAspect=true;
            var bundleLabel=Label("Label",bundleTab.transform,"Bundles",68,0,0,600,236);
            var itemTab=PathButton("ShopItemsTab",d,Art+"BuyGreen.png",740,2546,600,236,controller.ShowItems);
            itemTab.GetComponent<Image>().preserveAspect=true;
            var itemLabel=Label("Label",itemTab.transform,"Items",68,0,0,600,236);
            var status=Label("ShopStatus",d,"DEMO • No real payment",48,92,2834,1248,56);
            d.gameObject.AddComponent<ShopResponsiveLayout>().Configure((RectTransform)bundleList.parent,(RectTransform)itemList.parent,
                (RectTransform)bundleTab.transform,(RectTransform)itemTab.transform,status.rectTransform);
            controller.Configure(popup.gameObject,bundleList.parent.gameObject,itemList.parent.gameObject,catalog,rewards,audio,
                UnityEngine.Object.FindFirstObjectByType<MoreSettingsController>(),RewardSound(),status,noAds,noAdsText,bundleLabel,itemLabel);
            itemList.parent.gameObject.SetActive(false); popup.gameObject.SetActive(false); return controller;
        }
        static Button Purchase(Transform parent,ShopController controller,int index,float x,float y,float w,float h,string price)
        {
            var button=PathButton("ShopBuyButton"+index,parent,Art+"BuyGreen.png",x,y-31,w,236,null);
            CenterArtwork(button.GetComponent<Image>());
            var handler=button.gameObject.AddComponent<ShopPurchaseButton>(); handler.Configure(controller,index); UnityEventTools.AddPersistentListener(button.onClick,handler.Buy);
            Label("Price",button.transform,price,index==10?64:80,0,0,w,236); return button;
        }
        static Button CloseButton(string name,Transform parent,float x,float y,UnityAction callback)
        {
            float hitX=Mathf.Min(x-70,1196);
            var button=PathButton(name,parent,WheelArt+"Close.png",hitX,y-73,236,236,callback);
            button.GetComponent<Image>().color=Color.clear;
            var artwork=PathPicture("CloseArtwork",button.transform,WheelArt+"Close.png",x-hitX,73,96,90); artwork.preserveAspect=true;
            button.targetGraphic=artwork;
            return button;
        }
        static void CenterArtwork(Image image)
        {
            image.preserveAspect=true;
            var rect=image.rectTransform; var size=rect.sizeDelta;
            // uGUI aligns preserved artwork to the pivot; center it in the enlarged touch region.
            rect.anchoredPosition+=Vector2.Scale(Vector2.one*.5f-rect.pivot,size);
            rect.pivot=Vector2.one*.5f;
        }
        static RectTransform ScrollList(Transform parent,string name,float height)
        {
            var viewport=Rect(name,parent); Place(viewport,0,544,1432,2160);
            var image=viewport.gameObject.AddComponent<Image>(); image.color=Color.clear; viewport.gameObject.AddComponent<RectMask2D>();
            var content=Rect(name+"Content",viewport); Place(content,0,0,1432,height);
            var scroll=viewport.gameObject.AddComponent<ScrollRect>(); scroll.viewport=viewport; scroll.content=content;
            scroll.horizontal=false; scroll.vertical=true; scroll.movementType=ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity=90; scroll.decelerationRate=.12f; return content;
        }
        static Text Inventory(Transform parent,string name,string icon,float ix,float iy,float iw,float ih,float px,float py,float pw,float ph,float labelX)
        {
            Picture(name+"Pill",parent,"InventoryPill",px-3,py,pw+6,ph+6);
            PathPicture(name+"Icon",parent,BlockCrushMenuBuilder.Art+"Icons/"+icon+".png",ix,iy,iw,ih);
            return Label(name+"Balance",parent,"0",50,labelX-30,416,115,61);
        }
        static Text Wallet(Transform parent,float x,float y,float w,float h)
        {
            Picture("WalletPill",parent,"InventoryPill",x+25,y,w-25,h);
            PathPicture("WalletCoin",parent,WheelArt+"Coin50.png",x,y,Mathf.Min(h,108),h);
            var text=Label("CoinBalance",parent,"0",h<100?45:50,x+90,y+5,w-100,h-10); text.color=new Color(1,.9335f,.2019f);
            text.resizeTextForBestFit=true; text.resizeTextMinSize=26; text.resizeTextMaxSize=text.fontSize; return text;
        }
        static AudioClip RewardSound() => AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/BlockCrush/Audio/SFX/WheelReward.wav");
        static Sprite Icon(string name) => Sprite(BlockCrushMenuBuilder.Art+"Icons/"+name+".png");
        static Sprite Sprite(string path) { var s=AssetDatabase.LoadAssetAtPath<Sprite>(path); if(!s) throw new Exception("Missing sprite "+path); return s; }
        static RectTransform Flow(string name) { var r=Rect(name,canvas); Stretch(r); return r; }
        static RectTransform Design(Transform parent,string name,float w,float h)
        {
            var safe=Rect("SafeArea",parent); Stretch(safe); safe.gameObject.AddComponent<SafeAreaController>();
            var d=Rect(name,safe); d.gameObject.AddComponent<FigmaLayoutFitter>().Configure(new Vector2(w,h)); return d;
        }
        static void Background(Transform parent)
        {
            var image=Picture("PurpleBackground_7_16499",parent,"Background",0,0,1432,2934); Stretch(image.rectTransform); image.raycastTarget=true;
            var fitter=image.gameObject.AddComponent<AspectRatioFitter>(); fitter.aspectMode=AspectRatioFitter.AspectMode.EnvelopeParent; fitter.aspectRatio=1432f/2934;
        }
        static void Solid(string name,Transform parent,Color color) { var r=Rect(name,parent); Stretch(r); var i=r.gameObject.AddComponent<Image>(); i.color=color; }
        static void Fill(Image image) { image.type=Image.Type.Filled; image.fillMethod=Image.FillMethod.Horizontal; image.fillOrigin=0; image.fillAmount=0; }
        static RectTransform Rect(string name,Transform parent) { var go=new GameObject(name,typeof(RectTransform)); go.transform.SetParent(parent,false); return (RectTransform)go.transform; }
        static void Stretch(RectTransform r) { r.anchorMin=Vector2.zero; r.anchorMax=Vector2.one; r.offsetMin=r.offsetMax=Vector2.zero; }
        static void Place(RectTransform r,float x,float y,float w,float h) { r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1); r.anchoredPosition=new Vector2(x,-y); r.sizeDelta=new Vector2(w,h); }
        static Image Picture(string name,Transform parent,string asset,float x,float y,float w,float h) => PathPicture(name,parent,Art+asset+".png",x,y,w,h);
        static Image PathPicture(string name,Transform parent,string path,float x,float y,float w,float h)
        { var r=Rect(name,parent); Place(r,x,y,w,h); var i=r.gameObject.AddComponent<Image>(); i.sprite=Sprite(path); i.raycastTarget=false; return i; }
        static Button PathButton(string name,Transform parent,string path,float x,float y,float w,float h,UnityAction callback)
        {
            var image=PathPicture(name,parent,path,x,y,w,h); image.raycastTarget=true;
            var b=image.gameObject.AddComponent<Button>(); b.targetGraphic=image;
            if(callback!=null) UnityEventTools.AddPersistentListener(b.onClick,callback); return b;
        }
        static Text Label(string name,Transform parent,string text,int size,float x,float y,float w,float h)
        {
            var r=Rect(name,parent); Place(r,x,y,w,h); var t=r.gameObject.AddComponent<Text>(); t.font=font; t.fontSize=size; t.text=text;
            t.color=Color.white; t.alignment=TextAnchor.MiddleCenter; t.raycastTarget=false; t.horizontalOverflow=HorizontalWrapMode.Overflow; t.verticalOverflow=VerticalWrapMode.Overflow;
            var shadow=r.gameObject.AddComponent<Shadow>(); shadow.effectColor=new Color(0,0,0,.65f); shadow.effectDistance=new Vector2(0,-2); return t;
        }
    }
}
