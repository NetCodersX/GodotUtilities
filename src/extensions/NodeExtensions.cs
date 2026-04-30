using Godot;

namespace Utilities;

public static class NodeExtensions
{
    #region Child Search

    public static T GetFirstChildOfType<T>(this Node node, bool recursive = false) where T : Node
    {
        foreach (var child in node.GetChildren())
        {
            if (child is T t)
                return t;
            
            if (!recursive) continue;

            var result = child.GetFirstChildOfType<T>(recursive: true);
            
            if (result != null)
                return result;
        }

        return null;
    }

    public static bool TryGetFirstChildOfType<T>(this Node node, out T child) where T : Node
    {
        child = node.GetFirstChildOfType<T>();
        return child != null;
    }

    public static bool TryGetFirstChildOfType<T>(this Node node, bool recursive, out T child) where T : Node
    {
        child = node.GetFirstChildOfType<T>(recursive);
        return child != null;
    }

    #endregion

    public static void DestroyChildren(this Node node)
    {
        foreach (var child in node.GetChildren())
            child.QueueFree();
    }

    public static void AddChildDeferred(this Node node, Node child)
    {
        node.CallDeferred(Node.MethodName.AddChild, child);
    }

}
