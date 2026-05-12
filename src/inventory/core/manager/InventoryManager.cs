using Godot;
using Utilities.UI;
using Utilities.Persistence;
using Utilities.InventorySystem.Interaction;

namespace Utilities.InventorySystem;

public interface IInventoryPanel;

public partial class InventoryManager : Node, ISaveable
{
    [Signal] public delegate void HotbarUpdatedEventHandler(SlotData data);
    [Signal] public delegate void DropZoneRequestedEventHandler(SlotData data);

    [Save] public InventoryData PlayerInventory { get; private set; }
    [Save] public InventoryData HotbarData { get; private set; }

    public static InventoryManager Instance { get; private set; }
    public InventoryData ExternalData { get; private set; }

    private static Godot.Collections.Dictionary<StringName, ItemData> itemCache;

    private InventoryConfig config;
    private InteractionHandler interactionHandler;

    private Control dropZone;
    private InventoryUI hotbarUI;

    private SlotData heldItem;
    private ItemPreview itemPreview;
    private Timer stackTimer;

    private int currentIndex;

    public string SaveKey => "inventory"; 

    public override void _EnterTree()
    {
        Instance = this;
        SaveManager.Instance.Register(this);

        InitConfig();
        itemCache = config.GetItemCache();
    }

    public override void _ExitTree()
    {
        SaveManager.Instance.Unregister(this);
    }

    public override async void _Ready()
    {
        InitHotbar();

        stackTimer = config.CreateStackTimer(this);
        itemPreview = config.CreateItemPreview(UIManager.Instance.HudLayer);
        dropZone = config.CreateDropZone(UIManager.Instance.ScreenLayer);

        interactionHandler = new InteractionHandler(this, stackTimer);

        PlayerInventory = config.PlayerInventory;
        PlayerInventory.InventoryInteract += OnInventoryInteract;

        dropZone.GuiInput += OnDropZoneInput;
        UIManager.Instance.PanelOpened += OnPanelOpened;
        UIManager.Instance.PanelClosed += OnPanelClosed;

        // Wait until Godot draws UI
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        UpdateHotbar(0);
    }

    public override void _Input(InputEvent @event)
    {        
        interactionHandler.OnInputEvent(@event);
    }

    #region Utilities

    public bool TryStoreItem(SlotData item)
    {
        return HotbarData.TryStoreItem(item) || PlayerInventory.TryStoreItem(item);
    }

    #endregion

    #region Inventory Interact

    private void OnInventoryInteract(InventoryData data, int slotIndex, MouseButton button)
    {
        heldItem = interactionHandler.Interact(heldItem, data, slotIndex, button);
        UpdateItemPreview();
    }

    private void UpdateItemPreview()
    {
        itemPreview?.UpdateState(heldItem);
    }

    #endregion

    #region On Panel Open/Close

    private void OnPanelOpened(StringName panelName)
    {
        var panel = UIManager.Instance.GetPanel(panelName);
        if (panel is not IInventoryPanel) return;

        dropZone.Show();

        // Can access hotbar when inventory is open
        HotbarData.InventoryInteract += OnInventoryInteract;

        if (panel is ExternalInventoryPanel external)
        {
            if (external.Data is null)
            {
                Log.Error(nameof(InventoryManager), "ExternalInventoryPanel opened without data assigned. Call SetInventoryData before opening.");
                return;
            }

            ExternalData = external.Data;
            ExternalData.InventoryInteract += OnInventoryInteract;
        }
    }

    private void OnPanelClosed(StringName panelName)
    {
        if (UIManager.Instance.GetPanel(panelName) is not IInventoryPanel) return;

        dropZone.Hide();
        ClearHeldItem();

        if (ExternalData is not null)
        {
            ExternalData.InventoryInteract -= OnInventoryInteract;
            ExternalData = null;
        }

        // Can not access hotbar when inventory is closed
        HotbarData.InventoryInteract -= OnInventoryInteract;
    }

    #endregion
}
