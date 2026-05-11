using Godot;

namespace Utilities.UI;

[GlobalClass]
public partial class UIPanel : Control
{
    public enum PanelType
    {
        HUD,
        Popup,
        Screen,
    }

    [Export] public PanelType Type { get; private set; } = PanelType.Screen;

    public StringName Id { get; set; }
    public Vector2? DefaultPos { get; set; }

    public virtual void OnOpen()  { }
    public virtual void OnClose() { }
    public virtual void OnSetup() { }
}
