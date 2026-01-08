using BepInEx;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KeyWe_AP_Client;

[BepInPlugin("KeyWeAPClient", "KeyWe AP Client", "1.0.4")]
public class PluginMain : BaseUnityPlugin
{
    public static LoginMenuHandler LoginHandler;
    public static SaveDataHandler SaveDataHandler;
    public static ArchipelagoHandler ArchipelagoHandler;
    public static ItemHandler ItemHandler;
    public static LocationHandler LocationHandler;
    public static GameHandler GameHandler;

    private readonly Harmony _harmony = new("KeyWeAPClient");

    private void Awake()
    {
        _harmony.PatchAll();
        var handlerObj = new GameObject("ArchipelagoLoginHandler");
        LoginHandler = handlerObj.AddComponent<LoginMenuHandler>();
        DontDestroyOnLoad(handlerObj);
        handlerObj = new GameObject("ArchipelagoSaveDataHandler");
        SaveDataHandler = handlerObj.AddComponent<SaveDataHandler>();
        DontDestroyOnLoad(handlerObj);
        handlerObj = new GameObject("ArchipelagoGameHandler");
        GameHandler = handlerObj.AddComponent<GameHandler>();
        DontDestroyOnLoad(handlerObj);
        ItemHandler = new ItemHandler();
        LocationHandler = new LocationHandler();
        APConsole.Create();
    }
    
    private void Start()
    {
        Cursor.visible = true;
        ControllerVibrationHandler.Instance.enabled = false;
    }
}