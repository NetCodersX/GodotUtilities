using System;
using Godot;

namespace Utilities.Combat;

[GlobalClass]
public partial class HurtboxComponent : Area2D
{
    [Export] private HealthComponent healthComponent;
    [Export] private KnockbackComponent knockbackComponent;

    private CollisionShape2D collision;

    public bool Enabled { get; private set; }

    public override void _Ready()
    {   
        if (healthComponent is null)
            throw new Exception("[HurtboxComponent] health component is not assigned in the inspector");

        collision = GetNodeOrNull<CollisionShape2D>(nameof(CollisionShape2D));
    }

    public void ReceiveDamage(AttackData data)
    {
        if (healthComponent.TakeDamage(data))
            knockbackComponent?.Apply(data.Knockback);
    }

    public void Enable()
    {
        collision.SetDeferred(CollisionShape2D.PropertyName.Disabled, false);
        Enabled = true;
    }  

    public void Disable()
    {
        collision.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
        Enabled = false;
    }  
}
