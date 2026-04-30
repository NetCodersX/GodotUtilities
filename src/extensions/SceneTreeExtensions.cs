using System;
using Godot;

namespace Utilities;

public static class SceneTreeExtensions
{
    public static async void Delay(this SceneTree tree, double seconds, Action callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        await tree.ToSignal(tree.CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);
        callback();
    }

    public static async void NextFrame(this SceneTree tree, Action callback)
    {
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        callback?.Invoke();
    }

    public static SignalAwaiter Delay(this SceneTree tree, double seconds)
    {
        return tree.ToSignal(tree.CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);
    }

    public static SignalAwaiter NextFrame(this SceneTree tree)
    {
        return tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }
}

