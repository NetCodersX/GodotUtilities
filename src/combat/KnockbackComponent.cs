using System;
using Godot;

namespace Utilities.Combat;

[GlobalClass]
public partial class KnockbackComponent : Node
{
    [Signal] public delegate void KnockbackStartedEventHandler();
    [Signal] public delegate void KnockbackFinishedEventHandler();

    [Export] private float duration = 0.25f;
    [Export] private float weightFactor = 0.1f;

    private CharacterBody2D controller;
    private Func<float> controllerWeight;

    private double timer;

    public override void _Ready()
    {
        controller = GetOwner<CharacterBody2D>();
    }

    public override void _Process(double dt)
    {
        if (timer <= 0f) return;
        
        timer -= dt;

        if (controllerWeight != null)
            controller.Velocity = MathUtil.DeltaLerp(controller.Velocity, Vector2.Zero, (float)dt, weightFactor * controllerWeight());

        if (timer <= 0f)
            EmitSignalKnockbackFinished();
    }

    public void Apply(Vector2 force) => Apply(force, duration);

    public void Apply(Vector2 force, float duration)
    {
        controller.Velocity += force;
        EmitSignalKnockbackStarted();

        timer = duration;
    }

    public void SetAssignableWeight(Func<float> factory)
    {
        if (controllerWeight is not null)
        {
            GD.PushWarning("[KnockbackComponent]: Controller Weight Getter is assigned already");
            return;
        }

        controllerWeight = factory;
    }
}

