using System.IO;
using HarmonyLib;
using KeyWe.Profile.ProfileDataClasses;

namespace KeyWe_AP_Client;

public class LocationHandler
{
    private static int _wickertideCartCount;

    [HarmonyPatch(typeof(DataKeeper))]
    public class DataKeeper_Patch
    {
        [HarmonyPatch("CalenderLevelComplete")]
        [HarmonyPrefix]
        public static bool OnCalenderLevelComplete(
            DataKeeper __instance,
            LevelData levelData,
            DataKeeper.LevelCompletionInfo info,
            ref LevelData.Grade grade,
            ref ushort reward,
            ref ushort challengeReward,
            ref bool shouldSave)
        {
            var levelValid = Data.LevelNameToId.TryGetValue(levelData.Name, out var id);
            if (levelValid && id == 35)
            {
                PluginMain.ArchipelagoHandler.Release();
            }
            else
            {
                if (grade < PluginMain.ArchipelagoHandler.SlotData.GradeCheckThreshold)
                {
                    grade = LevelData.Grade.Failed;
                }
                else
                {
                    if (levelValid)
                    {
                        PluginMain.ArchipelagoHandler.CheckLocation(0x100 + 0x10 * id + 0x0);
                        SaveDataHandler.ArchipelagoSaveData.LevelCompletions[id] = true;
                    }
                }
                if (info.ChallengesCompleted is { Length: > 0 })
                    PluginMain.ArchipelagoHandler.CheckLocation(0x100 + 0x10 * id + 0xB);
            }

            reward = 0;
            challengeReward = 0;

            var levelResults = SystemHandler.Get<LevelResults>();
            LevelRecord previousProgress = null;
            var isFirstClear = false;
            if (__instance.ActiveLevelIndex.IsValidIndex(__instance.profile.LevelRecords))
            {
                previousProgress = __instance.Profile.LevelRecords[__instance.ActiveLevelIndex];
                isFirstClear = __instance.Profile.CurrentLevelIndex == __instance.ActiveLevelIndex;
            }

            levelResults.StoreMatchResults(levelData, previousProgress, grade, isFirstClear,
                __instance.activeLevelIndex, info.GoalsCleared, info.Duration, reward, -1,
                info.CollectibleID, challengeReward);
            __instance.Profile.LevelAttempted(levelResults.LastResults.NewRecord || levelResults.LastResults.NewGrade,
                __instance.ActiveLevelIndex, grade, info.GoalsCleared, info.Duration, info.TutorialDuration);
            shouldSave = true;
            return false;
        }

        [HarmonyPatch("OvertimeShiftComplete")]
        [HarmonyPrefix]
        public static bool OnOvertimeShiftComplete(
            DataKeeper __instance,
            ref bool __result,
            ref OvertimeShiftLevelData levelData,
            ref DataKeeper.LevelCompletionInfo info,
            ref LevelData.Grade grade,
            ref ushort reward,
            ref ushort challengeReward,
            ref bool shouldSave)
        {
            File.AppendAllText(SaveSystem.DataRoot + "Levels.txt",
                $"{levelData.Name}");

            if (grade < PluginMain.ArchipelagoHandler.SlotData.GradeCheckThreshold)
            {
                grade = LevelData.Grade.Failed;
                if (Data.OvertimeLevelNameToId.TryGetValue(levelData.Name, out var id))
                {
                    if (info.ChallengesCompleted is { Length: > 0 })
                        PluginMain.ArchipelagoHandler.CheckLocation(0x400 + 0x4 * id + 0x2);
                }
            }
            else
            {
                if (Data.OvertimeLevelNameToId.TryGetValue(levelData.Name, out var id))
                {
                    PluginMain.ArchipelagoHandler.CheckLocation(0x400 + 0x4 * id + 0x0);
                    if (info.ChallengesCompleted is { Length: > 0 })
                        PluginMain.ArchipelagoHandler.CheckLocation(0x400 + 0x4 * id + 0x2);
                    SaveDataHandler.ArchipelagoSaveData.OvertimeLevelCompletions[id] = true;
                }
            }
            


            reward = 0;
            challengeReward = 0;

            var overtimeShiftRecord = __instance.Profile.GetOvertimeShiftRecord((ushort)levelData.ID);
            var var = overtimeShiftRecord.WearableProgress;
            float collectibleReward = 0;
            if (grade > LevelData.Grade.Failed)
            {
                collectibleReward = __instance.OvertimeShifts.WearableUnlockThreshold / 3 * ((int)grade + 1);
                var = overtimeShiftRecord.WearableProgress + collectibleReward;
                shouldSave = true;
                if (var >= (double)__instance.OvertimeShifts.WearableUnlockThreshold &&
                    !__instance.Profile.IsUnlocked(levelData.WearableCategory, 0, (ushort)levelData.WearableID))
                {
                    if (Data.OvertimeLevelNameToId.TryGetValue(levelData.Name, out var overtimeId))
                        PluginMain.ArchipelagoHandler.CheckLocation(0x400 + 0x4 * overtimeId + 0x1);
                    Utility.UnsignedClamp(ref var, __instance.OvertimeShifts.WearableUnlockThreshold);
                    __instance.Profile.AddNotification(Notification.Type.NewWardrobeItem,
                        (int)levelData.WearableCategory, (int)levelData.WearableID);
                }
            }

            var wearableProgressBefore =
                overtimeShiftRecord.WearableProgress / __instance.OvertimeShifts.WearableUnlockThreshold;
            var wearableProgressAfter = var / __instance.OvertimeShifts.WearableUnlockThreshold;
            SystemHandler.Get<LevelResults>().StoreOvertimeResults(levelData, grade,
                (ushort)__instance.ActiveLevelIndex, info.GoalsCleared, info.Duration, reward, wearableProgressBefore,
                wearableProgressAfter, challengeReward);
            __result = __instance.Profile.OvertimeAttempted(__instance.ActiveLevelIndex, grade, info.GoalsCleared,
                info.Duration, collectibleReward, __instance.OvertimeShifts.WearableUnlockThreshold);
            return false;
        }

        [HarmonyPatch("TournamentCourseComplete")]
        [HarmonyPrefix]
        public static bool OnTournamentCourseComplete(
            LevelData levelData,
            DataKeeper.LevelCompletionInfo info,
            ref LevelData.Grade grade,
            ref ushort reward,
            ref bool shouldSave)
        {
            if (grade < PluginMain.ArchipelagoHandler.SlotData.GradeCheckThreshold)
            {
                grade = LevelData.Grade.Failed;
                reward = 0;
                return true;
            }

            var levelValid = Data.TournamentLevelNameToId.TryGetValue(levelData.Name, out var id);
            if (levelValid)
            {
                for (var i = 0; i < 3; i++)
                    if (levelValid)
                        if (info.TournyChallengeStates[i] == LevelResults.TournamentChallengeResult.Passed)
                            PluginMain.ArchipelagoHandler.CheckLocation(0x500 + 0x4 * id + 0x1 + i);
                PluginMain.ArchipelagoHandler.CheckLocation(0x500 + 0x4 * id + 0x0);
                SaveDataHandler.ArchipelagoSaveData.TournamentCourseCompletions[id] = true;
            }

            reward = 0;
            return true;
        }
    }

    [HarmonyPatch(typeof(ProfileData))]
    public class Collectible_Patch
    {
        [HarmonyPatch("HasCollectibleBeenFound")]
        [HarmonyPrefix]
        public static void HasCollectibleBeenFound(ushort id)
        {
            var levelName = SystemHandler.Get<DataKeeper>().CurrentLevelData.Name;
            if (Data.LevelNameToId.TryGetValue(levelName, out var levelId))
                PluginMain.ArchipelagoHandler.CheckLocation(0x100 + 0x10 * levelId + 0xA);
            SaveDataHandler.ArchipelagoSaveData.CollectiblesChecked[levelId] = true;
        }
    }

    [HarmonyPatch(typeof(MatchGameMode))]
    public class MatchGameMode_Patch
    {
        [HarmonyPatch("GoalCompleted")]
        [HarmonyPrefix]
        protected static void OnGoalCompleted(MatchGameMode __instance)
        {
            if (Data.LevelNameToId.TryGetValue(__instance.LevelData.Name, out var id))
                PluginMain.ArchipelagoHandler.CheckLocation(0x100 + 0x10 * id + 0x1 + __instance.NumGoalsCleared);
        }
    }

    [HarmonyPatch(typeof(DropoffWickertideGM))]
    public class ResetCartTracker
    {
        [HarmonyPatch("StartNextGoal")]
        [HarmonyPostfix]
        private static void Reset()
        {
            _wickertideCartCount = 0;
        }
    }

    [HarmonyPatch(typeof(PuzzlePackage))]
    public class PuzzlePackage_EndReached_Patch
    {
        [HarmonyPatch("EndReached")]
        [HarmonyPostfix]
        private static void EndReached_Postfix(PuzzlePackage __instance)
        {
            if (__instance is not WickertideOctoCart cart)
                return;
            var levelData = SystemHandler.Get<DataKeeper>().CurrentLevelData;
            if (Data.LevelNameToId.TryGetValue(levelData.Name, out var id))
                PluginMain.ArchipelagoHandler.CheckLocation(0x100 + 0x10 * id + 0x1 + _wickertideCartCount);
            _wickertideCartCount++;
        }
    }
}