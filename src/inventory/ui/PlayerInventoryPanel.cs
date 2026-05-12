using Godot;
using Utilities.UI;

namespace Utilities.InventorySystem;

[GlobalClass]
public partial class PlayerInventoryPanel : UIPanel, IInventoryPanel
{
    [Export] private InventoryData inventoryData;
    [Export] private InventoryUI inventoryUi;

    public override void OnSetup()
    {
        inventoryUi.Attach(inventoryData);
    }
}
