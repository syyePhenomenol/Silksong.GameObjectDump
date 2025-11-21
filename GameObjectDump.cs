using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Silksong.GameObjectDump.Logging;
using Silksong.GameObjectDump.Logging.Loggables;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Silksong.GameObjectDump;

public static class GameObjectDump
{
    /// <summary>
    /// Dumps the GameObject to a txt file. Can configure to filter for certain Components and FsmStateActions.
    /// Warning: the dumped file can be very large if you don't filter anything.
    /// </summary>
    /// <param name="go"></param>
    /// <param name="dumpOptions"></param>
    /// <param name="path"></param>
    /// <param name="append"></param>
    public static void Dump(this GameObject? go, string? path = null, bool append = false, DumpOptions? dumpOptions = null)
    {
        if (go == null) return;

        try
        {
            path ??= Path.Combine(GetAssemblyPath(), go.name + ".txt");
        }
        catch (Exception e)
        {
            GameObjectDumpPlugin.LogError($"Failed to retrieve assembly or file path: {e.Message}");
            return;
        }

        Dump([go], path, append, dumpOptions);
    }

    /// <summary>
    /// Dumps the scene's GameObjects to a txt file. Can configure to filter for certain Components and FsmStateActions.
    /// Warning: the dumped file can be very large if you don't filter anything.
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="dumpOptions"></param>
    /// <param name="path"></param>
    /// <param name="append"></param>
    public static void Dump(this Scene scene, string? path = null, bool append = false, DumpOptions? dumpOptions = null)
    {
        try
        {
            path ??= Path.Combine(GetAssemblyPath(), scene.name + ".txt");
        }
        catch (Exception e)
        {
            GameObjectDumpPlugin.LogError($"Failed to retrieve assembly or file path: {e.Message}");
            return;
        }

        scene.GetRootGameObjects().Dump(path, append, dumpOptions);
    }

    /// <summary>
    /// Dumps the GameObjects to a txt file. Can configure to filter for certain Components and FsmStateActions.
    /// You must specify the path of the file to dump to.
    /// Warning: the dumped file can be very large if you don't filter anything.
    /// </summary>
    /// <param name="go"></param>
    /// <param name="dumpOptions"></param>
    /// <param name="path"></param>
    /// <param name="append"></param>
    public static void Dump(this IEnumerable<GameObject?>? gameObjects, string? path, bool append = false, DumpOptions? dumpOptions = null)
    {
        if (gameObjects == null) return;

        LogContext ctx = new(dumpOptions ?? new());

        // First pass: traverse core hierarchy (DFS)
        try
        {
            ctx.RegisterCoreObjects(gameObjects);
        }
        catch (Exception e)
        {
            GameObjectDumpPlugin.LogError($"Failed to register core objects: {e.Message}");
            return;
        }

        List<LogEdge> rootEdges = [.. gameObjects.Select((go, idx) => new LogEdge(go, idx.ToString()) with { InCoreHierarchy = true })];

        // Second pass: fill in all LogNodes (DFS)
        try
        {
            foreach (var rootEdge in rootEdges)
            {
                rootEdge.Propagate(ctx);
            }
        }
        catch (Exception e)
        {
            GameObjectDumpPlugin.LogError($"Failed to fill log nodes: {e.Message}");
            return;
        }

        // Final pass: build string
        var sb = new StringBuilder();
        try
        {  
            foreach (var rootEdge in rootEdges)
            {
                if (ctx.DumpOptions.OmitIfNotFull && !ctx.IsCoreObject(rootEdge.Object)) continue;
                WriteEdge(sb, rootEdge, 0, ctx);
            }
        }
        catch (Exception e)
        {
            GameObjectDumpPlugin.LogError($"Failed to build string: {e.Message}");
            return;
        }

        try
        {
            if (!append) File.Delete(path);
            File.AppendAllText(path, sb.ToString());
        }
        catch (Exception e)
        {
            GameObjectDumpPlugin.LogError($"Failed to write to file: { e.Message }");
        }
    }

    /// <summary>
    /// Clearing caches for getting fields/properties by reflection.
    /// </summary>
    public static void ClearReflectionCaches()
    {
        LoggableRegistry.ClearReflectionCaches();
    }

    private static string GetAssemblyPath()
    {
        string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (string.IsNullOrEmpty(assemblyPath)) assemblyPath = Application.persistentDataPath;
        return assemblyPath;
    }

    private static void WriteEdge(StringBuilder sb, LogEdge edge, int indent, LogContext ctx)
    {
        if (edge.Node is null)
        {
            GameObjectDumpPlugin.LogWarning($"Null node! {edge.Header}");
            return;
        }

        var indentStr = new string(' ', indent * 2);
        var header = edge.Header;

        if (edge.Node is ValueLogNode vln)
        {
            header = !string.IsNullOrEmpty(header) ? $"{header}: " : ""; 
            sb.AppendLine($"{indentStr}{header}{vln.ConciseLog}");
            return;
        }
        else if (edge.Node is ReferenceLogNode rln)
        {
            var seenFirstTime = ctx.RegisterFinalId(rln);

            bool toFullLog = edge.InCoreHierarchy || (!ctx.IsCoreObject(edge.Object) && seenFirstTime);

            var idStr = rln.Id is int id ? $"[{(toFullLog ? "ID" : "REF")} {id}]" : null;

            if (!toFullLog || !rln.Children.Any())
            {
                header = JoinWithWhiteSpace([header, idStr]);
                header = !string.IsNullOrEmpty(header) ? $"{header}: " : "";
                sb.AppendLine($"{indentStr}{header}{rln.ConciseLog}");
                return;
            }

            header = JoinWithWhiteSpace([edge.Header, rln.ExtraHeader, idStr]);
            header = !string.IsNullOrEmpty(header) ? $"{header}:" : "[missing header]:";

            sb.AppendLine($"{indentStr}{header}");

            foreach (var child in rln.Children)
            {
                WriteEdge(sb, child, indent + 1, ctx);
            }
        }
    }

    private static string JoinWithWhiteSpace(List<string?> strings)
    {
        return string.Join(" ", strings.Where(s => !string.IsNullOrEmpty(s)));
    }
}