using Godot;
using Godot.Collections;
using Utilities.Persistence;

namespace Utilities.InventorySystem;

[GlobalClass]
public partial class SlotData : Resource, ISerializableResource
{
    [Export] public ItemData ItemData { get; set; }

    [Export] private int quantity = 1;

    public int Quantity => quantity;

    #region Duplication

    public static SlotData Copy(SlotData source) => Copy(source, source.quantity);

    public static SlotData Copy(SlotData source, int quantity)
    {
        return new() { ItemData = source.ItemData, quantity = quantity };
    }

    #endregion

    #region Merge

    public bool CanMergeWith(SlotData data) => ItemData != null 
                                            && ItemData.Stackable 
                                            && ItemData.Match(data.ItemData) 
                                            && quantity < ItemData.MaxStackSize;
    
    public SlotData MergeWith(SlotData data)
    {
        int totalQuantity = quantity + data.Quantity;

        if (totalQuantity <= ItemData.MaxStackSize)
        {
            quantity = totalQuantity;
            return null;
        }

        quantity = ItemData.MaxStackSize;
        data.SetQuantity(totalQuantity - ItemData.MaxStackSize);
        return data;
    } 

    #endregion

    #region Utilities

    public void SetQuantity(int value)
    {
        if (ItemData is null)
        {
            Log.Error("SlotData", "Invalid item data resource, check if it's assigned");
            return;
        };
        
        quantity = value;

        if (quantity > 1 && !ItemData.Stackable)
        {
            quantity = 1;
            GD.PushError($"Unstackable items quantity should be 1, item data: {ItemData.Id}");
        }
    }

    public SlotData ExtractOne()
    {
        if (quantity <= 0)
            return null;
        
        quantity--;
        return Copy(this, 1);
    }

    public bool TryConsume(bool clear)
    {
        if (clear)
        {
            quantity = 0;
            return true;
        }

        if (quantity <= 0)
            return false;

        quantity--;

        return true;
    }

    public bool IsEmpty() => Quantity <= 0;
    public bool IsFull() => ItemData != null && quantity >= ItemData.MaxStackSize;
    
    #endregion

    #region Serialize & Deserialize

    public Dictionary Serialize()
    {
        if (ItemData == null || quantity <= 0) return [];

        return new()
        {
            { "quantity", quantity },  
            { "id", ItemData.Id },  
        };
    }

    public void Deserialize(Dictionary data)
    {
        if (data.Count == 0) return;

        quantity = data["quantity"].AsInt32();
        
        StringName id = data["id"].AsString();

        // forced coupling in order to get the full features of ISerializableResource interface
        // no real issue :)
        ItemData = InventoryManager.GetItem(id);

        if (ItemData is null)
            Log.Error("SlotData", "Item data not found. Make sure item cache is assigned properly");
    }

    #endregion
}
