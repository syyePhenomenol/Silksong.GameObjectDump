using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Silksong.GameObjectDump.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Silksong.GameObjectDump;

public static class ComponentFsmActionCounter
{
    private static readonly BufferedYamlLogger _componentLogger = new("components.yaml");
    private static readonly BufferedYamlLogger _fsmActionLogger = new("fsmActions.yaml");
    private static Dictionary<string, int> _components = [];
    private static Dictionary<string, int> _fsmActions = [];

    private static bool _nonSceneObjectsCounted = false;

    public static void CountNonSceneComponentsAndFsmActions(Scene scene)
    {
        if (_nonSceneObjectsCounted) return;

        var nonSceneGameObjects = (GameObject[])UnityEngine.Object.FindObjectsByType(typeof(GameObject), FindObjectsInactive.Include, FindObjectsSortMode.None);
        CountComponentsAndFsmActions(nonSceneGameObjects);
        _nonSceneObjectsCounted = true;
    }

    public static void CountComponentsandFsmActions(Scene scene)
    {
        var sceneGOs = GameObjectUtils.GetAllGameObjectsInScene(scene);
        CountComponentsAndFsmActions(sceneGOs.Select(sg => sg.go));
    }

    public static void CountComponentsAndFsmActions(IEnumerable<GameObject> gos)
    {
        var assemblyCSharp = typeof(GameManager).Assembly;

        foreach (var go in gos)
        {
            foreach (var c in go.GetComponents<Component>())
            {
                if (c.GetType().Assembly == assemblyCSharp)
                {
                    var componentType = c.GetType().Name;

                    if (_components.ContainsKey(componentType))
                    {
                        _components[componentType]++;
                    }
                    else
                    {
                        _components[componentType] = 1;
                    }
                }

                if (c is PlayMakerFSM fsm)
                {
                    foreach (var action in fsm.FsmStates.SelectMany(state => state.actions))
                    {
                        var fsmActionType = action.GetType().Name;

                        if (_fsmActions.ContainsKey(fsmActionType))
                        {
                            _fsmActions[fsmActionType]++;
                        }
                        else
                        {
                            _fsmActions[fsmActionType] = 1;
                        }
                    }
                }
            }
        }
    }

    public static void LogResults()
    {
        NestedLog componentLog = new();
        foreach (var kvp in _components.OrderByDescending(kvp => kvp.Value))
        {
            componentLog.Add(kvp.Key, kvp.Value);
        }
        _componentLogger.Log(componentLog);

        NestedLog fsmActionLog = new();
        foreach (var kvp in _fsmActions.OrderByDescending(kvp => kvp.Value))
        {
            fsmActionLog.Add(kvp.Key, kvp.Value);
        }
        _fsmActionLogger.Log(fsmActionLog);
    }
}