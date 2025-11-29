using System.Collections.Generic;
using UnityEngine;

namespace Silksong.GameObjectDump.Logging.Loggables;

public class LoggableUnityObject : Loggable<ReferenceLogNode, Object>
{
    public override void Fill(ReferenceLogNode node, Object obj, LogContext ctx)
    {
        node.ConciseLog = $"{obj.name} [{obj.GetPrettyNameFromObject()}]";
        DefaultLoggable.FillFields(node, obj, ctx, ["name", "hideFlags"]);
    }
}

public class LoggableGameObject : Loggable<ReferenceLogNode, GameObject>
{
    public override void Fill(ReferenceLogNode node, GameObject obj, LogContext ctx)
    {
        node.ConciseLog = $"{obj.name} [{obj.GetPrettyNameFromObject()}]";

        if (!ctx.IsCoreObject(obj)) return;

        List<LogEdge> components = [.. GetComponentEdges(obj, ctx)];
        List<LogEdge> childGos = [.. GetChildGoEdges(obj, ctx)];

        node.Children.AddRange
        (
            [
                LogEdge.GetEdgeRef(obj, nameof(obj.layer)),
                LogEdge.GetEdgeRef(obj, nameof(obj.activeSelf)),
                LogEdge.GetEdgeRef(obj, nameof(obj.activeInHierarchy)),
                LogEdge.GetEdgeRef(obj, nameof(obj.isStatic)),
                LogEdge.GetEdgeRef(obj, nameof(obj.tag)),
                new(obj.scene.name, nameof(obj.scene)),
                LogEdge.GetEdgeRef(obj, nameof(obj.hideFlags))
            ]
        );

        ReferenceLogNode componentsNode = new();
        if (components.Count is not 0)
        {
            componentsNode.Children = components;
        }
        else
        {
            componentsNode.ConciseLog = "[empty]";
        }
        node.Children.Add(new() { Node = componentsNode, Header = "Components", InCoreHierarchy = true });

        ReferenceLogNode childGosNode = new();
        if (childGos.Count is not 0)
        {
            childGosNode.Children = childGos;
        }
        else
        {
            childGosNode.ConciseLog = "[empty]";
        }
        node.Children.Add(new() { Node = childGosNode, Header = "Children", InCoreHierarchy = true });
        
        node.ExtraHeader = $"===== {nameof(GameObject)} - {obj.name}";
    }

    public static IEnumerable<LogEdge> GetComponentEdges(GameObject go, LogContext ctx)
    {
        int idx = 0;
        foreach (var c in go.GetComponents<Component>())
        {
            if (!ctx.DumpOptions.OmitIfNotFull || ctx.IsCoreObject(c))
            {
                yield return new LogEdge(c, idx.ToString()) { InCoreHierarchy = true };
            }
            idx++;
        }
    }

    public static IEnumerable<LogEdge> GetChildGoEdges(GameObject go, LogContext ctx)
    {
        int idx = 0;
        foreach (Transform t in go.transform ?? [])
        {
            if (!ctx.DumpOptions.OmitIfNotFull || ctx.IsCoreObject(t.gameObject))
            {
                yield return new LogEdge(t.gameObject, idx.ToString()) { InCoreHierarchy = true };
            }
            idx++;
        }
    }
}

public class LoggableComponent : Loggable<ReferenceLogNode, Component>
{
    public static string GetComponentHeader(Component c) => GetComponentHeader(c.GetPrettyNameFromObject());
    public static string GetComponentHeader(string componentType) => $"--- {componentType}";

    public static void FillDefaultComponent(ReferenceLogNode node, Component obj)
    {
        var typeName = obj.GetPrettyNameFromObject();
        node.ConciseLog = $"{obj.name} [{typeName}]";
        node.ExtraHeader = GetComponentHeader(typeName);
    }

    public override void Fill(ReferenceLogNode node, Component obj, LogContext ctx)
    {
        FillDefaultComponent(node, obj);
        if (!ctx.IsCoreObject(obj)) return;
        DefaultLoggable.Fill(node, obj, ctx, ["name", "hideFlags", "gameObject", "transform", "tag"]);
    }
}

public class LoggableTransform : Loggable<ReferenceLogNode, Transform>
{
    public override void Fill(ReferenceLogNode node, Transform obj, LogContext ctx)
    {
        LoggableComponent.FillDefaultComponent(node, obj);
        if (!ctx.IsCoreObject(obj)) return;
        node.Children.AddRange
        (
            [
                LogEdge.GetEdgeRef(obj, nameof(obj.position)),
                LogEdge.GetEdgeRef(obj, nameof(obj.localPosition)),
                new(obj.rotation.eulerAngles, nameof(obj.rotation)),
                new(obj.localRotation.eulerAngles, nameof(obj.localRotation)),
                LogEdge.GetEdgeRef(obj, nameof(obj.localScale)),
                LogEdge.GetEdgeRef(obj, nameof(obj.childCount)),
            ]
        );
    }
}

public class LoggableMonoBehaviour : Loggable<ReferenceLogNode, MonoBehaviour>
{
    public override void Fill(ReferenceLogNode node, MonoBehaviour obj, LogContext ctx)
    {
        LoggableComponent.FillDefaultComponent(node, obj);
        if (!ctx.IsCoreObject(obj)) return;
        DefaultLoggable.Fill(node, obj, ctx, ["name", "hideFlags", "gameObject", "transform", "tag"]);
    }
}