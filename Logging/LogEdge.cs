using System;
using System.Reflection;
using Silksong.GameObjectDump.Logging.Loggables;

namespace Silksong.GameObjectDump.Logging;

public sealed record LogEdge
{
    public LogEdge() { }

    public LogEdge(object? obj, string? header)
    {
        Object = obj;
        Header = header;

        if (Object is null)
        {
            Node = new ValueLogNode() { ConciseLog = "[null]" };
            return;
        }

        if (Object is string s)
        {
            Node = new ValueLogNode() { ConciseLog = s };
            return;
        }

        if (Object is UnityEngine.Object uObj && uObj == null)
        {
            Node = new ValueLogNode() { ConciseLog = $"[null Unity object] ({Object.GetPrettyNameFromObject()})" };
            return;
        }
    }

    public object? Object { get; set; }
    public string? Header { get; set; }
    public LogNode? Node { get; set; }
    internal bool InCoreHierarchy { get; set; }

    // Gets the target object by cached reflection and returns the LogEdge.
    public static LogEdge GetEdgeRef(object parent, string memberName, string? headerOverride = null)
    {
        var header = headerOverride ?? memberName;
        var parentType = parent.GetType();
        var member = LoggableRegistry.GetMemberCached(parentType, memberName);
        if (member is null)
        {
            return new($"[Could not find member name {memberName} in {parentType.GetPrettyNameFromType()}]", header);
        }

        object? child;

        try
        {
            child = member switch
            {
                FieldInfo f => f.GetValue(parent),
                PropertyInfo p => p.GetValue(parent),
                _ => null
            };
        }
        catch
        {
            return new($"[Failed to get member {memberName} in {parentType.GetPrettyNameFromType()}]", header);
        }

        return new(child, headerOverride ?? memberName);
    }

    // Fills edges with children nodes in DFS order.
    internal void Propagate(LogContext ctx)
    {
        if (Node is ReferenceLogNode rln1)
        {
            // GameObjectDumpPlugin.Log($"Enqueing children in existing node: {Header}");
            foreach (var c in rln1.Children)
            {
                if (c.Node is not ValueLogNode) c.Propagate(ctx);
            }
            return;
        }
        
        if (Node is ValueLogNode)
        {
            GameObjectDumpPlugin.LogWarning($"A ValueLogNode was enqueued: {Header}");
            return;
        }

        if (Object == null)
        {
            GameObjectDumpPlugin.Log($"Object is null: {Header}");
            return;
        }

        if (ctx.TryGetCachedNode(Object, out ReferenceLogNode cached))
        {
            Node = cached;
            return;
        }

        Node = LoggableRegistry.ToLog(Object, ctx);

        if (Node is ReferenceLogNode rln2)
        {
            ctx.CacheAndRegisterNode(Object, rln2);
            foreach (var c in rln2.Children)
            {
                if (c.Node is not ValueLogNode) c.Propagate(ctx);
            }
        }
    }
}