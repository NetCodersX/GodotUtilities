using Godot;

namespace Utilities.InventorySystem;

public partial class SlotUI : Panel
{
    [Signal] public delegate void ClickedEventHandler(int index, MouseButton button);

    [Export] private TextureRect itemRect;
    [Export] private Label quantityLabel;

    public virtual void OnSelect() => Scale = Vector2.One * 1.1f;
    public virtual void OnDeselect() => Scale = Vector2.One;

    public virtual void SetData(SlotData data)
    {
        if (data is null)
        {
            itemRect.Texture = null;
            quantityLabel.Visible = false;
            return;
        }
        
        itemRect.Texture = data.ItemData.Art;
        quantityLabel.Text = "x" + data.Quantity.ToString();
        quantityLabel.Visible = data.ItemData.Stackable;
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButton && mouseButton.Pressed)
            EmitSignalClicked(GetIndex(), mouseButton.ButtonIndex);
    }
}
