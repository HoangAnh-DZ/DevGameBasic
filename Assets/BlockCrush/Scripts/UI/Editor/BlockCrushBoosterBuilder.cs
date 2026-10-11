using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static BlockCrush.UI.Editor.UiBuilderUtility;

namespace BlockCrush.UI.Editor
{
    public static class BlockCrushBoosterBuilder
    {
        [MenuItem("Block Crush/Update Gameplay Boosters")]
        public static string Build()
        {
            RequireSavedEditMode();
            string previous = SceneManager.GetActiveScene().path;
            try
            {
                foreach (var path in new[]
                {
                    BlockCrushMenuBuilder.GameplayPath,
                    BlockCrushScreenBuilder.AdventurePath
                }

                )
                {
                    var scene = EditorSceneManager.OpenScene(path);
                    ConfigureScene(scene);
                    EditorSceneManager.SaveScene(scene);
                }
            }
            finally
            {
                if (!string.IsNullOrEmpty(previous))
                    EditorSceneManager.OpenScene(previous);
            }

            return "Saved Bomb, Refresh and +1 single-block boosters in Classic and Adventure.";
        }

        public static void ConfigureScene(Scene scene)
        {
            var roots = scene.GetRootGameObjects();
            var game = roots.SelectMany(r => r.GetComponentsInChildren<BlockGameManager>(true)).Single();
            var settings = roots.SelectMany(r => r.GetComponentsInChildren<SettingsPanelController>(true)).Single();
            var controller = game.GetComponent<GameplayBoosterController>();
            if (!controller)
                controller = game.gameObject.AddComponent<GameplayBoosterController>();
            bool adventure = game.GetComponent<AdventureLevelController>();
            Transform design;
            if (adventure)
                design = roots.SelectMany(r => r.GetComponentsInChildren<Transform>(true)).Single(t => t.name == "AdventureDesign_7_1657");
            else
            {
                var safe = game.Spawner.Slots[0].parent.parent as RectTransform;
                var bar = safe.Find("ClassicBoosterBar") as RectTransform;
                if (!bar)
                {
                    bar = Rect("ClassicBoosterBar", safe);
                    bar.anchorMin = new Vector2(.06f, 0);
                    bar.anchorMax = new Vector2(.94f, 0);
                    bar.pivot = new Vector2(.5f, 0);
                    bar.offsetMin = bar.offsetMax = Vector2.zero;
                    bar.anchoredPosition = new Vector2(0, 2);
                    var aspect = bar.gameObject.AddComponent<AspectRatioFitter>();
                    aspect.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
                    aspect.aspectRatio = 1000f / 160;
                    var rect = Rect("ClassicBoosterDesign", bar);
                    rect.gameObject.AddComponent<FigmaLayoutFitter>().Configure(new Vector2(1000, 160));
                    CreateClassicButton(rect, "ClassicBombButton", 0, "Icon_AdventureBomb");
                    CreateClassicButton(rect, "ClassicReverseButton", 356, "Icon_AdventureReverse");
                    CreateClassicButton(rect, "ClassicThirdBoosterButton", 712, "Icon_AdventureBooster");
                    bar.gameObject.AddComponent<GameplayBoosterLayout>().Configure(bar, game.Spawner.Slots[0].parent as RectTransform, game.Spawner);
                }

                design = bar.Find("ClassicBoosterDesign");
                var guide = safe.GetComponentsInChildren<Text>(true).Single(t => t.name == "DragHint");
                var guideRect = guide.rectTransform;
                guideRect.SetParent(game.Board.transform, false);
                guideRect.anchorMin = new Vector2(0, 1);
                guideRect.anchorMax = Vector2.one;
                guideRect.pivot = new Vector2(.5f, 0);
                guideRect.offsetMin = guideRect.offsetMax = Vector2.zero;
                guideRect.sizeDelta = new Vector2(0, 40);
                guideRect.anchoredPosition = new Vector2(0, 22);
            }

            string prefix = adventure ? "Adventure" : "Classic";
            var bomb = FindButton(design, prefix + "BombButton");
            var refresh = FindButton(design, prefix + "ReverseButton");
            var single = FindButton(design, prefix + "ThirdBoosterButton");
            if (adventure)
            {
                ExpandAdventureTarget(bomb);
                ExpandAdventureTarget(refresh);
                ExpandAdventureTarget(single);
                var layout = game.Spawner.GetComponent<GameplayBoosterLayout>();
                if (!layout)
                    layout = game.Spawner.gameObject.AddComponent<GameplayBoosterLayout>();
                layout.Configure(design as RectTransform, game.Spawner.Slots[0].parent as RectTransform, game.Spawner, false);
                EditorUtility.SetDirty(layout);
            }

            Bind(bomb, controller, controller.ToggleBomb);
            Bind(refresh, controller, controller.RefreshTray);
            Bind(single, controller, controller.UseSingleBlock);
            var bombs = FindText(design, bomb.name + "Availability");
            var refreshes = FindText(design, refresh.name + "Availability");
            var singles = FindText(design, single.name + "Availability");
            foreach (var text in new[]
            {
                bombs,
                refreshes,
                singles
            }

            )
            {
                text.text = "0";
                text.resizeTextForBestFit = true;
                text.resizeTextMinSize = 28;
                text.resizeTextMaxSize = text.fontSize;
                ReadableText.OnButton(text);
            }

            var notice = design.Find("BoosterNotice") as RectTransform;
            if (!notice)
            {
                notice = Rect("BoosterNotice", design);
                Place(notice, adventure ? 74 : 0, adventure ? 2140 : -64, adventure ? 1285 : 1000, adventure ? 60 : 60);
                var background = notice.gameObject.AddComponent<Image>();
                background.color = new Color(.035f, .02f, .12f, .88f);
                background.raycastTarget = false;
                var label = Rect("Label", notice);
                Stretch(label);
                CreateText(label, "", adventure ? 42 : 36);
            }

            var status = notice.GetComponentInChildren<Text>(true);
            status.text = "";
            notice.gameObject.SetActive(false);
            var overlay = game.Board.transform.Find("BombTargetOverlay") as RectTransform;
            if (!overlay)
            {
                overlay = Rect("BombTargetOverlay", game.Board.transform);
                Stretch(overlay);
                overlay.gameObject.AddComponent<Image>().color = Color.clear;
                overlay.gameObject.AddComponent<BombTargetInput>();
                for (int i = 0; i < 9; i++)
                {
                    var highlight = Rect("BlastCell_" + i, overlay).gameObject.AddComponent<Image>();
                    highlight.raycastTarget = false;
                    highlight.enabled = false;
                }
            }

            var input = overlay.GetComponent<BombTargetInput>();
            var surface = overlay.GetComponent<Image>();
            var highlights = overlay.Cast<Transform>().Select(t => t.GetComponent<Image>()).ToArray();
            input.Configure(controller, surface, highlights);
            controller.Configure(game, settings, settings.GetComponent<MoreSettingsController>(), settings.GetComponent<SceneNavigationController>(), settings.GetComponent<GameAudioService>(), bomb, refresh, single, bombs, refreshes, singles, status, input);
            overlay.gameObject.SetActive(false);
            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(input);
            EditorSceneManager.MarkSceneDirty(scene);
        }

        static void CreateClassicButton(Transform parent, string name, float x, string icon)
        {
            var hit = Rect(name, parent);
            Place(hit, x, 0, 288, 160);
            hit.gameObject.AddComponent<Image>().color = Color.clear;
            var button = hit.gameObject.AddComponent<Button>();
            var art = Rect("BoosterArtwork", hit);
            Place(art, 0, 6.5f, 288, 147);
            button.targetGraphic = Picture(art, "Buttons/Btn_AdventureBooster");
            var symbol = Rect(name + "Icon", parent);
            Place(symbol, x + 18, 30, 96, 96);
            Picture(symbol, "Icons/" + icon);
            var count = Rect(name + "Availability", parent);
            Place(count, x + 192, 10, 85, 147);
            CreateText(count, "0", 64);
        }

        static void ExpandAdventureTarget(Button button)
        {
            var hit = (RectTransform)button.transform;
            var image = button.GetComponent<Image>();
            var art = hit.Find("BoosterArtwork") as RectTransform;
            if (!art)
            {
                art = Rect("BoosterArtwork", hit);
                art.sizeDelta = hit.sizeDelta;
                var added = art.gameObject.AddComponent<Image>();
                added.sprite = image.sprite;
                added.raycastTarget = false;
            }

            const float touchHeight = 260;
            float previousHeight = hit.sizeDelta.y;
            float artworkHeight = art.sizeDelta.y;
            Place(art, 0, (touchHeight - artworkHeight) * .5f, hit.sizeDelta.x, artworkHeight);
            image.sprite = null;
            image.color = Color.clear;
            hit.anchoredPosition += new Vector2(0, (touchHeight - previousHeight) * .5f);
            hit.sizeDelta = new Vector2(hit.sizeDelta.x, touchHeight);
            button.targetGraphic = art.GetComponent<Image>();
        }

        static void Bind(Button button, GameplayBoosterController controller, UnityAction action)
        {
            for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
                if (button.onClick.GetPersistentTarget(i) == controller || (button.onClick.GetPersistentTarget(i) is AdventureLevelController && button.onClick.GetPersistentMethodName(i) == "Booster"))
                    UnityEventTools.RemovePersistentListener(button.onClick, i);
            UnityEventTools.AddPersistentListener(button.onClick, action);
        }

        static Button FindButton(Transform parent, string name) => parent.Find(name).GetComponent<Button>();
        static Text FindText(Transform parent, string name) => parent.Find(name).GetComponent<Text>();
        static Image Picture(RectTransform rect, string asset)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BlockCrushMenuBuilder.Art + asset + ".png");
            if (!image.sprite)
                throw new InvalidOperationException("Missing booster sprite: " + asset);
            image.raycastTarget = false;
            return image;
        }

        static Text CreateText(RectTransform rect, string value, int size)
        {
            var text = rect.gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontStyle = FontStyle.Bold;
            text.fontSize = size;
            text.text = value;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            ReadableText.OnMixed(text, Color.white);
            return text;
        }
    }
}
