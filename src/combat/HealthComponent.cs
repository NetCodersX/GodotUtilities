using Godot;
using System;
using Godot.Collections;

namespace Utilities.Combat;

public enum DamageType
{
    Physical,
    Ranged,
    Magic,
    Disease,
    Emotional,
}

[GlobalClass]
public partial class HealthComponent : Node
{
    [Signal] public delegate void HealthChangedEventHandler(float old, float current);
    [Signal] public delegate void MaxHealthChangedEventHandler(float old, float current);

    [Signal] public delegate void DamagedEventHandler(AttackContext context);
    [Signal] public delegate void HealedEventHandler(float amount);

    [Signal] public delegate void DiedEventHandler(AttackContext context);
    [Signal] public delegate void RevivedEventHandler();

    [Signal] public delegate void DamagePreventedEventHandler();
    [Signal] public delegate void FullyHealedEventHandler();

    [Export] private bool destroyOnDeath;

    [Export(PropertyHint.Range, "1, 1000")] private float maxHealth = 3f;
    [Export(PropertyHint.Range, "0, 5")] private float invincibilityTime;

    [Export] private Dictionary<DamageType, float> resistances = new();

    private static int enumLength;

    private Array<DamageType> immunityInternal = new();
    private readonly System.Collections.Generic.HashSet<DamageType> immunity = new();

    public float Health { get; private set; }

    private float minHealth;
    
    private Cooldown invincibilityTimer;

    public float MaxHealth => maxHealth;
    public float MinHealth => minHealth;
    public float Percent => MathUtil.Normalize(Health, maxHealth);

    static HealthComponent()
    {
        enumLength = Enum.GetValues(typeof(DamageType)).Length;
    }

    public override void _Ready()
    {
        Health = maxHealth; 
    }

    public override void _Process(double delta) => 
        invincibilityTimer.Tick(delta);

    public bool TakeDamage(AttackContext data)
    {
        if (IsDead()) return false;
        
        if (IsInvincible() || IsImmuneTo(data.DamageType))
        {
            EmitSignalDamagePrevented();
            return false;
        }

        data.EffectiveDamage = ComputeEffectiveDamage(data.DamageType, data.RawDamage);
        SetHealth(Health - data.EffectiveDamage);

        if (invincibilityTime > 0f)
            MakeInvincible();
        EmitSignalDamaged(data);

        if (IsDead())
        {
            EmitSignalDied(data);
            if (destroyOnDeath) Owner.QueueFree();
        }
        return true;
    }

    private float ComputeEffectiveDamage(DamageType type, float damage)
    {
        if (resistances.TryGetValue(type, out float resistance))
            damage *= resistance;
        return damage;
    }

    #region Health Manipulation

    public void SetHealth(float value)
    {
        float oldHealth = Health;
        Health = Mathf.Clamp(value, minHealth, maxHealth);

        if (oldHealth != Health)
        {
            EmitSignal(SignalName.HealthChanged, oldHealth, Health);
            
            if (Health == maxHealth)
                EmitSignalFullyHealed();
        }
    }

    public void SetMaxHealth(float value, bool healToMax = false)
    {
        float oldValue = maxHealth;
        maxHealth = Mathf.Max(minHealth + 0.01f, value);

        if (oldValue != maxHealth) EmitSignal(SignalName.MaxHealthChanged, oldValue, maxHealth);
        if (healToMax) SetHealth(maxHealth);
    }

    public void SetMinHealth(float value)
    {
        minHealth = Mathf.Min(value, maxHealth);
    }

    public void Heal(float amount)
    {
        if (amount <= 0f)
        {
            GD.PushWarning($"[HealthComponent]: Heal amount should be a positive value");
            return;
        }

        float oldValue = Health;
        SetHealth(Health + amount);
        EmitSignal(SignalName.Healed, Health - oldValue);
    }

    public void Kill(AttackContext context)
    {
        if (IsDead()) 
            return;
        SetHealth(minHealth);
        EmitSignalDied(context);

        if (destroyOnDeath) Owner.QueueFree();
    }

    public void Revive() => Revive(maxHealth);

    public void Revive(float amount)
    {
        if (IsAlive()) return;

        invincibilityTimer.Stop();
        SetHealth(Mathf.Max(minHealth + 1f, amount));
        EmitSignalRevived();
    }

    #endregion

    #region Utilities

    public bool IsAlive() => Health > minHealth;
    public bool IsDead() => Health <= minHealth;
    public bool IsInvincible() => !invincibilityTimer.IsReady;

    public void MakeInvincible() => invincibilityTimer.Start(invincibilityTime);
    public void MakeInvincible(float duration) => invincibilityTimer.Start(duration);

    public void AddImmunity(DamageType type) => immunity.Add(type);
    public void RemoveImmunity(DamageType type) => immunity.Remove(type);

    public bool IsImmuneTo(DamageType type) => immunity.Contains(type);
    public bool IsFullyImmune() => immunity.Count == enumLength;

    public void SetResistance(DamageType type, float resistance) => resistances[type] = resistance;
    public bool RemoveResistance(DamageType type) => resistances.Remove(type);

    public void AddResistance(DamageType type, float amount01)
    {
        if (amount01 <= 0 || amount01 > 1)
        {
            Log.Warn(nameof(HealthComponent), $"Invalid amount -> 0.0 < {amount01} <= 1.0 ");
            return;
        }

        resistances[type] += amount01;
    }

    #endregion
}

public partial class AttackContext(Node2D source, float damage, DamageType type, 
    Vector2 knockback, float xpMultiplier = 1f) : RefCounted
{
    public Node2D Source         { get; init; } = source;

    public float EffectiveDamage { get; set;  } = damage;
    public float RawDamage       { get; init; } = damage;
    public float XPMultiplier    { get; init; } = xpMultiplier;

    public DamageType DamageType { get; init; } = type;
    public Vector2 Knockback     { get; init; } = knockback;
}
