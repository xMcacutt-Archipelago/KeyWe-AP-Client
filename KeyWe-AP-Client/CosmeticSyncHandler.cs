using System;
using System.Collections.Generic;
using System.Linq;
using ExitGames.Client.Photon;
using Global.Online;
using Photon.Pun;
using Photon.Realtime;

public class CosmeticSyncHandler : MonoBehaviourPunCallbacks
{
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (targetPlayer == PhotonNetwork.LocalPlayer)
            return;
        if (!changedProps.TryGetValue(Properties.Customization, out string itemIDsString))
            return;
        var itemIDs = itemIDsString.Split([','], StringSplitOptions.RemoveEmptyEntries).Select(uint.Parse).ToArray(); 
        ApplyRemoteCosmetics(itemIDs);
    }

    private void ApplyRemoteCosmetics(uint[] itemIDs)
    {
        var playerKiwis = FindObjectsOfType<Kiwi>();
        foreach (var kiwi in playerKiwis)
        {
            if (kiwi.IsLocalPlayer)
                continue;
            kiwi.Customization.Init(itemIDs, true);
        }
    }
}