using System;
using HutongGames.PlayMaker;
using UnityEngine;

namespace Silksong.GameObjectDump.Logging;

public enum ComponentFilterMode { Blacklist, Whitelist }

public record DumpOptions
{
    /// <summary>
    /// Whether or not to dump the given Component in full.
    /// </summary>
    public Func<Component, bool> DumpFullComponent { get; init; } = (c) => true;

    /// <summary>
    /// Whether or not to dump the given FsmAction in full.
    /// </summary>
    public Func<FsmStateAction, bool> DumpFullFsmAction { get; init; } = (a) => true;

    /// <summary>
    /// Whether or not to recursively dump children GameObjects in full.
    /// </summary>
    public bool DumpFullGameObjectChildren { get; init; } = true;

    /// <summary>
    /// Makes objects not dumped in full to be completely omitted.
    /// If all of a GameObject or PlayMakerFSM's content are null or omitted, the corresponding parents are also omitted.
    /// </summary>
    public bool OmitIfNotFull { get; init; } = true;

    /// <summary>
    /// Replaces large enough arrays acquired by reflection with short text.
    /// </summary>
    public int LargeArrayThreshold { get; init; } = 100;

    public static DumpOptions MonoBehavioursOnly { get; } = new()
    {
        DumpFullComponent = (c) => c is MonoBehaviour
    };

    public static DumpOptions FsmsOnly { get; } = new()
    {
        DumpFullComponent = (c) => c is PlayMakerFSM
    };
}