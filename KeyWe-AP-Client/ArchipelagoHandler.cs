using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Converters;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using Archipelago.MultiClient.Net.MessageLog.Parts;
using Archipelago.MultiClient.Net.Packets;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Random = System.Random;

namespace KeyWe_AP_Client;

public class ArchipelagoHandler(string server, int port, string slot, string password)
{
    private const string GameName = "KeyWe";
    public static bool IsConnected;
    public static bool IsConnecting;
    public static Action OnConnect;

    private readonly string[] _deathMessages =
    [
        "had a skill issue (died)",
        "forgot to dash (died)",
        "didn't see the edge of the table (died)",
        "thought kiwis could fly (died)",
        "forgot their floatie (died)",
        "got spooked to death by Zoey",
        "slept on the job (died)",
        "got left out in the cold (died)",
        "got abducted by the mailflies (died)",
        "had an argument with a cassowary (died)",
        "popped too much bubblewrap (died)",
        "fell into a shipping crate (died)",
        "called Papa Moon an ape (died)",
        "lost the snowball fight (died)",
        "was struck by lightning",
        "got trapped in the bungalow basin oven (died)",
        "swallowed a button (died)",
        "got hit by a cannonball (died)",
        "lost Herbert (died)",
        "was swallowed by a pitcher plant (died)",
        "tried to hitch a ride on Bartleby (died)",
        "got toasted (died)",
        "sank in quicksand (died)",
        "didn't try hard enough (died)",
        "made a typo (died)",
        "forgot the \'We\' (died)"
    ];

    private readonly ConcurrentQueue<long> _locationsToCheck = new();

    private readonly Random _random = new();

    private string _lastDeath;
    private LoginSuccessful _loginSuccessful;
    private ArchipelagoSession _session;
    public SlotData SlotData;

    private string Server { get; } = server;
    private int Port { get; } = port;
    private string Slot { get; } = slot;
    private string Password { get; } = password;

    private string Seed { get; set; }
    private double SlotInstance { get; set; }

    private void HandleDeathLink(string source, string cause)
    {
        if (!SlotData.DeathLink)
            return;
        APConsole.Instance.Log($"{cause}");
        if (source == Slot)
            return;
        GameHandler.SomeoneElseDied = true;
        PluginMain.GameHandler.Kill();
    }

    private void CreateSession()
    {
        SlotInstance = DateTime.Now.ToUnixTimeStamp();
        _session = ArchipelagoSessionFactory.CreateSession(Server, Port);
        _session.MessageLog.OnMessageReceived += OnMessageReceived;
        _session.Socket.SocketClosed += OnSocketClosed;
        _session.Socket.PacketReceived += PacketReceived;
        _session.Items.ItemReceived += ItemReceived;
    }
    
    private void OnSocketClosed(string reason)
    {
        APConsole.Instance.Log($"Connection closed ({reason}) Attempting reconnect...");
        IsConnected = false;
    }

    public void InitConnect()
    {
        IsConnecting = true;
        CreateSession();
        IsConnected = Connect();
        IsConnecting = false;
    }

    private bool Connect()
    {
        Seed = _session.ConnectAsync()?.Result?.SeedName;
        if (Seed != null)
            PluginMain.SaveDataHandler!.LoadProfile(Seed, Slot);

        var result = _session.LoginAsync(
            GameName,
            Slot,
            ItemsHandlingFlags.AllItems,
            new Version(6, 0, 0),
            [],
            password: Password
        ).Result;

        if (result.Successful)
        {
            _loginSuccessful = (LoginSuccessful)result;
            SlotData = new SlotData(_loginSuccessful.SlotData);
            PluginMain.GameHandler.InitOnConnect();
            new Thread(RunCheckLocationsFromList).Start();
            OnConnect.Invoke();
            return true;
        }

        var failure = (LoginFailure)result;
        var errorMessage = $"Failed to Connect to {Server}:{Port} as {Slot}:";
        errorMessage = failure.Errors.Aggregate(errorMessage, (current, error) => current + $"\n    {error}");
        errorMessage = failure.ErrorCodes.Aggregate(errorMessage, (current, error) => current + $"\n    {error}");
        APConsole.Instance.Log(errorMessage);
        APConsole.Instance.Log("Attempting reconnect...");
        return false;
    }

    private void ItemReceived(ReceivedItemsHelper helper)
    {
        try
        {
            while (helper.Any())
            {
                var itemIndex = helper.Index;
                var item = helper.DequeueItem();
                PluginMain.ItemHandler.HandleItem(itemIndex, item);
            }
        }
        catch (Exception ex)
        {
            APConsole.Instance.Log($"[ItemReceived ERROR] {ex}");
            throw;
        }
    }

    public void Release()
    {
        _session.SetGoalAchieved();
        _session.SetClientState(ArchipelagoClientState.ClientGoal);
    }

    public void CheckLocations(long[] ids)
    {
        ids.ToList().ForEach(id => _locationsToCheck.Enqueue(id));
    }

    public void CheckLocation(long id)
    {
        _locationsToCheck.Enqueue(id);
    }

    private void RunCheckLocationsFromList()
    {
        while (true)
            if (_locationsToCheck.TryDequeue(out var locationId))
                _session.Locations.CompleteLocationChecks(locationId);
            else
                Thread.Sleep(100);
    }

    public bool IsLocationChecked(long id)
    {
        return _session.Locations.AllLocationsChecked.Contains(id);
    }

    public int CountLocationsCheckedInRange(long start, long end)
    {
        var startId = start;
        var endId = end;
        return _session.Locations.AllLocationsChecked.Count(loc => loc >= startId && loc < endId);
    }

    public void UpdateTags(List<string> tags)
    {
        var packet = new ConnectUpdatePacket
        {
            Tags = tags.ToArray(),
            ItemsHandling = ItemsHandlingFlags.AllItems
        };
        _session.Socket.SendPacket(packet);
    }

    private void OnMessageReceived(LogMessage message)
    {
        string messageStr;
        if (message.Parts.Any(x => x.Type == MessagePartType.Player) &&
            PluginMain.FilterLog != null &&
            PluginMain.FilterLog.Value &&
            !message.Parts.Any(x => x.Text.Contains(_session!.Players.GetPlayerName(_session.ConnectionInfo.Slot))))
            return;
        if (message.Parts.Length == 1)
            messageStr = message.Parts[0].Text;
        else
        {
            var builder = new StringBuilder();
            foreach (var part in message.Parts)
                builder.Append($"{part.Text}");
            messageStr = builder.ToString();
        }
        APConsole.Instance.Log(messageStr);
    }

    private void PacketReceived(ArchipelagoPacketBase packet)
    {
        switch (packet)
        {
            case BouncePacket bouncePacket:
                BouncePacketReceived(bouncePacket);
                break;
        }
    }


    private static DateTime LastDeathLinkTime = DateTime.Now;
    public void SendDeath()
    {
        var packet = new BouncePacket();
        var now = DateTime.Now;

        if (now - LastDeathLinkTime < TimeSpan.FromSeconds(2))
            return;
        
        packet.Tags = ["DeathLink"];
        packet.Data = new Dictionary<string, JToken>
        {
            { "time", now.ToUnixTimeStamp() },
            { "source", Slot },
            { "cause", $"{Slot} {_deathMessages[_random.Next(_deathMessages.Length)]}" }
        };

        if (packet.Data.TryGetValue("source", out var sourceObj))
        {
            var source = sourceObj?.ToString() ?? "Unknown";
            if (packet.Data.TryGetValue("cause", out var causeObj))
            {
                var cause = causeObj?.ToString() ?? "Unknown";
                if (packet.Data.TryGetValue("time", out var timeObj))
                {
                    var time = timeObj?.ToString() ?? "Unknown";
                }
            }
        }
        
        LastDeathLinkTime = now;
        _session.Socket.SendPacket(packet);
    }

    private void BouncePacketReceived(BouncePacket packet)
    {
        if (SlotData.DeathLink)
            ProcessBouncePacket(packet, "DeathLink", ref _lastDeath, (source, data) =>
                HandleDeathLink(source, data["cause"]?.ToString() ?? "Unknown"));
    }

    private static void ProcessBouncePacket(BouncePacket packet, string tag, ref string lastTime,
        Action<string, Dictionary<string, JToken>> handler)
    {
        if (!packet.Tags.Contains(tag)) return;
        if (!packet.Data.TryGetValue("time", out var timeObj))
            return;
        if (lastTime == timeObj.ToString())
            return;
        lastTime = timeObj.ToString();
        if (!packet.Data.TryGetValue("source", out var sourceObj))
            return;
        var source = sourceObj?.ToString() ?? "Unknown";
        if (packet.Data.TryGetValue("cause", out var causeObj))
        {
            var cause = causeObj?.ToString() ?? "Unknown";
            //Console.WriteLine($"Received Bounce Packet with Tag: {tag} :: {cause}");
        }

        handler(source, packet.Data);
    }

    public bool IsFinalLocked()
    {
        if (SaveDataHandler.ArchipelagoSaveData.CollectiblesChecked.Count(x => x.Value)
            < PluginMain.ArchipelagoHandler.SlotData.RequiredCollectibleChecks)
            return true;
        if (SaveDataHandler.ArchipelagoSaveData.LevelCompletions.Count(x => x.Value)
            < PluginMain.ArchipelagoHandler.SlotData.RequiredLevelCompletions)
            return true;
        if (Enumerable.Range(0, 9)
            .Any(week =>
            {
                var requiredCompletions = PluginMain.ArchipelagoHandler.SlotData.RequiredLevelCompletionsPerWeek;
                if (week == 8)
                    requiredCompletions = Math.Min(requiredCompletions, 3);
                var completed = Enumerable.Range(0, 4)
                    .Count(day => SaveDataHandler.ArchipelagoSaveData.LevelCompletions[week * 4 + day]);
                return completed < requiredCompletions;
            }))
            return true;
        if (SaveDataHandler.ArchipelagoSaveData.Collectibles.Count(x => x.Value)
            < PluginMain.ArchipelagoHandler.SlotData.RequiredCollectibles)
            return true;
        if (SaveDataHandler.ArchipelagoSaveData.OvertimeLevelCompletions.Count(x => x.Value)
            < PluginMain.ArchipelagoHandler.SlotData.RequiredOvertimeCompletions)
            return true;
        if (SaveDataHandler.ArchipelagoSaveData.TournamentCourseCompletions.Count(x => x.Value)
            < PluginMain.ArchipelagoHandler.SlotData.RequiredTournamentCompletions)
            return true;
        return false;
    }

    public string GetRequirementsString()
    {
        var levelGoalsDone = SaveDataHandler.ArchipelagoSaveData.LevelCompletions.Count(x => x.Value);
        var levelGoalsReq = PluginMain.ArchipelagoHandler.SlotData.RequiredLevelCompletions;
        var weekGoalsDone = Enumerable.Range(0, 9)
            .Count(week =>
                {
                    var requiredCompletions = PluginMain.ArchipelagoHandler.SlotData.RequiredLevelCompletionsPerWeek;
                    if (week == 8)
                        requiredCompletions = Math.Min(requiredCompletions, 3);
                    var completed = Enumerable.Range(0, 4)
                        .Count(day => SaveDataHandler.ArchipelagoSaveData.LevelCompletions[week * 4 + day]);
                    return requiredCompletions <= completed;
                }
            );
        var colsCollected = SaveDataHandler.ArchipelagoSaveData.Collectibles.Count(x => x.Value);
        var colsReq = PluginMain.ArchipelagoHandler.SlotData.RequiredCollectibles;
        var colsChecked = SaveDataHandler.ArchipelagoSaveData.CollectiblesChecked.Count(x => x.Value);
        var colChecksReq = PluginMain.ArchipelagoHandler.SlotData.RequiredCollectibleChecks;
        var otGoalsDone = SaveDataHandler.ArchipelagoSaveData.OvertimeLevelCompletions.Count(x => x.Value);
        var otGoalsReq = PluginMain.ArchipelagoHandler.SlotData.RequiredOvertimeCompletions;
        var tptGoalsDone = SaveDataHandler.ArchipelagoSaveData.TournamentCourseCompletions.Count(x => x.Value);
        var tptGoalsReq = PluginMain.ArchipelagoHandler.SlotData.RequiredTournamentCompletions;
         
        var requirementsString = new StringBuilder();
        requirementsString.AppendLine($"Stand Your Post Requirements:");
        if (levelGoalsReq > 0)
            requirementsString.AppendLine($"{levelGoalsDone}/{levelGoalsReq} levels complete");
        if (PluginMain.ArchipelagoHandler.SlotData.RequiredLevelCompletionsPerWeek > 0)
            requirementsString.AppendLine($"{weekGoalsDone}/{9} weeks with {PluginMain.ArchipelagoHandler.SlotData.RequiredLevelCompletionsPerWeek} level(s) complete");
        if (colsReq > 0)
            requirementsString.AppendLine($"{colsCollected}/{colsReq} collectibles received");
        if (colChecksReq > 0)
            requirementsString.AppendLine($"{colsChecked}/{colChecksReq} collectible locations checked");
        if (otGoalsReq > 0)
            requirementsString.AppendLine($"{otGoalsDone}/{otGoalsReq} overtime shifts complete");
        if (tptGoalsReq > 0)
            requirementsString.AppendLine($"{tptGoalsDone}/{tptGoalsReq} tournament courses complete");
        return requirementsString.ToString();
    }
}