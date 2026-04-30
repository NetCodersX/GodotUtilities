@tool extends EditorPlugin

const AUTOLOADS: Dictionary = {
	"AudioManager":     "res://addons/utilities/src/audio/AudioManager.cs",
	"Physics2D":        "res://addons/utilities/src/logic/Physics2D.cs",
	"InventoryManager": "res://addons/utilities/src/inventory/scripts/core/manager/InventoryManager.cs",
	"ParticlesManager": "res://addons/utilities/src/particles/ParticlesManager.cs",
	"SaveManager": "res://addons/utilities/src/save_system/SaveManager.cs",
}

const SETTINGS: Dictionary = {
	"godot_utilities/inventory_config_path":   "res://inventory_config.tres",
	"godot_utilities/audio_config_path":       "res://audio_config.tres",
	"godot_utilities/particles_registry_path": "res://particles_registry.tres",
}

const GENERATED_AUDIO_PATH     = "res://addons/utilities/src/audio/AudioManager.Ids.cs"
const GENERATED_PARTICLES_PATH = "res://addons/utilities/src/particles/ParticlesManager.Ids.cs"

const AUDIO_CATEGORIES: Array = [
	{ "field": "sfx",      "enum": "SfxId",      "names": "SfxName",      "lookup": "_sfxNames" },
	{ "field": "music",    "enum": "MusicId",     "names": "MusicName",    "lookup": "_musicNames" },
	{ "field": "ambience", "enum": "AmbienceId",  "names": "AmbienceName", "lookup": "_ambienceNames" },
]

func _enter_tree() -> void:
	_add_settings()
	_add_autoload_singletons()
	ProjectSettings.save()
	add_tool_menu_item("GodotUtilities: Generate IDs", _generate_all)

func _exit_tree() -> void:
	remove_tool_menu_item("GodotUtilities: Generate IDs")
	_remove_autoload_singletons()
	ProjectSettings.save()
	print("GodotUtilities: plugin disabled. Generated ID files remain intact.")

func _generate_all() -> void:
	var audio_path     = ProjectSettings.get_setting("godot_utilities/audio_config_path")
	var particles_path = ProjectSettings.get_setting("godot_utilities/particles_registry_path")

	var audio     = load(audio_path)
	var particles = load(particles_path)

	var generated_count = 0

	if audio:
		generate_audio_ids(audio)
		generated_count += 1
	else:
		push_warning("IdGenerator: could not load AudioConfig at '%s'" % audio_path)

	if particles:
		generate_particle_ids(particles)
		generated_count += 1
	else:
		push_warning("IdGenerator: could not load ParticlesRegistry at '%s'" % particles_path)

	if generated_count == 0:
		push_error("IdGenerator: no files were generated — check your config paths in Project Settings.")
	else:
		print("IdGenerator: done (%d file(s) generated)." % generated_count)

func generate_audio_ids(config: Resource) -> void:
	var lines: Array[String] = []
	_write_header(lines, "Utilities.AudioManagement", "AudioManager")

	for cat in AUDIO_CATEGORIES:
		var keys = config.get(cat.field).keys()
		_write_enum(lines, cat.enum, keys)

	for cat in AUDIO_CATEGORIES:
		var keys = config.get(cat.field).keys()
		_write_string_name_class(lines, cat.names, keys)

	for cat in AUDIO_CATEGORIES:
		var keys = config.get(cat.field).keys()
		_write_lookup_array(lines, cat.lookup, keys, cat.names)

	_write_audio_overloads(lines)
	lines.append("}")

	_write_file(GENERATED_AUDIO_PATH, lines)

func generate_particle_ids(registry: Resource) -> void:
	var keys = registry.scenes.keys()

	var lines: Array[String] = []
	_write_header(lines, "Utilities.ParticlesManagement", "ParticlesManager")
	_write_enum(lines, "ParticleId", keys)
	_write_string_name_class(lines, "ParticleName", keys)
	_write_lookup_array(lines, "_particleNames", keys, "ParticleName")
	lines.append("\tpublic PooledParticle SpawnById(ParticleId id, Vector2 pos) { if (id == ParticleId.None) return null; return Spawn(_particleNames[(int)id], pos); }")
	lines.append("}")

	_write_file(GENERATED_PARTICLES_PATH, lines)

func _write_header(lines: Array, ns: String, cn: String) -> void:
	lines.append("// AUTO-GENERATED — do not edit manually")
	lines.append("using Godot;")
	lines.append("")
	lines.append("namespace %s;" % ns)
	lines.append("")
	lines.append("public partial class %s" % cn)
	lines.append("{")

func _write_enum(lines: Array, enum_name: String, keys: Array) -> void:
	var pascal_keys = keys.map(func(k): return _to_pascal_case(str(k)))
	lines.append("\tpublic enum %s { None = -1, %s }" % [enum_name, ", ".join(pascal_keys)])
	lines.append("")

func _write_string_name_class(lines: Array, c_name: String, keys: Array) -> void:
	lines.append("\tpublic static class %s" % c_name)
	lines.append("\t{")
	for key in keys:
		lines.append('\t\tpublic static readonly StringName %s = "%s";' % [_to_pascal_case(str(key)), str(key)])
	lines.append("\t}")
	lines.append("")

func _write_lookup_array(lines: Array, field_name: String, keys: Array, c_name: String) -> void:
	var entries = keys.map(func(k): return "%s.%s" % [c_name, _to_pascal_case(str(k))])
	lines.append("\tprivate static readonly StringName[] %s = { %s };" % [field_name, ", ".join(entries)])
	lines.append("")

func _write_audio_overloads(lines: Array) -> void:
	# SFX
	lines.append("\tpublic AudioStreamPlayer   PlaySfxWithId(SfxId id, float pitchVariance = 0f)")
	lines.append("\t\t{ if (id == SfxId.None) return null; return PlaySfx(_sfxNames[(int)id], pitchVariance); }")
	lines.append("\tpublic AudioStreamPlayer2D PlaySfx2DWithId(SfxId id, Vector2 pos, float pitchVariance = 0f)")
	lines.append("\t\t{ if (id == SfxId.None) return null; return PlaySfx2D(_sfxNames[(int)id], pos, pitchVariance); }")
	# Music
	lines.append("\tpublic void PlayMusicWithId(MusicId id, float fadeDuration = 1.5f)")
	lines.append("\t\t{ if (id == MusicId.None) return; PlayMusic(_musicNames[(int)id], fadeDuration); }")
	lines.append("\tpublic void ThenPlayMusicWithId(MusicId id, float fadeDuration = 1.5f)")
	lines.append("\t\t{ if (id == MusicId.None) return; ThenPlayMusic(_musicNames[(int)id], fadeDuration); }")
	# Ambience
	lines.append("\tpublic void PlayAmbienceWithId(AmbienceId id, float fadeDuration = 3f)")
	lines.append("\t\t{ if (id == AmbienceId.None) return; PlayAmbience(_ambienceNames[(int)id], fadeDuration); }")
	lines.append("\tpublic void ThenPlayAmbienceWithId(AmbienceId id, float fadeDuration = 3f)")
	lines.append("\t\t{ if (id == AmbienceId.None) return; ThenPlayAmbience(_ambienceNames[(int)id], fadeDuration); }")
	lines.append("")

func _write_file(path: String, lines: Array) -> void:
	var file = FileAccess.open(path, FileAccess.WRITE)
	if file == null:
		push_error("IdGenerator: could not write to '%s'" % path)
		return
	file.store_string("\n".join(lines))
	file.close()
	print("IdGenerator: generated '%s'" % path)

func _add_settings() -> void:
	for setting: String in SETTINGS.keys():
		if ProjectSettings.has_setting(setting):
			continue
		ProjectSettings.set_setting(setting, SETTINGS[setting])
		# Expose as a typed file-path field in the editor UI
		ProjectSettings.add_property_info({
			"name":  setting,
			"type":  TYPE_STRING,
			"hint":  PROPERTY_HINT_FILE,
			"hint_string": "*.tres",
		})

func _add_autoload_singletons() -> void:
	for name: String in AUTOLOADS.keys():
		if not ProjectSettings.has_setting("autoload/" + name):
			add_autoload_singleton(name, AUTOLOADS[name])

func _remove_autoload_singletons() -> void:
	for name: String in AUTOLOADS.keys():
		if ProjectSettings.has_setting("autoload/" + name):
			remove_autoload_singleton(name)

func _to_pascal_case(s: String) -> String:
	var result = ""
	for word in s.split("_"):
		if word.is_empty():
			continue
		result += word[0].to_upper() + word.substr(1)
	return result
