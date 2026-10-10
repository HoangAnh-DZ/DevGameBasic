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
    public static class BlockCrushWheelBuilder
    {
        const string Art="Assets/BlockCrush/Art/Figma/LuckyWheel/";
        static Font font;
        [MenuItem("Block Crush/Build Lucky Wheel")]
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
            var config=AssetDatabase.LoadAssetAtPath<LuckyWheelCatalog>("Assets/BlockCrush/Data/LuckyWheel.asset");
            if(!config) { config=ScriptableObject.CreateInstance<LuckyWheelCatalog>(); AssetDatabase.CreateAsset(config,"Assets/BlockCrush/Data/LuckyWheel.asset"); }
            font=AssetDatabase.LoadAssetAtPath<Font>("Assets/BlockCrush/Fonts/Montserrat-Bold.ttf");
            if(!font) throw new Exception("Montserrat Bold is required for dynamic Figma timer text.");
            var scene=EditorSceneManager.OpenScene(BlockCrushMenuBuilder.HomePath);
            var canvas=UnityEngine.Object.FindFirstObjectByType<Canvas>().transform;
            var old=canvas.Find("LuckyWheelFlow"); if(old) UnityEngine.Object.DestroyImmediate(old.gameObject);
            var flow=Rect("LuckyWheelFlow",canvas); Stretch(flow);
            var controller=flow.gameObject.AddComponent<LuckyWheelController>();
            var popup=Rect("LuckyWheelPopup",flow); Stretch(popup);
            var backdrop=Rect("ModalInputBlocker",popup); Stretch(backdrop);
            backdrop.gameObject.AddComponent<Image>().color=new Color(0,0,0,.65f);
            var safe=Rect("SafeArea",popup); Stretch(safe); safe.gameObject.AddComponent<SafeAreaController>();
            var design=Rect("WheelDesign",safe);
            design.gameObject.AddComponent<FigmaLayoutFitter>().Configure(new Vector2(1432,2560),1,new Vector2(-7,-33));
            Picture("Panel_7_16502",design,"Panel",73,164,1272,2298);
            Picture("Title_7_31085",design,"Title",365.7755f,243.8633f,688.5143f,93.167f);
            var disc=Rect("RotatingDisc",design); Center(disc,719,1126,892,892);
            Picture("Disc_7_31109",disc,"Disc",0,0,892,892);
            var slots=new RectTransform[8];
            // Exported amount nodes retain their own Figma rotation and shadow.
            string[] amounts={"1000","300","100","2","50","150","2","2"};
            float[,] number={{327.378f,11172.552f,140.575f,51.097f},{596.663f,11246.065f,103.549f,103.363f},
                {723.759f,11503.345f,53.037f,103.483f},{615.179f,11766.15f,55.912f,53.7f},
                {372.379f,11866.352f,78.081f,51.097f},{129.656f,11745.572f,89.007f,93.907f},
                {39.755f,11530.445f,51.059f,43.742f},{135.685f,11284.397f,55.802f,53.889f}};
            for(int i=0;i<8;i++)
            {
                slots[i]=Rect("RewardSlot"+i,disc); Center(slots[i],number[i,0]+37+number[i,2]/2,number[i,1]-11098+number[i,3]/2,0,0);
                // Counter-offset lets each highlighted slot pivot around its label, while retaining original artwork coordinates.
                var offset=Rect("FigmaCoordinates",slots[i]);
                Place(offset,-(number[i,0]+37+number[i,2]/2),-(number[i,1]-11098+number[i,3]/2),892,892);
                float[,] textGeometry={{333,11163,152,76,0},{636.584f,11238.605f,121.758f,75.812f,-44.2025f},
                    {785,11507.695f,109.123f,75.812f,-91.96764f},{679.408f,11805.66f,36.757f,75.812f,-135},
                    {445,11923,80,76,180},{181.818f,11841.859f,103.379f,75.812f,135.42145f},
                    {32.955f,11567.211f,36.757f,75.812f,92.70238f},{127.711f,11293.723f,36.757f,75.812f,43.39006f}};
                var amount=Label("Amount",offset,amounts[i],54,textGeometry[i,0]+37,textGeometry[i,1]-11098,textGeometry[i,2],textGeometry[i,3]);
                amount.alignment=TextAnchor.UpperLeft; amount.rectTransform.localRotation=Quaternion.Euler(0,0,textGeometry[i,4]);
            }
            var p0=slots[0].GetChild(0); var p1=slots[1].GetChild(0); var p2=slots[2].GetChild(0);
            Picture("Coin_7_31119",p0,"Coin1000",392,143,113,96);
            var ne=Picture("Coin_7_31120",p1,"CoinPile",633.657f,210.602f,106.869f,80.893f); ne.rectTransform.localRotation=Quaternion.Euler(0,0,-61.12836f);
            var east=Picture("Coin_7_31122",p2,"Coin100",728.245f,406.066f,80.395f,80.848f); east.rectTransform.localRotation=Quaternion.Euler(0,0,-80.14861f);
            ExistingRotated("Bomb_7_31139",slots[3].GetChild(0),"Icon_AdventureBomb",655.366f+37,11728.156f-11098,100.182f,98.465f,-135.1225f);
            Picture("Coin_7_31118",slots[4].GetChild(0),"Coin50",402,651,84,84);
            var coin=Picture("Coin_7_31121",slots[5].GetChild(0),"CoinPile",264.24f,687.371f,106.869f,80.893f).rectTransform;
            coin.localRotation=Quaternion.Euler(0,0,120);
            ExistingRotated("Booster_7_31138",slots[6].GetChild(0),"Icon_AdventureBooster",122.52f+37,11589.633f-11098,89.891f,86.029f,93.646f);
            ExistingRotated("Reverse_7_31123",slots[7].GetChild(0),"Icon_AdventureReverse",168.34f+37,11366.914f-11098,87.564f,87.564f,45.3441f);
            Picture("Hub_7_31125",design,"Hub",564,973,304,303);
            Picture("Rim_7_31126",design,"Rim",104,492,1222,1234);
            var pointer=Rect("PointerClip_7_31127",design); Place(pointer,623,526,189,226); pointer.gameObject.AddComponent<RectMask2D>();
            Picture("Pointer",pointer,"Pointer",0,-17,213,243);
            var tokens=new Image[3];
            for(int i=0;i<3;i++) tokens[i]=Existing("SpinToken"+i,design,"Icon_Wheel",481+180*i,1826,133,133);
            var video=Picture("VideoUnavailable",design,"Video",298,1843,132,108); video.color=new Color(1,1,1,.3f);
            var plus=Picture("ExtraSpinUnavailable",design,"Plus",1028,1831,130,132); plus.color=new Color(1,1,1,.3f);
            var spin=Button("LuckySpinButton",design,"SpinButton",456,2067,572,215,controller.Spin);
            var label=Label("SpinLabel_7_31149",design,"SPIN",90,630,2110,225,110); label.alignment=TextAnchor.UpperLeft;
            var timer=Rect("DailyTimer",design); Place(timer,456,2067,572,215);
            Picture("TimerButton_7_31086",timer,"TimerButton",0,0,572,215);
            Picture("Clock_7_31087",timer,"Clock",109,55,88,103);
            var time=Label("Countdown",timer,"",72,211,55,330,103);
            var close=Button("LuckyCloseButton",design,"Close",1147,251,96,90,controller.Close);
            var status=Label("SpinAvailability",design,"",44,200,1950,1032,70);
            var result=Label("WheelResult",design,"",54,150,1730,1132,80);
            Label("IntegrationStatus",design,"Coins and boosters: gameplay use pending",32,150,2320,1132,70);
            Label("ExtraSpinStatus",design,"Extra spins unavailable",30,150,2385,1132,50);
            var audio=UnityEngine.Object.FindFirstObjectByType<GameAudioService>();
            controller.Configure(popup.gameObject,disc,slots,tokens,spin,close,label.gameObject,timer.gameObject,time,result,status,config,audio,
                Clip("WheelTick"),Clip("WheelStop"),Clip("WheelReward"));
            UnityEngine.Object.FindFirstObjectByType<MainMenuController>().ConfigureWheel(controller);
            popup.gameObject.SetActive(false); BlockCrushTextContrastBuilder.ApplyToScene(scene);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            return "Lucky Wheel saved. " + BlockCrushCommerceBuilder.Build();
        }
        static AudioClip Clip(string name)
        {
            string p="Assets/BlockCrush/Audio/SFX/"+name+".wav"; AssetDatabase.ImportAsset(p,ImportAssetOptions.ForceSynchronousImport);
            var importer=(AudioImporter)AssetImporter.GetAtPath(p); importer.forceToMono=true; importer.loadInBackground=false;
            var serialized=new SerializedObject(importer); serialized.FindProperty("m_Normalize").boolValue=false; serialized.ApplyModifiedPropertiesWithoutUndo();
            var settings=importer.defaultSampleSettings; settings.loadType=AudioClipLoadType.DecompressOnLoad; settings.preloadAudioData=true;
            settings.compressionFormat=AudioCompressionFormat.PCM; settings.sampleRateSetting=AudioSampleRateSetting.PreserveSampleRate; importer.defaultSampleSettings=settings; importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<AudioClip>(p);
        }
        static RectTransform Rect(string name,Transform parent) { var go=new GameObject(name,typeof(RectTransform)); go.transform.SetParent(parent,false); return (RectTransform)go.transform; }
        static void Stretch(RectTransform r) { r.anchorMin=Vector2.zero; r.anchorMax=Vector2.one; r.offsetMin=r.offsetMax=Vector2.zero; }
        static void Place(RectTransform r,float x,float y,float w,float h) { r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1); r.anchoredPosition=new Vector2(x,-y); r.sizeDelta=new Vector2(w,h); }
        static void Center(RectTransform r,float x,float y,float w,float h) { Place(r,x,y,w,h); r.pivot=Vector2.one*.5f; }
        static Image Picture(string name,Transform parent,string asset,float x,float y,float w,float h)
        { return SpritePicture(name,parent,Art+asset+".png",x,y,w,h); }
        static Image Existing(string name,Transform parent,string asset,float x,float y,float w,float h)
        { return SpritePicture(name,parent,BlockCrushMenuBuilder.Art+"Icons/"+asset+".png",x,y,w,h); }
        static Image SpritePicture(string name,Transform parent,string path,float x,float y,float w,float h)
        {
            var r=Rect(name,parent); Place(r,x,y,w,h); var img=r.gameObject.AddComponent<Image>(); img.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if(!img.sprite) throw new Exception("Missing sprite "+path); img.raycastTarget=false; return img;
        }
        static void ExistingRotated(string name,Transform parent,string asset,float x,float y,float w,float h,float angle)
        { var img=Existing(name,parent,asset,x,y,w,h); img.rectTransform.localRotation=Quaternion.Euler(0,0,angle); }
        static Button Button(string name,Transform parent,string asset,float x,float y,float w,float h,UnityAction callback)
        {
            var image=Picture(name,parent,asset,x,y,w,h); image.raycastTarget=true;
            var button=image.gameObject.AddComponent<Button>(); button.targetGraphic=image; UnityEventTools.AddPersistentListener(button.onClick,callback); return button;
        }
        static Text Label(string name,Transform parent,string text,int size,float x,float y,float w,float h)
        {
            var rect=Rect(name,parent); Place(rect,x,y,w,h); var label=rect.gameObject.AddComponent<Text>(); label.font=font; label.fontSize=size;
            label.text=text; label.color=Color.white; label.alignment=TextAnchor.MiddleCenter; label.raycastTarget=false;
            label.horizontalOverflow=HorizontalWrapMode.Overflow; label.verticalOverflow=VerticalWrapMode.Overflow;
            var shadow=rect.gameObject.AddComponent<Shadow>(); shadow.effectColor=new Color(0,0,0,.75f); shadow.effectDistance=new Vector2(0,-2); return label;
        }
    }
    [CustomEditor(typeof(LuckyWheelCatalog))]
    public sealed class LuckyWheelCatalogInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var rewards=serializedObject.FindProperty("rewards");
            EditorGUILayout.LabelField("Figma rewards — clockwise from the top",EditorStyles.boldLabel);
            for(int i=0;i<rewards.arraySize;i++)
            {
                var item=rewards.GetArrayElementAtIndex(i); var kind=item.FindPropertyRelative("kind"); var amount=item.FindPropertyRelative("amount");
                EditorGUILayout.LabelField("Sector "+i+": "+amount.intValue+" "+kind.enumDisplayNames[kind.enumValueIndex]);
                EditorGUILayout.PropertyField(item.FindPropertyRelative("weight"),new GUIContent("Probability weight"));
            }
            EditorGUILayout.PropertyField(serializedObject.FindProperty("duration")); EditorGUILayout.PropertyField(serializedObject.FindProperty("turns"));
            serializedObject.ApplyModifiedProperties();
        }
    }
}
