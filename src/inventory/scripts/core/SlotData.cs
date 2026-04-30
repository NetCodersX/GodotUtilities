using Godot;
using Godot.Collections;
using Utilities.Persistence;

namespace Utilities.InventorySystem;

[GlobalClass]
public partial class SlotData : Resource, ISerializableResource
{
    [Export] public ItemData itemData;
    [Export] private int quantity = 1;

    public int Quantity => quantity;

    public void SetQuantity(int value)
    {
        if (!ValidateItemData())
            return;
        
        quantity = value;

        if (quantity > 1 && !itemData.stackable)
        {
            quantity = 1;
            GD.PushError($"Unstackable items quantity should be 1, item data: {itemData.id}");
        }
    }

    public bool CanMergeWith(SlotData data) => itemData != null 
                                            && itemData.stackable 
                                            && itemData.Match(data.itemData) 
                                            && quantity < itemData.MaxStackSize;
    
    public SlotData MergeWith(SlotData data)
    {
        if (!ValidateItemData())
            return null;

        int totalQuantity = quantity + data.Quantity;

        if (totalQuantity <= itemData.MaxStackSize)
        {
            quantity = totalQuantity;
            return null;
        }

        quantity = itemData.MaxStackSize;
        data.SetQuantity(totalQuantity - itemData.MaxStackSize);
        return data;
    } 

    public SlotData ExtractOne()
    {
        if (quantity <= 0)
            return null;
        
        SlotData slotData = Duplicate() as SlotData;
        slotData.quantity = 1;
        quantity--;

        return slotData;
    }

    public bool IsFull() => itemData != null && quantity >= itemData.MaxStackSize;
    
    private bool ValidateItemData()
    {
        if (itemData is null)
        {
            GD.PushError("[SlotData] Invalid item data resource, check if it's assigned");
            return false;
        }

        return true;
    }

    public Dictionary Serialize()
    {
        if (itemData == null || quantity <= 0) return [];

        return new()
        {
            { "id", itemData.id },
            { "quantity", quantity },  
        };
    }

    public void Deserialize(Dictionary data)
    {
        if (data.Count == 0) return;

        quantity = data["quantity"].AsInt32();
        
        StringName id = data["id"].AsStringName();
        itemData = InventoryManager.Instance.GetItemById(id);
    }
}
