using Godot;

namespace Utilities;

public static class GodotObjectExtensions
{
    public static void ConnectOnce(this GodotObject obj, StringName signal, Callable callable)
    {
        obj.Connect(signal, callable, (uint)GodotObject.ConnectFlags.OneShot);
    }
}