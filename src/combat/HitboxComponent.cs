using Godot;

namespace Utilities.Combat;

[GlobalClass]
public partial class HitboxComponent : Area2D
{
    [Signal] public delegate void AppliedDamageEventHandler(HurtboxComponent hurtbox);

    [Export] public DamageType damageType = DamageType.Physical;

    [Export(PropertyHint.Range, "1, 1000")] public float damage = 1f;
    [Export(PropertyHint.Range, "0, 1000")] public float knockbackForce;
    
    [Export(PropertyHint.Range, "1, 100")] public float XPMultiplier { get; set; } = 1f;

    private Node2D customDamageSource;
    private CollisionShape2D collision;

    public bool Enabled { get; private set; }

    public override void _Ready()
    { 
        AreaEntered += OnAreaEntered;
        collision = GetNodeOrNull<CollisionShape2D>(nameof(CollisionShape2D));
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

    public void SetCustomDamageSource(Node2D source) =>
        customDamageSource = source;

    private void OnAreaEntered(Area2D area)
    {
        if (area is not HurtboxComponent hurtbox) return;

        Vector2 direction = GlobalPosition.DirectionTo(hurtbox.GlobalPosition);
        AttackContext data = new(
            customDamageSource ?? this, 
            damage, 
            damageType,
            knockbackForce * direction,
            XPMultiplier
        );

        hurtbox.ReceiveDamage(data);
        EmitSignalAppliedDamage(hurtbox);
    }

}
