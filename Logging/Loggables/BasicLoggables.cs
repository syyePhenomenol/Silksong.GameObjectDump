using System;
using System.Collections;
using System.Collections.Generic;

namespace Silksong.GameObjectDump.Logging.Loggables;

public class LoggableBool : Loggable<ValueLogNode, bool>
{
    public override void Fill(ValueLogNode node, bool obj, LogContext ctx)
    {
        node.ConciseLog = obj.ToString();
    }
}

public class LoggableString : Loggable<ValueLogNode, string>
{
    public override void Fill(ValueLogNode node, string obj, LogContext ctx)
    {
        node.ConciseLog = obj;
    }
}

public class LoggableEnum : Loggable<ValueLogNode, Enum>
{
    public override void Fill(ValueLogNode node, Enum obj, LogContext ctx)
    {
        node.ConciseLog = Convert.ToString(obj);
    }
}

public class LoggableIFormattable : Loggable<ValueLogNode, IFormattable>
{
    public override void Fill(ValueLogNode node, IFormattable obj, LogContext ctx)
    {
        node.ConciseLog = Convert.ToString(obj)?.Replace("\n", ", ") ?? "[null]";
    }
}

public class LoggableIEnumerable : Loggable<ReferenceLogNode, IEnumerable>
{
    public override void Fill(ReferenceLogNode node, IEnumerable obj, LogContext ctx)
    {
        if (!obj.GetEnumerator().MoveNext())
        {
            node.ConciseLog = $"[empty {obj.GetPrettyNameFromObject()}]";
            return;
        } 
        
        List<LogEdge> children = [.. GetChildren(obj)];

        node.ConciseLog = $"[{children.Count} objects in {obj.GetPrettyNameFromObject()}]";

        if (children.Count > ctx.DumpOptions.LargeArrayThreshold) return;

        node.Children.AddRange(children);
    }

    public static IEnumerable<LogEdge> GetChildren(IEnumerable ie)
    {
        int idx = 0;
        foreach (var element in ie)
        {
            yield return new(element, idx.ToString());
            idx++;
        }
    }
}