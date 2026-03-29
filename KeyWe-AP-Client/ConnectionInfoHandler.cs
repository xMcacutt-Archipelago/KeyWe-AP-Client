using System.IO;
using Newtonsoft.Json;

namespace KeyWe_AP_Client;

public class ConnectionInfo
{
    public string Server { get; set; }
    public ushort Port { get; set; }
    public string Slot { get; set; }
    public string Password { get; set; }
}

public class ConnectionInfoHandler
{
    private static readonly string path = SaveSystem.DataRoot + "connection_info.json";

    public static void Save(string server, ushort port, string slot, string password)
    {
        var connectionInfo = new ConnectionInfo();
        connectionInfo.Server = server;
        connectionInfo.Port = port;
        connectionInfo.Slot = slot;
        connectionInfo.Password = password;
        var text = JsonConvert.SerializeObject(connectionInfo);
        File.WriteAllText(path, text);
    }

    public static void Load(ref string server, ref ushort port, ref string slotName, ref string password)
    {
        if (!File.Exists(path))
            Save("archipelago.gg", 65535, "Player1", "");
        var json = File.ReadAllText(path);
        var connectionInfo = JsonConvert.DeserializeObject<ConnectionInfo>(json);
        server = connectionInfo.Server;
        port = connectionInfo.Port;
        slotName = connectionInfo.Slot;
        password = connectionInfo.Password;
    }
}