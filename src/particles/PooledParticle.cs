using Utilities.Pooling;
using Godot;

namespace Utilities.ParticlesManagement;

[GlobalClass]
public partial class PooledParticle : GpuParticles2D, IPoolable
{
    private NodePool<PooledParticle> pool;

    public void Init(NodePool<PooledParticle> owningPool) => pool = owningPool;

    public void OnGet()
    {
        Finished += OnFinished;

        Restart();
        Emitting = true;
    }

    public void OnRelease()
    {
        Emitting = false;
        Finished -= OnFinished;
    }

    private void OnFinished() => pool.Release(this);
}
