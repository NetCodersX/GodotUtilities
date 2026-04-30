using Godot;
using Godot.Collections;

namespace Utilities.InventorySystem;

[GlobalClass]
public partial class InventoryConfig : Resource
{
    [Export] public InventoryData defaultInventory;
    [Export] public InventoryData hotbarData;
    [Export] public Array<ItemData> itemDatabase = new();

    [ExportGroup("Scenes")]
    [Export] public PackedScene defaultInventoryScene;
    [Export] public PackedScene hotbarScene;
    [Export] public PackedScene slotUiScene;
    [Export] public PackedScene lootItemScene;

    [Export] public Dictionary<StringName, PackedScene> externals = new();
}
