using System.Collections.Generic;
using UnityEngine;

namespace Silksong.GameObjectDump.Logging.Loggables;

public class Matrix4x4Loggable : Loggable<ReferenceLogNode, Matrix4x4>
{
    public override void Fill(ReferenceLogNode node, Matrix4x4 obj, LogContext ctx)
    {
        node.Children.AddRange(GetLines(obj));
    }

    private static IEnumerable<LogEdge> GetLines(Matrix4x4 obj)
    {
        for (int i = 0; i < 4; i++)
        {
            yield return new() { Node = new ValueLogNode() { ConciseLog = $"{obj[i, 0],12:F6}\t{obj[i, 1],12:F6}\t{obj[i, 2],12:F6}\t{obj[i, 3],12:F6}" }, Header = "" };
        }
    }
}