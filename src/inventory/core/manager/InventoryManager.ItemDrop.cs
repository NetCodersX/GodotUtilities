using Godot;

namespace Utilities.InventorySystem;

public partial class InventoryManager
{
    #region Item Drop

    public void TryConsumeHotbarItem(bool clearSlot = false) => HotbarData.TryConsume(currentIndex, clearSlot);

    public TType DropItem<TType>(InventoryData data, int slotIndex, Vector2 position, bool clear = false, Node parent = null) where TType : Node2D
    {
        SlotData slotData = data.GetSlotData(slotIndex);

        if (slotData == null) return null;

        try
        {
            if (clear || !slotData.ItemData.Stackable)
            {
                data.SetSlotData(slotIndex, null);
                return SpawnItem<TType>(slotData, position, parent);
            }

            var item = SpawnItem<TType>(slotData.ExtractOne(), position, parent);
            if (slotData.IsEmpty()) data.SetSlotData(slotIndex, null);
            return item;
        }
        finally { data.NotifyUpdated(); }
    }

    public TType DropHotbarItem<TType>(Vector2 position, bool clear = false, Node parent = null) where TType : Node2D
    {
        return DropItem<TType>(HotbarData, currentIndex, position, clear, parent);
    }

    public TType SpawnItem<TType>(SlotData slotData, Vector2 position, Node parent = null) where TType : Node2D
    {
        var item = config.LootItemScene.Instantiate<TType>();

        if (item is not ISlotDataReceiver receiver)
        {
            Log.Error("Inventory", $"Item '{item.GetType()}' is not an ISlotDataReceiver interface");
            return null;
        }
        
        receiver.Receive(slotData);

        item.GlobalPosition = position;
        Node currentParent  = parent ?? GetTree().CurrentScene;

        currentParent.CallDeferred(Node.MethodName.AddChild, item);
        return item;
    }

    #endregion

    #region Holder GUI Input

    private void OnDropZoneInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButton && mouseButton.Pressed)
        {
            if (mouseButton.ButtonIndex == MouseButton.Left) ClearHeldItem();
            else if (mouseButton.ButtonIndex == MouseButton.Right) ExtractOneFromHeldItem();
        }
    }

    private void ExtractOneFromHeldItem()
    {
        if (heldItem == null) return;

        if (!heldItem.ItemData.Stackable)
        {
            ClearHeldItem();
            return;
        }

        EmitSignalDropZoneRequested(heldItem.ExtractOne());

        if (heldItem.IsEmpty()) heldItem = null;
        UpdateItemPreview();
    }

    public void ClearHeldItem()
    {
        if (heldItem == null) return;

        EmitSignalDropZoneRequested(heldItem);
        heldItem = null;
        UpdateItemPreview();
    }

    #endregion
}

