using Godot;
using Utilities.UI;

namespace Utilities.InventorySystem;

[GlobalClass]
public partial class ExternalInventoryPanel : UIPanel, IInventoryPanel
{
    [Export] private InventoryUI externalUi;
    [Export] private InventoryUI playerUi;

    public InventoryData Data { get; private set; }

    public void SetInventoryData(InventoryData data) => Data = data;

    public void Attach(InventoryData player, InventoryData external)
    {
        playerUi.Attach(player);
        externalUi.Attach(external);
    }
}
