using System;
using System.Collections.Generic;
using System.Reflection;

namespace Utilities.Persistence.Reflection;

public static class ReflectionCache
{
    private static readonly BindingFlags Flags = 
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    
    private static readonly Dictionary<Type, SavedMember[]> Cache = new();

    public static SavedMember[] GetOrBuild(Type type)
    {
        if (Cache.TryGetValue(type, out var cached))
            return cached;

        var members = new List<SavedMember>();

        foreach (var field in type.GetFields(Flags))
        {
            var attr = field.GetCustomAttribute<SaveAttribute>();
            if (attr is null) continue;

            members.Add(new SavedMember(
                attr.Key ?? field.Name, 
                field.FieldType, 
                t => field.GetValue(t), 
                (t, v) => field.SetValue(t, v)
            ));
        }

        foreach (var prop in type.GetProperties(Flags))
        {
            var attr = prop.GetCustomAttribute<SaveAttribute>();
            if (attr is null || !prop.CanRead) continue;

            members.Add(new SavedMember(
                attr.Key ?? prop.Name, 
                prop.PropertyType, 
                t => prop.GetValue(t), 
                prop.CanWrite ? (t, v) => prop.SetValue(t, v) : null
            ));
        }

        return Cache[type] = members.ToArray();
    }
}