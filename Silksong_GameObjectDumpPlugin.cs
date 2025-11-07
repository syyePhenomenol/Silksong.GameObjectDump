using BepInEx;

using HarmonyLib;
using UnityEngine;

namespace Silksong.GameObjectDump;

/// <summary>
/// Performs a formatted dump of various useful component/FSM data from every game scene, during runtime.
/// </summary>
[BepInAutoPlugin(id: "io.github.silksong_gameobjectdump")]
public partial class Silksong_GameObjectDumpPlugin : BaseUnityPlugin
{
    private static GameObject? _sceneIterator = null;

    internal static Silksong_GameObjectDumpPlugin? Instance { get; private set; }

    internal static void Log(string text)
    {
        Instance?.Logger.LogInfo(text);
    }

    internal static void LogWarning(string text)
    {
        Instance?.Logger.LogWarning(text);
    }

    internal static void LogError(string text)
    {
        Instance?.Logger.LogError(text);
    }

    internal static void TryMakeSceneIterator()
    {
        if (_sceneIterator != null) return;

        Log($"Creating Scene Iterator");

        _sceneIterator = new GameObject("Scene Iterator");
        _sceneIterator.AddComponent<SceneIterator>();
        DontDestroyOnLoad(_sceneIterator);

        SceneIterator.OnProcessScene += CondensedSceneDumper.DumpScene;
        // SceneIterator.OnProcessScene += ComponentFsmActionCounter.CountNonSceneComponentsAndFsmActions;
        SceneIterator.OnProcessScene += ComponentFsmActionCounter.CountComponentsandFsmActions;
        SceneIterator.OnFinishedIteration += ComponentFsmActionCounter.LogResults;
    }

    private void Awake()
    {
        Instance = this;

        // Put your initialization logic here
        Logger.LogInfo($"Plugin {Name} ({Id}) has loaded!");

        Harmony harmony = new(Id);
        harmony.PatchAll();
    }
}

[HarmonyPatch(typeof(GameManager), nameof(GameManager.StartNewGame))]
internal static class GameManager_StartNewGame
{

    [HarmonyPostfix]
    private static void Postfix()
    {
        Silksong_GameObjectDumpPlugin.TryMakeSceneIterator();
    }
}

[HarmonyPatch(typeof(GameManager), nameof(GameManager.ContinueGame))]
internal static class GameManager_ContinueGame
{

    [HarmonyPostfix]
    private static void Postfix()
    {
        Silksong_GameObjectDumpPlugin.TryMakeSceneIterator();
    }
}