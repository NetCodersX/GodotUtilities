using Godot;
using System;
using System.Reflection;
using System.Collections.Generic;

namespace Utilities;

public static class NodeInjector
{
    private static readonly Dictionary<Type, NodeMember[]> cache = new();

    public static void WireNodes(this Node root)
    {
        Type type = root.GetType();

        if (!cache.TryGetValue(type, out NodeMember[] members))
        {
            members = BuildCache(type);
            cache[type] = members;
        }

        foreach (var member in members)
        {
            string path = member.Path ?? GetNodePath(member);
            var node = root.GetNodeOrNull(path);

            if (node == null)
            {
                GD.PushError($"[NodeInjector] Missing node at path '{path}' for '{type.Name}.{member.Name}'");
                continue; 
            }

            if (!member.MemberType.IsInstanceOfType(node))
            {
                GD.PushError($"[NodeInjector] Type mismatch at '{path}'. Expected {member.MemberType.Name}, got {node.GetType().Name}.");
                continue;
            }

            member.FieldInfo?.SetValue(root, node);
            member.PropertyInfo?.SetValue(root, node);
        }
    }

    private static NodeMember[] BuildCache(Type type)
    {
        var result = new List<NodeMember>();
        var flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        foreach (var field in type.GetFields(flags))
        {
            var attribute = field.GetCustomAttribute<NodeRefAttribute>();

            if (attribute is not null)
                result.Add(new NodeMember { MemberType = field.FieldType, Name = field.Name, FieldInfo = field, Path = attribute.Path });
        }

        foreach (var prop in type.GetProperties(flags))
        {
            var attribute = prop.GetCustomAttribute<NodeRefAttribute>();

            if (attribute is not null)
                result.Add(new NodeMember { MemberType = prop.PropertyType, Name = prop.Name, PropertyInfo = prop, Path = attribute.Path });
        }

        return result.ToArray();
    }

    private static string GetNodePath(NodeMember member)
    {
        return member.FieldInfo?.Name.ToPascalCase() ?? member.PropertyInfo.Name.ToPascalCase();
    }

    private readonly struct NodeMember
    {
        public readonly string Path { get; init; }
        public readonly string Name { get; init; }

        public readonly Type MemberType { get; init; }
        public readonly FieldInfo FieldInfo { get; init; }
        public readonly PropertyInfo PropertyInfo { get; init; }
    }
}