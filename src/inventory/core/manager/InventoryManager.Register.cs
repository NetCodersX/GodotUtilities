using Godot;

namespace Utilities.InventorySystem;

public partial class InventoryManager
{
    public void RegisterHotbarUI(InventoryUI ui)
    {
        hotbarUI = ui;
        UpdateHotbar(0);
    }

    private void InitConfig()
    {
        string path = ProjectSettings.GetSetting("godot_utilities/inventory_config_path").AsString();

        config = ResourceLoader.Load<InventoryConfig>(path) ?? 
            throw new System.Exception("Invalid Config Path. Check GodotUtilities Inventory Config Path in Project Settings");
    }
    
    #region Hotbar Handling

    public void UpdateHotbarDirection(int dir)
    {
        int value = (currentIndex + dir + HotbarData.Capacity) % HotbarData.Capacity;
        UpdateHotbar(value);
    } 

    public void UpdateHotbar(int index)
    {
        if (hotbarUI is null) return;
        if (index < 0 || index > HotbarData.Capacity - 1) return;

        hotbarUI.GetSlotUI(currentIndex)?.OnDeselect();
        currentIndex = index;
        hotbarUI.GetSlotUI(currentIndex)?.OnSelect();

        EmitSignalHotbarUpdated(HotbarData.GetSlotData(currentIndex));
    }

    #endregion

    #region Serialize & Deserialize

    public static ItemData GetItem(StringName id)
    {
        if (itemCache is null) return null;
        return itemCache.TryGetValue(id, out var result) ? result : null;
    }

    public void OnDeserialize(Godot.Collections.Dictionary data)
    {
        UpdateHotbar(0);
        UpdateItemPreview();
    }

    #endregion
}

