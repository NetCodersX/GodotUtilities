@tool
extends EditorPlugin

const AUTOLOADS: Dictionary = {
	"SaveManager": "res://addons/GodotUtilities/src/save_system/SaveManager.cs",
	"AudioManager": "res://addons/GodotUtilities/src/audio/AudioManager.cs",
	"ParticlesManager": "res://addons/GodotUtilities/src/particles/ParticlesManager.cs",
	"UIManager": "res://addons/GodotUtilities/src/ui_management/UIManager.cs",
	"InventoryManager": "res://addons/GodotUtilities/src/inventory/core/manager/InventoryManager.cs",
	"Physics2D": "res://addons/GodotUtilities/src/logic/Physics2D.cs",
}

const SETTINGS: Dictionary = {
	"godot_utilities/inventory_config_path": "",
	
	"godot_utilities/sfx_folder": "",
	"godot_utilities/music_folder": "",
	"godot_utilities/ambience_folder": "",
	"godot_utilities/particles_folder": "",
	
	"godot_utilities/ui_panels_folder": "",
	"godot_utilities/ui_hud_panels_folder": "",
	
	"godot_utilities/audio_sfx_pool_size": 4,
	"godot_utilities/audio_sfx_pool_trim_cooldown": 5.0,
}

const ID_GENERATOR = preload("res://addons/GodotUtilities/generator/id_generator.gd")

func _enter_tree() -> void:
	_add_settings()
	ProjectSettings.save()
	ID_GENERATOR.generate_all()

	add_tool_menu_item("GodotUtilities: Generate IDs", _on_generate_all)
	add_tool_menu_item("GodotUtilities: Add Autoloads", _add_autoload_singletons)

func _exit_tree() -> void:
	_remove_settings()

	remove_tool_menu_item("GodotUtilities: Generate IDs")
	remove_tool_menu_item("GodotUtilities: Add Autoloads")

	ProjectSettings.save()
	print("GodotUtilities: plugin disabled. Generated ID files remain intact.")

func _on_generate_all() -> void:
	ID_GENERATOR.generate_all()

func _add_settings() -> void:
	for setting: String in SETTINGS.keys():
		if ProjectSettings.has_setting(setting):
			continue

		var value = SETTINGS[setting]
		var type := typeof(value)

		ProjectSettings.set_setting(setting, value)

		var hint := PROPERTY_HINT_NONE
		var hint_string := ""

		if setting.ends_with("_folder"):
			hint = PROPERTY_HINT_DIR

		elif setting.ends_with("_path"):
			hint = PROPERTY_HINT_FILE
			hint_string = "*.tres"

		elif type in [TYPE_INT, TYPE_FLOAT, TYPE_BOOL]:
			hint = PROPERTY_HINT_NONE
			hint_string = ""

		ProjectSettings.add_property_info({
			"name": setting,
			"type": type,
			"hint": hint,
			"hint_string": hint_string,
		})

func _remove_settings() -> void:
	ProjectSettings.set_setting("godot_utilities", null)

	for setting: String in SETTINGS.keys():
		if ProjectSettings.has_setting(setting):
			ProjectSettings.set_setting(setting, null)

func _add_autoload_singletons() -> void:
	for name: String in AUTOLOADS.keys():
		if not ProjectSettings.has_setting("autoload/" + name):
			add_autoload_singleton(name, AUTOLOADS[name])


func _remove_autoload_singletons() -> void:
	for name: String in AUTOLOADS.keys():
		if ProjectSettings.has_setting("autoload/" + name):
			remove_autoload_singleton(name)
