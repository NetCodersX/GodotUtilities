using System;
using Godot;

namespace Utilities;

public static class TweenExtensions
{
    private const string PROPERTY_POSITION         = "position";
    private const string PROPERTY_GLOBAL_POSITION  = "global_position";
    private const string PROPERTY_SCALE            = "scale";
    private const string PROPERTY_ROTATION_DEGREES = "rotation_degrees";
    private const string PROPERTY_ROTATION         = "rotation";
    private const string PROPERTY_MODULATE_ALPHA   = "modulate:a";
    private const string PROPERTY_MODULATE         = "modulate";
    private const string PROPERTY_SELF_MODULATE    = "self_modulate";
    private const string PROPERTY_COLOR            = "color";
    private const string PROPERTY_VISIBLE_RATIO    = "visible_ratio";
    private const string PROPERTY_PIVOT_OFFSET     = "pivot_offset";
    private const string PROPERTY_VOLUME           = "volume_db";
    private const string PROPERTY_VOLUME_LINEAR    = "volume_linear";
    private const string PROPERTY_PROGRESS_RATIO   = "progress_ratio";
    private const string PROPERTY_SKEW             = "skew";
    private const string PROPERTY_OFFSET           = "offset";

    #region UI

    public static PropertyTweener TweenPivot(this Tween tween, Control control, Vector2 value, float duration) =>
        tween.TweenProperty(control, PROPERTY_PIVOT_OFFSET, value, duration);
    
    public static Tween TweenSlideIn(this Tween tween, Control target, Vector2 direction, float duration)
    {
        Vector2 start = target.Size * direction;
        Vector2 end   = target.Position;

        tween.TweenProperty(target, PROPERTY_POSITION,      end, duration).From(start);
        tween.TweenProperty(target, PROPERTY_MODULATE_ALPHA, 1f, duration).From(0f);
        return tween;
    }

    public static Tween TweenSlideOut(this Tween tween, Control target, Vector2 direction, float duration)
    {
        Vector2 end   = target.Size * direction;
        Vector2 start = target.Position;

        tween.TweenProperty(target, PROPERTY_POSITION,      end, duration).From(start);
        tween.TweenProperty(target, PROPERTY_MODULATE_ALPHA, 0f, duration).From(1f);
        return tween;
    }

    public static PropertyTweener TweenPopIn(this Tween tween, Control target, float duration) =>
        tween.TweenProperty(target, PROPERTY_SCALE, Vector2.One, duration).From(Vector2.Zero).Back().EaseOut();

    public static PropertyTweener TweenPopOut(this Tween tween, Control target, float duration) =>
        tween.TweenProperty(target, PROPERTY_SCALE, Vector2.Zero, duration).From(Vector2.One).Back().EaseIn();

    #endregion

    #region Follow Path

    public static PropertyTweener TweenFollowPath(this Tween tween, PathFollow2D follower, float duration) =>
        tween.TweenProperty(follower, PROPERTY_PROGRESS_RATIO, 1f, duration);

    #endregion

    #region Move

    public static PropertyTweener TweenPosition(this Tween tween, GodotObject target, Vector2 to, float duration) =>
        tween.TweenProperty(target, PROPERTY_POSITION, to, duration);

    public static PropertyTweener TweenGlobalPosition(this Tween tween, GodotObject target, Vector2 to, float duration) =>
        tween.TweenProperty(target, PROPERTY_GLOBAL_POSITION, to, duration);

    #endregion

    #region Rotation

    public static PropertyTweener TweenRotationDeg(this Tween tween, GodotObject target, float degrees, float duration) =>
        tween.TweenProperty(target, PROPERTY_ROTATION_DEGREES, degrees, duration);

    public static PropertyTweener TweenRotationRad(this Tween tween, GodotObject target, float rad, float duration) =>
        tween.TweenProperty(target, PROPERTY_ROTATION, rad, duration);

    #endregion

    #region Skew

    public static PropertyTweener TweenSkewRad(this Tween tween, Node2D target, float rad, float duration) =>
        tween.TweenProperty(target, PROPERTY_SKEW, rad, duration);

    public static PropertyTweener TweenSkewDeg(this Tween tween, Node2D target, float degrees, float duration) =>
        tween.TweenProperty(target, PROPERTY_SKEW, Mathf.DegToRad(degrees), duration);

    #endregion

    #region Scale

    public static PropertyTweener TweenScale(this Tween tween, GodotObject target, Vector2 scale, float duration) =>
        tween.TweenProperty(target, PROPERTY_SCALE, scale, duration);

    public static PropertyTweener TweenScaleUniform(this Tween tween, GodotObject target, float value, float duration) =>
        tween.TweenProperty(target, PROPERTY_SCALE, Vector2.One * value, duration);

    #endregion

    #region Offset

    public static PropertyTweener TweenOffset(this Tween tween, GodotObject target, Vector2 value, float duration) =>
        tween.TweenProperty(target, PROPERTY_OFFSET, value, duration);
    
    #endregion

    #region Squish

    public enum SquishDir { Up, Down }

    public static Tween TweenSquish(this Tween tween, GodotObject target, float duration, float ratio = 0.2f, SquishDir dir = SquishDir.Up)
    {
        float stepDuration = duration / 3f;

        var up   = new Vector2(1f - ratio, 1f + ratio);
        var down = new Vector2(1f + ratio, 1f - ratio);

        tween.TweenProperty(target, PROPERTY_SCALE, dir == SquishDir.Up   ? up   : down, stepDuration).Sine().EaseIn();
        tween.TweenProperty(target, PROPERTY_SCALE, dir == SquishDir.Down ? down : up,   stepDuration).Sine().EaseIn();
        tween.TweenProperty(target, PROPERTY_SCALE, Vector2.One,                         stepDuration).Sine().EaseOut();

        return tween;
    }

    #endregion

    #region Look At

    public static MethodTweener TweenLookAtFollow(this Tween tween, Node2D target, Func<Vector2> getter, float duration, float smoothness = 0)
    {
        void interpolate(float t)
        {
            Vector2 point = getter();
            float angle = target.GlobalPosition.DirectionTo(point).Angle();

            if (smoothness > 0)
                target.Rotation = Mathf.Lerp(target.Rotation, angle, smoothness);
            else
                target.Rotation = angle;
        }

        return tween.TweenMethod(Callable.From<float>(interpolate), 0f, 1f, duration);
    }

    #endregion

    #region Fade

    public static PropertyTweener TweenFade(this Tween tween, CanvasItem target, float value, float duration) =>
        tween.TweenProperty(target, PROPERTY_MODULATE_ALPHA, value, duration);

    public static PropertyTweener TweenFadeIn(this Tween tween, CanvasItem target, float duration) =>
        tween.TweenFade(target, 1f, duration);

    public static PropertyTweener TweenFadeOut(this Tween tween, CanvasItem target, float duration) =>
        tween.TweenFade(target, 0f, duration);

    #endregion

    #region Color

    public static PropertyTweener TweenModulate(this Tween tween, CanvasItem target, Color color, float duration) =>
        tween.TweenProperty(target, PROPERTY_MODULATE, color, duration);

    public static PropertyTweener TweenSelfModulate(this Tween tween, CanvasItem target, Color color, float duration) =>
        tween.TweenProperty(target, PROPERTY_SELF_MODULATE, color, duration);

    public static PropertyTweener TweenColor(this Tween tween, GodotObject target, Color color, float duration) =>
        tween.TweenProperty(target, PROPERTY_COLOR, color, duration);

    #endregion

    #region Shake

    public static Tween TweenShakePosition(this Tween tween, Node2D target, float duration, float strength = 10f)
    {
        var shaker = ShakePositionTweener.Get();
        shaker.Setup(target, strength);
        tween.TweenMethod(Callable.From<float>(shaker.Tick), 0f, 1f, duration);
        return tween;
    }

    public static Tween TweenShakeRotation(this Tween tween, Node2D target, float duration, float strength = 0.5f)
    {
        var shaker = ShakeRotationTweener.Get();
        shaker.Setup(target, strength);
        tween.TweenMethod(Callable.From<float>(shaker.Tick), 0f, 1f, duration);
        return tween;
    }
    
    public static Tween TweenShakePosition(this Tween tween, Control target, float duration, float strength = 10f)
    {
        var shaker = ShakePositionTweener.Get();
        shaker.Setup(target, strength);
        tween.TweenMethod(Callable.From<float>(shaker.Tick), 0f, 1f, duration);
        return tween;
    }

    public static Tween TweenShakeRotation(this Tween tween, Control target, float duration, float strength = 0.5f)
    {
        var shaker = ShakeRotationTweener.Get();
        shaker.Setup(target, strength);
        tween.TweenMethod(Callable.From<float>(shaker.Tick), 0f, 1f, duration);
        return tween;
    }

    #endregion

    #region Punch

    public static Tween TweenPunchPosition(this Tween tween, Node2D target, float duration, Vector2 amount)
    {
        var punch = PunchPositionTweener.Get();
        punch.Setup(target, amount);
        tween.TweenMethod(Callable.From<float>(punch.Tick), 0f, 1f, duration);
        return tween;
    }

    public static Tween TweenPunchScale(this Tween tween, Node2D target, float duration, Vector2 amount)
    {
        var punch = PunchScaleTweener.Get();
        punch.Setup(target, amount);
        tween.TweenMethod(Callable.From<float>(punch.Tick), 0f, 1f, duration);
        return tween;
    }

    public static Tween TweenPunchRotation(this Tween tween, Node2D target, float duration, float amount)
    {
        var punch = PunchRotationTweener.Get();
        punch.Setup(target, amount);
        tween.TweenMethod(Callable.From<float>(punch.Tick), 0f, 1f, duration);
        return tween;
    }

    public static Tween TweenPunchPosition(this Tween tween, Control target, float duration, Vector2 amount)
    {
        var punch = PunchPositionTweener.Get();
        punch.Setup(target, amount);
        tween.TweenMethod(Callable.From<float>(punch.Tick), 0f, 1f, duration);
        return tween;
    }

    public static Tween TweenPunchScale(this Tween tween, Control target, float duration, Vector2 amount)
    {
        var punch = PunchScaleTweener.Get();
        punch.Setup(target, amount);
        tween.TweenMethod(Callable.From<float>(punch.Tick), 0f, 1f, duration);
        return tween;
    }

    public static Tween TweenPunchRotation(this Tween tween, Control target, float duration, float amount)
    {
        var punch = PunchRotationTweener.Get();
        punch.Setup(target, amount);
        tween.TweenMethod(Callable.From<float>(punch.Tick), 0f, 1f, duration);
        return tween;
    }

    #endregion

    #region Wiggle

    public static Tween TweenWiggle(this Tween tween, GodotObject target, float degrees, float duration)
    {
        float stepDuration = duration / 3f;

        tween.TweenProperty(target, PROPERTY_ROTATION_DEGREES,  degrees, stepDuration).Sine().EaseIn();
        tween.TweenProperty(target, PROPERTY_ROTATION_DEGREES, -degrees, stepDuration).Sine().EaseIn();
        tween.TweenProperty(target, PROPERTY_ROTATION_DEGREES,    0f,    stepDuration).Sine().EaseOut();

        return tween;
    }

    #endregion

    #region Blink

    public static Tween TweenBlink(this Tween tween, CanvasItem item, int blinks, float duration = 0.1f, bool endVisible = true)
    {
        item.Modulate = item.Modulate with { A = 1f };

        for (int i = 0; i < blinks; i++)
        {
            tween.TweenProperty(item, PROPERTY_MODULATE_ALPHA, 0f, duration).EaseInOut();
            tween.TweenProperty(item, PROPERTY_MODULATE_ALPHA, 1f, duration).EaseInOut();
        }

        if (!endVisible)
            tween.TweenProperty(item, PROPERTY_MODULATE_ALPHA, 0f, 0f);

        return tween;
    }

    #endregion

    #region Typewriter

    public static PropertyTweener TweenTypewriter(this Tween tween, Label label, float duration) =>
        tween.TweenProperty(label, PROPERTY_VISIBLE_RATIO, 1f, duration).From(0f);

    #endregion

    #region Counter

    public static MethodTweener TweenCounter(this Tween tween, Label label, float from, float to, float duration) =>
        tween.TweenMethod(Callable.From<float>(value => label.Text = Mathf.RoundToInt(value).ToString()), from, to, duration);

    #endregion

    #region Shader

    public static PropertyTweener TweenShader(this Tween tween, ShaderMaterial material, string paramName, Variant value, float duration) =>
        tween.TweenProperty(material, $"shader_parameter/{paramName}", value, duration);

    #endregion

    #region Audio

    public static PropertyTweener TweenVolume(this Tween tween, AudioStreamPlayer player, float db, float duration) =>
        tween.TweenProperty(player, PROPERTY_VOLUME, db, duration);

    public static PropertyTweener TweenVolumeLinear(this Tween tween, AudioStreamPlayer player, float linear, float duration) =>
        tween.TweenProperty(player, PROPERTY_VOLUME_LINEAR, linear, duration);

    #endregion

    #region Awaiters

    // Await
    public static SignalAwaiter WaitToFinish(this Tween tween) => tween.ToSignal(tween, Tween.SignalName.Finished);
    public static SignalAwaiter WaitToFinish(this Tweener tween) => tween.ToSignal(tween, Tweener.SignalName.Finished);

    #endregion

    #region Signals

    public static void OnFinished(this Tween tween, Callable callable) =>
        tween.Connect(Tween.SignalName.Finished, callable, (uint)GodotObject.ConnectFlags.OneShot);

    public static void OnFinished(this Tween tween, Action action) =>
        tween.Connect(Tween.SignalName.Finished, Callable.From(action), (uint)GodotObject.ConnectFlags.OneShot);

    #endregion

    #region Other

    public static void KillIfValid(this Tween tween)
    {
        if (GodotObject.IsInstanceValid(tween) && tween.IsValid()) tween.Kill();
    }

    #endregion

    #region Transitions & Ease

    public static Tween Linear(this Tween tween)    => tween.SetTrans(Tween.TransitionType.Linear);
    public static Tween Sine(this Tween tween)      => tween.SetTrans(Tween.TransitionType.Sine);
    public static Tween Back(this Tween tween)      => tween.SetTrans(Tween.TransitionType.Back);
    public static Tween Bounce(this Tween tween)    => tween.SetTrans(Tween.TransitionType.Bounce);
    public static Tween Circ(this Tween tween)      => tween.SetTrans(Tween.TransitionType.Circ);
    public static Tween Spring(this Tween tween)    => tween.SetTrans(Tween.TransitionType.Spring);
    public static Tween Quad(this Tween tween)      => tween.SetTrans(Tween.TransitionType.Quad);
    public static Tween Quart(this Tween tween)     => tween.SetTrans(Tween.TransitionType.Quart);
    public static Tween Expo(this Tween tween)      => tween.SetTrans(Tween.TransitionType.Expo);
    public static Tween Quint(this Tween tween)     => tween.SetTrans(Tween.TransitionType.Quint);
    public static Tween Elastic(this Tween tween)   => tween.SetTrans(Tween.TransitionType.Elastic);
    public static Tween Cubic(this Tween tween)     => tween.SetTrans(Tween.TransitionType.Cubic);

    public static Tween EaseIn(this Tween tween)    => tween.SetEase(Tween.EaseType.In);
    public static Tween EaseOut(this Tween tween)   => tween.SetEase(Tween.EaseType.Out);
    public static Tween EaseOutIn(this Tween tween) => tween.SetEase(Tween.EaseType.OutIn);
    public static Tween EaseInOut(this Tween tween) => tween.SetEase(Tween.EaseType.InOut);

    public static PropertyTweener Linear(this PropertyTweener t)    => t.SetTrans(Tween.TransitionType.Linear);
    public static PropertyTweener Sine(this PropertyTweener t)      => t.SetTrans(Tween.TransitionType.Sine);
    public static PropertyTweener Back(this PropertyTweener t)      => t.SetTrans(Tween.TransitionType.Back);
    public static PropertyTweener Bounce(this PropertyTweener t)    => t.SetTrans(Tween.TransitionType.Bounce);
    public static PropertyTweener Circ(this PropertyTweener t)      => t.SetTrans(Tween.TransitionType.Circ);
    public static PropertyTweener Spring(this PropertyTweener t)    => t.SetTrans(Tween.TransitionType.Spring);
    public static PropertyTweener Quad(this PropertyTweener t)      => t.SetTrans(Tween.TransitionType.Quad);
    public static PropertyTweener Quart(this PropertyTweener t)     => t.SetTrans(Tween.TransitionType.Quart);
    public static PropertyTweener Expo(this PropertyTweener t)      => t.SetTrans(Tween.TransitionType.Expo);
    public static PropertyTweener Quint(this PropertyTweener t)     => t.SetTrans(Tween.TransitionType.Quint);
    public static PropertyTweener Elastic(this PropertyTweener t)   => t.SetTrans(Tween.TransitionType.Elastic);
    public static PropertyTweener Cubic(this PropertyTweener t)     => t.SetTrans(Tween.TransitionType.Cubic);

    public static PropertyTweener EaseIn(this PropertyTweener t)    => t.SetEase(Tween.EaseType.In);
    public static PropertyTweener EaseOut(this PropertyTweener t)   => t.SetEase(Tween.EaseType.Out);
    public static PropertyTweener EaseOutIn(this PropertyTweener t) => t.SetEase(Tween.EaseType.OutIn);
    public static PropertyTweener EaseInOut(this PropertyTweener t) => t.SetEase(Tween.EaseType.InOut);

    #endregion
}

