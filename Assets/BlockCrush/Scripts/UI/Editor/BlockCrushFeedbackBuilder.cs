using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace BlockCrush.UI.Editor
{
    public static class BlockCrushFeedbackBuilder
    {
        const string Folder = "Assets/BlockCrush/Audio/SFX/";
        const string MaterialPath = "Assets/BlockCrush/Art/HomeCyan.mat";
        [MenuItem("Block Crush/Update Win, SFX and Combo")]
        public static string Build()
        {
            UiBuilderUtility.RequireSavedEditMode();
            Directory.CreateDirectory(Folder);
            var names = Enum.GetNames(typeof(GameSfx));
            double[] lengths =
            {
                .05,
                .2,
                .27,
                .22,
                .09,
                .06,
                .7,
                .42
            };
            double[][] notes =
            {
                new[]
                {
                    620d,
                    850
                },
                new[]
                {
                    784d,
                    1175,
                    1568
                },
                new[]
                {
                    659d,
                    988,
                    1319,
                    1760
                },
                new[]
                {
                    880d,
                    1175,
                    1568
                },
                new[]
                {
                    330d,
                    260
                },
                new[]
                {
                    740d
                },
                new[]
                {
                    523d,
                    659,
                    784,
                    1047
                },
                new[]
                {
                    392d,
                    330,
                    262
                }
            };
            var clips = new AudioClip[8];
            for (int cue = 0; cue < 8; cue++)
            {
                string path = Folder + names[cue] + ".wav";
                WriteWave(path, lengths[cue], notes[cue]);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var importer = (AudioImporter)AssetImporter.GetAtPath(path);
                importer.forceToMono = true;
                importer.loadInBackground = false;
                var importFields = new SerializedObject(importer);
                var normalize = importFields.FindProperty("m_Normalize");
                if (normalize != null)
                {
                    normalize.boolValue = false;
                    importFields.ApplyModifiedPropertiesWithoutUndo();
                }

                var options = importer.defaultSampleSettings;
                options.loadType = AudioClipLoadType.DecompressOnLoad;
                options.preloadAudioData = true;
                options.compressionFormat = AudioCompressionFormat.PCM;
                options.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
                importer.defaultSampleSettings = options;
                importer.SaveAndReimport();
                clips[cue] = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            }

            var shader = Shader.Find("BlockCrush/UI Home Cyan");
            if (!shader)
                throw new Exception("Home shader missing.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (!material)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            else
            {
                material.shader = shader;
                EditorUtility.SetDirty(material);
            }

            var font = AssetDatabase.LoadAssetAtPath<Font>("Assets/BlockCrush/Fonts/LilitaOne-Regular.ttf");
            if (!font)
                throw new Exception("Figma Combo font Lilita One missing.");
            var prefab = PrefabUtility.LoadPrefabContents(BlockCrushMenuBuilder.PrefabPath);
            try
            {
                ConfigureAudio(prefab.GetComponent<GameAudioService>(), clips);
                AddClicks(prefab.GetComponentsInChildren<Button>(true), prefab.GetComponent<GameAudioService>());
                PrefabUtility.SaveAsPrefabAsset(prefab, BlockCrushMenuBuilder.PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefab);
            }

            foreach (var path in new[]
            {
                BlockCrushMenuBuilder.HomePath,
                BlockCrushMenuBuilder.GameplayPath,
                BlockCrushScreenBuilder.AdventurePath
            }

            )
            {
                var scene = EditorSceneManager.OpenScene(path);
                var audio = UnityEngine.Object.FindFirstObjectByType<GameAudioService>();
                ConfigureAudio(audio, clips);
                var game = UnityEngine.Object.FindFirstObjectByType<BlockGameManager>();
                if (game)
                    ConfigureGameplay(game, audio, font);
                if (path == BlockCrushScreenBuilder.AdventurePath)
                    ConfigureHome(audio.GetComponent<SceneNavigationController>(), material);
                AddClicks(UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None), audio);
                BlockCrushTextContrastBuilder.ApplyToScene(scene);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(BlockCrushMenuBuilder.HomePath);
            return "Saved Win Home, 8 synthesized PCM clips, 2 SFX voices and Figma Combo in both gameplay scenes.";
        }

        static void ConfigureAudio(GameAudioService audio, AudioClip[] clips)
        {
            var child = audio.transform.Find("SfxAccent");
            if (!child)
            {
                var obj = new GameObject("SfxAccent", typeof(AudioSource));
                obj.transform.SetParent(audio.transform, false);
                child = obj.transform;
            }

            var accent = child.GetComponent<AudioSource>();
            foreach (var source in new[]
            {
                audio.SoundSource,
                accent
            }

            )
            {
                source.playOnAwake = false;
                source.loop = false;
                source.spatialBlend = 0;
                source.volume = .5f;
                source.priority = 96;
            }

            audio.ConfigureEffects(accent, clips);
            EditorUtility.SetDirty(audio);
        }

        static void ConfigureGameplay(BlockGameManager game, GameAudioService audio, Font font)
        {
            var legacy = game.Board.transform.parent.parent.Find("ComboText");
            if (legacy)
                legacy.gameObject.SetActive(false);
            var root = game.Board.transform.Find("ComboFeedback") as RectTransform;
            if (!root)
            {
                root = (RectTransform)new GameObject("ComboFeedback", typeof(RectTransform), typeof(CanvasGroup)).transform;
                root.SetParent(game.Board.transform, false);
                root.anchorMin = root.anchorMax = new Vector2(.5f, 1);
                root.pivot = Vector2.one * .5f;
                root.sizeDelta = new Vector2(560, 160);
                var obj = new GameObject("ComboLabel_4_70_4_69", typeof(RectTransform), typeof(Text), typeof(Shadow));
                obj.transform.SetParent(root, false);
                var rect = (RectTransform)obj.transform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
            }

            var group = root.GetComponent<CanvasGroup>();
            group.alpha = 0;
            group.blocksRaycasts = false;
            group.interactable = false;
            var text = root.GetComponentInChildren<Text>(true);
            text.font = font;
            text.fontStyle = FontStyle.Normal;
            text.fontSize = 80;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            text.text = "";
            var shadow = text.GetComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, .4f);
            shadow.effectDistance = new Vector2(0, -10);
            shadow.useGraphicAlpha = true;
            var presenter = game.GetComponent<BlockGameFeedback>();
            if (!presenter)
                presenter = game.gameObject.AddComponent<BlockGameFeedback>();
            presenter.Configure(game, audio, root, group, text);
            EditorUtility.SetDirty(presenter);
            // Retain the original manager's legacy text reference, but hide its static presentation.
            var fields = new SerializedObject(game);
            var old = fields.FindProperty("comboText").objectReferenceValue as Text;
            if (old)
                old.gameObject.SetActive(false);
        }

        static void ConfigureHome(SceneNavigationController navigation, Material material)
        {
            var button = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(b => b.name == "AdventureCompleteMapButton");
            button.GetComponent<Image>().material = material;
            button.GetComponentInChildren<Text>(true).text = "Home";
            for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
                UnityEventTools.RemovePersistentListener(button.onClick, i);
            UnityEventTools.AddPersistentListener(button.onClick, navigation.CompleteHome);
            EditorUtility.SetDirty(button);
        }

        static void AddClicks(Button[] buttons, GameAudioService audio)
        {
            foreach (var button in buttons)
            {
                bool exists = false;
                for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
                    if (button.onClick.GetPersistentTarget(i) == audio && button.onClick.GetPersistentMethodName(i) == nameof(GameAudioService.Feedback))
                        exists = true;
                if (!exists)
                {
                    UnityEventTools.AddPersistentListener(button.onClick, audio.Feedback);
                    EditorUtility.SetDirty(button);
                }
            }
        }

        // Original synthesized short sounds; no samples or third-party recordings.
        static void WriteWave(string path, double seconds, double[] notes)
        {
            const int rate = 22050;
            int count = (int)(seconds * rate);
            using (var file = new BinaryWriter(File.Create(path)))
            {
                file.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
                file.Write(36 + count * 2);
                file.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
                file.Write(16);
                file.Write((short)1);
                file.Write((short)1);
                file.Write(rate);
                file.Write(rate * 2);
                file.Write((short)2);
                file.Write((short)16);
                file.Write(System.Text.Encoding.ASCII.GetBytes("data"));
                file.Write(count * 2);
                double section = seconds / notes.Length;
                for (int i = 0; i < count; i++)
                {
                    double t = i / (double)rate;
                    int note = Math.Min(notes.Length - 1, (int)(t / section));
                    double local = t - note * section;
                    double envelope = Math.Min(1, local / .003) * Math.Pow(Math.Max(0, 1 - local / section), 1.6);
                    double value = (Math.Sin(2 * Math.PI * notes[note] * local) + .22 * Math.Sin(4 * Math.PI * notes[note] * local)) * .26 * envelope;
                    file.Write((short)(Math.Max(-.45, Math.Min(.45, value)) * 32767));
                }
            }
        }
    }
}
