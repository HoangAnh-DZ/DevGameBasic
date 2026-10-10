using System;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BlockCrush.UI.Editor
{
    // Updates only the added screens; the original gameplay builder is never run.
    public static class BlockCrushScreenBuilder
    {
        static readonly string Art = BlockCrushMenuBuilder.Art;
        public const string AdventurePath = "Assets/Scenes/BlockCrushAdventure.unity";
        const string CatalogPath = "Assets/BlockCrush/Data/AdventureLevels.asset";
        static Font Font => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        public static string BuildGameOver()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
            if (EditorSceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Save the current scene first.");
            ImportSprites();
            var scene = EditorSceneManager.OpenScene(BlockCrushMenuBuilder.GameplayPath);
            var game = UnityEngine.Object.FindFirstObjectByType<BlockGameManager>();
            var navigation = UnityEngine.Object.FindFirstObjectByType<SceneNavigationController>();
            var root = (RectTransform)game.GameOverPanel.transform;
            root.gameObject.SetActive(false);
            while (root.childCount > 0) UnityEngine.Object.DestroyImmediate(root.GetChild(0).gameObject);
            root.GetComponent<Image>().color = Color.clear;
            Background(root, "Backgrounds/BG_GameOver");
            var safe = Rect("SafeArea", root); Stretch(safe); safe.gameObject.AddComponent<SafeAreaController>();
            var design = Rect("GameOverDesign_4_2", safe);
            design.gameObject.AddComponent<FigmaLayoutFitter>().Configure(new Vector2(1440, 2959));
            Picture("Title", design, "Misc/Label_GameOver", 270, 612, 918.9f, 130.2f);
            Picture("ScorePanel", design, "Panels/Panel_GameOverScore", 193, 831, 1088, 974);
            Picture("ScoreLabel", design, "Misc/Label_Score", 607.2f, 1145.1f, 225.9f, 46.4f);
            var score = Text("FinalScore", design, "0", 210, 320, 1196, 800, 300);
            score.resizeTextForBestFit = true; score.resizeTextMinSize = 96; score.resizeTextMaxSize = 210;
            // Figma has no Best field here. Keep the existing manager reference and persistence.
            var best = Text("FinalBest", design, "", 44, 320, 1460, 800, 70); best.gameObject.SetActive(false);
            Button("HomeButton", design, "Buttons/Btn_GameOverHome", 169, 1915, 499, 183, navigation.Home);
            var restart = Button("RestartButton", design, "Buttons/Btn_GameOverPlay", 751, 1918, 490, 180, null);
            var serialized = new SerializedObject(game);
            serialized.FindProperty("finalScoreText").objectReferenceValue = score;
            serialized.FindProperty("finalBestText").objectReferenceValue = best;
            serialized.FindProperty("restartButton").objectReferenceValue = restart;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            BlockCrushTextContrastBuilder.ApplyToScene(scene);
            EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(BlockCrushMenuBuilder.HomePath);
            return "Game Over updated from Figma; existing manager, board, score and Best Score preserved.";
        }

        public static string BuildAdventure()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
            if (EditorSceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Save the current scene first.");
            ImportSprites();
            var scene = EditorSceneManager.OpenScene(BlockCrushMenuBuilder.HomePath);
            var canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>().transform;
            var menu = UnityEngine.Object.FindFirstObjectByType<MainMenuController>();
            var settings = UnityEngine.Object.FindFirstObjectByType<SettingsPanelController>();
            var previous = canvas.Find("AdventureView_7_1657");
            if (previous) UnityEngine.Object.DestroyImmediate(previous.gameObject);
            previous = canvas.Find("AdventureMap_7_128");
            if (previous) UnityEngine.Object.DestroyImmediate(previous.gameObject);
            var root = Rect("AdventureMap_7_128", canvas); Stretch(root);
            var controller = root.gameObject.AddComponent<AdventurePanelController>();
            Background(root, "Backgrounds/BG_AdventureMap");
            var safe = Rect("SafeArea", root); Stretch(safe); safe.gameObject.AddComponent<SafeAreaController>();
            var design = Rect("AdventureMapDesign_7_128", safe);
            design.gameObject.AddComponent<FigmaLayoutFitter>().Configure(new Vector2(1440, 2959));
            Picture("Header", design, "Panels/AdventureMap_Header", -5, -27, 1440, 424);
            var headerTitle = Sprite("Misc/AdventureMap_Title");
            Picture("AdventureTitle", design, "Misc/AdventureMap_Title", 451, 129, headerTitle.rect.width, headerTitle.rect.height);
            var back = Rect("AdventureBackButton", design); Place(back, 0, 0, 250, 360);
            var backImage = back.gameObject.AddComponent<Image>(); backImage.color = Color.clear;
            var backButton = back.gameObject.AddComponent<Button>(); backButton.targetGraphic = backImage;
            UnityEventTools.AddPersistentListener(backButton.onClick, controller.BackHome);
            Picture("Logo", design, "Logo/Logo_BlockCrush", 399, 397, 656, 404);
            Picture("LevelPath", design, "Gameplay/AdventureMap_Path", 161.1f, 356, 23.7f, 470);
            var catalog = Catalog();
            var viewport = Rect("LevelViewport", design); Place(viewport, 0, 810, 1440, 1690);
            viewport.gameObject.AddComponent<RectMask2D>();
            var hit = viewport.gameObject.AddComponent<Image>(); hit.color = Color.clear;
            var content = Rect("LevelContent", viewport); float height = 1690 + (catalog.Count - 3) * 580;
            Place(content, 0, 0, 1440, height);
            Picture("LevelPath", content, "Gameplay/AdventureMap_Path", 161.1f, 0, 23.7f, height - 290);
            var scroll = viewport.gameObject.AddComponent<ScrollRect>(); scroll.viewport = viewport; scroll.content = content;
            scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 160;
            var buttons = new Button[catalog.Count]; var badges = new Image[catalog.Count]; var states = new Text[catalog.Count];
            for (int i = 0; i < catalog.Count; i++)
            {
                float y = height - 520 - i * 580;
                var binding = content.gameObject.AddComponent<AdventureLevelButton>(); binding.Configure(controller, i + 1);
                buttons[i] = Button("AdventureLevelButton" + (i + 1), content, "Gameplay/AdventureMap_Level" + (i % 3 + 1), 382, y, 735.1f, 431.8f, binding.Select);
                buttons[i].transition = Selectable.Transition.None;
                badges[i] = Picture("LevelBadge" + (i + 1), content, "Buttons/AdventureMap_Unlocked", 83, y + 140, 188, 188);
                badges[i].raycastTarget = true; var badgeButton = badges[i].gameObject.AddComponent<Button>(); badgeButton.targetGraphic = badges[i];
                badgeButton.transition = Selectable.Transition.None;
                UnityEventTools.AddPersistentListener(badgeButton.onClick, binding.Select);
                if (i < 3)
                { var number = Sprite("Misc/AdventureMap_Number" + (i + 1)); Picture("LevelNumber" + (i + 1), content, "Misc/AdventureMap_Number" + (i + 1), 134, y + 183, number.rect.width, number.rect.height); }
                else
                { var number = Text("LevelNumber" + (i + 1), content, (i + 1).ToString(), 90, 83, y + 140, 188, 188); number.gameObject.AddComponent<Outline>().effectColor = new Color(.7f,.15f,.15f); }
                states[i] = Text("LevelCompletion" + (i + 1), content, "", 44, 382, y + 432, 735, 80); states[i].color = new Color32(255, 224, 89, 255);
            }
            var play = Button("AdventurePlayButton", design, "Buttons/Btn_Green", 462, 2513, 568, 208, controller.PlaySelected);
            var label = Text("AdventurePlayLabel", play.transform, "▶ Level 1", 96, 20, 10, 528, 188);
            controller.Configure(menu.gameObject, settings, settings.GetComponent<SceneNavigationController>(), settings.GetComponent<GameAudioService>(), catalog, buttons, badges,
                Sprite("Buttons/AdventureMap_Unlocked"), Sprite("Buttons/AdventureMap_Locked"), label, scroll, states);
            menu.ConfigureAdventure(controller); EditorUtility.SetDirty(menu);
            settings.transform.SetAsLastSibling(); root.gameObject.SetActive(false);
            BlockCrushTextContrastBuilder.ApplyToScene(scene);
            EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            BuildAdventureGameplay(catalog);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(BlockCrushMenuBuilder.HomePath, true), new EditorBuildSettingsScene(BlockCrushMenuBuilder.GameplayPath, true), new EditorBuildSettingsScene(AdventurePath, true) };
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(BlockCrushMenuBuilder.HomePath);
            return "Adventure level map and playable target-score scene saved; sequential progress and first-move tutorial wired.";
        }

        static AdventureLevelCatalog Catalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<AdventureLevelCatalog>(CatalogPath);
            if (!catalog) { System.IO.Directory.CreateDirectory("Assets/BlockCrush/Data"); catalog = ScriptableObject.CreateInstance<AdventureLevelCatalog>(); AssetDatabase.CreateAsset(catalog, CatalogPath); }
            catalog.Configure(new[] { Sprite("Gameplay/AdventureMap_Level1"), Sprite("Gameplay/AdventureMap_Level2"), Sprite("Gameplay/AdventureMap_Level3") });
            EditorUtility.SetDirty(catalog); return catalog;
        }
        static void BuildAdventureGameplay(AdventureLevelCatalog catalog)
        {
            var scene = EditorSceneManager.OpenScene(BlockCrushMenuBuilder.GameplayPath);
            EditorSceneManager.SaveScene(scene, AdventurePath);
            var canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>().transform;
            var game = UnityEngine.Object.FindFirstObjectByType<BlockGameManager>();
            var settings = UnityEngine.Object.FindFirstObjectByType<SettingsPanelController>();
            var navigation = settings.GetComponent<SceneNavigationController>();
            var controller = game.gameObject.AddComponent<AdventureLevelController>();
            UnityEngine.Object.DestroyImmediate(canvas.GetComponent<BlockPortraitLayout>());
            var background = canvas.Find("Background").GetComponent<Image>(); background.sprite = Sprite("Backgrounds/BG_Adventure");
            background.rectTransform.anchorMin = background.rectTransform.anchorMax = background.rectTransform.pivot = Vector2.one * .5f;
            var backgroundFit = background.gameObject.AddComponent<AspectRatioFitter>(); backgroundFit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent; backgroundFit.aspectRatio = 1433f / 2971;
            var safe = canvas.Find("SafeArea") as RectTransform; Stretch(safe); safe.gameObject.AddComponent<SafeAreaController>();
            safe.Find("TopHUD").gameObject.SetActive(false); safe.Find("ComboText").gameObject.SetActive(false); safe.Find("DragHint").gameObject.SetActive(false);
            var design = Rect("AdventureDesign_7_1657", safe); design.gameObject.AddComponent<FigmaLayoutFitter>().Configure(new Vector2(1433, 2971));
            Picture("BestIcon", design, "Icons/Icon_AdventureBest", 63, 83, 168, 127);
            var best = Text("AdventureBestScore", design, "0", 64, 63, 203, 400, 100); best.alignment = TextAnchor.MiddleLeft; best.color = new Color32(255, 220, 72, 255);
            var level = Text("AdventureLevelLabel", design, "Level 01", 100, 410, 85, 614, 150);
            var moves = Text("AdventureMoves", design, "Moves 22", 56, 490, 240, 450, 80);
            Button("AdventureSettingsButton", design, "Icons/Icon_AdventureSettings", 1254, 90, 121, 133, settings.Open);
            Picture("ProgressFrame", design, "Gameplay/Adventure_ProgressFrame", 307, 333, 811, 108);
            var fill = Picture("AdventureProgressFill", design, "Gameplay/Adventure_ProgressFill", 324, 351, 777, 71);
            var progress = Text("AdventureProgressScore", design, "0", 64, 552, 327, 238, 110);
            Picture("TargetRing", design, "Gameplay/Adventure_TargetRing", 942, 286, 196, 201);
            var target = Text("AdventureTarget", design, "100", 64, 952, 326, 176, 120); target.color = new Color32(255, 220, 30, 255);
            var board = safe.Find("BoardArea") as RectTransform; board.SetParent(design, false); Place(board, 54, 552, 1337, 1337);
            Picture("BoardFrame", design, "Gameplay/Adventure_BoardFrame", 34, 536, 1373, 1374);
            Booster(design, "AdventureBombButton", 78, 246, "Icons/Icon_AdventureBomb", 98, 1988, 112, 110, controller.Booster);
            Booster(design, "AdventureReverseButton", 571, 740, "Icons/Icon_AdventureReverse", 592, 1990, 110, 110, controller.Booster);
            Booster(design, "AdventureThirdBoosterButton", 1074, 1242, "Icons/Icon_AdventureBooster", 1094, 1999, 96, 92, controller.Booster);
            var tray = safe.Find("PieceTray") as RectTransform; tray.SetParent(design, false); Place(tray, 0, 2200, 1433, 256);
            var drag = safe.Find("DragLayer") as RectTransform; drag.SetParent(design, false); Stretch(drag);
            var hand = Picture("TutorialArtwork", design, "Gameplay/Adventure_Hand", 936, 1576, 180, 185).gameObject; hand.SetActive(false);
            var managerFields = new SerializedObject(game); managerFields.FindProperty("scoreText").objectReferenceValue = progress; managerFields.ApplyModifiedPropertiesWithoutUndo();
            var win = Rect("AdventureCompletePanel", canvas); Stretch(win); win.gameObject.AddComponent<Image>().color = Color.clear;
            Background(win, "Backgrounds/BG_GameOver");
            var winSafe = Rect("SafeArea", win); Stretch(winSafe); winSafe.gameObject.AddComponent<SafeAreaController>();
            var winDesign = Rect("CompletionDesign", winSafe); winDesign.gameObject.AddComponent<FigmaLayoutFitter>().Configure(new Vector2(1440, 2959));
            Picture("ScorePanel", winDesign, "Panels/Panel_GameOverScore", 193, 831, 1088, 974);
            var title = Text("CompleteTitle", winDesign, "Level Complete", 90, 170, 592, 1100, 180);
            Picture("ScoreLabel", winDesign, "Misc/Label_Score", 607.2f, 1145.1f, 225.9f, 46.4f);
            var score = Text("CompleteScore", winDesign, "0", 210, 320, 1196, 800, 300);
            var map = Button("AdventureCompleteMapButton", winDesign, "Buttons/Btn_Green", 170, 1915, 540, 200, navigation.CompleteHome);
            map.GetComponent<Image>().material = AssetDatabase.LoadAssetAtPath<Material>("Assets/BlockCrush/Art/HomeCyan.mat");
            Text("Label", map.transform, "Home", 80, 20, 0, 500, 200);
            var next = Button("AdventureNextLevelButton", winDesign, "Buttons/Btn_Green", 750, 1915, 540, 200, controller.NextLevel);
            var nextLabel = Text("Label", next.transform, "▶ Level 2", 70, 20, 0, 500, 200);
            controller.Configure(game, catalog, navigation, settings.GetComponent<MoreSettingsController>(), level, progress, best, target, fill, hand, win.gameObject, title, score, next, nextLabel, moves);
            win.gameObject.SetActive(false); settings.transform.SetAsLastSibling();
            BlockCrushTextContrastBuilder.ApplyToScene(scene);
            EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        }

        static void Booster(Transform design, string name, float x, float counterX, string icon, float iconX, float iconY,
            float iconWidth, float iconHeight, UnityAction action)
        {
            Button(name, design, "Buttons/Btn_AdventureBooster", x, 1976, 288, 147, action);
            Picture(name + "Icon", design, icon, iconX, iconY, iconWidth, iconHeight);
            Text(name + "Availability", design, "—", 64, counterX - 10, 1976, 85, 147);
        }

        static void ImportSprites()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Art.TrimEnd('/') }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                if (importer.textureType == TextureImporterType.Sprite && importer.spriteImportMode == SpriteImportMode.Single &&
                    !importer.mipmapEnabled && importer.textureCompression == TextureImporterCompression.Uncompressed) continue;
                importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false; importer.alphaIsTransparency = true; importer.wrapMode = TextureWrapMode.Clamp;
                importer.maxTextureSize = 4096; importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
        }
        static Sprite Sprite(string asset)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Art + asset + ".png");
            if (!sprite) throw new InvalidOperationException("Missing Figma asset: " + asset); return sprite;
        }
        static RectTransform Rect(string name, Transform parent)
        { var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform; rect.SetParent(parent, false); return rect; }
        static void Stretch(RectTransform rect)
        { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
        static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(width, height);
        }
        static Image Picture(string name, Transform parent, string asset, float x, float y, float width, float height)
        {
            var rect = Rect(name, parent); Place(rect, x, y, width, height);
            var image = rect.gameObject.AddComponent<Image>(); image.sprite = Sprite(asset); image.raycastTarget = false; return image;
        }
        static void Background(Transform parent, string asset)
        {
            var image = Picture("Background", parent, asset, 0, 0, 1, 1);
            image.rectTransform.anchorMin = image.rectTransform.anchorMax = image.rectTransform.pivot = Vector2.one * .5f;
            var fit = image.gameObject.AddComponent<AspectRatioFitter>(); fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = image.sprite.rect.width / image.sprite.rect.height;
        }
        static Text Text(string name, Transform parent, string value, int size, float x, float y, float width, float height)
        {
            var rect = Rect(name, parent); Place(rect, x, y, width, height);
            var text = rect.gameObject.AddComponent<Text>(); text.font = Font; text.fontStyle = FontStyle.Bold;
            text.fontSize = size; text.text = value; text.color = Color.white; text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false; return text;
        }
        static Button Button(string name, Transform parent, string asset, float x, float y, float width, float height, UnityAction action)
        {
            var image = Picture(name, parent, asset, x, y, width, height); image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            if (action != null) UnityEventTools.AddPersistentListener(button.onClick, action);
            return button;
        }
    }
}
