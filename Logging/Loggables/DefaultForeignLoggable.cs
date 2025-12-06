using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Silksong.GameObjectDump.Logging.Loggables;

public class DefaultForeignLoggable : DefaultLoggable
{
    public override void Fill(ReferenceLogNode node, object obj, LogContext ctx)
    {
        FillAllMembers(node, obj, ctx);
    }

    public static void FillAllMembers(ReferenceLogNode node, object obj, LogContext ctx)
    {
        var header = $"[foreign] {obj.GetPrettyNameFromObject()}";
        node.ConciseLog ??= header;
        List<(Type type, LogEdge edge)> allMembers = [.. GetAllMembers(obj)];
        node.Children.AddRange(allMembers.Where(m => ShouldDumpReflectedMember(m.type, ctx)).Select(m => m.edge));
        node.ExtraHeader = $"--- {header}";
    }

    public static IEnumerable<(Type, LogEdge)> GetAllMembers(object parent)
    {
        foreach (var m in LoggableRegistry.GetAllMembersCached(parent.GetType()))
        {
            Type memberType = m switch
            {
                FieldInfo f => f.FieldType,
                PropertyInfo p => p.PropertyType,
                _ => throw new NotImplementedException(),
            };

            yield return (memberType, LogEdge.GetEdgeRef(parent, m));
        }
    }
}