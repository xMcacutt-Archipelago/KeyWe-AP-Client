using System;
using System.Collections.Generic;
using Global;

namespace KeyWe_AP_Client;

public class SlotData
{
    public readonly int RequiredCollectibleChecks;
    public readonly int RequiredCollectibles;
    public readonly int RequiredLevelCompletions;
    public readonly int RequiredLevelCompletionsPerWeek;
    public readonly int RequiredOvertimeCompletions;
    public readonly int RequiredTournamentCompletions;
    public readonly bool TournamentIncluded;
    public readonly bool OvertimeIncluded;
    public bool DeathLink;  
    public LevelData.Grade GradeCheckThreshold = LevelData.Grade.Bronze;

    public SlotData(Dictionary<string, object> slotDict)
    {
        foreach (var x in slotDict) Console.WriteLine($"{x.Key} {x.Value}");
        TournamentIncluded = (long)slotDict["TournamentIncluded"] == 1;
        OvertimeIncluded = (long)slotDict["TournamentIncluded"] == 1;
        RequiredLevelCompletions = (int)(long)slotDict["RequiredLevelCompletions"];
        RequiredLevelCompletionsPerWeek = (int)(long)slotDict["RequiredLevelCompletionsPerWeek"];
        RequiredCollectibles = (int)(long)slotDict["RequiredCollectibles"];
        RequiredCollectibleChecks = (int)(long)slotDict["RequiredCollectibleChecks"];
        RequiredOvertimeCompletions = (int)(long)slotDict["RequiredOvertimeCompletions"];
        RequiredTournamentCompletions = (int)(long)slotDict["RequiredTournamentCompletions"];
        var startingWeek = (int)(long)slotDict["StartingWeek"];
        var grade = (int)(long)slotDict["LevelCompletionCheckThreshold"];
        GradeCheckThreshold = grade switch
        {
            1 => LevelData.Grade.Bronze,
            2 => LevelData.Grade.Silver,
            3 => LevelData.Grade.Gold,
            _ => GradeCheckThreshold
        };
        DeathLink = (long)slotDict["DeathLink"] == 1;
        if (DeathLink)
            PluginMain.ArchipelagoHandler.UpdateTags(["DeathLink"]);

        SaveDataHandler.ArchipelagoSaveData.WeeksUnlocked[startingWeek - 1] = true;
        
        if (!SystemHandler.Get<DataKeeper>().Profile.CheckProgress(Enums.ProgressFlag.TournamentDLC) && TournamentIncluded) 
            APConsole.Instance.Log("Warning: You do not own the Telepost Tournament DLC but have its checks enabled.");
    }
}