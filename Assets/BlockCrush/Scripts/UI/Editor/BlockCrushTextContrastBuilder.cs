using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BlockCrush.UI.Editor
{
    public static class BlockCrushTextContrastBuilder
    {
        [MenuItem("Block Crush/Improve Text Contrast")]
        public static string Build()
        {
            if (EditorApplication.isPlaying)
                throw new InvalidOperationException("Exit Play Mode first.");
            if (EditorSceneManager.GetActiveScene().isDirty)
                throw new InvalidOperationException("Save the current scene first.");
            int labels = 0;
            var prefab = PrefabUtility.LoadPrefabContents(BlockCrushMenuBuilder.PrefabPath);
            try
            {
                labels += ApplyToHierarchy(prefab);
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
                labels += ApplyToScene(scene);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(BlockCrushMenuBuilder.HomePath);
            return "Saved readable text colors/edges for " + labels + " labels across Home, both gameplay scenes and Settings prefab.";
        }

        public static int ApplyToScene(Scene scene)
        {
            int count = 0;
            foreach (var root in scene.GetRootGameObjects())
                count += ApplyToHierarchy(root);
            return count;
        }

        public static int ApplyToHierarchy(GameObject root)
        {
            int count = 0;
            foreach (var text in root.GetComponentsInChildren<Text>(true))
            {
                bool shop = Below(text.transform, "ShopDesign"), daily = Below(text.transform, "DailyDesign");
                string name = text.name;
                if (name.EndsWith("Balance") || (daily && (name == "Reward" || name == "TaskTitle")) || (shop && (name == "Boosters" || name == "Reverses" || name == "Bombs" || name == "Coins" || name == "Amount" || name == "RemoveAdsLabel" || name == "BundleTitle")) || (name == "Countdown" && Below(text.transform, "DailyTimer")))
                    ReadableText.OnLight(text);
                else if (name == "Price" || name == "Claim" || name == "SpinLabel_7_31149" || name == "AdventurePlayLabel" || (name == "Label" && (text.transform.parent.name == "ShopBundlesTab" || text.transform.parent.name == "ShopItemsTab" || text.transform.parent.name == "AdventureCompleteMapButton" || text.transform.parent.name == "AdventureNextLevelButton")))
                    ReadableText.OnButton(text);
                else if (name == "Amount" && Below(text.transform, "RotatingDisc"))
                {
                    bool darkSector = Below(text.transform, "RewardSlot0") || Below(text.transform, "RewardSlot7");
                    if (darkSector)
                        ReadableText.OnMixed(text, Color.white);
                    else
                        ReadableText.OnLight(text);
                }
                else if (name == "TaskProgress" || name == "DailyClaimedCount" || name == "RewardHeading" || name == "RewardAmount" || name == "RewardDetails" || name == "RewardOKLabel" || name == "DailyCountdown" || name == "DailyRule" || name == "ShopStatus" || name.StartsWith("Adventure") || name.StartsWith("LevelCompletion") || name.StartsWith("ComboLabel") || name == "SpinAvailability")
                    ReadableText.OnMixed(text, text.color);
                else
                    continue;
                count++;
                EditorUtility.SetDirty(text);
                PrefabUtility.RecordPrefabInstancePropertyModifications(text);
            }

            foreach (var image in root.GetComponentsInChildren<Image>(true))
            {
                if (image.name != "Label" || !image.transform.parent)
                    continue;
                string button = image.transform.parent.name;
                if (button == "AdventureButton")
                    image.color = ReadableText.Ink;
                else if (button == "PlayButton_Classic" || button == "MoreButton")
                    image.color = ReadableText.ButtonInk;
                else if (button == "ReplayButton" || button == "BackHomeButton")
                    ReadableText.RefreshButtonArtwork(image.transform.parent.GetComponent<Button>());
                else
                    continue;
                count++;
                EditorUtility.SetDirty(image);
                PrefabUtility.RecordPrefabInstancePropertyModifications(image);
            }

            return count;
        }

        static bool Below(Transform node, string name)
        {
            for (var parent = node.parent; parent; parent = parent.parent)
                if (parent.name == name)
                    return true;
            return false;
        }
    }
}
