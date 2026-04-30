using Godot;

namespace Utilities.InventorySystem;

public partial class SlotUi : Panel
{
    [Signal]
    public delegate void ClickedEventHandler(int index, MouseButton button);

    [Export] private TextureRect itemRect;
    [Export] private Label quantityLabel;

    private const float SCALE_MULTIPLIER = 1.1f;

    private Tween tween;

    public override void _Ready()
    {
        MouseEntered += OnMouseEntered;
        MouseExited  += OnMouseExited;
    }

    public void SetData(SlotData data)
    {
        if (data == null)
        {
            itemRect.Texture      = null;
            quantityLabel.Visible = false;
            return;
        }
        
        itemRect.Texture      = data.itemData.art;
        quantityLabel.Text    = "x" + data.Quantity.ToString();
        quantityLabel.Visible = data.itemData.stackable;
    }

    public void Select()   => Scale = Vector2.One * SCALE_MULTIPLIER;
    public void Deselect() => Scale = Vector2.One;

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButton && mouseButton.Pressed)
            EmitSignalClicked(GetIndex(), mouseButton.ButtonIndex);
    }

    private void OnMouseEntered()
    {
        tween.KillIfValid();

        tween = CreateTween();
        tween.TweenScale(this, Vector2.One * 1.2f, 0.1f);
        tween.TweenWiggle(this, 5f, 0.3f);
    }

    private void OnMouseExited()
    {
        tween.KillIfValid();
        tween = CreateTween();
        tween.TweenScale(this, Vector2.One, 0.1f);
        tween.TweenRotationDeg(this, 0f, 0.1f);
    }
}
