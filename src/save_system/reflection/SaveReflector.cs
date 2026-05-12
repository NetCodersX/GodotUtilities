using System;

namespace Utilities.Persistence.Reflection;

public static class SaveReflector
{
    public static Godot.Collections.Dictionary Serialize(ISaveable target)
    {
        var dict = new Godot.Collections.Dictionary();
        
        foreach (var member in ReflectionCache.GetOrBuild(target.GetType()))
        {
            dict[member.Key] = VariantConverter.ToVariant(member.GetValue(target));
        }
        
        return dict;
    }

    public static void Deserialize(ISaveable target, Godot.Collections.Dictionary data)
    {
        foreach (var member in ReflectionCache.GetOrBuild(target.GetType()))
        {
            ResourceSerializer.DeserializeMember(member, target, data);
        }
    }
}

public class SavedMember(string key, Type type, Func<object, object> getValue, Action<object, object> setValue)
{
    public string Key => key;
    public Type Type => type;

    public object GetValue(object target) => getValue(target);
    public void SetValue(object target, object value) => setValue?.Invoke(target, value);
}