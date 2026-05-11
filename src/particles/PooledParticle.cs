using Utilities.Pooling;
using Godot;

namespace Utilities.ParticlesManagement;

[GlobalClass]
public partial class PooledParticle : GpuParticles2D
{
    private NodePool<PooledParticle> pool;

    public void Init(NodePool<PooledParticle> owningPool) => pool = owningPool;

    public override void _Ready() => Finished += OnFinished;

    public void Emit(Vector2 pos, Node parent)
    {
        if (parent is not null)
            Reparent(parent);

        GlobalPosition = pos;
        Restart();
    }

    private void OnFinished()
    {        
        var currentScene = GetTree().CurrentScene;
        if (GetParent() != currentScene)
            Reparent(currentScene);
        pool?.Release(this);
    }
}
