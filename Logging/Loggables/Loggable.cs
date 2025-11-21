using System;

namespace Silksong.GameObjectDump.Logging.Loggables
{
    public abstract class Loggable<T1, T2> where T1 : LogNode, new()
    {
        public LogNode ToLog(object obj, LogContext ctx)
        {
            try
            {
                T1 node = new();
                Fill(node, (T2)obj, ctx);
                return node;
            }
            catch (Exception e)
            {
                GameObjectDumpPlugin.LogError($"{e.Message}, {this.GetPrettyNameFromObject()}, {obj.GetPrettyNameFromObject()}");
            }

            return new ValueLogNode() { ConciseLog = "[Loggable failed to get LogNode]" };
        }

        public abstract void Fill(T1 node, T2 obj, LogContext ctx);
    }
}
