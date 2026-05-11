using Godot;

namespace Utilities.InventorySystem;

[GlobalClass]
public partial class ItemPreview : TextureRect
{
    [Export] private Label quantityLabel;

    public void UpdateState(SlotData slotData)
    {
        if (slotData is null)
        {
            Hide();
            return;
        }

        Show();
        Texture = slotData.ItemData.Art;
        GlobalPosition = GetGlobalMousePosition();

        quantityLabel.Show();
        quantityLabel.Text = slotData.Quantity == 1 ? string.Empty : $"x{slotData.Quantity}";
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventMouseMotion mouseMotion) return;
        GlobalPosition = mouseMotion.Position;
    }
}
