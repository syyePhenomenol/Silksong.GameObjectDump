using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace Silksong.GameObjectDump.Logging.Loggables
{
    public static class LoggableRegistry
    {
        public static DefaultLoggable DefaultLoggable { get; } = new();
        public static DefaultForeignLoggable DefaultForeignLoggable { get; } = new();
        private static readonly Dictionary<Type, object?> _loggableCache = [];
        private static readonly HashSet<Type> _registeredForeignTypes = [];
        private static readonly Dictionary<Type, object> _registeredLoggables = [];

        private static readonly Dictionary<Type, FieldInfo[]> _serializableFieldsCache = [];
        private static readonly Dictionary<Type, PropertyInfo[]> _getPropertiesCache = [];
        private static readonly Dictionary<Type, MemberInfo[]> _allMembersCache = [];
        private static readonly Dictionary<(Type type, string name), MemberInfo?> _memberLookupCache = [];
        private static readonly Dictionary<Type, MethodInfo?> _toLogMethodCache = [];

        /// <summary>
        /// Add a foreign Type to be logged in a default way. Every field and property of the type will get dumped.
        /// If you want more custom behaviour, register a Loggable instead.
        /// </summary>
        /// <param name="type"></param>
        public static void Register(Type type)
        {
            if (type == typeof(object)) return;

            _registeredForeignTypes.Add(type);
            GameObjectDumpPlugin.Log($"[LogRegistry] added foreign type {type} with default logging");
        }

        /// <summary>
        /// Add a Loggable to either add or override custom logging behaviour for certain types.
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <param name="instance"></param>
        public static void Register<T1, T2>(Loggable<T1, T2> instance) where T1 : LogNode, new()
        {
            if (typeof(T2) == typeof(object)) return;
            
            _registeredLoggables[typeof(T2)] = instance;
            GameObjectDumpPlugin.Log($"[LogRegistry] added {instance.GetType().Name} - {typeof(T2).FullName}");
        }

        public static LogNode ToLog(object obj, LogContext ctx)
        {
            var type = obj.GetType();

            if (_loggableCache.TryGetValue(type, out var cached))
                return cached != null ? InvokeToLog(cached, obj, ctx) : DefaultLoggable.ToLog(obj, ctx);

            if (_registeredForeignTypes.Any(ft => ft.IsAssignableFrom(type)))
            {
                _loggableCache[type] = DefaultForeignLoggable;
                return DefaultForeignLoggable.ToLog(obj, ctx);
            }

            if (_registeredLoggables.TryGetValue(type, out var exact))
            {
                _loggableCache[type] = exact;
                return InvokeToLog(exact, obj, ctx);
            }

            object? best = null;
            int bestDistance = int.MaxValue;

            foreach (var kv in _registeredLoggables)
            {
                if (!kv.Key.IsAssignableFrom(type)) continue;

                int distance = GetInheritanceDistance(type, kv.Key);
                if (distance < bestDistance)
                {
                    best = kv.Value;
                    bestDistance = distance;
                }
            }

            _loggableCache[type] = best;
            return best != null ? InvokeToLog(best, obj, ctx) : DefaultLoggable.ToLog(obj, ctx);
        }

        public static FieldInfo[] GetSerializableFieldsCached(Type type)
        {
            if (_serializableFieldsCache.TryGetValue(type, out var result))
                return result;

            result = [.. ComputeSerializableFields(type)];
            _serializableFieldsCache[type] = result;
            return result;
        }

        public static PropertyInfo[] GetGetPropertiesCached(Type type)
        {
            if (_getPropertiesCache.TryGetValue(type, out var result))
                return result;

            result = [.. type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
                .Where(f => f.GetCustomAttribute<ObsoleteAttribute>() == null)];
            _getPropertiesCache[type] = result;
            return result;
        }
        
        public static MemberInfo[] GetAllMembersCached(Type type)
        {
            if (_allMembersCache.TryGetValue(type, out var result))
                return result;

            result = [.. ComputeAllMembers(type)];
            _allMembersCache[type] = result;
            return result;
        }

        public static MemberInfo? GetMemberCached(Type type, string name)
        {
            var key = (type, name);
            if (_memberLookupCache.TryGetValue(key, out var member))
                return member;

            var current = type;
            while (current != null)
            {
                var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

                var field = current.GetField(name, flags);
                if (field != null)
                    return _memberLookupCache[key] = field;

                var prop = current.GetProperty(name, flags);
                if (prop != null)
                    return _memberLookupCache[key] = prop;

                current = current.BaseType!;
            }

            return _memberLookupCache[key] = null;
        }

        internal static void Initialize()
        {
            GameObjectDumpPlugin.Log("[LogRegistry] initializing...");

            foreach (var t in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (typeof(DefaultLoggable).IsAssignableFrom(t) || t.IsAbstract || t.IsInterface || t.ContainsGenericParameters)
                    continue;

                var baseType = FindLoggableBaseType(t);
                if (baseType == null)
                    continue;

                var targetType = baseType.GetGenericArguments()[1];

                try
                {
                    object instance =
                        t.GetConstructor(Type.EmptyTypes) is ConstructorInfo ctor
                            ? ctor.Invoke(null)
                            : FormatterServices.GetUninitializedObject(t);

                    _registeredLoggables[targetType] = instance;
                    GameObjectDumpPlugin.Log($"[LogRegistry] added {t.Name} - {targetType.FullName}");
                }
                catch (Exception e)
                {
                    GameObjectDumpPlugin.LogWarning($"[LogRegistry] Failed to create {t.FullName}: {e.Message}");
                }
            }
        }

        internal static void ClearReflectionCaches()
        {
            _serializableFieldsCache.Clear();
            _getPropertiesCache.Clear();
            _allMembersCache.Clear();
            _memberLookupCache.Clear();
            _toLogMethodCache.Clear();
        }
        
        private static IEnumerable<FieldInfo> ComputeSerializableFields(Type type)
        {
            BindingFlags flags =
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly;

            Type? current = type;
            while (current != null && current != typeof(object))
            {
                foreach (var f in current.GetFields(flags)
                    .Where(f => f.IsPublic || f.GetCustomAttribute<UnityEngine.SerializeField>() != null)
                    .Where(f => f.GetCustomAttribute<ObsoleteAttribute>() == null))
                {
                    yield return f;
                }

                current = current.BaseType;
            }
        }

        private static IEnumerable<MemberInfo> ComputeAllMembers(Type t)
        {
            BindingFlags flags =
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Instance |
                BindingFlags.Static |
                BindingFlags.DeclaredOnly;

            Type? current = t;
            while (current != null && current != typeof(object))
            {
                foreach (var f in current.GetFields(flags)) yield return f;
                foreach (var p in current.GetProperties(flags)) yield return p;
                current = current.BaseType;
            }
        }

        private static Type? FindLoggableBaseType(Type t)
        {
            while (t != null && t != typeof(object))
            {
                if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Loggable<,>))
                    return t;
                t = t.BaseType!;
            }
            return null;
        }

        private static MethodInfo? GetToLogMethodCached(Type t)
        {
            if (_toLogMethodCache.TryGetValue(t, out var m))
                return m;

            m = t.GetMethod("ToLog",
                BindingFlags.Public | BindingFlags.Instance);
            _toLogMethodCache[t] = m;
            return m;
        }

        // Computes distance from derived type to baseType or interface.
        // Base class distance: number of inheritance hops
        // Interface distance: minimal number of hops from class hierarchy to interface
        private static int GetInheritanceDistance(Type derived, Type baseType)
        {
            if (baseType.IsInterface)
            {
                return GetInterfaceDistance(derived, baseType);
            }

            int distance = 0;
            for (var cur = derived; cur != null; cur = cur.BaseType)
            {
                if (cur == baseType) return distance;
                distance++;
            }

            return int.MaxValue;
        }

        private static int GetInterfaceDistance(Type type, Type iface)
        {
            if (!iface.IsInterface || !iface.IsAssignableFrom(type))
                return int.MaxValue;

            // BFS through inheritance + interfaces to find minimal path
            var visited = new HashSet<Type>();
            var queue = new Queue<(Type t, int d)>();
            queue.Enqueue((type, 0));

            while (queue.Count > 0)
            {
                var (current, dist) = queue.Dequeue();
                if (current == iface) return dist;
                if (!visited.Add(current)) continue;
                if (current.BaseType != null) queue.Enqueue((current.BaseType, dist + 1));
                foreach (var i in current.GetInterfaces()) queue.Enqueue((i, dist + 1));
            }

            return int.MaxValue;
        }

        private static LogNode InvokeToLog(object loggable, object obj, LogContext ctx)
        {
            var m = GetToLogMethodCached(loggable.GetType());
            if (m is null)
            {
                GameObjectDumpPlugin.LogWarning(
                    $"[LogRegistry] Missing ToLog() on {loggable.GetType().FullName}"
                );
                return new ValueLogNode() { ConciseLog = "[reflection error]" };
            }

            return (LogNode)m.Invoke(loggable, [obj, ctx]);
        }
    }
}
