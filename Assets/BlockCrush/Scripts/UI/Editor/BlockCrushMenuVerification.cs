using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BlockCrush.UI.Editor
{
    // Integration checks use real mouse events through the saved scene's EventSystem.
    public static partial class BlockCrushMenuVerification
    {
        public static string Status = "Not started";
        static readonly Stack<IEnumerator> stack = new Stack<IEnumerator>();
        static readonly List<string> results = new List<string>();
        static readonly string[] keys = new[] { GamePreferences.SoundKey, GamePreferences.BgmKey, GamePreferences.VibrationKey, BlockGameManager.BestScoreKey, AdventureProgress.CompletedKey, AdventureProgress.TutorialKey, AdventureProgress.UnlockedKey }.Concat(Enumerable.Range(1,10).SelectMany(i=>new[]{AdventureProgress.BestKey(i),AdventureProgress.CompleteKey(i)})).ToArray();
        static readonly Dictionary<string, int?> prefs = new Dictionary<string, int?>();
        static Mouse mouse;
        static Keyboard navigationKeyboard;
        static int lastFrame;
        static double deadline;
        static bool runInBackground;
        static InputSettings.BackgroundBehavior background;
        static InputSettings.EditorInputBehaviorInPlayMode editorInput;
        static UnityEngine.Random.State random;
        static bool hadWheelSave;
        static string wheelSave;
        static bool hadEconomySave;
        static string economySave;
        static string reportPath="Captures/Figma/MenuVerification.md";

        public static string Start()
        {
            if (!EditorApplication.isPlaying || SceneManager.GetActiveScene().name != "Home") throw new InvalidOperationException("Launch Home in Play Mode first.");
            if (stack.Count != 0) throw new InvalidOperationException("Already running.");
            prefs.Clear(); foreach (var key in keys) prefs[key] = PlayerPrefs.HasKey(key) ? PlayerPrefs.GetInt(key) : (int?)null;
            hadWheelSave=PlayerPrefs.HasKey(LuckyWheelSave.Key); wheelSave=PlayerPrefs.GetString(LuckyWheelSave.Key,"");
            hadEconomySave=PlayerPrefs.HasKey(EconomySave.Key); economySave=PlayerPrefs.GetString(EconomySave.Key,"");
            reportPath="Captures/Figma/MenuVerification.md";
            runInBackground = Application.runInBackground; background = InputSystem.settings.backgroundBehavior;
            editorInput = InputSystem.settings.editorInputBehaviorInPlayMode; random = UnityEngine.Random.state;
            Application.runInBackground = true; InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            mouse = InputSystem.AddDevice<Mouse>("MenuVerificationMouse"); results.Clear();
            navigationKeyboard = InputSystem.AddDevice<Keyboard>("MenuVerificationKeyboard");
            lastFrame = -1; deadline = EditorApplication.timeSinceStartup + 360; Status = "Running";
            Directory.CreateDirectory("Captures/Figma"); stack.Push(Run()); EditorApplication.update += Tick; return Status;
        }
        static void Tick()
        {
            EditorApplication.QueuePlayerLoopUpdate();
            try
            {
                if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup > deadline) throw new Exception("Verification interrupted or timed out.");
                if (Time.frameCount == lastFrame) return; lastFrame = Time.frameCount;
                while (stack.Count > 0)
                {
                    var current = stack.Peek(); if (!current.MoveNext()) { stack.Pop(); continue; }
                    if (current.Current is IEnumerator child) { stack.Push(child); continue; } return;
                }
                Finish(null);
            }
            catch (Exception error) { Finish(error.ToString()); }
        }
        static void Finish(string error)
        {
            EditorApplication.update -= Tick; stack.Clear();
            var reward=Find<RewardPopupController>(); if(reward && reward.IsOpen) reward.Back();
            var daily=Find<DailyTasksController>(); if(daily) daily.Close();
            var shop=Find<ShopController>(); if(shop) shop.Close();
            var wheel=Find<LuckyWheelController>();
            if(wheel) { wheel.enabled=false; wheel.Close(); wheel.enabled=true; }
            if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse); mouse = null;
            if (navigationKeyboard != null && navigationKeyboard.added) InputSystem.RemoveDevice(navigationKeyboard); navigationKeyboard = null;
            foreach (var pair in prefs) { if (pair.Value.HasValue) PlayerPrefs.SetInt(pair.Key, pair.Value.Value); else PlayerPrefs.DeleteKey(pair.Key); }
            if(hadWheelSave) PlayerPrefs.SetString(LuckyWheelSave.Key,wheelSave); else PlayerPrefs.DeleteKey(LuckyWheelSave.Key);
            if(hadEconomySave) PlayerPrefs.SetString(EconomySave.Key,economySave); else PlayerPrefs.DeleteKey(EconomySave.Key);
            PlayerPrefs.Save(); UnityEngine.Random.state = random;
            AdventureProgress.ReturnToMap = false;
            var game = UnityEngine.Object.FindFirstObjectByType<BlockGameManager>(); if (game) { game.ResumeGame(); game.RestartGame(); }
            var audio = UnityEngine.Object.FindFirstObjectByType<GameAudioService>(); if (audio) audio.ApplyPreferences();
            var settings = UnityEngine.Object.FindFirstObjectByType<SettingsPanelController>(); if (settings) { if (settings.IsOpen) settings.Close(); settings.Refresh(); }
            InputSystem.settings.backgroundBehavior = background; InputSystem.settings.editorInputBehaviorInPlayMode = editorInput;
            Application.runInBackground = runInBackground;
            Status = error == null ? "PASS: " + results.Count + " checks" : "FAIL: " + error;
            File.WriteAllText(reportPath, Status + "\n\n" + string.Join("\n", results) +
                "\n\nInput: synthetic Mouse → InputSystemUIInputModule → EventSystem → saved Button.onClick listeners.\nOriginal preferences and best score restored.\n");
            Debug.Log("[BlockCrushMenuVerification] " + Status);
        }
        static void Check(bool condition, string label) { if (!condition) throw new Exception(label); results.Add("PASS: " + label); }
        static IEnumerator Frames(int count = 5) { for (int i = 0; i < count; i++) yield return null; }
        static IEnumerator Resolution(int width, int height)
        {
            BlockCrush.Editor.BlockCrushSceneBuilder.SetGameViewSize(width, height);
            // Game View repaint can apply the requested size after queued player frames.
            while (Screen.width != width || Screen.height != height) yield return null;
            yield return Frames(15);
            Canvas.ForceUpdateCanvases();
        }
        static IEnumerator Loaded(string scene)
        {
            while (SceneManager.GetActiveScene().name != scene) yield return null;
            while (!Find<SceneNavigationController>()) yield return null;
            yield return Frames(3);
            var overlay = Find<SceneNavigationController>().transform.Find("TransitionOverlay").GetComponent<CanvasGroup>();
            while (overlay.blocksRaycasts) yield return null;
            // Scene activation/fade may finish before the resized CanvasScaler and safe-area LateUpdate.
            yield return Frames(10); Canvas.ForceUpdateCanvases();
        }
        static T Find<T>() where T : UnityEngine.Object => UnityEngine.Object.FindFirstObjectByType<T>();
        static Button Button(string name) => GameObject.Find(name).GetComponent<Button>();
        static IEnumerator Click(Button button)
        {
            Check(button && button.isActiveAndEnabled && button.interactable, "Interactive button: " + button.name);
            var point = RectTransformUtility.WorldToScreenPoint(null, ((RectTransform)button.transform).TransformPoint(((RectTransform)button.transform).rect.center));
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point }); yield return Frames(2);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point }.WithButton(MouseButton.Left)); yield return Frames(2);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point }); yield return Frames(8);
        }
        static Button InSettings(string name) => Find<SettingsPanelController>().Panel.transform.Find("SafeArea/Card/" + name).GetComponent<Button>();
        static Button InMore(string name) => Find<MoreSettingsController>().transform.Find("MoreSettingsPopup/SafeArea/Card/" + name).GetComponent<Button>();
        static IEnumerator Capture(string name)
        {
            var settings = Find<SettingsPanelController>();
            while (settings && settings.IsOpen && settings.Panel.alpha < .999f) yield return null;
            yield return Frames(8); ScreenCapture.CaptureScreenshot("Captures/Figma/" + name + ".png"); yield return Frames(8);
        }
        static void ValidateScene()
        {
            var roots = SceneManager.GetActiveScene().GetRootGameObjects();
            Check(roots.SelectMany(r => r.GetComponentsInChildren<Transform>(true)).All(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) == 0), "No missing scripts: " + SceneManager.GetActiveScene().name);
            foreach (var component in roots.SelectMany(r => r.GetComponentsInChildren<MonoBehaviour>(true)).Where(c => c.GetType().Namespace == "BlockCrush.UI"))
            {
                if(component is WalletHud wallet) Check(wallet.Valid,"Wallet HUD has coin-only or complete inventory references");
                foreach (var field in component.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic).Where(f => f.IsDefined(typeof(SerializeField)) && typeof(UnityEngine.Object).IsAssignableFrom(f.FieldType)))
                {
                    if(component is WalletHud && field.Name!="coins") continue; // Coin-only wheel HUD intentionally omits item counters.
                    Check((UnityEngine.Object)field.GetValue(component), component.GetType().Name + "." + field.Name + " reference");
                }
            }
            // Empty core board cells deliberately have no sprite and are disabled.
            // Solid-color overlays/cards also deliberately use no sprite.
            var uiImages = roots.SelectMany(r => r.GetComponentsInChildren<Image>(true))
                .Where(i => SceneManager.GetActiveScene().name == "Home" || i.transform.IsChildOf(Find<SettingsPanelController>().transform));
            Check(uiImages.All(i => i.sprite || !i.enabled || i.color.a < .001f || i.name.EndsWith("Popup") || i.name == "TransitionOverlay" || i.name=="ModalInputBlocker"), "No missing sprites in Figma UI images");
            Check(Find<Canvas>().renderMode == RenderMode.ScreenSpaceOverlay && Find<CanvasScaler>().referenceResolution == new Vector2(1080, 1920), "Portrait Canvas configuration");
        }
        static IEnumerator Fits(RectTransform rect, Rect safe, string label)
        {
            // Measure natural layout after activation/resize over three stable player frames.
            // Do not force a fitter to repair the UI as part of the assertion.
            double until=EditorApplication.timeSinceStartup+1; int stable=0; var corners=new Vector3[4];
            while(stable<3 && EditorApplication.timeSinceStartup<until)
            {
                Canvas.ForceUpdateCanvases(); rect.GetWorldCorners(corners);
                bool fit=corners.All(c=>{var p=RectTransformUtility.WorldToScreenPoint(null,c);return p.x>=safe.xMin-2 && p.x<=safe.xMax+2 && p.y>=safe.yMin-2 && p.y<=safe.yMax+2;});
                stable=fit?stable+1:0; if(stable<3) yield return null;
            }
            Check(stable==3,label+(stable==3?"":"; screen="+Screen.width+"x"+Screen.height+"; safe="+safe+"; corners="+string.Join(" / ",corners.Select(c=>RectTransformUtility.WorldToScreenPoint(null,c).ToString()))));
        }
        static IEnumerator Run()
        {
            yield return Resolution(1080, 1920);
            Check(EditorBuildSettings.scenes[0].path == BlockCrushMenuBuilder.HomePath && EditorBuildSettings.scenes[1].path == BlockCrushMenuBuilder.GameplayPath, "Home startup / existing gameplay Build Settings order");
            ValidateScene(); Check(Find<MainMenuController>() && !Find<BlockGameManager>(), "Launch shows Home without gameplay running");
            yield return Capture("Home_Unity_1080x1920");
            // Sound fixtures must not depend on the player's saved ON/OFF state; Finish restores it.
            GamePreferences.Set(GamePreferences.SoundKey,true); Find<GameAudioService>().ApplyPreferences();
            yield return VerifyWheel();
            yield return VerifyCommerce();
            yield return VerifyAdventure();
            yield return VerifyFeedback();
            yield return Click(Button("SettingsButton")); Check(Find<SettingsPanelController>().IsOpen, "Home Settings opens");
            var keyButtons = new[] { "SoundToggle", "BgmToggle", "VibrationToggle" };
            for (int i = 0; i < 3; i++)
            {
                bool old = PlayerPrefs.GetInt(keys[i], 1) != 0;
                yield return Click(InSettings(keyButtons[i]));
                Check((PlayerPrefs.GetInt(keys[i]) != 0) != old, keys[i] + " changed and persisted");
                Check(InSettings(keyButtons[i]).GetComponent<Image>().sprite.name == (old ? "Toggle_Off" : "Toggle_On"), "Toggle image matches " + keys[i]);
            }
            Check(Find<GameAudioService>().SoundSource.mute == !GamePreferences.Sound && Find<GameAudioService>().MusicSource.mute == !GamePreferences.Bgm, "Sound and BGM AudioSource gates follow preferences");
            yield return Capture("Settings_Home_Unity_1080x1920");
            yield return Click(InSettings("MoreButton")); Check(Find<MoreSettingsController>().IsOpen, "More Settings opens");
            Check(Find<MoreSettingsController>().VersionText.text == "Version: " + Application.version, "Application.version shown");
            yield return Capture("MoreSettings_Unity_1080x1920");
            var actions = new[] { "ContactButton", "ShareButton", "TermsButton", "PrivacyButton", "AboutButton" };
            foreach (var action in actions)
            {
                yield return Click(InMore(action)); Check(Find<MoreSettingsController>().InformationOpen, action + " opens real information callback");
                yield return Click(Find<MoreSettingsController>().transform.Find("InformationPopup/SafeArea/Card/CloseButton").GetComponent<Button>());
                Check(!Find<MoreSettingsController>().InformationOpen && Find<MoreSettingsController>().IsOpen, "Return from information to More Settings");
            }
            yield return Click(InMore("BackButton")); Check(Find<SettingsPanelController>().IsOpen && !Find<MoreSettingsController>().IsOpen, "More Settings returns to Settings");
            yield return Click(InSettings("CloseButton"));
            yield return Resolution(1440, 2960);
            yield return Fits((RectTransform)GameObject.Find("HomeDesign_4_280").transform, Screen.safeArea, "1440x2960 Home fits safe area");
            yield return Capture("Home_Unity_1440x2960");
            yield return Click(Button("SettingsButton"));
            yield return Fits((RectTransform)Find<SettingsPanelController>().Panel.transform.Find("SafeArea/Card"), Screen.safeArea, "1440x2960 Settings fits safe area");
            yield return Capture("Settings_Home_Unity_1440x2960");
            yield return Click(InSettings("MoreButton"));
            yield return Fits((RectTransform)Find<MoreSettingsController>().transform.Find("MoreSettingsPopup/SafeArea/Card"), Screen.safeArea, "1440x2960 More Settings fits safe area");
            yield return Capture("MoreSettings_Unity_1440x2960");
            yield return Click(InMore("BackButton")); yield return Click(InSettings("CloseButton"));
            var safeController = GameObject.Find("Canvas/SafeArea").GetComponent<SafeAreaController>();
            var notch = new Rect(60, 100, Screen.width - 120, Screen.height - 260); safeController.enabled=false; safeController.Apply(notch); Canvas.ForceUpdateCanvases();
            GameObject.Find("HomeDesign_4_280").GetComponent<FigmaLayoutFitter>().Apply();
            yield return Fits((RectTransform)GameObject.Find("HomeDesign_4_280").transform, notch, "Simulated notch and bottom gesture area constrain Home content");
            safeController.enabled=true; safeController.Apply(Screen.safeArea);
            yield return Click(Button("PlayButton_Classic")); yield return Loaded("BlockCrushGameplay");
            Check(SceneManager.GetActiveScene().name == "BlockCrushGameplay" && Find<BlockGameManager>(), "Play navigates to existing gameplay scene");
            ValidateScene();
            var game = Find<BlockGameManager>(); var audio = Find<GameAudioService>();
            Check(audio.SoundSource.mute == !GamePreferences.Sound && audio.MusicSource.mute == !GamePreferences.Bgm, "Preferences restored in a fresh scene service");
            var piece = game.Spawner.SpawnPiece(0, BlockShapeData.Database[0], game.Spawner.BlockSprites[0]);
            Check(game.TryPlacePiece(piece, new Vector2Int(3, 3)), "Gameplay placement before opening Settings");
            int score = game.Score, combo = game.Combo, occupied = game.Board.OccupiedCount;
            var pieces = game.Spawner.Pieces.Where(p => p).Select(p => p.GetInstanceID()).ToArray();
            yield return Click(Button("Settings"));
            Check(game.IsPaused && !game.CanInteract && Find<SettingsPanelController>().IsOpen, "Gameplay Settings pauses interaction");
            Check(!game.TryPlacePiece(game.Spawner.Pieces.First(p => p), new Vector2Int(0, 0)), "Placement blocked while Settings open");
            yield return Capture("Settings_Gameplay_Unity_1440x2960");
            yield return Click(InSettings("CloseButton"));
            Check(!game.IsPaused && game.Score == score && game.Combo == combo && game.Board.OccupiedCount == occupied && pieces.SequenceEqual(game.Spawner.Pieces.Where(p => p).Select(p => p.GetInstanceID())), "Closing Settings preserves board, score, combo and piece instances");

            game.RestartGame(); var sprite = game.Spawner.BlockSprites[0];
            for (int x = 0; x < 7; x++) game.Board.SetOccupied(x, 0, sprite);
            piece = game.Spawner.SpawnPiece(0, BlockShapeData.Database[0], sprite);
            game.TryPlacePiece(piece, new Vector2Int(7, 0)); Find<SettingsPanelController>().Open();
            yield return Frames(12); Check(game.IsBusy && game.Board.OccupiedCount == 8 && game.Combo == 1, "Opening Settings freezes an in-progress line clear without losing state");
            yield return Click(InSettings("CloseButton")); while (game.IsBusy) yield return null;
            Check(game.Board.OccupiedCount == 0 && game.Score == 101 && game.Combo == 1, "Closing Settings resumes the same line clear");
            yield return Click(Button("Settings")); yield return Click(InSettings("ReplayButton"));
            Check(!game.IsPaused && !game.IsGameOver && game.Board.OccupiedCount == 0 && game.Score == 0 && game.Combo == 0 && game.Spawner.RemainingCount == 3, "Replay resets board / score / combo and creates three pieces");
            yield return Resolution(1080, 1920);
            yield return Click(Button("Settings"));
            yield return Fits((RectTransform)Find<SettingsPanelController>().Panel.transform.Find("SafeArea/Card"), Screen.safeArea, "1080x1920 Settings fits safe area");
            yield return Capture("Settings_Gameplay_Unity_1080x1920");
            yield return Click(InSettings("MoreButton"));
            yield return Fits((RectTransform)Find<MoreSettingsController>().transform.Find("MoreSettingsPopup/SafeArea/Card"), Screen.safeArea, "1080x1920 More Settings fits safe area");
            yield return Click(InMore("BackButton")); yield return Click(InSettings("BackHomeButton")); yield return Loaded("Home");
            Check(SceneManager.GetActiveScene().name == "Home" && !Find<BlockGameManager>() && SceneManager.sceneCount == 1, "Back Home unloads gameplay completely");
            yield return Fits((RectTransform)GameObject.Find("HomeDesign_4_280").transform, Screen.safeArea, "1080x1920 Home fits safe area");
            yield return Click(Button("PlayButton_Classic")); yield return Loaded("BlockCrushGameplay");
            Check(Find<BlockGameManager>().Score == 0 && Find<BlockGameManager>().Spawner.RemainingCount == 3, "Second Play starts a clean gameplay session");
            yield return VerifyGameOver();
        }

        static Button InformationClose() => Find<MoreSettingsController>().transform.Find("InformationPopup/SafeArea/Card/CloseButton").GetComponent<Button>();
        static IEnumerator VerifyAdventure()
        {
            foreach(var key in keys.Where(k=>k.StartsWith("BlockCrush.Adventure."))) PlayerPrefs.DeleteKey(key);
            int classicBest = PlayerPrefs.GetInt(BlockGameManager.BestScoreKey, 0), combo;
            Check(AdventureScoreSystem.Calculate(4,2,0,out combo)==29 && combo==1,"Adventure 4-cell / 2-line example scores exactly 29");
            int[] bonus={0,10,25,45,70,70};
            for(int lines=0;lines<bonus.Length;lines++) Check(AdventureScoreSystem.Calculate(1,lines,0,out combo)==1+bonus[lines],"Adventure line bonus " + lines);
            Check(AdventureScoreSystem.Calculate(1,1,8,out combo)==36 && combo==9,"Combo reward caps at 25");
            Check(AdventureScoreSystem.Calculate(3,0,5,out combo)==3 && combo==0,"A non-clearing move resets combo");
            yield return Click(Button("AdventureButton")); var map=Find<AdventurePanelController>(); var catalog=map.Catalog;
            Check(map.IsOpen && !Find<BlockGameManager>() && catalog.Count==10,"Adventure opens a ten-level map before gameplay");
            Check(AdventureProgress.Unlocked(catalog)==1 && Button("AdventureLevelButton1").interactable && !Button("AdventureLevelButton10").interactable,"Fresh save unlocks only level 1");
            Check(!AdventureProgress.Begin(0,catalog) && !AdventureProgress.Begin(2,catalog) && !AdventureProgress.Begin(11,catalog),"Invalid / locked level entry rejected");
            map.Select(10); Check(map.SelectedLevel==1,"Locked card cannot change selected level");
            var design=(RectTransform)map.transform.Find("SafeArea/AdventureMapDesign_7_128");
            var scroll=map.GetComponentInChildren<ScrollRect>();
            yield return Fits(design,Screen.safeArea,"1080 map fits safe area"); yield return Capture("AdventureMap_Unity_1080x1920");
            // Real mouse wheel reaches all ten cards, without moving the fixed title/footer.
            var point=RectTransformUtility.WorldToScreenPoint(null,scroll.viewport.TransformPoint(scroll.viewport.rect.center));
            InputSystem.QueueStateEvent(mouse,new MouseState{position=point,scroll=new Vector2(0,120)}); yield return Frames(10);
            Check(scroll.verticalNormalizedPosition>.01f,"Mouse wheel scrolls level content");
            scroll.verticalNormalizedPosition=1; yield return Frames(5);
            Check(RectTransformUtility.RectangleContainsScreenPoint(scroll.viewport, RectTransformUtility.WorldToScreenPoint(null, Button("AdventureLevelButton10").transform.position)),"Scroll can reveal level 10");
            scroll.verticalNormalizedPosition=0; yield return Frames(4);
            yield return Click(Button("AdventureBackButton")); Check(!map.IsOpen && Find<MainMenuController>(),"Map Back restores Home");
            // Legacy sequential saves migrate without resetting the completed levels.
            PlayerPrefs.SetInt(AdventureProgress.CompletedKey,3); PlayerPrefs.DeleteKey(AdventureProgress.UnlockedKey);
            Check(AdventureProgress.Unlocked(catalog)==4 && AdventureProgress.IsComplete(1) && AdventureProgress.IsComplete(3),"Old three-level progress migrates to level 4 unlocked");
            foreach(var key in keys.Where(k=>k.StartsWith("BlockCrush.Adventure."))) PlayerPrefs.DeleteKey(key);
            yield return Click(Button("AdventureButton")); yield return Resolution(1440,2960);
            yield return Fits(design,Screen.safeArea,"1440 map fits safe area"); yield return Capture("AdventureMap_Unity_1440x2960");
            var notch=new Rect(60,100,Screen.width-120,Screen.height-260);
            var safe=map.transform.Find("SafeArea").GetComponent<SafeAreaController>(); safe.enabled=false; safe.Apply(notch); design.GetComponent<FigmaLayoutFitter>().Apply(); Canvas.ForceUpdateCanvases();
            yield return Fits(design,notch,"Ten-level map respects notch / gesture bounds"); safe.enabled=true; safe.Apply(Screen.safeArea);
            yield return Click(Button("AdventurePlayButton")); yield return Loaded("BlockCrushAdventure"); ValidateScene();
            var game=Find<BlockGameManager>(); var level=Find<AdventureLevelController>();
            Check(level.Level==1 && game.Score==0 && level.MovesRemaining==catalog.MaxMoves(1) && level.TutorialVisible,"Level 1 starts with score zero, configured moves and initial guidance");
            Check(ReferenceEquals(game.Rules,level) && game.Spawner.RemainingCount==3 && game.Spawner.Pieces.Select(p=>p.Shape.id).Distinct().Count()==3,"Adventure rules attached before first unique three-piece tray");
            Check(game.Spawner.Pieces.All(p=>game.Board.CanShapeFitAnywhere(p.Shape)),"New tray contains fitting shapes when space exists");
            design=(RectTransform)GameObject.Find("AdventureDesign_7_1657").transform;
            yield return Fits(design,Screen.safeArea,"1440 gameplay fits safe area"); yield return Capture("AdventureLevel_Tutorial_Unity_1440x2960");
            var piece=game.Spawner.SpawnPiece(0,BlockShapeData.Database[0],game.Spawner.BlockSprites[0]); yield return Frames(4);
            Check(!game.TryPlacePiece(piece,new Vector2Int(-1,-1)) && level.MovesUsed==0 && game.Score==0,"Rejected placement consumes no move or score");
            var origin=RectTransformUtility.WorldToScreenPoint(null,piece.transform.position);
            var destination=RectTransformUtility.WorldToScreenPoint(null,game.Board.PlacementCenter(piece.Shape,new Vector2Int(3,3)));
            InputSystem.QueueStateEvent(mouse,new MouseState{position=origin}); yield return Frames(3);
            InputSystem.QueueStateEvent(mouse,new MouseState{position=origin}.WithButton(MouseButton.Left)); yield return Frames(3);
            InputSystem.QueueStateEvent(mouse,new MouseState{position=origin+new Vector2(20,25)}.WithButton(MouseButton.Left)); yield return Frames(3);
            InputSystem.QueueStateEvent(mouse,new MouseState{position=destination}.WithButton(MouseButton.Left)); yield return Frames(3);
            Check(piece.IsDragging && game.Board.PreviewCount==1,"Real Adventure drag shows board preview");
            InputSystem.QueueStateEvent(mouse,new MouseState{position=destination}); yield return Frames(8);
            Check(game.Score==1 && level.MovesUsed==1 && !level.TutorialVisible && PlayerPrefs.GetInt(AdventureProgress.TutorialKey)==1,"Accepted drop counts once and dismisses tutorial");
            Check(!game.TryPlacePiece(piece,new Vector2Int(4,4)) && game.Score==1 && level.MovesUsed==1,"Repeated callback cannot score a consumed piece twice");
            Check(GameObject.Find("AdventureProgressScore").GetComponent<Text>().text=="1" && GameObject.Find("AdventureMoves").GetComponent<Text>().text=="Moves "+level.MovesRemaining,"HUD shows real score and remaining moves");
            yield return Resolution(1080,1920); yield return Fits(design,Screen.safeArea,"1080 gameplay fits safe area"); yield return Capture("AdventureLevel_Unity_1080x1920");
            yield return Click(Button("AdventureBombButton")); Check(Find<MoreSettingsController>().InformationOpen,"Unspecified booster retains availability UI"); yield return Click(InformationClose());
            yield return Click(Button("AdventureSettingsButton")); Check(game.IsPaused,"Adventure Settings pauses level");
            yield return Click(InSettings("ReplayButton")); yield return Frames(3);
            Check(game.Score==0 && level.MovesUsed==0 && game.Combo==0 && game.Spawner.RemainingCount==3 && !level.TutorialVisible,"Replay resets session without deleting progress/tutorial state");
            // Intersecting row/column fixture tests exact-once live scoring and animation.
            var sprite=game.Spawner.BlockSprites[0];
            for(int i=0;i<7;i++) { game.Board.SetOccupied(i,7,sprite); game.Board.SetOccupied(7,i,sprite); }
            piece=game.Spawner.SpawnPiece(0,BlockShapeData.Database[0],sprite);
            Check(game.TryPlacePiece(piece,new Vector2Int(7,7)) && game.Score==26 && game.Combo==1 && level.MovesUsed==1,"Intersecting row and column score 1+25 once");
            int moves=level.MovesUsed; Check(!game.TryPlacePiece(game.Spawner.Pieces.First(p=>p),new Vector2Int(0,0)) && level.MovesUsed==moves,"Animation rejects another placement without consuming a move");
            while(game.IsBusy) yield return null; yield return Frames(3);
            Check(game.Board.OccupiedCount==0 && game.Score==26,"Animation callback does not score cleared cells twice");
            // Exhaust exactly the configured budget without line bonuses.
            game.RestartGame();
            for(int turn=0;turn<catalog.MaxMoves(1);turn++) { game.Board.ResetBoard(); piece=game.Spawner.SpawnPiece(0,BlockShapeData.Database[0],sprite); Check(game.TryPlacePiece(piece,new Vector2Int(3,3)),"Move exhaustion placement "+(turn+1)); }
            yield return Frames(3);
            Check(level.Lost && !level.Completed && level.MovesRemaining==0 && game.IsGameOver && game.GameOverPanel.activeSelf && AdventureProgress.Completed==0,"Exhausted moves lose without unlocking a level");
            yield return Capture("AdventureLose_Unity_1080x1920"); yield return Click(game.RestartButton); yield return Frames(3);
            Check(!level.Lost && !level.Completed && level.MovesUsed==0 && game.Score==0 && game.Board.OccupiedCount==0 && game.Spawner.RemainingCount==3,"Lose Retry resets board, score, moves, combo and tray");
            game.Board.ResetBoard(); for(int y=0;y<8;y++) for(int x=0;x<8;x++) if((x+y)%2==0) game.Board.SetOccupied(x,y,sprite);
            var domino=BlockShapeData.Database.First(p=>p.cells.Length==2); game.Spawner.ClearPieces(); for(int slot=0;slot<3;slot++) game.Spawner.SpawnPiece(slot,domino,sprite);
            Check(game.CheckGameOver() && level.Lost && level.FailureReason=="No space" && level.MovesRemaining==catalog.MaxMoves(1),"No available piece fit loses before move exhaustion");
            yield return Click(game.RestartButton); yield return Frames(3);
            for(int number=1;number<=10;number++)
            {
                game=Find<BlockGameManager>(); level=Find<AdventureLevelController>();
                Check(level.Level==number && level.MovesRemaining==catalog.MaxMoves(number),"Configured level "+number+" starts correctly");
                if(number==2) { yield return Resolution(1440,2960); yield return Capture("AdventureLevel_Unity_1440x2960"); }
                yield return CompleteAdventureTarget();
                Check(level.Completed && !level.Lost && !game.IsGameOver && !game.CanInteract && AdventureProgress.IsComplete(number),"Level "+number+" wins exclusively and persists completion");
                Check(AdventureProgress.Best(number)==game.Score && PlayerPrefs.GetInt(BlockGameManager.BestScoreKey,0)==classicBest,"Per-level Best persists independently of Classic for level "+number);
                Check(AdventureProgress.Unlocked(catalog)==Math.Min(10,number+1),"Sequential unlock after level "+number);
                int before=AdventureProgress.Unlocked(catalog); game.CheckGameOver(); level.EndTurn(game.Score,true);
                Check(level.Completed && !level.Lost && before==AdventureProgress.Unlocked(catalog),"Repeated terminal callbacks do not trigger a second result");
                var audio=Find<GameAudioService>(); int wins=audio.Played(GameSfx.LevelComplete); level.EndTurn(game.Score,true);
                Check(wins==1 && audio.Played(GameSfx.LevelComplete)==wins,"Level "+number+" complete SFX fires once");
                var homeButton=Button("AdventureCompleteMapButton"); var nextButton=Button("AdventureNextLevelButton");
                Check(homeButton.GetComponentInChildren<Text>().text=="Home" && homeButton.GetComponent<Image>().material.shader.name=="BlockCrush/UI Home Cyan","Level "+number+" Home is cyan with the Home label");
                Check(nextButton.GetComponent<Image>().sprite.name=="Btn_Green" && !nextButton.GetComponent<Image>().material.name.Contains("HomeCyan"),"Level "+number+" Next retains the original green design");
                if(number==1) { yield return Capture("AdventureComplete_Unity_1080x1920"); }
                int savedBest=AdventureProgress.Best(number); yield return Click(homeButton); yield return Loaded("Home"); map=UnityEngine.Object.FindFirstObjectByType<AdventurePanelController>(FindObjectsInactive.Include);
                Check(!map.IsOpen && Find<MainMenuController>() && AdventureProgress.Unlocked(catalog)==before && AdventureProgress.Best(number)==savedBest && PlayerPrefs.GetInt(BlockGameManager.BestScoreKey,0)==classicBest,"Level "+number+" Home returns to Home and preserves both saves");
                yield return Click(Button("AdventureButton")); map.Select(number); yield return Click(Button("AdventurePlayButton")); yield return Loaded("BlockCrushAdventure");
                yield return CompleteAdventureTarget(); game=Find<BlockGameManager>(); level=Find<AdventureLevelController>();
                if(number<10)
                { yield return Click(Button("AdventureNextLevelButton")); while(!Find<BlockGameManager>() || Find<BlockGameManager>()==game) yield return null; yield return Loaded("BlockCrushAdventure"); }
            }
            yield return Click(Button("AdventureNextLevelButton")); yield return Loaded("Home"); map=Find<AdventurePanelController>();
            Check(map.IsOpen && map.SelectedLevel==10 && AdventureProgress.Unlocked(catalog)==10 && !AdventureProgress.Begin(11,catalog),"Final level returns to map with level 10 as maximum");
            Check(GameObject.Find("LevelCompletion10").GetComponent<Text>().text.Contains("Best"),"Map shows completed state and individual Best");
            yield return Capture("AdventureMap_Level10_Unity_1440x2960");
            PlayerPrefs.Save(); yield return Click(Button("AdventureBackButton")); yield return Click(Button("AdventureButton"));
            Check(map.SelectedLevel==10 && AdventureProgress.IsComplete(1) && AdventureProgress.IsComplete(10),"Map reopens with persisted highest unlock and completion states");
            scroll=map.GetComponentInChildren<ScrollRect>(); scroll.verticalNormalizedPosition=0; yield return Frames(4);
            yield return Click(Button("LevelBadge1")); yield return Click(Button("AdventurePlayButton")); yield return Loaded("BlockCrushAdventure");
            Check(Find<AdventureLevelController>().Level==1 && Find<BlockGameManager>().BestScore==AdventureProgress.Best(1),"Completed level replays and reloads its own Best");
            yield return Click(Button("AdventureSettingsButton")); yield return Click(InSettings("BackHomeButton")); yield return Loaded("Home");
            Check(Find<AdventurePanelController>().IsOpen,"Back from gameplay returns to the saved map");
            InputSystem.QueueStateEvent(navigationKeyboard,new KeyboardState(Key.Escape)); yield return Frames(2); InputSystem.QueueStateEvent(navigationKeyboard,new KeyboardState()); yield return Frames(8);
            Check(Find<MainMenuController>() && !Find<AdventurePanelController>(),"Map Escape returns Home"); yield return Resolution(1080,1920);
        }

        static IEnumerator CompleteAdventureTarget()
        {
            var game=Find<BlockGameManager>(); var level=Find<AdventureLevelController>(); game.RestartGame(); yield return Frames(3);
            // Integration fixtures verify terminal ordering; balance simulations use no fixtures.
            int budget=level.MovesRemaining, threshold=level.Target;
            int terminalMove=Enumerable.Range(1,budget).First(n=>budget-n+11*n+Enumerable.Range(0,n).Sum(i=>Math.Min(25,i*5))>=threshold);
            // Waste the spare budget so the winning clear happens exactly on the final allowed move.
            int waste=budget-terminalMove;
            for(int i=0;i<waste;i++) { game.Board.ResetBoard(); var p=game.Spawner.SpawnPiece(0,BlockShapeData.Database[0],game.Spawner.BlockSprites[0]); Check(game.TryPlacePiece(p,new Vector2Int(3,3)),"Legal no-clear move before last-move win"); }
            for(int turn=0;!level.Completed && !level.Lost && turn<budget;turn++)
            {
                game.Board.ResetBoard(); for(int x=0;x<7;x++) game.Board.SetOccupied(x,0,game.Spawner.BlockSprites[0]);
                var piece=game.Spawner.SpawnPiece(0,BlockShapeData.Database[0],game.Spawner.BlockSprites[0]);
                Check(game.TryPlacePiece(piece,new Vector2Int(7,0)),"Adventure line clear scores toward target");
                while(game.IsBusy) yield return null; yield return Frames(2);
            }
            Check(level.Completed && game.Score>=level.Target && level.MovesRemaining==0,"Target reached on final move wins, with no simultaneous loss");
        }
        static void TriggerGameOver(BlockGameManager game)
        {
            var sprite = game.Spawner.BlockSprites[0]; game.RestartGame();
            var single = game.Spawner.SpawnPiece(0, BlockShapeData.Database[0], sprite);
            Check(game.TryPlacePiece(single, new Vector2Int(3, 3)) && game.Score == 1, "Game Over score comes from a real accepted placement");
            Check(!game.CheckGameOver(), "Game Over remains hidden while a remaining piece fits");
            game.Board.ResetBoard();
            for (int y = 0; y < 8; y++) for (int x = 0; x < 8; x++) if ((x + y) % 2 == 0) game.Board.SetOccupied(x, y, sprite);
            game.Spawner.ClearPieces();
            var domino = BlockShapeData.Database.First(shape => shape.cells.Length == 2);
            for (int slot = 0; slot < 3; slot++) game.Spawner.SpawnPiece(slot, domino, sprite);
            Check(game.CheckGameOver() && game.IsGameOver && game.GameOverPanel.activeSelf && !game.CanInteract, "Game Over appears through the existing no-fit detection");
        }
        static IEnumerator VerifyGameOver()
        {
            var game = Find<BlockGameManager>(); TriggerGameOver(game); yield return Frames(12);
            var design = (RectTransform)game.GameOverPanel.transform.Find("SafeArea/GameOverDesign_4_2");
            var score = design.Find("FinalScore").GetComponent<Text>(); int best = game.BestScore;
            Check(score.text == game.Score.ToString() && score.cachedTextGenerator.characterCountVisible == score.text.Length, "Actual final score is correct and generates visible glyphs");
            Check(!design.Find("FinalBest").gameObject.activeSelf && PlayerPrefs.GetInt(BlockGameManager.BestScoreKey) == best, "Figma hides Best on Game Over while its existing persistence is preserved");
            Check(design.GetComponentsInChildren<Image>(true).All(i => i.sprite), "Game Over has no missing Figma sprites");
            yield return Fits(design, Screen.safeArea, "1080x1920 Game Over fits safe area");
            yield return Capture("GameOver_Unity_1080x1920");
            yield return Resolution(1440, 2960);
            yield return Fits(design, Screen.safeArea, "1440x2960 Game Over fits safe area");
            Check(score.cachedTextGenerator.characterCountVisible == score.text.Length, "Final score remains visible at 1440x2960");
            yield return Capture("GameOver_Unity_1440x2960");
            var notch = new Rect(60, 100, Screen.width - 120, Screen.height - 260);
            var safe = game.GameOverPanel.transform.Find("SafeArea").GetComponent<SafeAreaController>(); safe.enabled=false; safe.Apply(notch); Canvas.ForceUpdateCanvases(); design.GetComponent<FigmaLayoutFitter>().Apply();
            yield return Fits(design, notch, "Game Over respects simulated notch and bottom gesture area"); safe.enabled=true; safe.Apply(Screen.safeArea);
            yield return Click(game.RestartButton);
            Check(!game.IsGameOver && game.Score == 0 && game.Combo == 0 && game.Board.OccupiedCount == 0 && game.Spawner.RemainingCount == 3 && game.BestScore == best, "Game Over Play/Restart resets only the session and preserves Best Score");
            TriggerGameOver(game); yield return Frames(8);
            yield return Click(design.Find("HomeButton").GetComponent<Button>()); yield return Loaded("Home");
            Check(SceneManager.GetActiveScene().name == "Home" && SceneManager.sceneCount == 1 && !Find<BlockGameManager>(), "Game Over Home button unloads gameplay and opens Home");
            Check(PlayerPrefs.GetInt(BlockGameManager.BestScoreKey) == best, "Best Score persists through Game Over Home navigation");
        }
    }
}


