using System.Collections.Generic;
using Godot;

namespace Utilities.InventorySystem;

[GlobalClass]
public partial class InventoryUI : Control
{
    [Export] private PackedScene slotScene;
    [Export] private Control slotsContainer;

    private readonly List<SlotUI> slots = new();

    private InventoryData attachedInventory;

    public override void _ExitTree() => TryDetach();

    #region Attach & Detach

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
        
        foreach (SlotUI slot in slots)
            slot.Clicked -= attachedInventory.OnSlotClicked;
        attachedInventory.InventoryUpdated -= UpdateSlots;

        slots.Clear();
        attachedInventory = null;
        return;
    }

    #endregion

    #region Utilities

    public IEnumerable<SlotUI> GetSlotsUI()
    {
        foreach (var slot in slots)
            yield return slot;
    }

    public SlotUI GetSlotUI(int index)
    {
        if (index < 0 || index >= slots.Count)
        {
            Log.Warn("Inventory", $"Slot UI index is out of bounds, index: {index}, slotCount: {slots.Count}");
            return null;   
        }

        return slots[index];
    }

    #endregion

    #region Update

    public void UpdateSlots(InventoryData data)
    {
        while (slots.Count > data.Capacity)
            RemoveExistingSlot(data);

        while (slots.Count < data.Capacity)
            CreateNewSlot(data);
        
        for (int i = 0; i < data.Capacity; i++)
        {
            SlotUI slot = slots[i];
            SlotData slotData = data.GetSlotData(i);

            slot.Show();
            slot.SetData(slotData);
        }
    }

    private void CreateNewSlot(InventoryData data)
    {
        SlotUI slot = slotScene.Instantiate<SlotUI>();
        slotsContainer.AddChild(slot);

        slot.Clicked += data.OnSlotClicked;
        slots.Add(slot);
    }

    private void RemoveExistingSlot(InventoryData data)
    {
        SlotUI slotUI = slots[^1];
        slotUI.Clicked -= data.OnSlotClicked;
        slotUI.QueueFree();
        slots.RemoveAt(slots.Count - 1);
    }

    #endregion

}
