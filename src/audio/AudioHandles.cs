using Godot;

namespace Utilities.AudioManagement;

public struct SfxHandle(PooledAudioPlayer player)
{
    public readonly bool IsPlaying => player.Playing;

    public readonly void SetPitch(float scale)  { if (player.Playing) player.PitchScale    = scale;  }
    public readonly void SetVolume(float linear) { if (player.Playing) player.VolumeLinear = linear; }
}

public struct Sfx2DHandle(PooledAudioPlayer2D player)
{
    public readonly bool IsPlaying => player.Playing;

    public readonly void SetPitch(float scale)    { if (player.Playing) player.PitchScale    = scale;  }
    public readonly void SetVolume(float linear)  { if (player.Playing) player.VolumeLinear  = linear; }
    public readonly void SetPosition(Vector2 pos) { if (player.Playing) player.GlobalPosition = pos;   }
}



