using Godot;
using System;
using System.Collections.Generic;

namespace Utilities.InventorySystem;

public interface ISlotDataReceiver
{
    void Receive(SlotData slotData);
}

[GlobalClass]
public partial class InventoryData : Resource
{
    [Signal]
    public delegate void InventoryUpdatedEventHandler(InventoryData inventoryData);

    [Signal]
    public delegate void InventoryInteractEventHandler(InventoryData inventoryData, int slotIndex, MouseButton button);

    [Export(PropertyHint.Range, "1, 32")]
    private int slotCount;

    public int Size => slotCount;

    private readonly List<SlotData> slots = new();

    public void OnSlotClicked(int index, MouseButton button)
    {
        EmitSignalInventoryInteract(this, index, button);
    }

    private bool ValidateIndex(int index)
    {
        if (index < 0 || index >= slotCount)
        {
            GD.PushWarning("[InventoryData] Index is out of bounds");
            return false;
        }

        return true;
    }

    #region Take

    public SlotData Take(int index)
    {
        if (!ValidateIndex(index) || slots[index] == null)
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
        if (!ValidateIndex(index))
            return null;

        SlotData slotData = slots[index];

        try
        {
            if (slotData == null)
                return SetSlotData(index, item);
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
        if (!ValidateIndex(index))
            return null;
        
        SlotData slotData = slots[index];

        if (slotData == null)
            SetSlotData(index, item.ExtractOne());
        else if (slotData.CanMergeWith(item))
            slotData.MergeWith(item.ExtractOne());
        
        NotifyUpdated();
        return item.Quantity > 0 ? item : null;
    }

    #endregion

    #region Split

    public SlotData Split(int index)
    {
        if (!ValidateIndex(index))
            return null;
        
        SlotData slotData = slots[index];

        bool canSplit = slotData != null && slotData.Quantity > 1;

        if (!canSplit)
            return Take(index);
        
        int originalQuantity = slotData.Quantity;
        int keepQuantity     = originalQuantity / 2;
        int splitQuantity    = originalQuantity - keepQuantity;

        slotData.SetQuantity(keepQuantity);

        SlotData splitData = slotData.Duplicate() as SlotData;
        splitData.SetQuantity(splitQuantity);
        NotifyUpdated();

        return splitData;
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
        if (!item.itemData.stackable || item.IsFull())
            return;

        foreach (InventoryData inventoryData in inventoryDatas)
            StackSimilarLogic(item, inventoryData);
    }

    private static void StackSimilarLogic(SlotData item, InventoryData inventoryData)
    {
        for (int i = 0; i < inventoryData.slotCount; i++)
        {
            if (item == null || item.Quantity <= 0)
                return;

            SlotData slotData = inventoryData.slots[i];

            if (slotData == null || slotData.IsFull())
                continue;
            
            if (item.CanMergeWith(slotData))
            {
                SlotData overflow = item.MergeWith(slotData);
                inventoryData.SetSlotData(i, overflow);
            }
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

        bool stored = item.Quantity <= 0 || TryPlaceIntoEmptySlot(item);
        NotifyUpdated();
        return stored;
    }

    private void TryMergeIntoStacks(SlotData incoming)
    {
        if (!incoming.itemData.stackable)
            return;

        for (int i = 0; i < slotCount && incoming.Quantity > 0; i++)
        {
            SlotData slot = slots[i];
            if (slot != null && slot.CanMergeWith(incoming) && !slot.IsFull())
            {
                SlotData overflow = slot.MergeWith(incoming);
                incoming.SetQuantity(overflow != null ? overflow.Quantity : 0);
            }
        }
    }

    private bool TryPlaceIntoEmptySlot(SlotData item)
    {
        for (int i = 0; i < slotCount; i++)
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
        SlotData slotData = slots[index];

        if (slotData == null)
            return false;
        
        try
        {
            if (slotData.Quantity > 1 && !clear)
            {
                slotData.SetQuantity(slotData.Quantity - 1);
                if (slotData.Quantity == 0) slots[index] = null;

                return true;
            }

            slots[index] = null;
            return true;
        }
        finally
        {
            NotifyUpdated();
        }   
    }

    #region Initialization

    public void Initialize()
    {
        if (slots.Count > 0)
            return;

        for (int i = 0; i < slotCount; i++)
            slots.Add(null);
    }

    #endregion

    #region Utilities

    public void NotifyUpdated()        => EmitSignalInventoryUpdated(this);
    public bool IsSlotEmpty(int index) => ValidateIndex(index) && slots[index] == null;

    public SlotData SetSlotData(int index, SlotData data)
    {
        if (!ValidateIndex(index))
            return null;
        
        slots[index] = data;
        return null;
    }

    public SlotData GetSlotData(int index)
    {
        if (ValidateIndex(index))
            return slots[index];
        return null;
    }

    public void Clear()
    {
        for (int i = 0; i < slotCount; i++)
            slots[i] = null;
        NotifyUpdated();
    }

    #endregion

    #region Serialize & Deserialize

    public Godot.Collections.Array Serialize()
    {
        var array = new Godot.Collections.Array();

        for (int i = 0; i < slotCount; i++)
        {
            if (slots[i] == null) array.Add(new Godot.Collections.Dictionary());
            else array.Add(slots[i].Serialize());
        }

        return array;
    }

    public void Deserialize(Godot.Collections.Array savedArray, Func<StringName, ItemData> itemLookup)
    {
        Initialize();

        for (int i = 0; i < savedArray.Count; i++)
        {
            var dict = savedArray[i].AsGodotDictionary();

            if (dict.Count == 0 || !dict.ContainsKey("id"))
            {
                slots[i] = null;
                continue;
            }

            // Note: project is supposed to 100% have this item data but the warning is added for debugging
            // So the pre-creating a slot data is 100% safe

            SlotData slot = new();
            slot.Deserialize(dict);

            if (slot.itemData is null)
            {
                GD.PushWarning($"[InventoryData] Could not find item with ID {dict["id"]} during load.");
                slots[i] = null;
            }
            else { slots[i] = slot; } // add slot to inventory
        }

        NotifyUpdated();
    }

    #endregion
}

