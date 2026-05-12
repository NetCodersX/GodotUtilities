using Godot;

namespace Utilities.InventorySystem;

[GlobalClass]
public partial class ItemData : Resource
{
    public const int DEFAULT_SLOT_SIZE = 64;

    [Export] public StringName Id { get; private set; } = new();
    [Export] public AtlasTexture Art { get; private set; }

    [Export] public bool Stackable { get; private set; }
    [Export] private int maxStackSize = -1;

    [Export(PropertyHint.MultilineText)] 
    public string Description { get; private set; }

    public int MaxStackSize => maxStackSize > 0 ? maxStackSize : DEFAULT_SLOT_SIZE;

    public bool Match(ItemData other)
    {
        return other.Id == Id;
    }

}
