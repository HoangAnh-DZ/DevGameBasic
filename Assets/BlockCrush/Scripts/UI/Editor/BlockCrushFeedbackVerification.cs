using System;
using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

namespace BlockCrush.UI.Editor
{
    public static partial class BlockCrushMenuVerification
    {
        public static string StartFeedback()
        {
            Start();
            stack.Clear();
            // Isolated feedback fixtures; Start/Finish snapshot and restore every Adventure key.
            PlayerPrefs.SetInt(AdventureProgress.UnlockedKey, 10);
            stack.Push(VerifyFeedback());
            return Status;
        }

        static IEnumerator Seconds(float seconds)
        {
            double until = EditorApplication.timeSinceStartup + seconds;
            while (EditorApplication.timeSinceStartup < until)
                yield return null;
        }

        static int SfxCount(GameAudioService audio) => Enum.GetValues(typeof(GameSfx)).Cast<GameSfx>().Sum(audio.Played);
        static IEnumerator VerifyFeedback()
        {
            GamePreferences.Set(GamePreferences.SoundKey, true);
            GamePreferences.Set(GamePreferences.BgmKey, true);
            Find<GameAudioService>().ApplyPreferences();
            for (int mode = 0; mode < 2; mode++)
            {
                if (mode == 0)
                {
                    yield return Click(Button("PlayButton_Classic"));
                    yield return Loaded("BlockCrushGameplay");
                }
                else
                {
                    yield return Click(Button("AdventureButton"));
                    Find<AdventurePanelController>().Select(10);
                    yield return Click(Button("AdventurePlayButton"));
                    yield return Loaded("BlockCrushAdventure");
                }

                var game = Find<BlockGameManager>();
                var audio = Find<GameAudioService>();
                var visual = Find<BlockGameFeedback>();
                string name = mode == 0 ? "Classic" : "Adventure";
                ValidateScene();
                Check(visual && ReferenceEquals(game.Feedback, visual), name + " shared presentation is attached");
                Check(UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length == 3, name + " has exactly two SFX voices and the existing BGM source");
                Check(!visual.Root.GetComponent<CanvasGroup>().blocksRaycasts && !visual.Label.raycastTarget, name + " combo never blocks dragging");
                Check(visual.Label.font.name == "LilitaOne-Regular", name + " Combo uses Figma Lilita One font");
                foreach (GameSfx cue in Enum.GetValues(typeof(GameSfx)))
                {
                    var clip = audio.Clip(cue);
                    Check(clip && clip.loadState == AudioDataLoadState.Loaded && clip.channels == 1 && clip.frequency == 22050, name + " loaded mobile PCM clip " + cue);
                    var samples = new float[clip.samples];
                    Check(clip.GetData(samples, 0) && samples.Any(v => Mathf.Abs(v) > .01f) && samples.All(v => Mathf.Abs(v) <= .45f), name + " non-silent bounded waveform " + cue);
                }

                yield return Resolution(1080, 1920);
                game.RestartGame();
                yield return Frames(3);
                var piece = game.Spawner.SpawnPiece(0, BlockShapeData.Database[0], game.Spawner.BlockSprites[0]);
                yield return Frames(3);
                int place = audio.Played(GameSfx.BlockPlace), invalid = audio.Played(GameSfx.InvalidPlacement);
                var origin = RectTransformUtility.WorldToScreenPoint(null, piece.transform.position);
                var outside = new Vector2(8, Screen.height - 8);
                InputSystem.QueueStateEvent(mouse, new MouseState { position = origin });
                yield return Frames(3);
                InputSystem.QueueStateEvent(mouse, new MouseState { position = origin }.WithButton(MouseButton.Left));
                yield return Frames(3);
                InputSystem.QueueStateEvent(mouse, new MouseState { position = origin + new Vector2(22, 26) }.WithButton(MouseButton.Left));
                yield return Frames(3);
                InputSystem.QueueStateEvent(mouse, new MouseState { position = outside }.WithButton(MouseButton.Left));
                yield return Frames(3);
                InputSystem.QueueStateEvent(mouse, new MouseState { position = outside });
                yield return Frames(3);
                Check(audio.Played(GameSfx.InvalidPlacement) == invalid + 1 && audio.Played(GameSfx.BlockPlace) == place && game.Score == 0, name + " invalid real drag plays only invalid SFX");
                Check(game.TryPlacePiece(piece, new Vector2Int(3, 3)) && audio.Played(GameSfx.BlockPlace) == place + 1, name + " accepted placement plays one pop");
                Check(!game.TryPlacePiece(piece, new Vector2Int(4, 4)) && audio.Played(GameSfx.BlockPlace) == place + 1 && game.Score == 1, name + " consumed callback cannot double-score or double-audio");
                game.RestartGame();
                yield return Frames(3);
                int expectedScore = 0;
                for (int tier = 1; tier <= 4; tier++)
                {
                    if (tier == 4)
                        yield return Resolution(1440, 2960);
                    game.Board.ResetBoard();
                    for (int x = 0; x < 7; x++)
                        game.Board.SetOccupied(x, 0, game.Spawner.BlockSprites[0]);
                    piece = game.Spawner.SpawnPiece(0, BlockShapeData.Database[0], game.Spawner.BlockSprites[0]);
                    int line = audio.Played(GameSfx.LineClear), combo = audio.Played(GameSfx.Combo);
                    Check(game.TryPlacePiece(piece, new Vector2Int(7, 0)) && audio.Played(GameSfx.LineClear) == line + 1, name + " clear starts one line SFX at tier " + tier);
                    yield return Seconds(.18f);
                    if (tier >= 2)
                        Check(audio.Played(GameSfx.Combo) == combo + 1 && visual.Visible && visual.Label.text == "COMBO+" + tier, name + " real clear combo visual/SFX synchronized at tier " + tier);
                    else
                        Check(audio.Played(GameSfx.Combo) == combo && !visual.Visible, name + " first clear does not invent a consecutive combo");
                    if (tier == 2 || tier == 4)
                    {
                        Canvas.ForceUpdateCanvases();
                        var corners = new Vector3[4];
                        visual.Root.GetWorldCorners(corners);
                        Check(corners.All(c =>
                        {
                            var p = RectTransformUtility.WorldToScreenPoint(null, c);
                            return Screen.safeArea.Contains(p);
                        }), name + " Combo fits safe area at " + Screen.width + "x" + Screen.height);
                        Check(visual.Label.color == (tier == 2 ? new Color32(133, 219, 12, 255) : new Color32(241, 193, 30, 255)), name + " Figma Combo tier color " + tier);
                        ScreenCapture.CaptureScreenshot("Captures/Figma/" + name + "_Combo" + tier + "_Unity_" + Screen.width + "x" + Screen.height + ".png");
                    }

                    while (game.IsBusy)
                        yield return null;
                    yield return Frames(2);
                    expectedScore += mode == 0 ? 101 + 25 * (tier - 1) : 11 + 5 * (tier - 1);
                    Check(game.Combo == tier && game.Score == expectedScore && audio.Played(GameSfx.LineClear) == line + 1, name + " animation preserves existing score and audio count at tier " + tier);
                }

                yield return Seconds(.95f);
                Check(!visual.Visible && visual.Root.GetComponent<CanvasGroup>().alpha == 0, name + " Combo fades out cleanly");
                piece = game.Spawner.SpawnPiece(0, BlockShapeData.Database[0], game.Spawner.BlockSprites[0]);
                Check(game.TryPlacePiece(piece, new Vector2Int(3, 3)) && game.Combo == 0 && !visual.Visible, name + " non-clear resets Combo without extra visual");
                game.RestartGame();
                game.Board.ResetBoard();
                for (int n = 0; n < 7; n++)
                {
                    game.Board.SetOccupied(n, 7, game.Spawner.BlockSprites[0]);
                    game.Board.SetOccupied(7, n, game.Spawner.BlockSprites[0]);
                }

                piece = game.Spawner.SpawnPiece(0, BlockShapeData.Database[0], game.Spawner.BlockSprites[0]);
                int multi = audio.Played(GameSfx.MultipleLineClear);
                Check(game.TryPlacePiece(piece, new Vector2Int(7, 7)) && audio.Played(GameSfx.MultipleLineClear) == multi + 1 && audio.AccentSource.clip == audio.Clip(GameSfx.MultipleLineClear), name + " intersecting lines select one Multi Clear clip");
                // Wait for the real DSP buffers; this Editor uses 1024x4 at 48kHz (~85ms).
                var output = new float[512];
                double audioDeadline = EditorApplication.timeSinceStartup + .5;
                bool audible = false;
                yield return Frames(1);
                while (!audible && EditorApplication.timeSinceStartup < audioDeadline)
                {
                    audio.AccentSource.GetOutputData(output, 0);
                    audible = output.Any(v => Mathf.Abs(v) > .00001f);
                    if (!audible)
                        yield return null;
                }

                Check(audible, name + " Sound ON produces actual DSP output");
                while (game.IsBusy)
                    yield return null;
                Check(game.Score == (mode == 0 ? 401 : 26) && audio.Played(GameSfx.MultipleLineClear) == multi + 1, name + " multi clear does not duplicate scoring or audio");
                // UI callbacks may call Feedback as well as the saved listener; one click only.
                int clicks = audio.Played(GameSfx.ButtonClick);
                Find<SettingsPanelController>().Open();
                Check(audio.Played(GameSfx.ButtonClick) == clicks + 1, name + " Settings click plays once");
                yield return Frames(8);
                if (GamePreferences.Bgm)
                    yield return Click(InSettings("BgmToggle"));
                Check(!GamePreferences.Bgm && audio.MusicSource.mute && !audio.SoundSource.mute, name + " BGM OFF leaves SFX ON");
                yield return Click(InSettings("CloseButton"));
                game.RestartGame();
                piece = game.Spawner.SpawnPiece(0, BlockShapeData.Database[0], game.Spawner.BlockSprites[0]);
                place = audio.Played(GameSfx.BlockPlace);
                game.TryPlacePiece(piece, new Vector2Int(3, 3));
                Check(audio.Played(GameSfx.BlockPlace) == place + 1, name + " SFX still plays with BGM OFF");
                Find<SettingsPanelController>().Open();
                yield return Frames(8);
                yield return Click(InSettings("BgmToggle"));
                audio.PlayMusic(audio.Clip(GameSfx.LevelComplete));
                yield return Click(InSettings("SoundToggle"));
                Check(!GamePreferences.Sound && GamePreferences.Bgm && audio.MusicSource.isPlaying && !audio.MusicSource.mute && audio.SoundSource.mute && audio.AccentSource.mute, name + " Sound OFF keeps independent BGM playing");
                int silent = SfxCount(audio);
                yield return Click(InSettings("CloseButton"));
                game.RestartGame();
                for (int tier = 0; tier < 2; tier++)
                {
                    game.Board.ResetBoard();
                    for (int x = 0; x < 7; x++)
                        game.Board.SetOccupied(x, 0, game.Spawner.BlockSprites[0]);
                    piece = game.Spawner.SpawnPiece(0, BlockShapeData.Database[0], game.Spawner.BlockSprites[0]);
                    game.TryPlacePiece(piece, new Vector2Int(7, 0));
                    while (game.IsBusy)
                        yield return null;
                }

                yield return Seconds(.09f);
                Check(SfxCount(audio) == silent && !audio.SoundSource.isPlaying && !audio.AccentSource.isPlaying && visual.Visible, name + " Sound OFF suppresses place/clear/combo/UI while visuals still work");
                game.RestartGame();
                Check(!visual.Visible && visual.Label.text == "" && !audio.SoundSource.isPlaying && !audio.AccentSource.isPlaying, name + " Restart removes old Combo and SFX");
                Find<SettingsPanelController>().Open();
                yield return Frames(8);
                yield return Click(InSettings("SoundToggle"));
                yield return Click(InSettings("CloseButton"));
                audio.MusicSource.Stop();
                audio.MusicSource.clip = null;
                audio.ApplyPreferences();
                TriggerGameOver(game);
                int failures = audio.Played(GameSfx.GameOver);
                game.CheckGameOver();
                Check(failures > 0 && audio.Played(GameSfx.GameOver) == failures && audio.AccentSource.clip == audio.Clip(GameSfx.GameOver), name + " failure audio is guarded against repeated callbacks");
                yield return Click(game.RestartButton);
                yield return Frames(3);
                Check(!visual.Visible && !audio.AccentSource.isPlaying && !game.IsGameOver && game.Score == 0, name + " actual Retry clears terminal sound and visual state");
                Find<SceneNavigationController>().CompleteHome();
                yield return Loaded("Home");
            }
        }
    }
}
