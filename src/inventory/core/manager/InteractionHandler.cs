using Godot;

namespace Utilities.InventorySystem.Interaction;

public sealed class InteractionHandler(InventoryManager manager, Timer stackTimer)
{
    private InventoryManager Manager { get; } = manager;
    private Timer StackTimer { get; } = stackTimer;

    private bool isHoldingShift;

    public SlotData Interact(SlotData heldItem, InventoryData data, int index, MouseButton button)
    {
        return (heldItem, button) switch
        {
            (null, MouseButton.Left)  => isHoldingShift ? Transfer(data, index) : Take(data, index),
            (null, MouseButton.Right) => data.Split(index),

            (_, MouseButton.Left) => StackItems(heldItem) ? heldItem : data.Place(heldItem, index),
            (_, MouseButton.Right) => data.PlaceOne(heldItem, index),

            (_, _) => heldItem,
        };
    }

    public void OnInputEvent(InputEvent @event)
    {
        if (@event is InputEventKey eventKey && eventKey.Keycode == Key.Shift)
            isHoldingShift = eventKey.Pressed;
    }

    private SlotData Take(InventoryData data, int slotIndex)
    {
        StackTimer.Start();
        return data.Take(slotIndex);
    }

    private bool StackItems(SlotData receiver)
    {
        if (StackTimer.IsStopped()) return false;

        if (Manager.ExternalData is not null)
            InventoryData.StackSimilar(receiver, Manager.ExternalData, Manager.PlayerInventory, Manager.HotbarData);
        else
            InventoryData.StackSimilar(receiver, Manager.PlayerInventory, Manager.HotbarData);
        return true;
    }

    private SlotData Transfer(InventoryData data, int slotIndex)
    {
        var player   = Manager.PlayerInventory;
        var external = Manager.ExternalData;
        var hotbar   = Manager.HotbarData;

        if (external is null)
        {
            var target = data == hotbar ? player : hotbar;
            data.TransferItemTo(slotIndex, target);
            return null;
        }

        if (data == player) TransferWithFallback(data, slotIndex, external, hotbar);
        else if (data == external) TransferWithFallback(data, slotIndex, hotbar, player);
        else TransferWithFallback(data, slotIndex, external, player);
        return null;
    }

    private static void TransferWithFallback(InventoryData source, int index, InventoryData primary, InventoryData fallback)
    {
        if (!source.TransferItemTo(index, primary))
            source.TransferItemTo(index, fallback);
    }
}

