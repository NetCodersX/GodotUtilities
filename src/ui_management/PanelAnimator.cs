using Godot;

namespace Utilities.UI;

public enum PanelTransition
{
    Constant,
    Fade,
    Pop,
    Slide,
}

public static class PanelAnimator
{
    private const float FADE_DURATION  = 0.1f;
    private const float POP_DURATION   = 0.15f;
    
    private const float SLIDE_DURATION = 0.2f;
    private const float SLIDE_AMOUNT   = 10f;

    public static Tween Animate(UIPanel panel, PanelTransition transition, bool isOpen)
    {
        Tween tween = panel.CreateTween();

        switch (transition)
        {
            case PanelTransition.Fade:     OnFade(panel, tween, isOpen);  break;
            case PanelTransition.Pop:      OnPop(panel, tween, isOpen);   break;
            case PanelTransition.Slide:    OnSlide(panel, tween, isOpen); break;
            case PanelTransition.Constant: tween.TweenCallback(Callable.From(DummyMethod)); break;
        }

        return tween;
    }

    private static void OnFade(UIPanel panel, Tween tween, bool isOpen)
    {
        tween.TweenFade(panel, isOpen.ToSingle(), FADE_DURATION).From((!isOpen).ToSingle());
    }

    private static void OnPop(UIPanel panel, Tween tween, bool isOpen)
    {
        panel.PivotOffset = panel.Size / 2f;

        if (isOpen) tween.TweenPopIn(panel, POP_DURATION);
        else tween.TweenPopOut(panel, POP_DURATION);
    }

    private static void OnSlide(UIPanel panel, Tween tween, bool isOpen)
    {
        panel.DefaultPos ??= panel.Position;
            
        Vector2 startPos = isOpen
            ? panel.DefaultPos.Value + Vector2.Down * SLIDE_AMOUNT
            : panel.DefaultPos.Value;

        Vector2 endPos = isOpen
            ? panel.DefaultPos.Value
            : panel.DefaultPos.Value + Vector2.Down * SLIDE_AMOUNT;

        tween.SetParallel();
        tween.TweenFade(panel, isOpen.ToSingle(), SLIDE_DURATION).From((!isOpen).ToSingle());
        tween.TweenPosition(panel, endPos, SLIDE_DURATION).From(startPos);
    }

    private static void DummyMethod() { }
}