using System.Collections.Generic;
using HutongGames.PlayMaker;
using UnityEngine;

namespace Silksong.GameObjectDump.Logging;

public class LogContext(DumpOptions? dumpOptions = null)
{
    private readonly HashSet<object> _coreObjects = [];
    private readonly Dictionary<object, ReferenceLogNode> _logNodeCache = new(ReferenceComparer.Instance);
    private int _nextId = 0;

    public DumpOptions DumpOptions => dumpOptions ?? new();

    internal void CacheAndRegisterNode(object obj, ReferenceLogNode node)
    {
        _logNodeCache.Add(obj, node);
    }

    internal bool TryGetCachedNode(object obj, out ReferenceLogNode node)
    {
        if (!_logNodeCache.TryGetValue(obj, out node)) return false;
        node.ApplyId = true;
        return true;
    }

    /// <summary>
    /// Traverse hierarchy to get all core objects (GameObject, Compomnent, FsmState and FsmStateAction) that should be fully logged.
    /// </summary>
    /// <param name="gameObjects"></param>
    internal void RegisterCoreObjects(IEnumerable<GameObject?> gameObjects)
    {
        foreach (var go in gameObjects)
        {
            if (go != null && RegisterGameObject(go))
            {
                _coreObjects.Add(go);
            }
        }
    }

    internal bool IsCoreObject(object? obj)
    {
        return obj != null && _coreObjects.Contains(obj);
    }

    internal bool RegisterFinalId(ReferenceLogNode node)
    {
        if (!node.ApplyId) return true;
        if (node.Id is not null) return false;
        node.Id = _nextId++;
        return true;
    }

    private bool RegisterGameObject(GameObject go)
    {
        bool notEmpty = false;

        foreach (Component c in go.GetComponents<Component>())
        {
            if (c == null || !DumpOptions.DumpFullComponent.Invoke(c)
                || (c is PlayMakerFSM fsm && !RegisterFsm(fsm))) continue;

            _coreObjects.Add(c);
            notEmpty = true;
        }

        if (!DumpOptions.DumpFullGameObjectChildren)
        {
            return notEmpty;
        }

        foreach (Transform child in go.transform)
        {
            if (child != null && RegisterGameObject(child.gameObject))
            {
                _coreObjects.Add(child.gameObject);
                notEmpty = true;
            }
        }

        return notEmpty;
    }

    private bool RegisterFsm(PlayMakerFSM fsm)
    {
        if (fsm == null) return false;

        bool fsmNotEmpty = false;

        foreach (FsmState state in fsm.FsmStates)
        {
            foreach (FsmStateAction action in state.Actions)
            {
                if (DumpOptions.DumpFullFsmAction.Invoke(action))
                {
                    _coreObjects.Add(action);
                    fsmNotEmpty = true;
                }
            }

            // Always add states even when there aren't actions
            _coreObjects.Add(state);
        }

        return fsmNotEmpty;
    }
}

public sealed class ReferenceComparer : IEqualityComparer<object>
{
    public static readonly ReferenceComparer Instance = new();

    private ReferenceComparer() { }

    public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);

    public int GetHashCode(object obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
}
