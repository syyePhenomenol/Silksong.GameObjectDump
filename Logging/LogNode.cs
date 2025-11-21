using System.Collections.Generic;

namespace Silksong.GameObjectDump.Logging;

public abstract class LogNode
{
    public string? ConciseLog { get; set; }
}

public sealed class ValueLogNode : LogNode;

public sealed class ReferenceLogNode : LogNode
{
    public List<LogEdge> Children = [];

    // Appends the header inherited by the parent node.
    public string? ExtraHeader { get; set; }

    // // If Children all resolve to ConciseLogs, serialize them as a combined single line.
    // public bool ConcatChildren { get; set; }

    public bool ApplyId { get; set; }

    public int? Id { get; set; }
}