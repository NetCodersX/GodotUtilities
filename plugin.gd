@tool extends EditorPlugin

const AUTOLOADS: Dictionary = {
	"SaveManager": "res://addons/utilities/src/save_system/SaveManager.cs",
	"AudioManager": "res://addons/utilities/src/audio/AudioManager.cs",
	"ParticlesManager":"res://addons/utilities/src/particles/ParticlesManager.cs",
	"UIManager": "res://addons/utilities/src/ui_management/UIManager.cs",
	"InventoryManager":"res://addons/utilities/src/inventory/core/manager/InventoryManager.cs",
	"Physics2D": "res://addons/utilities/src/logic/Physics2D.cs",
}

const SETTINGS: Dictionary = {
	"godot_utilities/inventory_config_path": "res://resources/inventory_config.tres",
	"godot_utilities/audio_config_path": "res://resources/audio_config.tres",

	"godot_utilities/sfx_folder":            "res://assets/audio/sfx",
	"godot_utilities/music_folder":          "res://assets/audio/music",
	"godot_utilities/ambience_folder":       "res://assets/audio/ambience",
	"godot_utilities/particles_folder":      "res://scenes/particles",
	"godot_utilities/ui_panels_folder":      "res://scenes/ui/panels",
}

const ID_GENERATOR = preload("res://addons/utilities/generator/id_generator.gd")

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
		ProjectSettings.set_setting(setting, SETTINGS[setting])
		ProjectSettings.add_property_info({
			"name": setting,
			"type": TYPE_STRING,
			"hint": PROPERTY_HINT_DIR if setting.ends_with("_folder") else PROPERTY_HINT_FILE,
			"hint_string": "" if setting.ends_with("_folder") else "*.tres",
		})

func _remove_settings() -> void:
	ProjectSettings.set_setting("godot_utilities", null)
	
	for setting: String in SETTINGS.keys():
		if not ProjectSettings.has_setting(setting):
			continue
		ProjectSettings.set_setting(setting, null)

func _add_autoload_singletons() -> void:
	for name: String in AUTOLOADS.keys():
		if not ProjectSettings.has_setting("autoload/" + name):
			add_autoload_singleton(name, AUTOLOADS[name])

func _remove_autoload_singletons() -> void:
	for name: String in AUTOLOADS.keys():
		if ProjectSettings.has_setting("autoload/" + name):
			remove_autoload_singleton(name)
