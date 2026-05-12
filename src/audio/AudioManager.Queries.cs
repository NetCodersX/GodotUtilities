using Godot;

namespace Utilities.AudioManagement;

public partial class AudioManager
{
    #region Volume API

    public void SetMasterVolume(float linear) => 
       MasterVolume = SetVolume(MasterVolume, MASTER_BUS, linear);

    public void SetMusicVolume(float linear) =>
        MusicVolume = SetVolume(MusicVolume, MUSIC_BUS, linear);

    public void SetSfxVolume(float linear) =>
        SfxVolume = SetVolume(SfxVolume, SFX_BUS, linear);

    public void SetAmbienceVolume(float linear) =>
        AmbienceVolume = SetVolume(AmbienceVolume, AMBIENCE_BUS, linear);

    private float SetVolume(float volume, string bus, float linear)
    {
        volume = MathUtil.Clamp01(linear);
        int index = AudioServer.GetBusIndex(bus);
        AudioServer.SetBusVolumeLinear(index, volume);
        return volume;
    }

    #endregion

    #region Music

    public void PlayMusic(StringName id, float fadeDuration = 1.5f) =>
        musicChannel.Play(GetMusic(id), fadeDuration);

    public void ThenPlayMusic(StringName id, float fadeDuration = 1.5f) =>
        musicChannel.ThenPlay(GetMusic(id), fadeDuration);

    public void StopMusic(float fadeDuration = 1f) => 
        musicChannel.Stop(fadeDuration);

    public void PauseMusic() => musicChannel.Pause();
    public void ResumeMusic() => musicChannel.Resume();

    #endregion

    #region Sfx

    private float Vary(float variance) =>
        variance == 0f ? 1f : 1f + MathUtil.RandfRange(-variance, variance);

    public SfxHandle PlaySfx(StringName id, float pitchVariance = 0f, float volumeLinear = 1f, int cycles = 1)
    {
        var player = sfxPool.Get();
        player.Pool = sfxPool;
        player.PlaySound(GetSfx(id), Vary(pitchVariance), volumeLinear, cycles);

        return new SfxHandle(player);
    }
    
    public Sfx2DHandle PlaySfx2D(StringName id, Vector2 pos, float pitchVariance = 0f, float volumeLinear = 1f, int cycles = 1)
    {
        var player = sfxPool2D.Get(); 
        player.Pool = sfxPool2D;
        player.PlaySound(GetSfx(id), pos, Vary(pitchVariance), volumeLinear, cycles);

        return new Sfx2DHandle(player);
    }

    #endregion

    #region Ambience

    public void PlayAmbience(StringName id, float fadeDuration = 3f) =>
        ambienceChannel.Play(GetAmbience(id), fadeDuration);

    public void ThenPlayAmbience(StringName id, float fadeDuration = 3f) =>
        ambienceChannel.ThenPlay(GetAmbience(id), fadeDuration);

    public void StopAmbience(float fadeDuration = 1f) => 
        ambienceChannel.Stop(fadeDuration);

    public void PauseAmbience() => ambienceChannel.Pause();
    public void ResumeAmbience() => ambienceChannel.Resume();

    #endregion


}

