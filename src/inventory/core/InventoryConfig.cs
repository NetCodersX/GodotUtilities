using Godot.Collections;
using Godot;

namespace Utilities.InventorySystem;

[GlobalClass]
public partial class InventoryConfig : Resource
{
    [Export(PropertyHint.Dir)] private string itemsFolder;

    [Export] public InventoryData PlayerInventory { get; private set; }
    [Export] public InventoryData HotbarData { get; private set; }

    [Export] private double itemsStackWindow = 0.15;

    [ExportGroup("Scenes")]
    [Export] public PackedScene LootItemScene { get; private set; }
    [Export] private PackedScene itemPreviewScene;

    public Dictionary<StringName, ItemData> GetItemCache()
    {
        var items = ResourceLoaderUtil.LoadResourcesFromRecursive<ItemData>(itemsFolder);
        var dict = new Dictionary<StringName, ItemData>();

        foreach (ItemData item in items)
            dict[item.Id] = item;
        return dict;
    }

    public Timer CreateStackTimer(InventoryManager parent)
    {
        Timer timer = new() { WaitTime = itemsStackWindow, OneShot = true };
        parent.AddChild(timer);
        return timer;
    }

    public Control CreateDropZone(Node parent)
    {
        var zone = new Control { Name = "InventoryHolder", Visible = false };
        zone.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        parent.AddChild(zone);
        parent.MoveChild(zone, 0);
        return zone;
    }

    public ItemPreview CreateItemPreview(Node parent)
    {
        var itemPreview = itemPreviewScene.Instantiate<ItemPreview>();
        parent.AddChild(itemPreview);
        itemPreview.UpdateState(null);
        return itemPreview;
    }
}
