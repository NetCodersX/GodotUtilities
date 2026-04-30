using Godot;
using System;
using System.Reflection;
using System.Collections.Generic;

namespace Utilities.Persistence.Reflection;

public class SavedMember(string key, Type type, Func<object, object> getValue, Action<object, object> setValue)
{
    public string Key => key;
    public Type Type => type;

    public object GetValue(object target) => getValue(target);
    public void SetValue(object target, object value) => setValue?.Invoke(target, value);
}

public static class SaveReflector
{
    private static readonly BindingFlags Flags = 
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    
    private static readonly Dictionary<Type, SavedMember[]> cache = new();

    private const string RESOURCE_TYPE_ID = "__type__";

    public static Godot.Collections.Dictionary Serialize(ISaveable target)
    {
        var dict = new Godot.Collections.Dictionary();
        foreach (var member in GetOrBuild(target.GetType()))
            dict[member.Key] = ToVariant(member.GetValue(target));
        return dict;
    }

    public static void Deserialize(ISaveable target, Godot.Collections.Dictionary data)
    {
        foreach (var member in GetOrBuild(target.GetType()))
        {
            if (!data.ContainsKey(member.Key)) continue;

            if (typeof(ISerializableResource).IsAssignableFrom(member.Type))
            {
                if (member.Type.IsInterface || member.Type.IsAbstract)
                {
                    GD.PrintErr($"[Saving] Cannot instantiate '{member.Type.Name}' for '{member.Key}': " +
                                $"type must be a concrete class with a parameterless constructor.");
                    continue;
                }

                var instance = (ISerializableResource)Activator.CreateInstance(member.Type);
                instance.Deserialize(data[member.Key].AsGodotDictionary());
                member.SetValue(target, instance);
            }
            else if (IsResourceListType(member.Type))
            {
                var savedArr  = data[member.Key].AsGodotArray();

                // Godot.Collections.Array<T>
                if (member.Type.GetGenericTypeDefinition() == typeof(Godot.Collections.Array<>))
                {
                    var typedArr = Activator.CreateInstance(member.Type);
                    var addMethod = member.Type.GetMethod("Add");
                    foreach (var elem in savedArr)
                    {
                        var res = DeserializeResource(elem.AsGodotDictionary());
                        if (res != null) addMethod!.Invoke(typedArr, [res]);
                    }
                    member.SetValue(target, typedArr);
                }
                else // System.Collections.Generic.List<T>
                {
                    var list = (System.Collections.IList)Activator.CreateInstance(member.Type);
                    foreach (var elem in savedArr)
                    {
                        var res = DeserializeResource(elem.AsGodotDictionary());
                        if (res != null) list.Add(res);
                    }
                    member.SetValue(target, list);
                }
            }
            else if (IsResourceDictType(member.Type))
            {
                var args      = member.Type.GetGenericArguments();
                var keyType   = args[0];
                var savedDict = data[member.Key].AsGodotDictionary();

                // Godot.Collections.Dictionary<TKey, TValue>
                if (member.Type.GetGenericTypeDefinition() == typeof(Godot.Collections.Dictionary<,>))
                {
                    var typedDict  = Activator.CreateInstance(member.Type);
                    var addMethod  = member.Type.GetMethod("Add");
                    foreach (var kvp in savedDict)
                    {
                        var key = FromVariant(kvp.Key, keyType);
                        var res = DeserializeResource(kvp.Value.AsGodotDictionary());
                        if (res != null) addMethod!.Invoke(typedDict, [key, res]);
                    }
                    member.SetValue(target, typedDict);
                }
                else // System.Collections.Generic.Dictionary<TKey, TValue>
                {
                    var dict = (System.Collections.IDictionary)Activator.CreateInstance(member.Type);
                    foreach (var kvp in savedDict)
                    {
                        var key = FromVariant(kvp.Key, keyType);
                        var res = DeserializeResource(kvp.Value.AsGodotDictionary());
                        if (res != null) dict.Add(key, res);
                    }
                    member.SetValue(target, dict);
                }
            }
            else
            {
                member.SetValue(target, FromVariant(data[member.Key], member.Type));
            }
        }
    }

    private static SavedMember[] GetOrBuild(Type type)
    {
        if (cache.TryGetValue(type, out var cached))
            return cached;

        var members = new List<SavedMember>();

        foreach (var field in type.GetFields(Flags))
        {
            var attr = field.GetCustomAttribute<SaveAttribute>();
            if (attr is null) continue;

            var member = new SavedMember(
                attr.Key ?? field.Name, 
                field.FieldType, 
                t => field.GetValue(t), 
                (t, v) => field.SetValue(t, v)
            );
            members.Add(member);
        }

        foreach (var prop in type.GetProperties(Flags))
        {
            var attr = prop.GetCustomAttribute<SaveAttribute>();
            if (attr is null || !prop.CanRead) continue;

            var member = new SavedMember(
                attr.Key ?? prop.Name, 
                prop.PropertyType, 
                t => prop.GetValue(t), 
                prop.CanWrite ? (t, v) => prop.SetValue(t, v) : null
            );
            members.Add(member);
        }

        return cache[type] = members.ToArray();
    }

    private static Godot.Collections.Dictionary SerializeResource(ISerializableResource res)
    {
        var dict = res.Serialize();
        dict[RESOURCE_TYPE_ID] = res.GetType().AssemblyQualifiedName;
        return dict;
    }

    private static ISerializableResource DeserializeResource(Godot.Collections.Dictionary data)
    {
        if (!data.TryGetValue(RESOURCE_TYPE_ID, out var typeVar))
        {
            GD.PrintErr("[Saving] Missing '__type__' key in serialized resource — cannot deserialize.");
            return null;
        }

        var type = Type.GetType(typeVar.AsString());
        if (type == null)
        {
            GD.PrintErr($"[Saving] Could not resolve type '{typeVar.AsString()}' — was the class renamed or moved?");
            return null;
        }

        var instance = (ISerializableResource)Activator.CreateInstance(type);
        instance.Deserialize(data);
        return instance;
    }

    private static bool IsResourceListType(Type t)
    {
        if (!t.IsGenericType) return false;
        var args = t.GetGenericArguments();
        if (args.Length != 1) return false;
        if (!typeof(ISerializableResource).IsAssignableFrom(args[0])) return false;

        var def = t.GetGenericTypeDefinition();
        return def == typeof(List<>)
            || def == typeof(Godot.Collections.Array<>);
    }

    private static bool IsResourceDictType(Type t)
    {
        if (!t.IsGenericType) return false;
        var args = t.GetGenericArguments();
        if (args.Length != 2) return false;
        if (!typeof(ISerializableResource).IsAssignableFrom(args[1])) return false;

        var def = t.GetGenericTypeDefinition();
        return def == typeof(Dictionary<,>)
            || def == typeof(Godot.Collections.Dictionary<,>);
    }

    private static bool IsResourceEnumerable(System.Collections.IEnumerable e)
        => IsResourceListType(e.GetType());

    private static bool IsResourceDict(System.Collections.IDictionary d)
        => IsResourceDictType(d.GetType());

    private static Godot.Collections.Array SerializeResourceEnumerable(System.Collections.IEnumerable list)
    {
        var arr = new Godot.Collections.Array();
        foreach (var item in list)
            if (item is ISerializableResource res)
                arr.Add(SerializeResource(res));
        return arr;
    }

    private static Godot.Collections.Dictionary SerializeResourceDict(System.Collections.IDictionary dict)
    {
        var result = new Godot.Collections.Dictionary();
        foreach (System.Collections.DictionaryEntry kvp in dict)
            if (kvp.Value is ISerializableResource res)
                result[ToVariant(kvp.Key)] = SerializeResource(res);
        return result;
    }

    private static Variant ToVariant(object value) => value switch
    {
        int      i => Variant.From(i),
        float    f => Variant.From(f),
        double   d => Variant.From(d),
        bool     b => Variant.From(b),
        string   s => Variant.From(s),

        Vector2  v => Variant.From(v),
        Vector2I v => Variant.From(v),

        Vector3  v => Variant.From(v),
        Vector3I v => Variant.From(v),

        Vector4  v => Variant.From(v),
        Vector4I v => Variant.From(v),

        Rect2    v => Variant.From(v),
        Rect2I   v => Variant.From(v),

        Transform2D v => Variant.From(v),
        Transform3D v => Variant.From(v),

        Color c => Variant.From(c),
        Enum  e => Variant.From(Convert.ToInt32(e)),

        Godot.Collections.Array arr   => Variant.From(arr),
        Godot.Collections.Dictionary gDict => Variant.From(gDict),

        ISerializableResource res => Variant.From(SerializeResource(res)),

        System.Collections.IEnumerable list when IsResourceEnumerable(list)
            => Variant.From(SerializeResourceEnumerable(list)),

        System.Collections.IDictionary sDict when IsResourceDict(sDict)
            => Variant.From(SerializeResourceDict(sDict)),

        _ => throw new NotSupportedException(
            $"Cannot convert '{value.GetType().Name}' to Variant. " +
            $"Supported types are primitives, vectors, colors, arrays, dictionaries, and enums.")
    };

    private static object FromVariant(Variant v, Type t)
    {
        if (t == typeof(int))      return v.AsInt32();
        if (t == typeof(float))    return v.AsSingle();
        if (t == typeof(double)) return v.AsDouble();
        if (t == typeof(bool))     return v.AsBool();
        if (t == typeof(string))   return v.AsString();
        if (t == typeof(Vector2))  return v.AsVector2();
        if (t == typeof(Vector2I)) return v.AsVector2I();
        if (t == typeof(Vector3))  return v.AsVector3();
        if (t == typeof(Vector3I)) return v.AsVector3I();
        if (t == typeof(Color))    return v.AsColor();
        if (t == typeof(Vector4))  return v.AsVector4();
        if (t == typeof(Vector4I)) return v.AsVector4I();
        if (t == typeof(Rect2)) return v.AsRect2();
        if (t == typeof(Rect2I)) return v.AsRect2I();
        if (t == typeof(Transform2D)) return v.AsTransform2D();
        if (t == typeof(Transform3D)) return v.AsTransform3D();

        if (t == typeof(Godot.Collections.Array)) return v.AsGodotArray();
        if (t == typeof(Godot.Collections.Dictionary)) return v.AsGodotDictionary();
        if (t.IsEnum) return Enum.ToObject(t, v.AsInt32());

        throw new NotSupportedException(
            $"[Saving] No FromVariant converter for '{t.Name}'. " +
            $"Use OnDeserialize() for this field.");
    }
}


