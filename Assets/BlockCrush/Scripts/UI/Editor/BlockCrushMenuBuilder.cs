using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using static BlockCrush.UI.Editor.UiBuilderUtility;

namespace BlockCrush.UI.Editor
{
    public static class BlockCrushMenuBuilder
    {
        public const string Art = "Assets/BlockCrush/Art/Figma/";
        public const string HomePath = "Assets/Scenes/Home.unity";
        public const string GameplayPath = "Assets/Scenes/BlockCrushGameplay.unity";
        public const string PrefabPath = "Assets/BlockCrush/Prefabs/SettingsUI.prefab";
        static Font font;
        [MenuItem("Block Crush/Build Figma Menus")]
        public static string Build()
        {
            RequireSavedEditMode();
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Directory.CreateDirectory("Assets/BlockCrush/Prefabs");
            ImportSprites();
            var home = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(0, 0, -10);
            camera.GetComponent<Camera>().orthographic = true;
            var light = new GameObject("Directional Light", typeof(Light));
            light.GetComponent<Light>().type = LightType.Directional;
            var canvas = Rect("Canvas", null);
            canvas.gameObject.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = .5f;
            canvas.gameObject.AddComponent<GraphicRaycaster>();
            var background = Picture("Background", canvas, "Backgrounds/BG_Home", 0, 0, 1452, 2959);
            Stretch(background.rectTransform);
            var safe = Rect("SafeArea", canvas);
            Stretch(safe);
            safe.gameObject.AddComponent<SafeAreaController>();
            var content = Rect("HomeDesign_4_280", safe);
            content.gameObject.AddComponent<FigmaLayoutFitter>().Configure(new Vector2(1451.5f, 2959));
            var flow = BuildFlow(canvas);
            var prefab = PrefabUtility.SaveAsPrefabAsset(flow.gameObject, PrefabPath);
            UnityEngine.Object.DestroyImmediate(flow.gameObject);
            flow = (RectTransform)((GameObject)PrefabUtility.InstantiatePrefab(prefab, canvas)).transform;
            Stretch(flow);
            var settings = flow.GetComponent<SettingsPanelController>();
            var more = flow.GetComponent<MoreSettingsController>();
            var menu = content.gameObject.AddComponent<MainMenuController>();
            menu.Configure(flow.GetComponent<SceneNavigationController>(), settings, more);
            Picture("Logo", content, "Logo/Logo_BlockMatchBlast", 263, 417.5f, 1006, 783);
            var logoShadow = content.Find("Logo").gameObject.AddComponent<Shadow>();
            logoShadow.effectDistance = new Vector2(10, -10);
            logoShadow.effectColor = new Color(0, 0, 0, .4f);
            var artMask = Rect("UnderTheSea_Clip", content);
            Place(artMask, 349, 1358, 839, 584);
            artMask.gameObject.AddComponent<RectMask2D>();
            Picture("OriginalArtwork", artMask, "Misc/UnderTheSea_Artwork", -306, -15, 1154, 517);
            Picture("EventFrame", content, "Panels/Panel_Event", 267, 1236, 1003, 1022);
            Picture("EventTitleFrame", content, "Panels/Panel_EventTitle", 495, 1206, 572, 181);
            Picture("EventTitle", content, "Misc/Label_UnderTheSea", 543, 1285.5f, 365, 43, true);
            Picture("EventTime", content, "Misc/Label_Time", 512, 1893.5f, 421, 45, true);
            var adventure = Button("AdventureButton", content, "Buttons/Btn_Orange", 424, 1960, 705.5f, 259, menu.Adventure);
            Picture("Label", adventure.transform, "Misc/Label_Adventure", 130, 63, 446, 109, true);
            var play = Button("PlayButton_Classic", content, "Buttons/Btn_Green", 424, 2393.73f, 706, 258.54f, menu.Play);
            Picture("Label", play.transform, "Misc/Label_Classic", 206, 65.27f, 294, 128, true);
            Button("SettingsButton", content, "Icons/Icon_Settings", 1277, 143, 94, 95, menu.OpenSettings).GetComponent<Image>().color = new Color(1, 1, 1, .8f);
            Button("ShopButton", content, "Icons/Icon_Shop", 77, 183, 123.7f, 136.36f, menu.Shop);
            Button("ChallengeButton", content, "Icons/Icon_Challenge", 82, 376, 112, 112, menu.Challenge);
            Button("WheelButton", content, "Icons/Icon_Wheel", 56, 523, 157, 157, menu.Wheel);
            Button("CalendarButton", content, "Icons/Icon_Calendar", 1269, 364, 111, 127.29f, menu.Calendar);
            Picture("CalendarNotification", content, "Icons/Icon_Notification", 1339.65f, 338, 70.35f, 80.74f, true);
            flow.SetAsLastSibling();
            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystem.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            BlockCrushTextContrastBuilder.ApplyToScene(home);
            EditorSceneManager.SaveScene(home, HomePath);
            var gameplay = EditorSceneManager.OpenScene(GameplayPath, OpenSceneMode.Single);
            var gameCanvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
            var previous = gameCanvas.transform.Find("SettingsUI");
            if (previous)
                UnityEngine.Object.DestroyImmediate(previous.gameObject);
            var gameFlow = (GameObject)PrefabUtility.InstantiatePrefab(prefab, gameCanvas.transform);
            Stretch((RectTransform)gameFlow.transform);
            var gameSettings = gameFlow.GetComponent<SettingsPanelController>();
            var settingsButton = gameCanvas.transform.Find("SafeArea/TopHUD/Settings").GetComponent<Button>();
            settingsButton.onClick = new Button.ButtonClickedEvent();
            UnityEventTools.AddPersistentListener(settingsButton.onClick, gameSettings.Open);
            settingsButton.GetComponent<Image>().sprite = Sprite("Icons/Icon_Settings");
            BlockCrushTextContrastBuilder.ApplyToScene(gameplay);
            EditorSceneManager.SaveScene(gameplay);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(HomePath, true),
                new EditorBuildSettingsScene(GameplayPath, true)
            };
            EditorSceneManager.OpenScene(HomePath, OpenSceneMode.Single);
            BlockCrushScreenBuilder.BuildAdventure();
            BlockCrushWheelBuilder.Build();
            // Replacing SettingsUI invalidates the gameplay presenters' old audio reference.
            BlockCrushFeedbackBuilder.Build();
            BlockCrushBoosterBuilder.Build();
            BlockCrush.Editor.BlockCrushSceneBuilder.SetGameViewSize(1080, 1920);
            AssetDatabase.SaveAssets();
            return "Imported Figma sprites, shared Settings UI prefab, Home, Adventure, gameplay integration and startup Build Settings saved.";
        }

        static void ImportSprites()
        {
            AssetDatabase.Refresh();
            foreach (var path in Directory.GetFiles(Art, "*.png", SearchOption.AllDirectories))
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(path.Replace('\\', '/'));
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = 4096;
                importer.spritePixelsPerUnit = 100;
                if (path.Contains("Btn_"))
                    importer.spriteBorder = Vector4.one * 110;
                importer.SaveAndReimport();
            }
        }

        static RectTransform BuildFlow(Transform canvas)
        {
            var root = Rect("SettingsUI", canvas);
            Stretch(root);
            var settings = root.gameObject.AddComponent<SettingsPanelController>();
            var more = root.gameObject.AddComponent<MoreSettingsController>();
            var navigation = root.gameObject.AddComponent<SceneNavigationController>();
            var audio = root.gameObject.AddComponent<GameAudioService>();
            var sfx = new GameObject("SoundSource", typeof(AudioSource));
            sfx.transform.SetParent(root, false);
            var bgm = new GameObject("MusicSource", typeof(AudioSource));
            bgm.transform.SetParent(root, false);
            sfx.GetComponent<AudioSource>().playOnAwake = false;
            bgm.GetComponent<AudioSource>().playOnAwake = false;
            bgm.GetComponent<AudioSource>().loop = true;
            audio.Configure(sfx.GetComponent<AudioSource>(), bgm.GetComponent<AudioSource>());
            var settingsRoot = Modal("SettingsPopup", root, new Vector2(1229, 1729), out var card);
            Picture("Panel", card, "Panels/Panel_Settings", 0, 0, 1229, 1729);
            Picture("Title", card, "Misc/Label_Settings", 402, 49, 418, 117, true);
            Button("CloseButton", card, "Icons/Icon_Close", 1081, 71, 66, 62, settings.Close);
            for (int i = 0; i < 5; i++)
                Line(card, 127, 468.2f + i * 199, 968, "Settings/Separator_Settings");
            Picture("SoundIcon", card, "Icons/Icon_Sound", 198, 329, 110, 103, true);
            Picture("BgmIcon", card, "Icons/Icon_Music", 216, 521, 71, 102, true);
            Picture("VibrationIcon", card, "Icons/Icon_Vibration", 208, 722, 94, 94, true);
            Picture("ReplayIcon", card, "Icons/Icon_Replay", 212, 912, 90, 93, true);
            Picture("HomeIcon", card, "Icons/Icon_BackHome", 202, 1125, 92, 80, true);
            Picture("MoreIcon", card, "Icons/Icon_SettingsRow", 208, 1307, 94, 95, true);
            Picture("SoundLabel", card, "Misc/Label_Sound", 334, 348, 182, 66, true);
            Picture("BgmLabel", card, "Misc/Label_BGM", 334, 538, 135, 66, true);
            Picture("VibrationLabel", card, "Misc/Label_Vibration", 326, 740, 261, 66, true);
            Picture("ReplayLabel", card, "Misc/Label_Replay", 329, 933, 193, 66, true);
            Picture("HomeLabel", card, "Misc/Label_BackHome", 330, 1132, 395, 66, true);
            Picture("MoreLabel", card, "Misc/Label_MoreSettings", 330, 1317, 395, 66, true);
            var sound = Button("SoundToggle", card, "Settings/Toggle_On", 832, 330, 213, 95, settings.ToggleSound);
            var music = Button("BgmToggle", card, "Settings/Toggle_On", 832, 523, 213, 95, settings.ToggleBgm);
            var vibration = Button("VibrationToggle", card, "Settings/Toggle_On", 832, 719, 213, 95, settings.ToggleVibration);
            var replay = Button("ReplayButton", card, "Buttons/Btn_Green", 754, 918, 296, 109, settings.Replay);
            Picture("Label", replay.transform, "Misc/Label_Play", 78, 9, 144, 78, true);
            var home = Button("BackHomeButton", card, "Buttons/Btn_Green", 754, 1111, 296, 109, settings.BackHome);
            Picture("Label", home.transform, "Misc/Label_Back", 66, 11, 169, 78, true);
            var set = Button("MoreButton", card, "Buttons/Btn_Green", 754, 1300, 296, 109, settings.OpenMore);
            Picture("Label", set.transform, "Misc/Label_Set", 96, 11, 109, 78, true);
            var moreRoot = Modal("MoreSettingsPopup", root, new Vector2(1345, 1892), out card);
            Picture("Panel", card, "Panels/Panel_MoreSettings", 0, 0, 1345, 1892);
            Picture("Title", card, "Misc/Label_MoreTitle", 325, 71, 701, 117, true);
            Button("BackButton", card, "Icons/Icon_Close", 1200, 78, 66, 62, more.Close);
            Picture("AppIcon", card, "Misc/AppIcon", 212, 406.3f, 223.9f, 223.9f, true);
            Picture("AppName", card, "Misc/Label_AppName", 527, 428, 567, 73, true);
            var version = Text("Version", card, "Version: " + Application.version, 60, 533, 510, 600, 73);
            Picture("ShareDecoration", card, "Misc/ShareIcon", 566, 996.3f, 198, 197.9f, true);
            foreach (float y in new[]
            {
                817.2f,
                973.2f,
                1330.2f,
                1486.2f
            }

            )
                Line(card, 176, y, 998, "Settings/Separator_MoreSettings");
            Row("ContactButton", card, "Misc/Label_Contact", 715, 311, more.Contact);
            Row("ShareButton", card, "Misc/Label_Share", 871, 544, more.Share);
            Row("TermsButton", card, "Misc/Label_Terms", 1213, 467, more.Terms);
            Row("PrivacyButton", card, "Misc/Label_Privacy", 1367, 393, more.Privacy);
            Row("AboutButton", card, "Misc/Label_About", 1525, 262, more.About);
            var infoRoot = Modal("InformationPopup", root, new Vector2(1229, 1729), out card);
            Picture("Panel", card, "Panels/Panel_Settings", 0, 0, 1229, 1729);
            var infoTitle = Text("Title", card, "", 72, 190, 65, 850, 135, TextAnchor.MiddleCenter);
            var infoBody = Text("Body", card, "", 50, 220, 350, 790, 1050);
            infoBody.horizontalOverflow = HorizontalWrapMode.Wrap;
            infoBody.verticalOverflow = VerticalWrapMode.Truncate;
            Button("CloseButton", card, "Icons/Icon_Close", 1081, 71, 66, 62, more.CloseInformation);
            settings.Configure(settingsRoot.GetComponent<CanvasGroup>(), sound.GetComponent<Image>(), music.GetComponent<Image>(), vibration.GetComponent<Image>(), Sprite("Settings/Toggle_On"), Sprite("Settings/Toggle_Off"), replay, home, more, navigation, audio);
            more.Configure(moreRoot.gameObject, infoRoot.gameObject, version, infoTitle, infoBody, settings, audio);
            var transition = Rect("TransitionOverlay", root);
            Stretch(transition);
            transition.gameObject.AddComponent<Image>().color = new Color(.035f, .06f, .17f);
            var group = transition.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0;
            group.blocksRaycasts = false;
            navigation.Configure(group, audio);
            settingsRoot.gameObject.SetActive(false);
            moreRoot.gameObject.SetActive(false);
            infoRoot.gameObject.SetActive(false);
            BlockCrushTextContrastBuilder.ApplyToHierarchy(root.gameObject);
            return root;
        }

        static RectTransform Modal(string name, Transform parent, Vector2 size, out RectTransform card)
        {
            var root = Rect(name, parent);
            Stretch(root);
            root.gameObject.AddComponent<CanvasGroup>();
            var dim = root.gameObject.AddComponent<Image>();
            dim.color = new Color(.015f, .025f, .10f, .62f);
            var safe = Rect("SafeArea", root);
            Stretch(safe);
            safe.gameObject.AddComponent<SafeAreaController>();
            card = Rect("Card", safe);
            card.gameObject.AddComponent<FigmaLayoutFitter>().Configure(size, .9f);
            return root;
        }

        static void Row(string name, Transform parent, string label, float y, float width, UnityAction action)
        {
            var button = Button(name, parent, null, 176, y - 35, 998, 130, action);
            Picture("Label", button.transform, label, 40, 35, width, 66, true);
            Picture("Arrow", button.transform, "Icons/Icon_Arrow", 917.44f, 44, 26.413f, 45.36f, true);
        }

        static void Line(Transform parent, float x, float y, float width, string asset)
        {
            var rect = Rect("Separator", parent);
            Place(rect, x, y, width, 5);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = Sprite(asset);
            image.raycastTarget = false;
        }

        static Sprite Sprite(string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Art + path + ".png");
            if (!sprite)
                throw new InvalidOperationException("Missing Figma sprite: " + path);
            return sprite;
        }

        static Image Picture(string name, Transform parent, string asset, float x, float y, float w, float h, bool preserve = false)
        {
            var rect = Rect(name, parent);
            Place(rect, x, y, w, h);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = Sprite(asset);
            image.raycastTarget = false;
            image.preserveAspect = preserve;
            return image;
        }

        static Button Button(string name, Transform parent, string asset, float x, float y, float w, float h, UnityAction action)
        {
            var rect = Rect(name, parent);
            Place(rect, x, y, w, h);
            var image = rect.gameObject.AddComponent<Image>();
            if (asset != null)
            {
                image.sprite = Sprite(asset);
                image.preserveAspect = true;
            }
            else
                image.color = Color.clear;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.highlightedColor = new Color(.92f, .96f, 1);
            colors.pressedColor = new Color(.75f, .8f, .92f);
            colors.disabledColor = new Color(.6f, .6f, .6f, .6f);
            button.colors = colors;
            UnityEventTools.AddPersistentListener(button.onClick, action);
            return button;
        }

        static Text Text(string name, Transform parent, string value, int size, float x, float y, float w, float h, TextAnchor anchor = TextAnchor.MiddleLeft)
        {
            var rect = Rect(name, parent);
            Place(rect, x, y, w, h);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.color = Color.white;
            text.alignment = anchor;
            text.text = value;
            text.raycastTarget = false;
            return text;
        }
    }
}
