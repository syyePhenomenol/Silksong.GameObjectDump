using System.Collections.Generic;

namespace Silksong.GameObjectDump.Logging.Loggables;

public class DefaultLoggable : Loggable<ReferenceLogNode, object>
{
    public override void Fill(ReferenceLogNode node, object obj, LogContext ctx)
    {
        Fill(node, obj);
    }

    /// <summary>
    /// Tries to fill with serializable fields, otherwise tries to fill with public property getters.
    /// </summary>
    /// <param name="node"></param>
    /// <param name="obj"></param>
    /// <param name="ignoreMembers"></param>
    public static void Fill(ReferenceLogNode node, object obj, ICollection<string>? ignoreMembers = null, bool allowForeignTypes = false)
    {
        var assemblyName = obj.GetType().Assembly.FullName;

        if (!allowForeignTypes
            && !assemblyName.Contains("Unity")
            && !assemblyName.Contains("Assembly-CSharp")
            && !assemblyName.Contains("TeamCherry")
            && !assemblyName.Contains("HutongGames")
            && !assemblyName.Contains("PlayMaker"))
        {
            node.ConciseLog = $"[unhandled type {obj.GetType().GetPrettyNameFromType()}]";
            return;
        }

        if (!FillFields(node, obj, ignoreMembers))
        {
            FillProperties(node, obj, ignoreMembers);
        }
    }

    public static bool FillFields(ReferenceLogNode node, object obj, ICollection<string>? ignoreFields = null)
    {
        List<LogEdge> serializableFields = [.. GetSerializableFields(obj, ignoreFields)];

        if (serializableFields.Count is 0)
        {
            if (node.ConciseLog is not null)
            {
                node.ConciseLog += " [no auto-fields]";
            }
            else
            {
                node.ConciseLog = $"[no auto-fields in {obj.GetPrettyNameFromObject()}]";
            }
            return false;
        }

        node.ConciseLog ??= obj.GetPrettyNameFromObject();
        node.Children.AddRange(serializableFields);
        return true;
    }

    public static bool FillProperties(ReferenceLogNode node, object obj, ICollection<string>? ignoreProperties = null)
    {
        List<LogEdge> serializableProperties = [.. GetSerializableProperties(obj, ignoreProperties)];

        if (serializableProperties.Count is 0)
        {
            node.ConciseLog = $"{obj.GetPrettyNameFromObject()} [no auto-properties]";
            return false;
        }

        node.ConciseLog = obj.GetPrettyNameFromObject();
        node.Children.AddRange(serializableProperties);
        return true;
    }

    public static IEnumerable<LogEdge> GetSerializableFields(object parent, ICollection<string>? ignoreFields = null)
    {
        foreach (var f in LoggableRegistry.GetSerializableFieldsCached(parent.GetType()))
        {
            if (ignoreFields?.Contains(f.Name) ?? false) continue;
            yield return LogEdge.GetEdgeRef(parent, f.Name);
        }
    }

    public static IEnumerable<LogEdge> GetSerializableProperties(object parent, ICollection<string>? ignoreProperties = null)
    {
        foreach (var p in LoggableRegistry.GetGetPropertiesCached(parent.GetType()))
        {
            if (ignoreProperties?.Contains(p.Name) ?? false) continue;
            yield return LogEdge.GetEdgeRef(parent, p.Name);
        }
    }
}