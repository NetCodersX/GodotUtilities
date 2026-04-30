using Godot;

namespace Utilities.InventorySystem;

[GlobalClass]
public partial class ExternalInventoryUi : Control
{
    [Export] private InventoryUi externalUi;
    [Export] private InventoryUi playerUi;

    protected InventoryData PlayerInventory   { get; private set; }
    protected InventoryData ExternalInventory { get; private set; }

    public void Open(InventoryData playerInventory, InventoryData data)
    {
        PlayerInventory   = playerInventory;
        ExternalInventory = data;

        externalUi?.Attach(data);
        playerUi?.Attach(playerInventory);

        OnOpen();
    }

    public virtual void OnClose() { }

    // Override only when you need custom behavior
    protected virtual void OnOpen() { }
}

