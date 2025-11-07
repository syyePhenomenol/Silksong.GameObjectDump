using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Silksong.GameObjectDump;

public static class GameObjectUtils
{
    /// <summary>
    /// Returns a list of all GameObjects in the given scene, with their full hierarchy paths.
    /// Traversal order is depth-first (parent before children).
    /// </summary>
    public static List<(string path, GameObject go)> GetAllGameObjectsInScene(Scene scene)
    {
        var result = new List<(string, GameObject)>();

        if (!scene.isLoaded)
        {
            Silksong_GameObjectDumpPlugin.LogWarning($"Scene '{scene.name}' is not loaded.");
            return result;
        }

        foreach (var root in scene.GetRootGameObjects())
        {
            Traverse(root, root.name, result);
        }

        return result;
    }

    /// <summary>
    /// Recursive depth-first traversal of GameObject hierarchy.
    /// </summary>
    private static void Traverse(GameObject go, string path, List<(string, GameObject)> result)
    {
        result.Add((path, go));

        foreach (Transform child in go.transform)
        {
            Traverse(child.gameObject, $"{path}/{child.name}", result);
        }
    }
}