using Utilities.Events;
using System;
using Godot;
using System.Collections.Generic;
using Utilities.Persistence;

namespace Utilities.InventorySystem;

public partial class InventoryManager : CanvasLayer, ISaveable
{
    public record HotbarUpdated(SlotData Data);
    public record HeldItemDropRequested(SlotData Data);

    public static InventoryManager Instance { get; private set; }

    public string SaveKey => "inventory_manager";

    private readonly Dictionary<StringName, ExternalInventoryUi> externalInventories = new();
    private readonly Godot.Collections.Dictionary<StringName, ItemData> itemCache = new();

    private InventoryConfig config;
    private Control         inventoryHolder;

    private InventoryUi         hotbarUi;
    private InventoryUi         playerInventoryUi;
    private ExternalInventoryUi externalInventoryUi;

    private InventoryData playerInventory;
    private InventoryData externalInventory;
    private InventoryData hotbar;

    private SlotData heldItem;
    private SlotUi   itemPreview;
    private Timer    gatherWindowTimer;

    private int  currentIndex;
    private bool isHoldingShift;
    
    public override async void _Ready()
    {
        Instance = this;
        
        SetupConfig();
        BuildItemCache();
        AddToGroup(SaveManager.SaveGroup);

        gatherWindowTimer = new Timer() { WaitTime = 0.15, OneShot = true };
        AddChild(gatherWindowTimer);

        Node currentScene         = GetTree().CurrentScene;
        inventoryHolder           = new Control { Name = "InventoryHolder", Visible = false };
        inventoryHolder.GuiInput += OnInventoryHolderGuiInput;

        AddChild(inventoryHolder);
        inventoryHolder.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        RegisterDefault();
        RegisterHotbar();
        RegisterExternals();
        InitializeItemPreview();

        // godot Ui nodes finalize layout after the first frame,
        // so hotbar selection must be deferred.
        //GetTree().NextFrame(UpdateHotbar);

        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        UpdateHotbar();
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey eventKey && eventKey.Keycode == Key.Shift)
            isHoldingShift = eventKey.Pressed;
        
        if (@event is InputEventMouseMotion mouseMotion)
            itemPreview.GlobalPosition = heldItem != null ? mouseMotion.Position : Vector2.Zero;
    }

    #region Inventory Interact

    private void OnInventoryInteract(InventoryData data, int slotIndex, MouseButton button)
    {
        heldItem = (heldItem, button) switch
        {
            (null, MouseButton.Left)  => TakeOrTransferItem(data, slotIndex),
            (null, MouseButton.Right) => data.Split(slotIndex),

            (_, MouseButton.Left)     => StackMergeOrPlace(data, slotIndex),
            (_, MouseButton.Right)    => data.PlaceOne(heldItem, slotIndex),

            (_, _)                    => heldItem,
        };

        UpdateItemPreview();
    }

    private SlotData TakeOrTransferItem(InventoryData data, int slotIndex)
    {
        if (isHoldingShift)
        {
            if      (data == hotbar)            data.TransferItemTo(slotIndex, externalInventory ?? playerInventory);
            else if (data == playerInventory)   data.TransferItemTo(slotIndex, externalInventory ?? hotbar);
            else if (data == externalInventory) data.TransferItemTo(slotIndex, hotbar);

            return null;
        }

        gatherWindowTimer.Start();
        return data.Take(slotIndex);
    }

    private SlotData StackMergeOrPlace(InventoryData data, int slotIndex)
    {
        if (gatherWindowTimer.IsStopped())
            return data.Place(heldItem, slotIndex);

        if (externalInventory != null) InventoryData.StackSimilar(heldItem, externalInventory, playerInventory, hotbar);
        else                           InventoryData.StackSimilar(heldItem, playerInventory, hotbar);
        return heldItem;
    }

    public void UpdateItemPreview()
    {
        if (itemPreview is null)
            return;
        
        itemPreview.SetData(heldItem);
        itemPreview.Visible = heldItem != null;
        itemPreview.GlobalPosition = heldItem != null ? itemPreview.GetGlobalMousePosition() : Vector2.Zero;
    }

    #endregion

    #region Hotbar Handling

    private void UpdateHotbar() => UpdateHotbar(0);

    public void UpdateHotbarDirection(int dir)
    {
        int value = (currentIndex + dir + hotbar.Size) % hotbar.Size;
        UpdateHotbar(value);
    } 

    public void UpdateHotbar(int index)
    {
        if (index < 0 || index > hotbar.Size - 1)
            return;

        int max   = hotbar.Size - 1;
        int value = index;

        if (value > max)    value = 0;
        else if (value < 0) value = max;

        hotbarUi.GetSlot(currentIndex).Deselect();
        currentIndex = value;
        hotbarUi.GetSlot(currentIndex).Select();

        TriggerHorbarUpdatedEvent(hotbar);
    }

    private void TriggerHorbarUpdatedEvent(InventoryData data)
    {
        SlotData currentData = data.GetSlotData(currentIndex);
        EventBus.Trigger(new HotbarUpdated(currentData));
    }

    #endregion

    #region Utilities

    public void ToggleInventory()
    {
        if (inventoryHolder.Visible) Close();
        else                         Open();
    }

    public bool TryStore(SlotData data) => hotbar.TryStoreItem(data) || playerInventory.TryStoreItem(data);
    
    public void Open()
    {
        playerInventoryUi.Show();
        inventoryHolder.Show();
    }

    public void OpenExternal(StringName id, InventoryData data)
    {
        playerInventoryUi.Hide();
        Register(id, data);
        inventoryHolder.Show();
    }

    public void Close()
    {
        externalInventoryUi?.Hide();
        externalInventoryUi = null;
        externalInventory   = null;

        playerInventoryUi.Show();
        inventoryHolder.Hide();
    }

    #endregion

    private void SetupConfig()
    {
        string path = ProjectSettings.GetSetting("godot_utilities/inventory_config_path").AsString();

        config = GD.Load<InventoryConfig>(path) ?? 
            throw new Exception("Invalid Config Path. Check GodotUtilities Inventory Config Path in Project Settings");
    }

    #region Serialize & Deserialize

    private void BuildItemCache()
    {
        foreach (var item in config.itemDatabase)
            itemCache[item.id] = item;
    }

    public ItemData GetItemById(StringName id)
    {
        return itemCache.TryGetValue(id, out var item) ? item : null;
    }

    public void OnSerialize(Godot.Collections.Dictionary data)
    {
        data["player_inventory"] = playerInventory.Serialize();
        data["hotbar"] = hotbar.Serialize();
    }

    public void OnDeserialize(Godot.Collections.Dictionary data)
    {
        if (data.TryGetValue("player_inventory", out var playerInv))
            playerInventory.Deserialize(playerInv.AsGodotArray(), GetItemById);
        
        if (data.TryGetValue("hotbar", out var hbData))
            hotbar.Deserialize(hbData.AsGodotArray(), GetItemById);
        
        UpdateHotbar();
        UpdateItemPreview();
    }

    #endregion

}
