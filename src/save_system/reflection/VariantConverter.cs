using System;
using Godot;

namespace Utilities.Persistence.Reflection;

public static class VariantConverter
{
    public static Variant ToVariant(object value) => value switch
    {
        int i => Variant.From(i),
        float f => Variant.From(f),
        double d => Variant.From(d),
        bool b => Variant.From(b),
        string s => Variant.From(s),

        Vector2 v => Variant.From(v),
        Vector2I v => Variant.From(v),
        Vector3 v => Variant.From(v),
        Vector3I v => Variant.From(v),
        Vector4 v => Variant.From(v),
        Vector4I v => Variant.From(v),

        Rect2 v => Variant.From(v),
        Rect2I v => Variant.From(v),
        Transform2D v => Variant.From(v),
        Transform3D v => Variant.From(v),

        Color c => Variant.From(c),
        Enum e => Variant.From(Convert.ToInt32(e)),

        Godot.Collections.Array arr => Variant.From(arr),
        Godot.Collections.Dictionary gDict => Variant.From(gDict),

        ISerializableResource res => Variant.From(ResourceSerializer.SerializeResource(res)),

        System.Collections.IEnumerable list when ResourceSerializer.IsResourceEnumerable(list)
            => Variant.From(ResourceSerializer.SerializeResourceEnumerable(list)),

        System.Collections.IDictionary sDict when ResourceSerializer.IsResourceDict(sDict)
            => Variant.From(ResourceSerializer.SerializeResourceDict(sDict)),

        _ => throw new NotSupportedException(
            $"Cannot convert '{value.GetType().Name}' to Variant. " +
            $"Supported types are primitives, vectors, colors, arrays, dictionaries, enums & ISerializableResources.")
    };

    public static object FromVariant(Variant v, Type t)
    {
        if (t == typeof(int)) return v.AsInt32();
        if (t == typeof(float)) return v.AsSingle();
        if (t == typeof(double)) return v.AsDouble();
        if (t == typeof(bool)) return v.AsBool();
        if (t == typeof(string)) return v.AsString();
        
        if (t == typeof(Vector2)) return v.AsVector2();
        if (t == typeof(Vector2I)) return v.AsVector2I();
        if (t == typeof(Vector3)) return v.AsVector3();
        if (t == typeof(Vector3I)) return v.AsVector3I();
        if (t == typeof(Vector4)) return v.AsVector4();
        if (t == typeof(Vector4I)) return v.AsVector4I();
        
        if (t == typeof(Rect2)) return v.AsRect2();
        if (t == typeof(Rect2I)) return v.AsRect2I();
        if (t == typeof(Transform2D)) return v.AsTransform2D();
        if (t == typeof(Transform3D)) return v.AsTransform3D();
        if (t == typeof(Color)) return v.AsColor();

        if (t == typeof(Godot.Collections.Array)) return v.AsGodotArray();
        if (t == typeof(Godot.Collections.Dictionary)) return v.AsGodotDictionary();
        if (t.IsEnum) return Enum.ToObject(t, v.AsInt32());

        throw new NotSupportedException(
            $"[Saving] No FromVariant converter for '{t.Name}'. " +
            $"Use OnDeserialize() for this field.");
    }
}