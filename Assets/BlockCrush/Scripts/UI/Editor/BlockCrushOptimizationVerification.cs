using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace BlockCrush.UI.Editor
{
    public static partial class BlockCrushMenuVerification
    {
        public static string StartOptimization()
        {
            Start();
            stack.Clear();
            reportPath = "Captures/Figma/OptimizationVerification.md";
            stack.Push(VerifyOptimization());
            return Status;
        }

        static int LayoutApplications() => FindObjects<FigmaLayoutFitter>().Sum(f => f.LayoutApplications) + FindObjects<SafeAreaController>().Sum(f => f.LayoutApplications) + FindObjects<ShopResponsiveLayout>().Sum(f => f.LayoutApplications) + FindObjects<BlockPortraitLayout>().Sum(f => f.LayoutApplications);
        static T[] FindObjects<T>()
            where T : UnityEngine.Object => UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        static IEnumerator StableView(string name)
        {
            yield return Frames(12);
            int loads = EconomySave.LoadCount, layouts = LayoutApplications();
            yield return Seconds(1.2f);
            Check(EconomySave.LoadCount == loads, name + " idle does not reparse wallet JSON");
            Check(LayoutApplications() == layouts, name + " stable geometry is not reapplied every frame");
        }

        static IEnumerator VerifyOptimization()
        {
            yield return Resolution(1080, 1920);
            PlayerPrefs.DeleteKey(EconomySave.Key);
            PlayerPrefs.DeleteKey(LuckyWheelSave.Key);
            var initial = EconomySave.Load();
            initial.dailyPlays = 1;
            initial.Save();
            var daily = Find<DailyTasksController>();
            daily.Open();
            yield return StableView("Daily");
            Check(!Button("DailyClaimButton0").interactable, "One round keeps the original first milestone locked");
            initial.dailyPlays = 2;
            initial.Save();
            yield return Frames(3);
            Check(Button("DailyClaimButton0").interactable && GameObject.Find("DailyTask0/TaskProgress").GetComponent<Text>().text == "2/2", "A completed save updates Daily without reopening or polling JSON");
            var refreshDaily = typeof(DailyTasksController).GetMethod("Refresh", BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(DateTime) }, null);
            var tomorrow = DateTime.UtcNow.Date.AddDays(1);
            refreshDaily.Invoke(daily, new object[] { tomorrow });
            Check(!Button("DailyClaimButton0").interactable && EconomySave.Load(tomorrow).dailyPlays == 0, "UTC rollover still resets only Daily progress");
            refreshDaily.Invoke(daily, new object[] { tomorrow.AddDays(-1) });
            Check(!Button("DailyClaimButton0").interactable, "Clock rollback does not reopen an old claim");
            daily.Close();
            var shop = Find<ShopController>();
            shop.Open();
            yield return StableView("Shop wallet");
            var wallet = EconomySave.Load();
            wallet.coins = 37;
            wallet.bombs = 2;
            wallet.boosters = 3;
            wallet.reverses = 4;
            wallet.Save();
            yield return Frames(3);
            Check(GameObject.Find("ShopDesign/CoinBalance").GetComponent<Text>().text == "37" && GameObject.Find("ShopDesign/BombBalance").GetComponent<Text>().text == "2" && GameObject.Find("ShopDesign/BoosterBalance").GetComponent<Text>().text == "3" && GameObject.Find("ShopDesign/ReverseBalance").GetComponent<Text>().text == "4", "Wallet observes all four persisted amounts immediately");
            shop.Close();
            var wheel = Find<LuckyWheelController>();
            wheel.Open();
            yield return StableView("Wheel");
            var reward = Find<RewardPopupController>();
            var host = (RectTransform)GameObject.Find("Panel_7_16502").transform;
            reward.ShowReward(WheelRewardKind.Coins, 100, "", null, host);
            yield return StableView("Wheel reward");
            yield return VerifyWheelRewardBounds("Cached layout");
            var originalPosition = host.anchoredPosition;
            try
            {
                host.anchoredPosition += new Vector2(20, -10);
                yield return Frames(3);
                yield return VerifyWheelRewardBounds("Host translation without screen resize");
                yield return Resolution(1440, 2960);
                yield return VerifyWheelRewardBounds("Live resize with cached containment");
            }
            finally
            {
                host.anchoredPosition = originalPosition;
            }

            reward.Confirm();
            wheel.Close();
            Find<SceneNavigationController>().Gameplay();
            yield return Loaded("BlockCrushGameplay");
            yield return StableView("Classic layout");
            var game = Find<BlockGameManager>();
            var shape = BlockShapeData.Database.First(s => s.id == "Horizontal3");
            var sprite = game.Spawner.BlockSprites[0];
            game.Board.ShowPreview(shape, new Vector2Int(1, 1), sprite);
            long allocated = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++)
                game.Board.ShowPreview(shape, new Vector2Int(1, 1), sprite);
            long stableBytes = GC.GetAllocatedBytesForCurrentThread() - allocated;
            Check(stableBytes == 0 && game.Board.PreviewCount == 3 && game.Board.OccupiedCount == 0, "1000 same-anchor previews allocate zero managed bytes and preserve occupancy");
            game.Board.ShowPreview(shape, new Vector2Int(2, 2), sprite);
            Check(!game.Board.CellAt(1, 1).IsPreview && game.Board.CellAt(2, 2).IsPreview, "Changed anchor removes old highlights and shows the new cells");
            game.Board.SetOccupied(2, 2, sprite);
            game.Board.ShowPreview(shape, new Vector2Int(2, 2), sprite);
            Check(game.Board.PreviewCount == 0 && game.Board.OccupiedCount == 1, "Changed occupancy invalidates a previously cached preview");
            game.RestartGame();
            Find<SceneNavigationController>().CompleteHome();
            yield return Loaded("Home");
            var map = UnityEngine.Object.FindFirstObjectByType<AdventurePanelController>(FindObjectsInactive.Include);
            map.Open();
            map.PlaySelected();
            yield return Loaded("BlockCrushAdventure");
            var level = Find<AdventureLevelController>();
            yield return Frames(5);
            int hudUpdates = level.HudUpdates;
            yield return Frames(30);
            Check(level.HudUpdates == hudUpdates, "Unchanged Adventure HUD is not regenerated every frame");
            string beforeGuard = PlayerPrefs.GetString(EconomySave.Key);
            bool rejected = false;
            try
            {
                BlockCrush.Editor.BlockCrushPlayVerification.Start();
            }
            catch (InvalidOperationException)
            {
                rejected = true;
            }

            Check(rejected && PlayerPrefs.GetString(EconomySave.Key) == beforeGuard, "Classic verification rejects Adventure before modifying player data");
            game = Find<BlockGameManager>();
            var piece = game.Spawner.SpawnPiece(0, BlockShapeData.Database[0], game.Spawner.BlockSprites[0]);
            Check(game.TryPlacePiece(piece, new Vector2Int(3, 3)), "Accepted placement remains legal after optimization");
            yield return Frames(3);
            Check(level.HudUpdates > hudUpdates && game.Score == 1 && level.MovesUsed == 1 && GameObject.Find("AdventureProgressScore").GetComponent<Text>().text == "1", "Adventure HUD updates real score and moves after placement");
            Find<SceneNavigationController>().CompleteHome();
            yield return Loaded("Home");
            yield return Resolution(1080, 1920);
        }
    }
}
