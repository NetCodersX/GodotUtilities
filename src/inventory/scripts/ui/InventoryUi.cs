using System.Collections.Generic;
using Godot;

namespace Utilities.InventorySystem;

public partial class InventoryUi : Control
{
    [Export] private PackedScene slotScene;
    [Export] private Control slotsContainer;

    public IEnumerable<SlotUi> GetSlots() => slots;

    private readonly List<SlotUi> slots = new();

    private InventoryData attachedInventory;

    public SlotUi GetSlot(int index)
    {
        if (index < 0 || index >= slots.Count)
        {
            GD.PushWarning($"[Inventory UI] slot index is out of bounds, index: {index}, slotCount: {slots.Count}");
            return null;   
        }

        return slots[index];
    }

    public override void _ExitTree() => TryDetach();

    public void Attach(InventoryData inventoryData)
    {
        TryDetach();

        foreach (var child in slotsContainer.GetChildren()) 
            child.QueueFree();

        attachedInventory = inventoryData;
        attachedInventory.InventoryUpdated += UpdateSlots;

        UpdateSlots(attachedInventory);
    }

    public void TryDetach()
    {
        if (attachedInventory is null)
            return;
        
        attachedInventory.InventoryUpdated -= UpdateSlots;

        foreach (SlotUi slot in slots)
            slot.Clicked -= attachedInventory.OnSlotClicked;
        slots.Clear();

        return;
    }

    public void UpdateSlots(InventoryData data)
    {
        int size = data.Size;

        if (slots.Count < size)
            CreateNewSlots(data, size - slots.Count);
        
        for (int i = 0; i < size; i++)
        {
            SlotUi slot       = slots[i];
            SlotData slotData = data.GetSlotData(i);

            slot.Show();
            slot.SetData(slotData);
        }
    }

    private void CreateNewSlots(InventoryData data, int count)
    {
        for (int i = 0; i < count; i++)
        {
            SlotUi slot = slotScene.Instantiate<SlotUi>();
            slotsContainer.AddChild(slot);

            slot.Clicked += data.OnSlotClicked;
            slots.Add(slot);
        }

        data.Initialize();
    }

}
