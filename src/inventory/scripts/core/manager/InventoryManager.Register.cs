using Godot;

namespace Utilities.InventorySystem;

public partial class InventoryManager
{
    #region Inventory Register

    private void Register(string id, InventoryData data)
    {
        externalInventoryUi?.OnClose();
        externalInventoryUi?.Hide();

        if (externalInventory != null)
            externalInventory.InventoryInteract -= OnInventoryInteract;

        externalInventory = data;
        externalInventory.InventoryInteract += OnInventoryInteract;

        var external = externalInventories[id];

        external.Show();
        external.Open(playerInventory, externalInventory);

        externalInventoryUi = external;
    }

    private void RegisterDefault()
    {
        playerInventory   = config.defaultInventory;
        playerInventoryUi = config.defaultInventoryScene.Instantiate<InventoryUi>();

        inventoryHolder.AddChild(playerInventoryUi);
        playerInventoryUi.Attach(playerInventory);

        playerInventory.InventoryInteract += OnInventoryInteract;
    }

    private void RegisterHotbar()
    {
        hotbar   = config.hotbarData;
        hotbarUi = config.hotbarScene.Instantiate<InventoryUi>();

        AddChild(hotbarUi);
        hotbarUi.Attach(hotbar);
        
        hotbar.InventoryInteract += OnInventoryInteract;
        hotbar.InventoryUpdated  += TriggerHorbarUpdatedEvent;
    }
    
    #endregion

    #region ItemPreview Initialization

    private void InitializeItemPreview()
    {
        itemPreview = config.slotUiScene.Instantiate<SlotUi>();
        AddChild(itemPreview);

        itemPreview.Name                   = "ItemPreview";
        itemPreview.SelfModulate           = Colors.Transparent;
        itemPreview.MouseFilter            = Control.MouseFilterEnum.Ignore;
        itemPreview.MouseBehaviorRecursive = Control.MouseBehaviorRecursiveEnum.Disabled;

        itemPreview.Hide();
    }

    #endregion

    private void RegisterExternals()
    {
        if (config.externals.Count == 0) return;

        foreach (var kvp in config.externals)
        {
            var external = kvp.Value.Instantiate<ExternalInventoryUi>();
            inventoryHolder.AddChildDeferred(external);

            externalInventories[kvp.Key] = external;
            external.Hide();
        }
    }
}

