using Godot;
using System;

namespace Utilities;

public static class TweenVirtual
{
    public static Tween Float(Node caller, float from, float to, float duration, Action<float> action)
    {
        Tween tween = caller.CreateTween();
        tween.TweenMethod(Callable.From(action), from, to, duration);
        return tween;
    }

    public static Tween Int(Node caller, int from, int to, float duration, Action<int> action)
    {
        Tween tween = caller.CreateTween();
        tween.TweenMethod(Callable.From(action), from, to, duration);
        return tween;
    }

    public static Tween Vector2(Node caller, Vector2 from, Vector2 to, float duration, Action<Vector2> action)
    {
        Tween tween = caller.CreateTween();
        tween.TweenMethod(Callable.From(action), from, to, duration);
        return tween;
    }

    public static Tween Vector3(Node caller, Vector3 from, Vector3 to, float duration, Action<Vector3> action)
    {
        Tween tween = caller.CreateTween();
        tween.TweenMethod(Callable.From(action), from, to, duration);
        return tween;
    }

    public static Tween Color(Node caller, Color from, Color to, float duration, Action<Color> action)
    {
        Tween tween = caller.CreateTween();
        tween.TweenMethod(Callable.From(action), from, to, duration);
        return tween;
    }
}
