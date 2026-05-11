using System.Linq;
using Godot;

namespace Utilities;

public static class NodeExtensions
{
    public static bool TryGetChild<T>(this Node node, out T result) where T : Node
    {
        foreach (var child in node.GetChildren())
        {
            if (child is T t)
            {
                result = t;
                return true;
            }
        }

        result = null;
        return false;
    }

    public static T GetComponent<T>(this Node node) where T : Node
    {
        return node.GetChildren().OfType<T>().FirstOrDefault();
    }

    public static T RequireComponent<T>(this Node node) where T : Node
    {
        var component = GetComponent<T>(node) 
            ?? throw new System.Exception($"Missing required component: {typeof(T).Name}");
        return component;
    }

    public static void DestroyChildren(this Node node)
    {
        foreach (var child in node.GetChildren())
            child.QueueFree();
    }
}
