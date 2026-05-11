using System.Collections.Generic;
using Utilities.Pooling;
using Godot;

namespace Utilities.ParticlesManagement;

public partial class ParticlesManager : Node
{
    public static ParticlesManager Instance { get; private set; }

    private readonly Dictionary<StringName, NodePool<PooledParticle>> pools = new();

    private Timer trimTimer;

    public override void _EnterTree()
    {
        Instance = this;
        
        GetTree().SceneChanged += () =>
        {
            foreach (var (_, pool) in pools)
                pool.Destroy();
        };
    }

    public override void _Ready() => SetupTrimTimer();

    private void SetupTrimTimer()
    {
        trimTimer = new Timer { Autostart = true, OneShot = false, WaitTime = 5f };
        AddChild(trimTimer);

        trimTimer.Timeout += () =>
        {
            foreach (var (_, pool) in pools)
                pool.Trim(1, DestroyParticle);  
        };
    }

    #region Spawn

    public ParticleHandle Spawn(StringName id, Vector2 pos, Node uniqueParent = null)
    {
        var pool = GetOrCreatePool(id);
        if (pool is null) return new();

        var pooled = pool.Get();
        pooled.Emit(pos, uniqueParent);
        return new ParticleHandle(pooled);
    }

    #endregion

    #region Lazy Load

    private NodePool<PooledParticle> GetOrCreatePool(StringName id)
    {
        if (pools.TryGetValue(id, out var existing))
            return existing;

        if (!ParticlePaths.TryGetValue(id, out string path))
        {
            GD.PushError($"ParticlesManager: unknown particle id '{id}'");
            return null;
        }

        var scene = ResourceLoader.Load<PackedScene>(path);
        if (scene is null)
        {
            GD.PushError($"ParticlesManager: failed to load scene at path '{path}' for id '{id}'");
            return null;
        }

        NodePool<PooledParticle> pool = null;
        pool = new(scene, GetTree().CurrentScene, onInstantiate: node => node.Init(pool));
        pools[id] = pool;
        return pool;
    }

    #endregion

    #region Unload API

    public void Unload(StringName id)
    {
        if (pools.TryGetValue(id, out var pool))
        {
            pool.Destroy();
            pools.Remove(id);
        }
    }

    public void UnloadAll()
    {
        foreach (var (_, pool) in pools)
            pool.Destroy();

        pools.Clear();
    }

    private void DestroyParticle(PooledParticle particle) => particle.QueueFree();

    #endregion

    #region Warm Up

    public void WarmUp(params StringName[] ids)
    {
        foreach (var id in ids) GetOrCreatePool(id);
    }

    #endregion
}

public struct ParticleHandle(PooledParticle particle)
{
    private readonly bool Valid => particle != null && particle.Emitting;

    public readonly void SetPosition(Vector2 pos) { if (Valid) particle.GlobalPosition = pos; }
    public readonly void SetDirection(Vector2 direction) { if (Valid) particle.SetDirection(direction); }
    public readonly bool IsInGroup(StringName group) { if (Valid) return particle.IsInGroup(group); return false; }
}


