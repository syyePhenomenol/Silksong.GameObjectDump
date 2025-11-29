using System.Collections.Generic;
using System.Linq;
using HutongGames.PlayMaker;

namespace Silksong.GameObjectDump.Logging.Loggables;

public class LoggablePlayMakerFSM : Loggable<ReferenceLogNode, PlayMakerFSM>
{
    public override void Fill(ReferenceLogNode node, PlayMakerFSM obj, LogContext ctx)
    {
        node.ConciseLog = $"{obj.FsmName} [{obj.GetPrettyNameFromObject()}]";

        if (!ctx.IsCoreObject(obj)) return;

        node.Children.AddRange(
        [
            LogEdge.GetEdgeRef(obj, nameof(obj.Active)),
            LogEdge.GetEdgeRef(obj, nameof(obj.ActiveStateName)),
            LogEdge.GetEdgeRef(obj, nameof(obj.UsesTemplate)),
            LogEdge.GetEdgeRef(obj, nameof(obj.FsmVariables)),
            LogEdge.GetEdgeRef(obj, nameof(obj.FsmEvents)),
            LogEdge.GetEdgeRef(obj, nameof(obj.FsmGlobalTransitions))
        ]);

        List<LogEdge> stateEdges = [.. GetStateEdges(obj, ctx)];
        ReferenceLogNode statesNode = new();
        if (stateEdges.Count is not 0)
        {
            statesNode.Children = stateEdges;
        }
        else
        {
            statesNode.ConciseLog = "[empty]";
        }
        node.Children.Add(new() { Node = statesNode, Header = "States", InCoreHierarchy = true });

        node.ExtraHeader = $"{LoggableComponent.GetComponentHeader(obj)} - {obj.FsmName}";
    }

    public static IEnumerable<LogEdge> GetStateEdges(PlayMakerFSM fsm, LogContext ctx)
    {
        int idx = 0;
        foreach (var s in fsm.FsmStates)
        {
            if (!ctx.DumpOptions.OmitIfNotFull || ctx.IsCoreObject(s))
            {
                yield return new LogEdge(s, idx.ToString()) { InCoreHierarchy = true };
            }
            idx++;
        }
    }
}

public class LoggableFsmState : Loggable<ReferenceLogNode, FsmState>
{
    public override void Fill(ReferenceLogNode node, FsmState obj, LogContext ctx)
    {
        node.ConciseLog = $"{obj.Name} ({obj.GetPrettyNameFromObject()})";

        if (!ctx.IsCoreObject(obj)) return;

        node.Children.Add(LogEdge.GetEdgeRef(obj, nameof(obj.Transitions)));

        List<LogEdge> actionEdges = [.. GetActionEdges(obj, ctx)];
        ReferenceLogNode actionsNode = new();
        if (actionEdges.Count is not 0)
        {
            actionsNode.Children = actionEdges;
        }
        else
        {
            actionsNode.ConciseLog = "[empty]";
        }
        node.Children.Add(new() { Node = actionsNode, Header = "Actions", InCoreHierarchy = true });
        node.ExtraHeader = $"--- {typeof(FsmState).Name} - {obj.Name}";
    }

    public static IEnumerable<LogEdge> GetActionEdges(FsmState state, LogContext ctx)
    {
        int idx = 0;
        foreach (var a in state.Actions)
        {
            if (!ctx.DumpOptions.OmitIfNotFull || ctx.IsCoreObject(a))
            {
                yield return new LogEdge(a, idx.ToString()) { InCoreHierarchy = true};
            }
            idx++;
        }
    }
}

public class LoggableFsmTransition : Loggable<ValueLogNode, FsmTransition>
{
    public override void Fill(ValueLogNode node, FsmTransition obj, LogContext ctx)
    {
        node.ConciseLog = $"{obj.EventName} => {obj.ToState}";
    }
}

public class LoggableFsmEvent : Loggable<ReferenceLogNode, FsmEvent>
{
    public override void Fill(ReferenceLogNode node, FsmEvent obj, LogContext ctx)
    {
        var typeName = obj.GetPrettyNameFromObject();
        node.ConciseLog = $"{obj.Name} [{typeName}]";
        DefaultLoggable.FillFields(node, obj, ctx, []);
        node.ExtraHeader = typeName;
    }
}

public class LoggableFsmStateAction : Loggable<ReferenceLogNode, FsmStateAction>
{
    public override void Fill(ReferenceLogNode node, FsmStateAction obj, LogContext ctx)
    {
        var typeName = obj.GetPrettyNameFromObject();
        node.ConciseLog = $"{typeName} [{typeof(FsmStateAction).Name}]";
        if (!ctx.IsCoreObject(obj)) return;
        DefaultLoggable.FillFields(node, obj, ctx, []);
        node.ExtraHeader = $"--- {typeof(FsmStateAction).Name} - {typeName}";
    }
}

public class LoggableFsmVariables : Loggable<ReferenceLogNode, FsmVariables>
{
    public override void Fill(ReferenceLogNode node, FsmVariables obj, LogContext ctx)
    {
        new LoggableIEnumerable().Fill(node, obj.GetAllNamedVariablesSorted(), ctx);
    }
}

public class LoggableFsmVar : Loggable<ReferenceLogNode, FsmVar>
{
    public override void Fill(ReferenceLogNode node, FsmVar obj, LogContext ctx)
    {
        object value = obj.GetValue();
        node.ConciseLog = value?.GetType()?.Name ?? "[null]";
        node.Children.Add(new(value, "Value"));
    }
}

public class LoggableNamedVariable : Loggable<ReferenceLogNode, NamedVariable>
{
    public override void Fill(ReferenceLogNode node, NamedVariable obj, LogContext ctx)
    {
        var typeName = obj.GetPrettyNameFromObject();
        if (obj.RawValue is not null)
        {
            var valueNode = LoggableRegistry.ToLog(obj.RawValue, ctx);

            if (obj is FsmArray)
            {
                node.Children.Add(LogEdge.GetEdgeRef(obj, nameof(obj.Name)));
                node.Children.Add(new() { Node = valueNode, Header = "Value" });
                node.ExtraHeader = $"[{typeName}]";
            }
            else
            {
                node.ConciseLog = !string.IsNullOrEmpty(obj.Name)
                    ? $"{{ Name: {obj.Name}, Value: {valueNode.ConciseLog} }} [{typeName}]"
                    : $"{valueNode.ConciseLog} [{typeName}]";
            }
        }
        else
        {
            node.ConciseLog = !string.IsNullOrEmpty(obj.Name)
                ? $"{{ Name: {obj.Name} }} [{typeName}]"
                : $"[empty {typeName}]";
        }
    }
}