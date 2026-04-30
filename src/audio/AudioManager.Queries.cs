using Godot;

namespace Utilities.AudioManagement;

public partial class AudioManager : Node2D
{
    #region Volume API

    public void SetMasterVolume(float linear)
    {
        MasterVolume = MathUtil.Clamp01(linear);
        int index = AudioServer.GetBusIndex(MASTER_BUS);
        AudioServer.SetBusVolumeLinear(index, MasterVolume);
    }

    public void SetMusicVolume(float linear)
    {
        MusicVolume = MathUtil.Clamp01(linear);
        int index = AudioServer.GetBusIndex(MUSIC_BUS);
        AudioServer.SetBusVolumeLinear(index, MusicVolume);
    }

    public void SetSfxVolume(float linear)
    {
        SfxVolume = MathUtil.Clamp01(linear);
        int index = AudioServer.GetBusIndex(SFX_BUS);
        AudioServer.SetBusVolumeLinear(index, SfxVolume);
    }

    public void SetAmbienceVolume(float linear)
    {
        AmbienceVolume = MathUtil.Clamp01(linear);
        int index = AudioServer.GetBusIndex(AMBIENCE_BUS);
        AudioServer.SetBusVolumeLinear(index, AmbienceVolume);
    }

    #endregion

    #region Music

    public void PlayMusic(StringName id, float fadeDuration = 1.5f)
    {
        if (ValidateMusicId(id, out var stream))
            musicChannel.Play(stream, fadeDuration);
    }

    public void ThenPlayMusic(StringName id, float fadeDuration = 1.5f)
    {
        if (ValidateMusicId(id, out var stream))
            musicChannel.ThenPlay(stream, fadeDuration);
    }

    public void StopMusic(float fadeDuration = 1f) => musicChannel.Stop(fadeDuration);
    public void PauseMusic()                       => musicChannel.Pause();
    public void ResumeMusic()                      => musicChannel.Resume();

    private bool ValidateMusicId(StringName id, out AudioStream stream)
    {
        if (!music.TryGetValue(id, out stream))
        {
            GD.PushWarning($"AudioManager: Music id '{id}' not found.");
            return false;
        }

        return true;
    }

    #endregion

    #region Sfx

    public AudioStreamPlayer PlaySfx(StringName id, float pitchVariance = 0f)
    {
        if (!sfx.TryGetValue(id, out var stream))
        {
            GD.PushWarning($"AudioManager: SFX id '{id}' not found.");
            return null;
        }

        var player          = sfxPool.Get();
        player.Stream       = stream;
        player.PitchScale   = ApplyVariance(pitchVariance);
        player.VolumeLinear = 1f;
        player.Play();

        return player;
    }
    
    public AudioStreamPlayer2D PlaySfx2D(StringName id, Vector2 pos, float pitchVariance = 0f)
    {
        if (!sfx.TryGetValue(id, out var stream))
        {
            GD.PushWarning($"AudioManager: SFX id '{id}' not found.");
            return null;
        }

        var player            = sfxPool2D.Get();
        player.GlobalPosition = pos;
        player.Stream         = stream;
        player.PitchScale     = ApplyVariance(pitchVariance);
        player.VolumeLinear   = 1f;
        player.Play();

        return player;
    }

    private float ApplyVariance(float variance) => 1f + MathUtil.RandfRange(-variance, variance);

    #endregion

    #region Ambience

    public void PlayAmbience(StringName id, float fadeDuration = 3f)
    {
        if (ValidateAmbienceId(id, out var stream))
            ambienceChannel.Play(stream, fadeDuration);
    }

    public void ThenPlayAmbience(StringName id, float fadeDuration = 3f)
    {
        if (ValidateAmbienceId(id, out var stream))
            ambienceChannel.ThenPlay(stream, fadeDuration);
    }

    public void StopAmbience(float fadeDuration = 1f) => ambienceChannel.Stop(fadeDuration);
    public void PauseAmbience()                       => ambienceChannel.Pause();
    public void ResumeAmbience()                      => ambienceChannel.Resume();

    private bool ValidateAmbienceId(StringName id, out AudioStream stream)
    {
        if (!ambience.TryGetValue(id, out stream))
        {
            GD.PushWarning($"AudioManager: Ambience id '{id}' not found.");
            return false;
        }

        return true;
    }

    #endregion   
}