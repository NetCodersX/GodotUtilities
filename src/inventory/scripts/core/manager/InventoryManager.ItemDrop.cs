using Utilities.Events;
using Godot;

namespace Utilities.InventorySystem;

public partial class InventoryManager
{
    #region Item Drop

    public static bool TryConsumeItem(InventoryData data, int index, bool clear = false)
    {
        return data.TryConsume(index, clear);
    }

    public void TryConsumeHotbarItem(bool clearSlot = false) => TryConsumeItem(hotbar, currentIndex, clearSlot);

    public TType DropItem<TType>(InventoryData data, int slotIndex, Vector2 position, bool clearSlot = false, Node parent = null) where TType : Node2D
    {
        SlotData slotData = data.GetSlotData(slotIndex);

        if (slotData == null)
            return null;

        if (!clearSlot && slotData.itemData.stackable)
        {
            var item = SpawnItem<TType>(slotData.ExtractOne(), position, parent);

            if (slotData.Quantity <= 0)
                data.SetSlotData(slotIndex, null);
            data.NotifyUpdated();
            return item;
        }

        data.SetSlotData(slotIndex, null);
        data.NotifyUpdated();

        return SpawnItem<TType>(slotData, position, parent);
    }

    public TType DropHotbarItem<TType>(Vector2 position, bool clearSlot = false, Node parent = null) where TType : Node2D
    {
        return DropItem<TType>(hotbar, currentIndex, position, clearSlot, parent);
    }

    public TType SpawnItem<TType>(SlotData slotData, Vector2 position, Node parent = null) where TType : Node2D
    {
        var item = config.lootItemScene.Instantiate<TType>();

        if (item is not ISlotDataReceiver receiver)
            return null;
        
        receiver.Receive(slotData);

        item.GlobalPosition = position;
        Node currentParent  = parent ?? GetTree().CurrentScene;

        currentParent.CallDeferred(Node.MethodName.AddChild, item);
        return item;
    }

    #endregion

    #region Holder GUI Input

    private void OnInventoryHolderGuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButton && mouseButton.Pressed)
        {
            if      (mouseButton.ButtonIndex == MouseButton.Left)  ClearHeldItem();
            else if (mouseButton.ButtonIndex == MouseButton.Right) ExtractOneFromHeldItem();
        }
    }

    private void ExtractOneFromHeldItem()
    {
        if (heldItem == null) return;

        if (!heldItem.itemData.stackable)
        {
            ClearHeldItem();
            return;
        }

        EventBus.Trigger(new HeldItemDropRequested(heldItem.ExtractOne()));

        if (heldItem.Quantity <= 0)
            heldItem = null;

        UpdateItemPreview();
    }

    public void ClearHeldItem()
    {
        if (heldItem == null) return;

        EventBus.Trigger(new HeldItemDropRequested(heldItem));
        heldItem = null;
        UpdateItemPreview();
    }

    #endregion
}

