using Godot.Collections;
using Godot;

namespace Utilities.AudioManagement;

[GlobalClass]
public partial class AudioConfig : Resource
{
    [Export] public int   sfxPoolSize         = 16;
    [Export] public float sfxPoolTrimCooldown = 15f;

    [Export] public Dictionary<StringName, AudioStream> music    = new();
    [Export] public Dictionary<StringName, AudioStream> sfx      = new();
    [Export] public Dictionary<StringName, AudioStream> ambience = new();
}
