using System;
using System.Linq;

namespace Silksong.GameObjectDump;

public static class TypeExtensions
{
    public static string GetPrettyNameFromObject(this object obj)
    {
        return obj.GetType().GetPrettyNameFromType();
    }

    public static string GetPrettyNameFromType(this Type type)
    {
        if (type.IsGenericType)
        {
            var typeName = type.Name;
            var backtickIndex = typeName.IndexOf('`');
            if (backtickIndex > 0)
            {
                typeName = typeName[..backtickIndex];
            }

            var genericArgs = type.GetGenericArguments().Select(GetPrettyNameFromType);

            return $"{typeName}<{string.Join(", ", genericArgs)}>";
        }

        if (type.IsArray)
        {
            return $"{type.GetElementType()!.GetPrettyNameFromType()}[]";
        }

        if (Nullable.GetUnderlyingType(type) is Type underlying)
        {
            return $"{underlying.GetPrettyNameFromType()}?";
        }

        return type switch
        {
            { } t when t == typeof(int)    => "int",
            { } t when t == typeof(string) => "string",
            { } t when t == typeof(bool)   => "bool",
            { } t when t == typeof(float)  => "float",
            { } t when t == typeof(double) => "double",
            { } t when t == typeof(decimal)=> "decimal",
            { } t when t == typeof(void)   => "void",
            _ => type.Name
        };
    }
}