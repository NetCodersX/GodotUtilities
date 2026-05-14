// AUTO-GENERATED — do not edit manually
using Godot;

namespace Utilities.UI;

public partial class UIManager
{
	/// <summary>
	/// Auto-generated StringName constants. Use these instead of raw strings to avoid typos and benefit from IDE autocomplete.
	/// </summary>
	public static class PanelName
	{
		/// <summary>
		/// Refers to <c>hotbar_panel</c>.
		/// </summary>
		public static readonly StringName HotbarPanel = "hotbar_panel";
		/// <summary>
		/// Refers to <c>inventory_panel</c>.
		/// </summary>
		public static readonly StringName InventoryPanel = "inventory_panel";
	}

	/// <summary>
	/// Comma-separated list of all names. Intended for use with <c>[Export(PropertyHint.Enum, PANEL_NAMES)]</c>.
	/// </summary>
	public const string PANEL_NAMES = "hotbar_panel,inventory_panel";

	/// <summary>
	/// Maps each PanelName constant to its resource path. Used internally to load scenes on demand.
	/// </summary>
	public static readonly Godot.Collections.Dictionary<StringName, string> PanelPaths = new()
	{
		[PanelName.InventoryPanel] = "res://scenes/ui_panels/inventory_panel.tscn",
		[PanelName.HotbarPanel] = "res://scenes/hud_panels/hotbar_panel.tscn",
	};

	/// <summary>
	/// Maps each PanelName constant to its resource path. Used internally to load scenes on demand.
	/// </summary>
	public static readonly Godot.Collections.Dictionary<StringName, string> HudPanelPaths = new()
	{
		[PanelName.HotbarPanel] = "res://scenes/hud_panels/hotbar_panel.tscn",
	};

}