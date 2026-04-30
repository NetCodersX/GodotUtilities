using System.Collections.Generic;
using Utilities.Pooling;
using System.Linq;
using Godot;

namespace Utilities.AudioManagement;

public partial class AudioManager : Node2D
{
    private const string MASTER_BUS   = "Master";
    private const string MUSIC_BUS    = "Music";
    private const string SFX_BUS      = "SFX";
    private const string AMBIENCE_BUS = "Ambience";

    public static AudioManager Instance { get; private set; }

    private AudioConfig config;

    private Dictionary<StringName, AudioStream> music    = new();
    private Dictionary<StringName, AudioStream> sfx      = new();
    private Dictionary<StringName, AudioStream> ambience = new();

    private CrossfadeChannel musicChannel;
    private CrossfadeChannel ambienceChannel;

    private ObjectPool<AudioStreamPlayer>   sfxPool;
    private ObjectPool<AudioStreamPlayer2D> sfxPool2D;

    public float MasterVolume   { get; private set; } = 1f;
    public float MusicVolume    { get; private set; } = 1f;
    public float SfxVolume      { get; private set; } = 1f;
    public float AmbienceVolume { get; private set; } = 1f;

    private float sfxPoolTrimTimer;

    public override void _Ready()
    {
        Instance = this;

        musicChannel    = new CrossfadeChannel(this, MUSIC_BUS);
        ambienceChannel = new CrossfadeChannel(this, AMBIENCE_BUS);

        Init();
    }

    public override void _Process(double delta)
    {
        if (config == null) return;

        sfxPoolTrimTimer -= (float)delta;

        if (sfxPoolTrimTimer <= 0f)
        {
            sfxPool.Trim(config.sfxPoolSize, OnTrim);
            sfxPool2D.Trim(config.sfxPoolSize, OnTrim2D);

            sfxPoolTrimTimer = config.sfxPoolTrimCooldown;
        }
    }

    private void OnTrim(AudioStreamPlayer p)     => p.QueueFree();
    private void OnTrim2D(AudioStreamPlayer2D p) => p.QueueFree();

    private void Init()
    {
        SetupConfig();

        music    = config.music.ToDictionary();
        sfx      = config.sfx.ToDictionary();
        ambience = config.ambience.ToDictionary();

        sfxPool   = new(PlayerFactory,   config.sfxPoolSize);
        sfxPool2D = new(Player2DFactory, config.sfxPoolSize);

        sfxPoolTrimTimer = config.sfxPoolTrimCooldown;
    }

    private void SetupConfig()
    {
        string path = ProjectSettings.GetSetting("godot_utilities/audio_config_path").AsString();

        config = GD.Load<AudioConfig>(path);

        if (config is null)
            GD.PushError("Invalid Config Path. Check GodotUtilities Audio Config Path in Project Settings");
    }

    private AudioStreamPlayer PlayerFactory()
    {
        var player = new AudioStreamPlayer() { Bus = SFX_BUS };
        CallDeferred(Node.MethodName.AddChild, player);

        player.Finished += () => 
        {
            player.Stream       = null;
            player.PitchScale   = 1f;
            player.VolumeLinear = 1f;
            sfxPool.Release(player);
        };

        return player;
    }

    private AudioStreamPlayer2D Player2DFactory()
    {
        var player = new AudioStreamPlayer2D() { Bus = SFX_BUS };
        CallDeferred(Node.MethodName.AddChild, player);

        player.Finished += () => 
        {
            player.Stream         = null;
            player.Autoplay       = false;
            player.MaxDistance    = 2000f;
            player.PitchScale     = 1f;
            player.VolumeLinear   = 1f;
            player.GlobalPosition = Vector2.Zero;
            sfxPool2D.Release(player);
        };

        return player;
    }
}

