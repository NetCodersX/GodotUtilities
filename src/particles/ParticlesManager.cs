using System.Collections.Generic;
using Utilities.Pooling;
using Godot;

namespace Utilities.ParticlesManagement;

public partial class ParticlesManager : Node
{
    private const string SETTING_NAME = "godot_utilities/particles_registry_path";

    public static ParticlesManager Instance { get; private set; }

    private ParticlesRegistry registry;

    private readonly Dictionary<StringName, NodePool<PooledParticle>> pools = new();

    public override void _Ready()
    {
        Instance = this; 

        var path = ProjectSettings.GetSetting(SETTING_NAME).AsString();
        registry = ResourceLoader.Load<ParticlesRegistry>(path);

        if (registry is null)
        {
            GD.PrintErr("ParticlesManager: ParticlesRegistry failed to load.");
            return;
        }

        foreach (var kvp in registry.scenes)
            pools[kvp.Key] = BuildPool(kvp.Value, 5);
    }

    public PooledParticle Spawn(StringName id, Vector2 pos)
    {
        if (!pools.TryGetValue(id, out var pool))
        {
            GD.PushWarning($"ParticlesManager: no pool registered for '{id}'");
            return null;
        }

        var pooled = pool.Get();
        if (pooled != null) pooled.GlobalPosition = pos;

        return pooled;
    }

    private NodePool<PooledParticle> BuildPool(PackedScene scene, int size)
    {
        NodePool<PooledParticle> pool = null;
        pool = new(scene, GetTree().CurrentScene, size, onInstantiate: node => node.Init(pool));
        return pool;
    }


}
