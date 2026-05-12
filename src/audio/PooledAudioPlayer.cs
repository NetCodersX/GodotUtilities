using Utilities.Pooling;
using Godot;

namespace Utilities.AudioManagement;

[GlobalClass]
public partial class PooledAudioPlayer : AudioStreamPlayer
{
    public ObjectPool<PooledAudioPlayer> Pool { get; set; }

    public int CurrentCycle { get; private set; } = 1;
    public int Cycles { get; private set; }

    public override void _Ready() => Finished += OnFinished;

    private void OnFinished()
    {
        if (CurrentCycle++ < Cycles)
        {
            Play();
            return;
        }

        Stream = null;
        Cycles = 1;
        CurrentCycle = 1;
        PitchScale = 1f;
        VolumeLinear = 1f;
        Pool.Release(this);   
    }

    public void PlaySound(AudioStream stream, float pitch, float volumeLinear = 1f, int cycles = 1)
    {
        Cycles = cycles;
        Stream = stream;
        PitchScale = pitch;
        VolumeLinear = volumeLinear;
        Play();
    }

}
