using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using TemplatePlugin;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KeyWe_AP_Client;

[BepInPlugin(TemplatePluginInfo.PLUGIN_GUID, TemplatePluginInfo.PLUGIN_NAME, Version)]
public class PluginMain : BaseUnityPlugin
{
    public const string GameName = TemplatePluginInfo.GAME_NAME;
    private const string Version = "1.1.0";
    public static ConfigEntry<bool>? EnableDebugLogging;
    public static ConfigEntry<bool>? FilterLog;
    public static ConfigEntry<float>? MessageInTime;
    public static ConfigEntry<float>? MessageHoldTime;
    public static ConfigEntry<float>? MessageOutTime;
    public static LoginMenuHandler LoginHandler;
    public static SaveDataHandler SaveDataHandler;
    public static ArchipelagoHandler ArchipelagoHandler;
    public static ItemHandler ItemHandler;
    public static LocationHandler LocationHandler;
    public static GameHandler GameHandler;

    private readonly Harmony _harmony = new(TemplatePluginInfo.PLUGIN_GUID);

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
        APConsole.Instance.Log($"Welcome to {GameName} Archipelago!");
        
        EnableDebugLogging = Config.Bind(
            "Logging",
            "EnableDebugLogging",
            false,
            "Enables or disables debug logging in the Archipelago Console."
        );
            
        FilterLog = Config.Bind(
            "Logging",
            "FilterLog",
            false,
            "Filter the archipelago log to only show messages relevant to you."
        );
            
        MessageInTime = Config.Bind(
            "Logging",
            "MessageInTime",
            0.25f,
            "How long messages take to animate in."
        );
            
        MessageHoldTime = Config.Bind(
            "Logging",
            "MessageHoldTime",
            3f,
            "How long messages stay in the log before animating out."
        );
            
        MessageOutTime = Config.Bind(
            "Logging",
            "MessageOutTime",
            0.5f,
            "How long messages stay in the log before animating out."
        );
    }
    
    private void Start()
    {
        Cursor.visible = true;
        ControllerVibrationHandler.Instance.enabled = false;
    }
}