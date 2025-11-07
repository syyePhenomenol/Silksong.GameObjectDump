using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GlobalEnums;
using Silksong.GameObjectDump.Logging;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace Silksong.GameObjectDump;

public class SceneIterator : MonoBehaviour
{
    private static readonly HashSet<string> _nonGameplayScenes =
    [
        "Cinematic_Ending_A",
        "Cinematic_Ending_B",
        "Cinematic_Ending_C",
        "Cinematic_Ending_D",
        "Cinematic_Ending_E",
        "Cinematic_MrMushroom",
        "Cinematic_Stag_travel",
        "Demo End",
        "Demo Start",
        "End_Credits_Scroll",
        "End_Credits",
        "End_Game_Completion",
        "Menu_Credits",
        "Menu_Title",
        "Opening_Sequence_Act3",
        "Opening_Sequence",
        "PermaDeath",
        "Pre_Menu_Intro",
        "Pre_Menu_Loader",
        "Quit_To_Menu"
    ];

    private static readonly HashSet<string> _additiveScenes =
    [
        "Aqueduct_05_caravan",
        "Aqueduct_05_festival",
        "Aqueduct_05_pre",
        "Bellshrine_Lore_Additive",
        "Belltown_cutscene",
        "Bellway_01_boss",
        "Bellway_02_boss",
        "Bellway_03_boss",
        "Bellway_04_boss",
        "Bellway_Centipede_additive",
        "Bone_05_bellway",
        "Bone_05_boss",
        "Bone_East_08_boss_beastfly",
        "Bone_East_08_boss_golem_rest",
        "Bone_East_08_boss_golem",
        "Bonetown_boss",
        "City_Lace_cutscene",
        "Cog_Dancers_boss",
        "Greymoor_05_boss",
        "Greymoor_08_boss",
        "Greymoor_08_caravan",
        "Greymoor_08_mapper",
        "Hang_04_boss",
        "Ward_02_boss",
    ];

    public static event Action<Scene>? OnProcessFirstScene;
    public static event Action<Scene>? OnProcessScene;
    public static event Action? OnFinishedIteration;
    public static KeyCode Hotkey { get; set; } = KeyCode.D;
    public static KeyCode HotkeyModifier { get; set; } = KeyCode.LeftControl;

    private static bool _runningIteration;
    private static SortedDictionary<string, bool> _scenesProcessed = [];

    public void StartIteration()
    {
        _runningIteration = true;
        StartCoroutine(IterateAllScenes());
    }

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (Input.GetKey(HotkeyModifier) && Input.GetKeyDown(Hotkey)
            && !_runningIteration)
        {
            StartIteration();
        }
    }

    private static IEnumerator IterateAllScenes()
    {
        _scenesProcessed = new(SceneTeleportMap.Instance.sceneList.RuntimeData.Keys.ToDictionary(s => s, s => false));

        yield return new WaitUntil(() => HeroController.instance != null && HeroController.instance.CanTakeControl());

        // Handle loading all scenes, including additive scenes, in alphabetical order
        foreach (var scene in SceneTeleportMap.Instance.sceneList.RuntimeData.Keys.OrderBy(s => s))
        {
            var pd = PlayerData.instance;

            switch (scene)
            {
                case "Aqueduct_05":
                    pd.CaravanTroupeLocation = CaravanTroupeLocations.Aqueduct;
                    yield return TrySceneLoadAndProcess(scene);
                    pd.FleaGamesCanStart = true;
                    yield return TrySceneLoadAndProcess(scene);
                    pd.CaravanTroupeLocation = CaravanTroupeLocations.Bone;
                    pd.FleaGamesCanStart = false;
                    yield return TrySceneLoadAndProcess(scene);
                    break;

                case "Bellshrine":
                    yield return TrySceneLoadAndProcessManualAdditive(scene, "Bellshrine_Lore_Additive");
                    break;

                case "Bellway_01":
                case "Bellway_02":
                case "Bellway_03":
                case "Bellway_04":
                    pd.UnlockedFastTravel = true;
                    yield return TrySceneLoadAndProcess(scene);
                    pd.UnlockedFastTravel = false;
                    break;

                case "Bellway_Centipede_additive":
                    pd.blackThreadWorld = true;
                    yield return TrySceneLoadAndProcess("Belltown_basement");
                    pd.blackThreadWorld = false;
                    break;

                case "Bellway_City":
                    pd.visitedCitadel = true;
                    yield return TrySceneLoadAndProcess(scene);
                    pd.visitedCitadel = false;
                    break;

                case "Bone_05":
                    pd.defeatedBellBeast = true;
                    yield return TrySceneLoadAndProcess(scene);
                    pd.defeatedBellBeast = false;
                    yield return TrySceneLoadAndProcess(scene);
                    break;

                case "Bone_East_08":
                    pd.defeatedSongGolem = true;
                    pd.QuestCompletionData.SetData("Beastfly Hunt", new() { IsAccepted = true });
                    yield return TrySceneLoadAndProcess(scene);
                    pd.defeatedSongGolem = false;
                    pd.QuestCompletionData.SetData("Beastfly Hunt", new() { IsAccepted = false });
                    pd.encounteredSongGolem = true;
                    pd.hasBrolly = true;
                    yield return TrySceneLoadAndProcess(scene);
                    pd.encounteredSongGolem = false;
                    pd.hasBrolly = false;
                    yield return TrySceneLoadAndProcess(scene);
                    break;

                case "Bonetown":
                    pd.skullKingWillInvade = true;
                    yield return TrySceneLoadAndProcess(scene);
                    pd.skullKingWillInvade = false;
                    break;

                case "City_Lace_cutscene":
                    yield return TrySceneLoadAndProcess("Bellway_City");
                    break;

                case "Greymoor_05":
                    pd.allowVampireGnatInAltLoc = true;
                    pd.CaravanTroupeLocation = CaravanTroupeLocations.Greymoor;
                    pd.visitedCitadel = true;
                    yield return TrySceneLoadAndProcess(scene);
                    pd.allowVampireGnatInAltLoc = false;
                    pd.CaravanTroupeLocation = CaravanTroupeLocations.Bone;
                    pd.visitedCitadel = false;
                    break;

                case "Greymoor_08":
                    pd.visitedBellhart = false;
                    yield return TrySceneLoadAndProcess(scene, "top1");
                    pd.CaravanTroupeLocation = CaravanTroupeLocations.Greymoor;
                    yield return TrySceneLoadAndProcess(scene);
                    pd.CaravanTroupeLocation = CaravanTroupeLocations.Bone;
                    pd.defeatedVampireGnatBoss = true;
                    pd.QuestCompletionData.SetData("Shakra Final Quest", new() { IsCompleted = true });
                    yield return TrySceneLoadAndProcess(scene);
                    pd.defeatedVampireGnatBoss = false;
                    pd.QuestCompletionData.SetData("Shakra Final Quest", new() { IsCompleted = false });
                    break;

                case "Ward_02":
                    pd.collectedWardBossKey = true;
                    yield return TrySceneLoadAndProcess(scene);
                    pd.collectedWardBossKey = false;
                    break;

                default:
                    yield return TrySceneLoadAndProcess(scene);
                    break;
            }
        }

        Silksong_GameObjectDumpPlugin.Log("All scenes iterated successfully.");
        _runningIteration = false;

        BufferedYamlLogger sceneLogger = new("scenesProcessed.yaml");
        NestedLog sceneLog = new();
        foreach (var kvp in _scenesProcessed)
        {
            sceneLog.Add(kvp.Key, kvp.Value);
        }
        sceneLogger.Log(sceneLog);

        try
        {
            OnFinishedIteration?.Invoke();
        }
        catch (Exception e)
        {
            Silksong_GameObjectDumpPlugin.Log(e.Message);
        }
    }

    private static IEnumerator TrySceneLoadAndProcessManualAdditive(string scene, string additiveScene, string? gate = null)
    {
        yield return TrySceneLoadAndProcess(scene, gate);
        var scenePath = $"Scenes/{additiveScene}";
        AsyncOperationHandle<SceneInstance>? handle = ScenePreloader.TakeSceneLoadOperation(scenePath, LoadSceneMode.Additive) ?? Addressables.LoadSceneAsync(scenePath, LoadSceneMode.Additive);
        yield return handle;
        ProcessScene(SceneManager.GetSceneByName(additiveScene));
        yield return Addressables.UnloadSceneAsync(handle.Value);
    }

    private static IEnumerator TrySceneLoadAndProcess(string scene, string? gate = null)
    {
        if (_nonGameplayScenes.Contains(scene))
        {
            Silksong_GameObjectDumpPlugin.Log($"{scene} is not a gameplay scene. Skipping load");
            yield break;
        }

        if (_additiveScenes.Contains(scene))
        {
            Silksong_GameObjectDumpPlugin.Log($"{scene} is additive scene. Skipping load as active scene");
            yield break;
        }

        var sceneInfo = SceneTeleportMap.Instance.sceneList.RuntimeData[scene];

        if (gate is not null)
        {
            Silksong_GameObjectDumpPlugin.Log($"Loading {scene} with transition gate {gate}");
        }
        else if (sceneInfo.TransitionGates.Any())
        {
            gate =
                sceneInfo.TransitionGates.FirstOrDefault(g => g.Contains("left"))
                ?? sceneInfo.TransitionGates.FirstOrDefault(g => g.Contains("right"))
                ?? sceneInfo.TransitionGates.FirstOrDefault(g => g.Contains("top"))
                ?? sceneInfo.TransitionGates.FirstOrDefault(g => g.Contains("bot"))
                ?? sceneInfo.TransitionGates[0];

            Silksong_GameObjectDumpPlugin.Log($"Loading {scene} with transition gate {gate}");
        }
        else if (sceneInfo.RespawnPoints.Any())
        {
            GameManager.UnsafeInstance.RespawningHero = true;
            PlayerData.instance.tempRespawnMarker = sceneInfo.RespawnPoints[0];
            Silksong_GameObjectDumpPlugin.Log($"Loading {scene} with respawn point {sceneInfo.RespawnPoints[0]}");
        }
        // The one non-additive scene that has no transition gates or respawn points
        else if (scene is "Room_Caravan_Interior_Travel")
        {
            Silksong_GameObjectDumpPlugin.Log($"Loading {scene} without tgates or rps");
        }
        else
        {
            Silksong_GameObjectDumpPlugin.Log($"{scene} does not have valid load info - should not happen! Check if a game update has changed something.");
            yield break;
        }

        GameManager.SceneLoadInfo sceneLoadInfo = new()
        {
            SceneName = scene,
            EntryGateName = gate,
            PreventCameraFadeOut = false,
            WaitForSceneTransitionCameraFade = true,
            Visualization = GameManager.SceneLoadVisualizations.Default,
            AlwaysUnloadUnusedAssets = true,
            IsFirstLevelForPlayer = false,
        };

        yield return GameManager.UnsafeInstance.BeginSceneTransitionRoutine(sceneLoadInfo);
        yield return new WaitUntil(() => GameManager.instance.sceneLoad == null);

        for (int _ = 0; _ < 5; _++)
        {
            yield return null;
        }

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            ProcessScene(SceneManager.GetSceneAt(i));
        }

        yield return new WaitUntil(() => HeroController.instance != null && HeroController.instance.CanTakeControl());
    }
    
    private static void ProcessScene(Scene scene)
    {
        if (_scenesProcessed[scene.name])
        {
            Silksong_GameObjectDumpPlugin.Log($"Scene already previously processed: {scene.name}.");
            return;
        }

        _scenesProcessed[scene.name] = true;

        try
        {
            OnProcessScene?.Invoke(scene);
        }
        catch (Exception e)
        {
            Silksong_GameObjectDumpPlugin.Log(e.Message);
        }
    }
}

