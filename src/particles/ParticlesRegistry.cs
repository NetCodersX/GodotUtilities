using Godot.Collections;
using Godot;

namespace Utilities.ParticlesManagement;

[GlobalClass]
public partial class ParticlesRegistry : Resource
{
    [Export] public Dictionary<StringName, PackedScene> scenes = new();
}
