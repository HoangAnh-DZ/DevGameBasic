using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace BlockCrush.Editor
{
    public static class BlockCrushSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/BlockCrushGameplay.unity";
        static readonly Color White = new Color(.93f, .97f, 1);
        static Font font;

        [MenuItem("Block Crush/Build Gameplay Scene")]
        public static string Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before rebuilding.");
            var active = EditorSceneManager.GetActiveScene();
            if (active.isDirty) throw new InvalidOperationException("Current scene has unsaved changes; save it before building.");
            Directory.CreateDirectory("Assets/Prefabs/BlockCrush");
            Directory.CreateDirectory("Assets/Art/BlockCrushSpriteReferences");
            Directory.CreateDirectory("Assets/Scenes");
            AssetDatabase.Refresh();
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var sprites = new [] { "1 20 4", "2 689 4", "3 23 4", "4 37 11", "5 21 4", "7 15 4", "8 17 4", "9 12 4", "11 10 9" }
                .Select(n => ReferenceSprite("Assets/UI/arrow 3D/" + n + ".png", "Block_" + n.Replace(' ', '_'))).ToArray();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("BlockCrushGameplay");
            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.transform.SetParent(root.transform); cameraObject.tag = "MainCamera";
            var cam = cameraObject.GetComponent<Camera>(); cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(.04f, .1f, .28f); cam.orthographic = true;
            cam.transform.position = new Vector3(0, 0, -10);
            var light = new GameObject("Directional Light", typeof(Light)); light.transform.SetParent(root.transform);
            light.GetComponent<Light>().type = LightType.Directional; light.transform.rotation = Quaternion.Euler(50, -30, 0);
            var managerObject = new GameObject("GameManager", typeof(BlockGameManager)); managerObject.transform.SetParent(root.transform);
            var manager = managerObject.GetComponent<BlockGameManager>();

            var canvasRT = Rect("Canvas", root.transform);
            var canvas = canvasRT.gameObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasRT.gameObject.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920); scaler.matchWidthOrHeight = .5f;
            canvasRT.gameObject.AddComponent<GraphicRaycaster>();
            var background = Picture("Background", canvasRT, AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Asset 23-8.png"));
            var safe = Rect("SafeArea", canvasRT); Stretch(safe);
            var hud = Rect("TopHUD", safe);
            Label("LevelText", hud, "LEVEL 1", 32, new Vector2(.5f, 1), new Vector2(0, -30), new Vector2(500, 60));
            Label("ScoreLabel", hud, "SCORE", 26, new Vector2(.5f, 1), new Vector2(0, -100), new Vector2(500, 45));
            var score = Label("ScoreText", hud, "0", 90, new Vector2(.5f, 1), new Vector2(0, -175), new Vector2(700, 120));
            var best = Label("BestScoreText", hud, "BEST  0", 28, new Vector2(.5f, 1), new Vector2(0, -265), new Vector2(700, 50));
            var settings = Picture("Settings", hud, AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Setting 3.png"));
            Position(settings.rectTransform, new Vector2(1, 1), new Vector2(-48, -42), new Vector2(72, 72));
            settings.raycastTarget = true; settings.gameObject.AddComponent<Button>().targetGraphic = settings;

            var area = Rect("BoardArea", safe);
            var boardBg = Picture("BoardBackground", area, AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Group.png")); Stretch(boardBg.rectTransform);
            var boardRect = Rect("Board", area); Stretch(boardRect);
            boardRect.anchorMin = Vector2.one * .0055f; boardRect.anchorMax = Vector2.one * .9945f;
            var board = boardRect.gameObject.AddComponent<BlockBoard>();
            var cellObject = new GameObject("BoardCell", typeof(RectTransform), typeof(Image), typeof(BlockCell));
            cellObject.GetComponent<BlockCell>().Configure(0, 0);
            var prefab = PrefabUtility.SaveAsPrefabAsset(cellObject, "Assets/Prefabs/BlockCrush/BoardCell.prefab");
            UnityEngine.Object.DestroyImmediate(cellObject);
            board.SetCellPrefab(prefab.GetComponent<BlockCell>()); board.GenerateBoard();
            var combo = Label("ComboText", safe, "", 40, new Vector2(.5f, .26f), Vector2.zero, new Vector2(800, 80));
            combo.color = new Color(1, .85f, .28f);
            var tray = Rect("PieceTray", safe);
            var slots = new RectTransform[3];
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i] = Rect("PieceSlot_" + (i + 1), tray);
                slots[i].anchorMin = new Vector2(i / 3f, 0); slots[i].anchorMax = new Vector2((i + 1f) / 3, 1);
                slots[i].offsetMin = new Vector2(8, 0); slots[i].offsetMax = new Vector2(-8, 0);
            }
            var spawner = tray.gameObject.AddComponent<BlockPieceSpawner>(); spawner.Configure(slots, sprites, manager);
            Label("DragHint", safe, "DRAG BLOCKS TO THE GRID", 25, new Vector2(.5f, .055f), Vector2.zero, new Vector2(900, 50));
            var drag = Rect("DragLayer", safe); Stretch(drag);

            var modal = Rect("GameOverPanel", canvasRT); Stretch(modal);
            var dim = modal.gameObject.AddComponent<Image>(); dim.color = new Color(.01f, .025f, .09f, .87f);
            var card = Rect("Card", modal);
            Position(card, new Vector2(.5f, .5f), Vector2.zero, new Vector2(830, 650));
            var cardImage = card.gameObject.AddComponent<Image>(); cardImage.color = new Color(.045f, .105f, .28f);
            Label("Title", card, "GAME OVER", 70, new Vector2(.5f, .5f), new Vector2(0, 200), new Vector2(800, 100));
            var finalScore = Label("FinalScore", card, "Score  0", 44, new Vector2(.5f, .5f), new Vector2(0, 65), new Vector2(760, 70));
            var finalBest = Label("FinalBest", card, "Best  0", 35, new Vector2(.5f, .5f), new Vector2(0, -25), new Vector2(760, 60));
            var restartRT = Rect("RestartButton", card);
            Position(restartRT, new Vector2(.5f, .5f), new Vector2(0, -180), new Vector2(540, 110));
            var restartImage = restartRT.gameObject.AddComponent<Image>(); restartImage.color = new Color(.12f, .6f, .92f);
            var restart = restartRT.gameObject.AddComponent<Button>(); restart.targetGraphic = restartImage;
            var buttonLabel = Label("Label", restartRT, "RESTART", 42, new Vector2(.5f, .5f), Vector2.zero, new Vector2(530, 100));
            buttonLabel.raycastTarget = false;
            manager.Configure(board, spawner, drag, score, best, combo, modal.gameObject, finalScore, finalBest, restart);
            modal.gameObject.SetActive(false);
            var layout = canvasRT.gameObject.AddComponent<BlockPortraitLayout>(); layout.Configure(safe, hud, area, tray, background);

            var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule)); events.transform.SetParent(root.transform);
            events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true; PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false; PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.defaultScreenWidth = 1080; PlayerSettings.defaultScreenHeight = 1920;
            SetGameViewSize(1080, 1920); Canvas.ForceUpdateCanvases(); layout.Apply();
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene, ScenePath);
            var buildScenes = EditorBuildSettings.scenes.ToList();
            if (!buildScenes.Any(s => s.path == ScenePath)) buildScenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = buildScenes.ToArray(); AssetDatabase.SaveAssets();
            Selection.activeGameObject = canvasRT.gameObject;
            return "Saved " + ScenePath + "; cells=" + board.CellCount + "; block sprites=" + sprites.Length;
        }

        static Sprite ReferenceSprite(string path, string name)
        {
            string output = "Assets/Art/BlockCrushSpriteReferences/" + name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(output); if (existing) return existing;
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (!texture) throw new FileNotFoundException(path);
            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * .5f, 100, 0, SpriteMeshType.FullRect);
            sprite.name = name; AssetDatabase.CreateAsset(sprite, output); return sprite;
        }

        static RectTransform Rect(string name, Transform parent)
        {
            var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rt.SetParent(parent, false); return rt;
        }
        static void Stretch(RectTransform rt)
        { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero; }
        static void Position(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
        { rt.anchorMin = rt.anchorMax = anchor; rt.pivot = new Vector2(.5f, .5f); rt.anchoredPosition = pos; rt.sizeDelta = size; }
        static Image Picture(string name, Transform parent, Sprite sprite)
        { var rt = Rect(name, parent); var image = rt.gameObject.AddComponent<Image>(); image.sprite = sprite; image.raycastTarget = false; return image; }
        static Text Label(string name, Transform parent, string value, int size, Vector2 anchor, Vector2 position, Vector2 dimensions)
        {
            var rt = Rect(name, parent); Position(rt, anchor, position, dimensions);
            var text = rt.gameObject.AddComponent<Text>(); text.font = font; text.text = value; text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter; text.color = White; text.raycastTarget = false; return text;
        }

        public static string SetGameViewSize(int width, int height)
        {
            var assembly = typeof(UnityEditor.Editor).Assembly;
            var sizesType = assembly.GetType("UnityEditor.GameViewSizes");
            var singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            var sizes = singleton.GetProperty("instance").GetValue(null);
            var groupType = assembly.GetType("UnityEditor.GameViewSizeGroupType");
            var group = sizesType.GetMethod("GetGroup").Invoke(sizes, new [] { Enum.Parse(groupType, "Standalone") });
            var sizeType = assembly.GetType("UnityEditor.GameViewSize");
            var typeEnum = assembly.GetType("UnityEditor.GameViewSizeType");
            int count = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, null);
            int index = -1;
            for (int i = 0; i < count; i++)
            {
                var size = group.GetType().GetMethod("GetGameViewSize").Invoke(group, new object[] { i });
                if ((int)sizeType.GetProperty("width").GetValue(size) == width && (int)sizeType.GetProperty("height").GetValue(size) == height) { index = i; break; }
            }
            if (index < 0)
            {
                var size = Activator.CreateInstance(sizeType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null,
                    new object[] { Enum.Parse(typeEnum, "FixedResolution"), width, height, "Block Crush " + width + "x" + height }, null);
                group.GetType().GetMethod("AddCustomSize").Invoke(group, new [] { size }); index = count;
            }
            var gameType = assembly.GetType("UnityEditor.GameView");
            var window = EditorWindow.GetWindow(gameType);
            gameType.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(window, index);
            window.Focus(); window.Repaint(); return width + "x" + height;
        }
    }
}
