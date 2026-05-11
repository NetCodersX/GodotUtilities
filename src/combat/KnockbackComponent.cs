using System;
using Godot;

namespace Utilities.Combat;

[GlobalClass]
public partial class KnockbackComponent : Node
{
    public enum KnockbackType { Additional, Constant }

    [Signal] public delegate void KnockbackStartedEventHandler();
    [Signal] public delegate void KnockbackFinishedEventHandler();

    [Export] private KnockbackType type = KnockbackType.Additional;
    [Export] private float duration = 0.25f;
    [Export] private float weightFactor = 0.1f;

    private CharacterBody2D controller;
    private Func<float> controllerWeight;

    private float timer;

    public override void _Ready()
    {
        controller = GetOwner<CharacterBody2D>();
    }

    public override void _Process(double delta)
    {
        if (timer <= 0f) return;
        
        float dt = (float)delta;
        timer -= dt;

        if (controllerWeight != null)
            controller.Velocity = MathUtil.DeltaLerp(controller.Velocity, Vector2.Zero, dt, weightFactor * controllerWeight());

        if (timer <= 0f)
            EmitSignalKnockbackFinished();
    }

    public void Knockback(Vector2 force) => Knockback(force, duration);

    public void Knockback(Vector2 force, float duration)
    {
        if (type == KnockbackType.Additional) 
            controller.Velocity += force;
        else controller.Velocity = force;

        EmitSignalKnockbackStarted();
        timer = duration;
    }

    public void SetAssignableWeight(Func<float> factory)
    {
        if (controllerWeight is not null)
        {
            Log.Error("Combat", "Controller Weight Getter is assigned already");
            return;
        }

        controllerWeight = factory;
    }
}

