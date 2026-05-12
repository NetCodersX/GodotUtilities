using System;
using System.Collections.Generic;
using Godot;

namespace Utilities.Persistence.Reflection;

public static class ResourceSerializer
{
    private const string RESOURCE_TYPE_ID = "__type__";

    public static Godot.Collections.Dictionary SerializeResource(ISerializableResource res)
    {
        var dict = res.Serialize();
        dict[RESOURCE_TYPE_ID] = res.GetType().AssemblyQualifiedName;
        return dict;
    }

    public static ISerializableResource DeserializeResource(Godot.Collections.Dictionary data)
    {
        if (!data.TryGetValue(RESOURCE_TYPE_ID, out var typeVar))
        {
            GD.PrintErr("[Saving] Missing '__type__' key in serialized resource.");
            return null;
        }

        var type = Type.GetType(typeVar.AsString());
        if (type == null)
        {
            GD.PrintErr($"[Saving] Could not resolve type '{typeVar.AsString()}'.");
            return null;
        }

        var instance = (ISerializableResource)Activator.CreateInstance(type);
        instance.Deserialize(data);
        return instance;
    }

    public static void DeserializeMember(SavedMember member, object target, Godot.Collections.Dictionary data)
    {
        if (!data.ContainsKey(member.Key)) return;

        if (typeof(ISerializableResource).IsAssignableFrom(member.Type))
            HandleSingleResource(member, target, data);
        else if (IsResourceListType(member.Type))
            HandleResourceList(member, target, data);
        else if (IsResourceDictType(member.Type))
            HandleResourceDict(member, target, data);
        else
            member.SetValue(target, VariantConverter.FromVariant(data[member.Key], member.Type));
    }

    private static void HandleSingleResource(SavedMember member, object target, Godot.Collections.Dictionary data)
    {
        if (member.GetValue(target) is ISerializableResource existing)
        {
            existing.Deserialize(data[member.Key].AsGodotDictionary());
        }
        else
        {
            if (member.Type.IsInterface || member.Type.IsAbstract)
            {
                GD.PrintErr($"[Saving] Cannot instantiate abstract/interface '{member.Type.Name}'.");
                return;
            }
            var instance = (ISerializableResource)Activator.CreateInstance(member.Type);
            instance.Deserialize(data[member.Key].AsGodotDictionary());
            member.SetValue(target, instance);
        }
    }

    private static void HandleResourceList(SavedMember member, object target, Godot.Collections.Dictionary data)
    {
        var savedArr = data[member.Key].AsGodotArray();
        bool isGodotArray = member.Type.GetGenericTypeDefinition() == typeof(Godot.Collections.Array<>);
        
        var collection = isGodotArray 
            ? Activator.CreateInstance(member.Type) 
            : (System.Collections.IList)Activator.CreateInstance(member.Type);
            
        var addMethod = isGodotArray ? member.Type.GetMethod("Add") : null;

        foreach (var elem in savedArr)
        {
            var res = DeserializeResource(elem.AsGodotDictionary());
            if (res == null) continue;

            if (isGodotArray) addMethod!.Invoke(collection, new object[] { res });
            else ((System.Collections.IList)collection).Add(res);
        }
        member.SetValue(target, collection);
    }

    private static void HandleResourceDict(SavedMember member, object target, Godot.Collections.Dictionary data)
    {
        var keyType = member.Type.GetGenericArguments()[0];
        var savedDict = data[member.Key].AsGodotDictionary();
        bool isGodotDict = member.Type.GetGenericTypeDefinition() == typeof(Godot.Collections.Dictionary<,>);

        var dict = isGodotDict
            ? Activator.CreateInstance(member.Type)
            : (System.Collections.IDictionary)Activator.CreateInstance(member.Type);

        var addMethod = isGodotDict ? member.Type.GetMethod("Add") : null;

        foreach (var kvp in savedDict)
        {
            var key = VariantConverter.FromVariant(kvp.Key, keyType);
            var res = DeserializeResource(kvp.Value.AsGodotDictionary());
            if (res == null) continue;

            if (isGodotDict) addMethod!.Invoke(dict, new object[] { key, res });
            else ((System.Collections.IDictionary)dict).Add(key, res);
        }
        member.SetValue(target, dict);
    }

    public static bool IsResourceListType(Type t) => t.IsGenericType && t.GetGenericArguments().Length == 1 &&
        typeof(ISerializableResource).IsAssignableFrom(t.GetGenericArguments()[0]) &&
        (t.GetGenericTypeDefinition() == typeof(List<>) || t.GetGenericTypeDefinition() == typeof(Godot.Collections.Array<>));

    public static bool IsResourceDictType(Type t) => t.IsGenericType && t.GetGenericArguments().Length == 2 &&
        typeof(ISerializableResource).IsAssignableFrom(t.GetGenericArguments()[1]) &&
        (t.GetGenericTypeDefinition() == typeof(Dictionary<,>) || t.GetGenericTypeDefinition() == typeof(Godot.Collections.Dictionary<,>));

    public static bool IsResourceEnumerable(System.Collections.IEnumerable e) => IsResourceListType(e.GetType());
    public static bool IsResourceDict(System.Collections.IDictionary d) => IsResourceDictType(d.GetType());

    public static Godot.Collections.Array SerializeResourceEnumerable(System.Collections.IEnumerable list)
    {
        var arr = new Godot.Collections.Array();
        foreach (var item in list)
            if (item is ISerializableResource res) arr.Add(SerializeResource(res));
        return arr;
    }

    public static Godot.Collections.Dictionary SerializeResourceDict(System.Collections.IDictionary dict)
    {
        var result = new Godot.Collections.Dictionary();
        foreach (System.Collections.DictionaryEntry kvp in dict)
            if (kvp.Value is ISerializableResource res) result[VariantConverter.ToVariant(kvp.Key)] = SerializeResource(res);
        return result;
    }
}