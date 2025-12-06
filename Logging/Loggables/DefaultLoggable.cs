using System;
using System.Collections.Generic;
using System.Linq;

namespace Silksong.GameObjectDump.Logging.Loggables;

public class DefaultLoggable : Loggable<ReferenceLogNode, object>
{
    public override void Fill(ReferenceLogNode node, object obj, LogContext ctx)
    {
        FillBaseGameType(node, obj, ctx);
    }

    /// <summary>
    /// Tries to fill with serializable fields, otherwise tries to fill with public property getters.
    /// Only base game types get handled by this method.
    /// </summary>
    /// <param name="node"></param>
    /// <param name="obj"></param>
    /// <param name="ignoreMembers"></param>
    public static void FillBaseGameType(ReferenceLogNode node, object obj, LogContext ctx, ICollection<string>? ignoreMembers = null)
    {
        var assemblyName = obj.GetType().Assembly.GetName().Name;

        if (!assemblyName.StartsWith("Unity")
            && !assemblyName.StartsWith("Assembly-CSharp")
            && !assemblyName.StartsWith("TeamCherry")
            && !assemblyName.StartsWith("HutongGames")
            && !assemblyName.StartsWith("PlayMaker")
            && !assemblyName.StartsWith("InControl")
            && !assemblyName.StartsWith("QuestPlayMakerActions")
            && !assemblyName.StartsWith("StateMachineBehaviours"))
        {
            node.ConciseLog = $"[unhandled type {obj.GetPrettyNameFromObject()}]";
            return;
        }

        if (!FillFields(node, obj, ctx, ignoreMembers))
        {
            FillProperties(node, obj, ctx, ignoreMembers);
        }
    }

    public static bool FillFields(ReferenceLogNode node, object obj, LogContext ctx, ICollection<string>? ignoreFields = null)
    {
        List<(Type type, LogEdge edge)> serializableFields = [.. GetSerializableFields(obj, ignoreFields)];

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
        node.Children.AddRange(serializableFields.Where(f => ShouldDumpReflectedMember(f.type, ctx)).Select(f => f.edge));
        return true;
    }

    public static bool FillProperties(ReferenceLogNode node, object obj, LogContext ctx, ICollection<string>? ignoreProperties = null)
    {
        List<(Type type, LogEdge edge)> serializableProperties = [.. GetSerializableProperties(obj, ignoreProperties)];

        if (serializableProperties.Count is 0)
        {
            if (node.ConciseLog is not null)
            {
                node.ConciseLog += " [no auto-properties]";
            }
            else
            {
                node.ConciseLog = $"[no auto-properties in {obj.GetPrettyNameFromObject()}]";
            }
            return false;
        }

        node.ConciseLog = obj.GetPrettyNameFromObject();
        node.Children.AddRange(serializableProperties.Where(p => ShouldDumpReflectedMember(p.type, ctx)).Select(p => p.edge));
        return true;
    }

    public static IEnumerable<(Type, LogEdge)> GetSerializableFields(object parent, ICollection<string>? ignoreFields = null)
    {
        foreach (var f in LoggableRegistry.GetSerializableFieldsCached(parent.GetType()))
        {
            if (ignoreFields?.Contains(f.Name) ?? false) continue;
            yield return (f.FieldType, LogEdge.GetEdgeRef(parent, f));
        }
    }

    public static IEnumerable<(Type, LogEdge)> GetSerializableProperties(object parent, ICollection<string>? ignoreProperties = null)
    {
        foreach (var p in LoggableRegistry.GetGetPropertiesCached(parent.GetType()))
        {
            if (ignoreProperties?.Contains(p.Name) ?? false) continue;
            yield return (p.PropertyType, LogEdge.GetEdgeRef(parent, p));
        }
    }

    public static bool ShouldDumpReflectedMember(Type type, LogContext ctx)
    {
        try
        {
            return ctx.DumpOptions.DumpReflectedType.Invoke(type);
        }
        catch (Exception e)
        {
            GameObjectDumpPlugin.LogError($"Failed to invoke {nameof(ctx.DumpOptions.DumpReflectedType)}: {e.Message}");
        }
        return true;
    }
}