using Godot;
using Utilities.UI;

namespace Utilities.InventorySystem;

[GlobalClass]
public partial class ExternalInventoryPanel : UIPanel, IInventoryPanel
{
    [Export] private InventoryUI externalUi;
    [Export] private InventoryUI playerUi;

    public InventoryData Data { get; private set; }

    public void SetInventoryData(InventoryData data)
    {
        Data = data;
    }

    public override void OnOpen()
    {
        playerUi.Attach(InventoryManager.Instance.PlayerInventory);
        externalUi.Attach(Data);
    }

    public override void OnClose()
    {
        Data = null;
    }

}
