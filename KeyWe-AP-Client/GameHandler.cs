using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Global;
using Global.Online;
using HarmonyLib;
using KeyWe.Profile.ProfileDataClasses;
using KeyWe.Tournament;
using Photon.Pun;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace KeyWe_AP_Client;

public class GameHandler : MonoBehaviour
{
    public static bool IsUnlocking = false;
    public static bool SomeoneElseDied;

    public void InitOnConnect()
    {
        gameObject.AddComponent<CosmeticSyncHandler>();
    }

    public void LogWearables()
    {
        var data = "";
        var cat = 0;
        while (cat < 7)
        {
            data += "Category " + cat + " \n";
            foreach (var wearable in SystemHandler.Get<DataKeeper>().GetWearables((Customizables.Categories)cat).Items)
            {
                data += wearable.Id + " " + wearable.Name + "\n";
                data += wearable.Description + "\n";
                data += wearable.incompatibleCategories + "\n";
                data += wearable.RewardCategory + "\n\n";
            }

            data += "\n";
            cat++;
        }

        File.WriteAllText(SaveSystem.DataRoot + "Help.txt", data);
    }

    public void Kill()
    {
        StartCoroutine(KillDelayed());
    }

    private IEnumerator KillDelayed()
    {
        yield return null;
        var kiwis = FindObjectsOfType<Kiwi>();
        foreach (var kiwi in kiwis)
        {
            if (!kiwi.IsLocalPlayer)
                continue;
            Respawn(kiwi, false);
        }
        SomeoneElseDied = false;
    }
    
    public void Respawn(Kiwi kiwi, bool inWater)
    {
        kiwi.ShowRespawnEffect(inWater);
        if (OnlineEventRelay.Instance.IsInRoom)
            kiwi.photonView.RPC("Kiwi_Respawn_RPC", RpcTarget.Others, (object) inWater);
        kiwi.ChangeState(CommonKiwi.StateID.Respawning);
    }

    public void EquipRandom(Customizables.Categories category)
    {
        var dataKeeper = SystemHandler.Get<DataKeeper>();
        if (dataKeeper == null 
            || Data.ExcludedLevels == null 
            || Data.LevelNameToId == null 
            || dataKeeper.CurrentLevelData == null)
        {
            APConsole.Instance.Log($"Failed to equip a cosmetic (this is a failsafe, don't panic)");
            return;
        }
        switch (dataKeeper.CurrentState)
        {
            case DataKeeper.State.InMatch when Data.ExcludedLevels.Contains(Data.LevelNameToId[dataKeeper.CurrentLevelData.Name]):
            case DataKeeper.State.InOvertimeShift when Data.ExcludedOvertimeShifts.Contains(Data.OvertimeLevelNameToId[dataKeeper.CurrentLevelData.Name]):
            case DataKeeper.State.InTournament when Data.ExcludedTournamentCourses.Contains(Data.TournamentLevelNameToId[dataKeeper.CurrentLevelData.Name]):
                APConsole.Instance.Log($"Cannot equip cosmetics in this level");
                return;
            default:
                StartCoroutine(EquipRandomDelayed(category));
                break;
        }
    }

    private IEnumerator EquipRandomDelayed(Customizables.Categories category)
    {
        yield return null;

        var dataKeeper = SystemHandler.Get<DataKeeper>();
        var kiwis = FindObjectsOfType<Kiwi>();
        if (kiwis.Length == 0)
            yield break;
        foreach (var kiwi in kiwis)
        {
            if (!kiwi.IsLocalPlayer)
                continue;
            var customizationIndex = !PhotonNetwork.IsConnected
                ? kiwi.playerIndex
                : dataKeeper.OnlineSelectedKiwi;
            var customization = dataKeeper.Profile.GetCustomization(customizationIndex);
            var wearables = dataKeeper.GetWearables(category).Items;
            var filtered = wearables.Where(x =>
                    (dataKeeper.Profile.CheckProgress(Enums.ProgressFlag.TournamentDLC) ||
                     x.RewardCategory != Customizables.RewardCategory.Tournament) &&
                    (dataKeeper.Profile.CheckProgress(Enums.ProgressFlag.Preorder) ||
                     x.RewardCategory != Customizables.RewardCategory.Preorder) &&
                    x.RewardCategory != Customizables.RewardCategory.SwitchPlatform)
                .ToList();
            if (filtered.Count == 0)
                continue;
            var wearable = filtered.GetRandom();

            var mask = 1;
            for (var i = 0; i < 7; i++)
            {
                var incompatible = FlagsHelper.IsSet(wearable.IncompatibleCategories, (Customizables.CategoryMask)mask);
                if (incompatible)
                    customization.EquipItem((Customizables.Categories)i, 0);
                mask <<= 1;
            }
            UnequipIncompatibleItems(wearable, customizationIndex, category);
            customization.EquipItem(category, wearable.Id);
            dataKeeper.SaveProfile();
            kiwi.Customization.InitAll();
            if (PhotonNetwork.IsConnected)
            {
                var localPlayer = PhotonNetwork.LocalPlayer;
                var propertiesToSet = new Hashtable();
                propertiesToSet.Add(Properties.Customization, dataKeeper.Profile.GetCustomization(dataKeeper.OnlineSelectedKiwi).GetItemIDsString());
                localPlayer.SetCustomProperties(propertiesToSet);
            }
        }
    }


    private void UnequipIncompatibleItems(Customizables.Customizable wearable, int playerIndex,
        Customizables.Categories category)
    {
        var dataKeeper = SystemHandler.Get<DataKeeper>();
        var customization = dataKeeper.Profile.GetCustomization(playerIndex);
        var currentCategoryMask = (Customizables.CategoryMask)(1 << (int)category);
        for (var i = 0; i < 7; i++)
        {
            var cat = (Customizables.Categories)i;
            var sameCategory = cat == category;
            var selectedIncompatibleWithThis =
                !sameCategory &&
                FlagsHelper.IsSet(wearable.IncompatibleCategories, (Customizables.CategoryMask)(1 << i));
            var equippedItemId = customization.ItemIDs[i];
            var equippedWearable = dataKeeper.GetWearable(cat, equippedItemId);
            var existingIncompatibleWithSelected =
                equippedWearable != null &&
                FlagsHelper.IsSet(equippedWearable.IncompatibleCategories, currentCategoryMask);
            if (selectedIncompatibleWithThis || existingIncompatibleWithSelected)
                customization.EquipItem(cat, 0);
        }
    }

    public static void UpdateKiwiMovement(Kiwi kiwi)
    {
        kiwi.walkSpeed = SaveDataHandler.ArchipelagoSaveData.WalkSpeed;
        kiwi.swimSpeed = SaveDataHandler.ArchipelagoSaveData.SwimSpeed;
        kiwi.dashing.dashForce = SaveDataHandler.ArchipelagoSaveData.DashForce;
        kiwi.dashing.DashCooldownTimer.maxTime = SaveDataHandler.ArchipelagoSaveData.DashCooldown;
        kiwi.dashing.dashCooldownTimer.Length = SaveDataHandler.ArchipelagoSaveData.DashCooldown;
        kiwi.jumping.initialSpeed = SaveDataHandler.ArchipelagoSaveData.JumpHeight;
        kiwi.jumping.maxJumpForce = SaveDataHandler.ArchipelagoSaveData.JumpHeight;
        kiwi.fallSpeed = SaveDataHandler.ArchipelagoSaveData.RespawnFallSpeed;
        kiwi.fallMovementSpeed = SaveDataHandler.ArchipelagoSaveData.RespawnFallMoveSpeed;
        kiwi.vocalTimer.maxTime = SaveDataHandler.ArchipelagoSaveData.ChirpCooldown;
        kiwi.vocalTimer.Length = SaveDataHandler.ArchipelagoSaveData.ChirpCooldown;
        kiwi.peckTimer.maxTime = SaveDataHandler.ArchipelagoSaveData.PeckCooldown;
        kiwi.peckTimer.Length = SaveDataHandler.ArchipelagoSaveData.PeckCooldown;
    }

    public static Action onReceivedMovementUpgrade;

    [HarmonyPatch(typeof(Overmap))]
    public class Overmap_Patch
    {
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        public static void OnStart(Overmap __instance)
        {
            __instance.options.FirstOrDefault(o => o.name == "SwitchProfile")?.transform.Translate(-10000f, 0f, 0f);
            __instance.options.FirstOrDefault(o => o.option == Overmap.Options.Nest)?.SetActive(false);
            __instance.options.FirstOrDefault(o => o.option == Overmap.Options.Wardrobe)?.SetActive(false);
        }
    }

    [HarmonyPatch(typeof(Kiwi))]
    public class Kiwi_Patch
    {
        private static readonly ConditionalWeakTable<Kiwi, Action> handlers = new();
        
        [HarmonyPatch("Init")]
        [HarmonyPostfix]
        public static void OnInit(Kiwi  __instance)
        {
            UpdateKiwiMovement(__instance);
            var handler = () => UpdateKiwiMovement(__instance);
            handlers.Add(__instance, handler);
            onReceivedMovementUpgrade += () => { UpdateKiwiMovement(__instance); };
        }

        [HarmonyPatch("OnDestroy")]
        [HarmonyPrefix]
        public static void OnDestroy(Kiwi __instance)
        {
            if (handlers.TryGetValue(__instance, out var handler))
            {
                onReceivedMovementUpgrade -= handler;
                handlers.Remove(__instance);
            }
        }
        
        [HarmonyPatch("Respawn")]
        [HarmonyPrefix]
        public static void OnRespawn(Kiwi __instance, bool inWater)
        {
            try
            {
                if (__instance.IsPaused)
                    return;
                if (PluginMain.ArchipelagoHandler.SlotData.DeathLink)
                    PluginMain.ArchipelagoHandler.SendDeath();
            }
            catch (Exception ex)
            {
                APConsole.Instance.Log(ex.Message);
            }
        }
    }

    [HarmonyPatch(typeof(CourseButton))]
    public class CourseButton_Patch
    {
        [HarmonyPatch("SetData")]
        [HarmonyPrefix]
        private static void OnSetData(ref bool isLocked, int index, TournamentRecord record, TournamentLevelData data)
        {
            var tournamentUnlocked = SaveDataHandler.ArchipelagoSaveData.TournamentUnlocked;
            if (SystemHandler.Get<DataKeeper>().Profile.CheckProgress(Enums.ProgressFlag.TournamentDLC))
                isLocked = !tournamentUnlocked;
            else if (tournamentUnlocked)
                APConsole.Instance.Log("Warning: You have made an attempt to DLC protection measures. " +
                                       "Please support the devs. They worked hard on this game. Thank you - xMcacutt");
        }
    }

    [HarmonyPatch(typeof(OvertimeShiftBoard))]
    public class OvertimeShiftBoard_Patch
    {
        [HarmonyPatch("Overmap_OnRefreshScene")]
        [HarmonyPostfix]
        private static void Overmap_OnRefreshScene(OvertimeShiftBoard __instance)
        {
            __instance.topRow.ForEachItem(d =>
                d.SetActive(SaveDataHandler.ArchipelagoSaveData.OvertimeSummerUnlocked));
            __instance.middleRow.ForEachItem(d =>
                d.SetActive(SaveDataHandler.ArchipelagoSaveData.OvertimeFallUnlocked));
            __instance.bottomRow.ForEachItem(d =>
                d.SetActive(SaveDataHandler.ArchipelagoSaveData.OvertimeWinterUnlocked));
        }
    }

    [HarmonyPatch(typeof(OvermapSubMenu))]
    public class OvermapSubMenu_Patch
    {
        [HarmonyPatch("SetUIShowing")]
        [HarmonyPostfix]
        private static void SetUIShowing(OvermapSubMenu __instance, bool activate)
        {
            if (SaveDataHandler.ArchipelagoSaveData == null)
                return;
            if (__instance is OvertimeShiftBoard overtimeBoard)
            {
                overtimeBoard.topRow.ForEachItem(d =>
                    d.SetActive(SaveDataHandler.ArchipelagoSaveData.OvertimeSummerUnlocked));
                overtimeBoard.middleRow.ForEachItem(d =>
                    d.SetActive(SaveDataHandler.ArchipelagoSaveData.OvertimeFallUnlocked));
                overtimeBoard.bottomRow.ForEachItem(d =>
                    d.SetActive(SaveDataHandler.ArchipelagoSaveData.OvertimeWinterUnlocked));
            }
        }
    }

    [HarmonyPatch(typeof(Calendar))]
    public class Calendar_Patch
    {
        [HarmonyPatch("Day_OnSelected")]
        [HarmonyPostfix]
        public static void OnDaySelected(DayButton button)
        {
            var dataKeeper = SystemHandler.Get<DataKeeper>();
            var level = dataKeeper.Levels[button.DataIndex];
            if (level.Name != "Stand Your Post" || !button.IsLocked)
                return;
            APConsole.Instance.Log(PluginMain.ArchipelagoHandler.GetRequirementsString());
        }
        
        [HarmonyPatch("InitRewards")]
        [HarmonyPrefix]
        private static bool InitRewards(Calendar __instance)
        {
            if (__instance.results?.BestResults == null || !__instance.results.BestResults.Grade.IsPassed() ||
                !__instance.results.BestResults.NewGrade)
                return false;
            __instance.results?.ResetData();
            __instance.OnAwardsDone();
            return false;
        }


        [HarmonyPatch("SetUpButtons")]
        [HarmonyPrefix]
        public static bool OnSetUpButtons(Calendar __instance, LevelOrder.SeasonRange range, int numLevels)
        {
            for (var index1 = 0; index1 < __instance.dayButtons.Length; ++index1)
            {
                var dayButton = __instance.dayButtons[index1];
                var index2 = range.StartIndex + index1;
                var week = range.StartIndex / 12 * 3 + index1 / 4;
                var isLocked = SaveDataHandler.ArchipelagoSaveData.WeeksUnlocked[week] == false;
                if (index2 == 35)
                    isLocked = PluginMain.ArchipelagoHandler.IsFinalLocked();
                if (index1 < numLevels)
                {
                    var level = __instance.dataKeeper.Levels[index2];
                    var modeIcon = __instance.modeInfo.GetModeIcon(level.Mode);
                    var challengesStatus = __instance.GetChallengesStatus(index2, level);
                    dayButton.Init(__instance.dataKeeper.Profile.LevelRecords[index2].HighestGrade, modeIcon, isLocked,
                        (short)index2, (short)index1, challengesStatus);
                }
                else
                {
                    dayButton.Init(LevelData.Grade.None, null, true, -1, (short)index1, new bool?());
                }
            }

            __instance.FocusButton(range);
            return false;
        }

        [HarmonyPatch("UpdateSeason")]
        [HarmonyPrefix]
        public static bool OnUpdateSeason(Calendar __instance)
        {
            __instance.details.Hide();
            if (__instance.pcFields.StartButton != null)
                __instance.pcFields.StartButton.interactable = false;
            var seasonsIndex = __instance.dataKeeper?.Levels.SeasonsIndices[__instance.seasonIndex];
            __instance.SetUpButtons(seasonsIndex, seasonsIndex.Count);
            __instance.isNextSeasonLocked = false;
            __instance.UpdateSeasonPlates();
            __instance.UpdatePCButtons();
            __instance.RefreshHighlightedDay();
            __instance.UpdateIntroButton();
            return false;
        }
    }

    [HarmonyPatch(typeof(ProfileData))]
    public class ProfileData_Patch
    {
        [HarmonyPatch("ChallengesCompleted")]
        [HarmonyPrefix]
        public static bool OnChallengesCompleted(
            ProfileData __instance,
            ref ushort __result,
            Enums.Mode mode,
            int levelID,
            ushort[] challengeIDs,
            ushort reward
        )
        {
            ushort num = 0;
            if (challengeIDs == null)
            {
                __result = num;
                return false;
            } 
            
            foreach (var challengeId in challengeIDs)
            {
                if (__instance.IsChallengeCompleted(mode, levelID, challengeId, out var record)) continue;
                if (record == null) continue;
                record.CompletedChallengeIndices ??= [];
                record?.CompletedChallengeIndices.Add(challengeId);
                num += reward;
            }
            
            __result = num;
            return false;
        }
    }
    

    [HarmonyPatch(typeof(DataKeeper))]
    public class DataKeeper_Patch
    {
        [HarmonyPatch("GiveReward")]
        [HarmonyPrefix]
        public static bool OnGiveReward(LevelData.Grade grade, ref ushort extraStamps, LevelData levelData)
        {
            extraStamps = 0;
            return true;
        }
        
        [HarmonyPatch("CheckForCinematicTrigger")]
        [HarmonyPrefix]
        private static bool OnCheckForCinematicTrigger(ref Enums.Cinematics __result)
        {
            __result = Enums.Cinematics.None;
            return false;
        }

        [HarmonyPatch("GetActiveSeason")]
        [HarmonyPrefix]
        public static bool GetActiveSeason(ref Enums.Season __result)
        {
            __result = Enums.Season.Winter;
            return false;
        }
    }
}