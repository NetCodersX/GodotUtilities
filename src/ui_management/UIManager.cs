using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

namespace Utilities.UI;

public partial class UIManager : Node
{
    [Signal] public delegate void PanelOpenedEventHandler(StringName panelName);
    [Signal] public delegate void PanelClosedEventHandler(StringName panelName);

    public static UIManager Instance { get; private set; }

    private readonly Dictionary<StringName, UIPanel> panels = new();
    private readonly Stack<UIPanel> stack = new();

    public CanvasLayer HudLayer    { get; private set; }
    public CanvasLayer PopupLayer  { get; private set; }
    public CanvasLayer ScreenLayer { get; private set; }

    public override void _EnterTree()
    {
        Instance = this;

        ScreenLayer = new() { Layer = 0, Name = "Screen" }; AddChild(ScreenLayer);
        HudLayer    = new() { Layer = 1, Name = "HUD" };    AddChild(HudLayer);
        PopupLayer  = new() { Layer = 2, Name = "Popup" };  AddChild(PopupLayer);
    }

    public override void _Ready()
    {
        foreach (var (id, _) in HudPanelPaths)
            TryGetPanel(id, out _);
    }

    #region Panel Loading

    private CanvasLayer ChooseLayer(UIPanel.PanelType type) => type switch
    {
        UIPanel.PanelType.HUD    => HudLayer,
        UIPanel.PanelType.Popup  => PopupLayer,
        UIPanel.PanelType.Screen => ScreenLayer,
        _ => throw new System.ArgumentException($"Invalid Panel Type: '{type}'")
    };

    #endregion

    #region Show Panel

    public void ShowPanel(StringName panelName, PanelTransition transition = PanelTransition.Constant) =>
        _ = ShowPanelAsync(panelName, transition);

    public async Task ShowPanelAsync(StringName panelName, PanelTransition transition = PanelTransition.Constant)
    {
        if (!TryGetPanel(panelName, out var panel))
        {
            GD.PushWarning($"[UIManager] ShowPanel: panel '{panelName}' not found.");
            return;
        }

        if (panel.Type == UIPanel.PanelType.Popup)
        {
            GD.PushWarning($"[UIManager] Use ShowPopup() for popup panel '{panelName}'.");
            return;
        }

        if (panel.Type == UIPanel.PanelType.Screen)
        {
            if (stack.Count > 0 && stack.Peek() == panel)
                return;

            if (stack.Count > 0)
                await CloseScreenAsync(stack.Pop(), transition);

            stack.Push(panel);
        }

        await OpenPanelAsync(panel, transition);
    }

    public async void ShowPopup(StringName panelName, float delay = 2f)
    {
        if (!TryGetPanel(panelName, out var popup))
        {
            GD.PushWarning($"[UIManager] ShowPopup: panel '{panelName}' not found.");
            return;
        }

        if (popup.Type != UIPanel.PanelType.Popup)
        {
            GD.PushWarning($"[UIManager] ShowPopup: '{panelName}' is not a Popup panel.");
            return;
        }

        await OpenPanelAsync(popup, PanelTransition.Pop);
        await GetTree().Delay(delay);
        await ClosePanelAsync(popup, PanelTransition.Pop);
    }

    #endregion

    #region Hide Panel

    public void HidePanel(StringName panelName, PanelTransition transition = PanelTransition.Constant) =>
        _ = HidePanelAsync(panelName, transition);

    public async Task HidePanelAsync(StringName panelName, PanelTransition transition = PanelTransition.Constant)
    {
        if (!TryGetPanel(panelName, out var panel))
        {
            GD.PushWarning($"[UIManager] HidePanel: panel '{panelName}' not found.");
            return;
        }

        if (panel.Type == UIPanel.PanelType.Screen)
        {
            await GoBackAsync(transition);
            return;
        }

        if (!panel.Visible) return;

        await ClosePanelAsync(panel, transition);
    }

    #endregion

    #region Toggle Panel

    public void TogglePanel(StringName panelName, PanelTransition transition = PanelTransition.Constant) =>
        _ = TogglePanelAsync(panelName, transition);

    public async Task TogglePanelAsync(StringName panelName, PanelTransition transition = PanelTransition.Constant)
    {
        if (!TryGetPanel(panelName, out var panel))
        {
            GD.PushWarning($"[UIManager] TogglePanel: panel '{panelName}' not found.");
            return;
        }

        if (panel.Type == UIPanel.PanelType.Popup)
        {
            GD.PushWarning($"[UIManager] TogglePanel: use ShowPopup() for popup panel '{panelName}'.");
            return;
        }

        if (!panel.Visible) await ShowPanelAsync(panelName, transition);
        else await HidePanelAsync(panelName, transition);
    }

    #endregion

    #region Go Back

    public void GoBack(PanelTransition transition = PanelTransition.Constant) =>
        _ = GoBackAsync(transition);

    public async Task GoBackAsync(PanelTransition transition = PanelTransition.Constant)
    {
        if (stack.Count == 0) return;

        var current = stack.Pop();
        await CloseScreenAsync(current, transition);

        if (stack.Count > 0)
        {
            var prev = stack.Peek();
            await OpenPanelAsync(prev, transition);
        }
    }

    #endregion

    #region Internal Open / Close Helpers

    private async Task OpenPanelAsync(UIPanel panel, PanelTransition transition)
    {
        panel.Show();
        EmitSignalPanelOpened(panel.Id);
        panel.OnOpen();
        await PanelAnimator.Animate(panel, transition, isOpen: true).WaitToFinish();
    }

    private async Task ClosePanelAsync(UIPanel panel, PanelTransition transition)
    {
        await PanelAnimator.Animate(panel, transition, isOpen: false).WaitToFinish();
        panel.OnClose();
        panel.Hide();
        EmitSignalPanelClosed(panel.Id);
    }

    private Task CloseScreenAsync(UIPanel panel, PanelTransition transition) =>
        ClosePanelAsync(panel, transition);

    #endregion

    #region Utilities

    public UIPanel GetPanel(StringName id) =>
        TryGetPanel(id, out UIPanel panel) ? panel : null;

    public T GetPanel<T>(StringName id) where T : UIPanel =>
        TryGetPanel(id, out UIPanel panel) ? panel as T : null;

    public bool TryGetPanel<T>(StringName id, out T panel) where T : UIPanel
    {
        panel = TryGetPanel(id, out UIPanel value) ? value as T : null;
        return panel != null;
    }

    public bool TryGetPanel(StringName id, out UIPanel panel)
    {
        if (panels.TryGetValue(id, out panel))
            return true;
        
        if (PanelPaths.TryGetValue(id, out string path))
        {
            var scene = ResourceLoader.Load<PackedScene>(path);
            if (scene is not null)
            {
                panel = scene.Instantiate<UIPanel>();
                ChooseLayer(panel.Type).AddChild(panel);

                panel.Id = id;
                panels[id] = panel;
                
                if (panel.Type != UIPanel.PanelType.HUD)
                    panel.Hide();
                panel.OnSetup();

                return true;
            }
            Log.Warn(nameof(UIManager), $"failed to load panel for '{id}' at path '{path}'");
        }
        return false;
    }

    #endregion
}

