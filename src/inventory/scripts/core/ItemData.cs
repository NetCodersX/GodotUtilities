using Godot;

namespace Utilities.InventorySystem;

[GlobalClass]
public partial class ItemData : Resource
{
    public const int DEFAULT_SLOT_SIZE = 64;

    [Export] public StringName id = new();
    [Export] public AtlasTexture art;

    [Export] public bool stackable;
    [Export] public int maxStackSize = -1;

    [Export(PropertyHint.MultilineText)] 
    public string description;

    public int MaxStackSize => maxStackSize > 0 ? maxStackSize : DEFAULT_SLOT_SIZE;

    public bool Match(ItemData other)
    {
        return other.id == id;
    }
}
