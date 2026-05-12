@tool extends RefCounted

const GENERATED_AUDIO_PATH     = "res://addons/utilities/generated/AudioManager.gen.cs"
const GENERATED_PARTICLES_PATH = "res://addons/utilities/generated/ParticlesManager.gen.cs"
const GENERATED_UI_PATH        = "res://addons/utilities/generated/UIManager.gen.cs"

const AUDIO_CATEGORIES: Array = [
	{ "setting": "godot_utilities/sfx_folder",      "names": "SfxName"      },
	{ "setting": "godot_utilities/music_folder",    "names": "MusicName"    },
	{ "setting": "godot_utilities/ambience_folder", "names": "AmbienceName" },
]

const ID_WRITER = preload("res://addons/utilities/generator/id_writer.gd")

static func generate_all() -> void:
	var generated_count = 0

	if _generate_audio():
		generated_count += 1

	if _generate_particles():
		generated_count += 1

	if _generate_ui():
		generated_count += 1

	if generated_count == 0:
		push_error("IdGenerator: no files were generated — check your folder paths in Project Settings.")
	else:
		print("IdGenerator: done (%d file(s) generated)." % generated_count)

static func _generate_audio() -> bool:
	var lines: Array[String] = []
	ID_WRITER.write_header(lines, "Utilities.AudioManagement", "AudioManager")

	for cat in AUDIO_CATEGORIES:
		var folder = ProjectSettings.get_setting(cat.setting, "")
		var mapping = scan_folder(folder)   # Dictionary<StringName, string> (basename -> full path)
		var keys    = mapping.keys()
		keys.sort()
		ID_WRITER.write_string_name_class(lines, cat.names, keys)
		ID_WRITER.write_names_const(lines, cat.names.trim_suffix("Name") + "Names", keys)
		# Add the paths dictionary (SfxPaths, MusicPaths, AmbiencePaths)
		var dict_name = cat.names.trim_suffix("Name") + "Paths"
		ID_WRITER.write_paths_dictionary(lines, dict_name, mapping)

	lines.append("}")
	ID_WRITER.write_file(GENERATED_AUDIO_PATH, lines)
	return true

static func _generate_particles() -> bool:
	var folder = ProjectSettings.get_setting("godot_utilities/particles_folder", "")
	var mapping = scan_folder(folder)
	var keys    = mapping.keys()
	keys.sort()

	var lines: Array[String] = []
	ID_WRITER.write_header(lines, "Utilities.ParticlesManagement", "ParticlesManager")
	ID_WRITER.write_string_name_class(lines, "ParticleName", keys)
	ID_WRITER.write_names_const(lines, "ParticleNames", keys)
	ID_WRITER.write_paths_dictionary(lines, "ParticlePaths", mapping)
	lines.append("}")

	ID_WRITER.write_file(GENERATED_PARTICLES_PATH, lines)
	return true

static func _generate_ui() -> bool:
	var folder = ProjectSettings.get_setting("godot_utilities/ui_panels_folder", "")
	var mapping = scan_folder(folder)
	var keys    = mapping.keys()
	keys.sort()

	var lines: Array[String] = []
	ID_WRITER.write_header(lines, "Utilities.UI", "UIManager")
	ID_WRITER.write_string_name_class(lines, "PanelName", keys)
	ID_WRITER.write_names_const(lines, "PanelNames", keys)
	# Optional: add path dictionary for UI panels as well – uncomment if you want to use it
	ID_WRITER.write_paths_dictionary(lines, "PanelPaths", mapping)
	lines.append("}")

	ID_WRITER.write_file(GENERATED_UI_PATH, lines)
	return true

# Returns a dictionary: basename (String) -> full "res://" path (String)
static func scan_folder(path: String) -> Dictionary:
	var dict := {}
	if path.is_empty():
		push_warning("IdGenerator: folder path is empty — skipping.")
		return dict

	var dir = DirAccess.open(path)
	if dir == null:
		push_warning("IdGenerator: could not open folder '%s' — skipping." % path)
		return dict

	dir.list_dir_begin()
	var file = dir.get_next()
	while file != "":
		if not dir.current_is_dir() and not file.begins_with(".") and not file.ends_with(".import"):
			var base = file.get_basename()
			var full = path.path_join(file)
			if dict.has(base):
				push_warning("IdGenerator: duplicate basename '%s' in folder '%s'. Using first found." % [base, path])
			else:
				dict[base] = full
		file = dir.get_next()
	dir.list_dir_end()
	return dict
