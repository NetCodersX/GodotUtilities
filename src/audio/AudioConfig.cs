using Godot;

namespace Utilities.AudioManagement;

[GlobalClass]
public partial class AudioConfig : Resource
{
    [Export] public int SfxPoolSize { get; private set; } = 16;
    [Export] public double SfxPoolTrimCooldown { get; private set; } = 5.0;
}
