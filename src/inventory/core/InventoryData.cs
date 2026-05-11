using Godot;
using Godot.Collections;
using Utilities.Persistence;

namespace Utilities.InventorySystem;

public interface ISlotDataReceiver
{
    void Receive(SlotData slotData);
}

[GlobalClass]
public partial class InventoryData : Resource, ISerializableResource
{
    [Signal]
    public delegate void InventoryUpdatedEventHandler(InventoryData inventoryData);

    [Signal]
    public delegate void InventoryInteractEventHandler(InventoryData inventoryData, int slotIndex, MouseButton button);

    [Export] private Array<SlotData> slots = [];

    public int Capacity => slots.Count;
    
    public void OnSlotClicked(int index, MouseButton button)
    {
        EmitSignalInventoryInteract(this, index, button);
    }

    private void AssertIndex(int index)
    {
        if (index < 0 || index >= Capacity)
            throw new System.IndexOutOfRangeException($"[InventoryData] Index {index} out of range (Capacity: {Capacity})");
    }

    #region Take

    public SlotData Take(int index)
    {
        AssertIndex(index);

        if (slots[index] == null)
            return null;
        
        SlotData slotData = slots[index];
        slots[index] = null;
        NotifyUpdated();
        
        return slotData;
    }

    #endregion

    #region Place

    public SlotData Place(SlotData item, int index)
    {
        AssertIndex(index);

        SlotData slotData = slots[index];

        try
        {
            if (slotData == null)
                { slots[index] = item; return null; }
            else if (slotData.CanMergeWith(item))
                return slotData.MergeWith(item);
            else
            {
                SlotData oldItem = slots[index];
                slots[index] = item;
                return oldItem;
            }
        }
        finally { NotifyUpdated(); }
    }

    #endregion

    #region Place One

    public SlotData PlaceOne(SlotData item, int index)
    {
        AssertIndex(index);
        
        SlotData slotData = slots[index];

        if (slotData == null) slots[index] = item.ExtractOne();
        else if (slotData.CanMergeWith(item)) slotData.MergeWith(item.ExtractOne());
        
        NotifyUpdated();
        return item.Quantity > 0 ? item : null;
    }

    #endregion

    #region Split

    public SlotData Split(int index)
    {
        AssertIndex(index);
        
        SlotData slotData = slots[index];

        bool canSplit = slotData != null && slotData.Quantity > 1;

        if (!canSplit)
            return Take(index);
        
        int originalQuantity = slotData.Quantity;
        int keepQuantity = originalQuantity / 2;
        int splitQuantity = originalQuantity - keepQuantity;

        slotData.SetQuantity(keepQuantity);
        NotifyUpdated();
        
        return SlotData.Copy(slotData, splitQuantity);
    }

    #endregion

    #region Similar Slots Stacking

    /// <summary>
    /// Collects similar items & store them in the held item using double click shortcut
    /// </summary>
    /// <param name="item"></param>
    /// <param name="inventoryDatas"></param>
    public static void StackSimilar(SlotData item, params InventoryData[] inventoryDatas)
    {
        if (!item.ItemData.Stackable || item.IsFull())
            return;

        foreach (InventoryData inventoryData in inventoryDatas)
            StackSimilarLogic(item, inventoryData);
    }

    private static void StackSimilarLogic(SlotData item, InventoryData inventoryData)
    {
        for (int i = 0; i < inventoryData.Capacity; i++)
        {
            if (item == null || item.IsFull()) return;

            SlotData slotData = inventoryData.slots[i];

            if (slotData == null || slotData.IsEmpty()) continue;
            if (!item.CanMergeWith(slotData)) continue;

            SlotData overflow = item.MergeWith(slotData);
            inventoryData.SetSlotData(i, overflow);
        }
        inventoryData.NotifyUpdated();
    }

    #endregion

    #region Item Store

    public bool TryStoreItem(SlotData item)
    {
        if (item == null)
            return false;

        TryMergeIntoStacks(item);

        bool stored = item.IsEmpty() || TryPlaceIntoEmptySlot(item);
        NotifyUpdated();
        return stored;
    }

    public void TryMergeIntoStacks(SlotData incoming)
    {
        if (!incoming.ItemData.Stackable)
            return;

        for (int i = 0; i < slots.Count && !incoming.IsEmpty(); i++)
        {
            SlotData slot = slots[i];
            if (slot != null && slot.CanMergeWith(incoming) && !slot.IsFull())
            {
                SlotData overflow = slot.MergeWith(incoming);
                incoming.SetQuantity(overflow != null ? overflow.Quantity : 0);
            }
        }
    }

    public bool TryPlaceIntoEmptySlot(SlotData item)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] == null)
            {
                slots[i] = item;
                return true;
            }
        }
        return false;
    }

    #endregion

    #region Item Transfer

    public bool TransferItemTo(int fromIndex, InventoryData target)
    {
        AssertIndex(fromIndex);
        SlotData item = slots[fromIndex];

        try
        {
            if (target.TryStoreItem(item))
            {
                slots[fromIndex] = null;
                return true;
            }
        }
        finally
        {
            NotifyUpdated();
        }

        return false;
    }

    #endregion

    public bool TryConsume(int index, bool clear = false)
    {
        AssertIndex(index);
        SlotData slotData = slots[index];

        if (slotData == null)
            return false;
        
        bool consumed = slotData.TryConsume(clear);

        if (slotData.IsEmpty()) slots[index] = null;
        NotifyUpdated();
        return consumed;
    }

    #region Utilities

    public void Resize(int newSize)
    {
        if (newSize < 0) return;

        slots.Resize(newSize);
        NotifyUpdated();
    }

    public bool IsFull()
    {
        for (int i = 0; i < Capacity; i++)
        {
            if (slots[i] == null)
                return false;
        }

        return true;
    }

    public void NotifyUpdated() => EmitSignalInventoryUpdated(this);

    public bool HasItemAt(int index)
    {
        AssertIndex(index);
        return slots[index] != null;
    } 

    public void SetSlotData(int index, SlotData data)
    {
        AssertIndex(index);
        slots[index] = data;
    }

    public SlotData GetSlotData(int index)
    {
        AssertIndex(index);
        return slots[index];
    }

    public void Clear()
    {
        for (int i = 0; i < Capacity; i++)
            slots[i] = null;
        NotifyUpdated();
    }

    #endregion

    #region Serialize & Deserialize

    public Dictionary Serialize()
    {
        var array = new Array();

        for (int i = 0; i < Capacity; i++)
        {
            if (slots[i] == null) array.Add(new Dictionary());
            else array.Add(slots[i].Serialize());
        }

        return new() { { "slots", array } };
    }

    public void Deserialize(Dictionary data)
    {
        var savedArray = data["slots"].AsGodotArray();

        for (int i = 0; i < savedArray.Count; i++)
        {
            var dict = savedArray[i].AsGodotDictionary();

            if (dict.Count == 0 || !dict.ContainsKey("id"))
            {
                slots[i] = null;
                continue;
            }

            // Project is supposed to have this item data but the warning is added for debugging
            // So the pre-creating a slot data is 100% safe

            SlotData slot = new();
            slot.Deserialize(dict);

            if (slot.ItemData is null)
            {
                Log.Warn("Inventory", $"Could not find item with ID {dict["id"]} during load.");
                slots[i] = null;
            }
            else { slots[i] = slot; } // add slot to inventory
        }

        NotifyUpdated();
    }

    #endregion
}

