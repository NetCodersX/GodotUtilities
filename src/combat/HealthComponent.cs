using Godot.Collections;
using Godot;
using System;

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

    [Signal] public delegate void DamagedEventHandler(Node2D source, float amount);
    [Signal] public delegate void HealedEventHandler(float amount);

    [Signal] public delegate void DiedEventHandler(Node2D source);
    [Signal] public delegate void RevivedEventHandler();

    [Signal] public delegate void DamagePreventedEventHandler();
    [Signal] public delegate void FullyHealedEventHandler();

    [Export] private bool destroyOnDeath;

    [Export(PropertyHint.Range, "1, 1000")] private float maxHealth = 3f;
    [Export(PropertyHint.Range, "0, 5")] private float invincibilityTime;
    [Export(PropertyHint.Range, "0, 0.95")] private float defense;

    [Export] private Dictionary<DamageType, float> resistances = new();
    [Export] private Array<DamageType> immunity = new();

    public float Health { get; private set; }

    private float minHealth;
    private int cachedEnumLength;

    private Cooldown invincibilityTimer;

    public float MaxHealth => maxHealth;
    public float MinHealth => minHealth;
    public float Percent   => MathUtil.Normalize(Health, maxHealth);

    public override void _Ready() { Health = maxHealth; cachedEnumLength = Enum.GetValues(typeof(DamageType)).Length; }
    public override void _Process(double delta) => invincibilityTimer.Tick(delta);

    public bool TakeDamage(AttackData data)
    {
        if (IsDead()) return false;
        
        if (IsInvincible() || IsImmuneTo(data.DamageType))
        {
            EmitSignalDamagePrevented();
            return false;
        }

        float finalDamage = CalculateDamage(data.DamageType, data.Damage);
        SetHealth(Health - finalDamage);

        if (invincibilityTime > 0f)
            MakeInvincible();

        EmitSignalDamaged(data.Source, finalDamage);

        if (IsDead())
        {
            EmitSignalDied(data.Source);
            
            if (destroyOnDeath) Owner.QueueFree();
        }
        return true;
    }

    private float CalculateDamage(DamageType type, float damage)
    {
        float result = damage;
        result *= 1f - defense; // how much remains

        if (resistances.TryGetValue(type, out float resistance))
            result *= resistance; // how much should be
        return result;
    }

    public void SetResistance(DamageType type, float resistance)
    {
        resistances[type] = resistance;
    }

    public bool RemoveResistance(DamageType type)
    {
        return resistances.Remove(type);
    }

    #region Health Manipulation

    private void SetHealth(float value)
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

    public void Kill() => Kill(null);

    public void Kill(Node2D source)
    {
        if (IsDead()) 
            return;
        SetHealth(minHealth);
        EmitSignal(SignalName.Died, source);

        if (destroyOnDeath) Owner.QueueFree();
    }

    public void Revive() => Revive(maxHealth);

    public void Revive(float amount)
    {
        if (IsAlive()) return;

        invincibilityTimer.Stop();
        SetHealth(Mathf.Max(minHealth + 0.1f, amount));
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

    public bool IsFullyImmune()
    {
        int count = 0;

        for (int value = 0; value < cachedEnumLength; value++)
            if (IsImmuneTo((DamageType)value)) count++;
        
        return count >= (cachedEnumLength - 1);
    }

    #endregion
}

public readonly struct AttackData(Node2D source, float damage, DamageType type, Vector2 knockback)
{
    public float Damage => damage;

    public Node2D Source => source;
    public Vector2 Knockback => knockback;
    public DamageType DamageType => type;
}
