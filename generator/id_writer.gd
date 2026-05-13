@tool extends RefCounted

static func write_header(lines: Array, ns: String, cn: String) -> void:
	lines.append("// AUTO-GENERATED — do not edit manually")
	lines.append("using Godot;")
	lines.append("")
	lines.append("namespace %s;" % ns)
	lines.append("")
	lines.append("public partial class %s" % cn)
	lines.append("{")

static func write_summary(lines: Array, text: String, indent: String = "\t") -> void:
	lines.append("%s/// <summary>" % indent)
	lines.append("%s/// %s" % [indent, text])
	lines.append("%s/// </summary>" % indent)

static func write_string_name_class(lines: Array, c_name: String, keys: Array) -> void:
	write_summary(lines, "Auto-generated StringName constants. Use these instead of raw strings to avoid typos and benefit from IDE autocomplete.")
	lines.append("\tpublic static class %s" % c_name)
	lines.append("\t{")
	for key in keys:
		write_summary(lines, 'Refers to <c>%s</c>.' % str(key), "\t\t")
		lines.append('\t\tpublic static readonly StringName %s = "%s";' % [str(key).to_pascal_case(), str(key)])
	lines.append("\t}")
	lines.append("")

static func write_names_const(lines: Array, const_name: String, keys: Array) -> void:
	write_summary(lines, "Comma-separated list of all names. Intended for use with <c>[Export(PropertyHint.Enum, %s)]</c>." % const_name.to_snake_case().to_upper())
	var joined = ",".join(keys.map(func(k): return str(k)))
	lines.append('\tpublic const string %s = "%s";' % [const_name.to_snake_case().to_upper(), joined])
	lines.append("")

static func write_paths_dictionary(lines: Array, dict_name: String, entries: Dictionary, key_class: String = "") -> void:
	var resolved_key_class = key_class if key_class != "" else dict_name.trim_suffix("Paths") + "Name"
	write_summary(lines, "Maps each %s constant to its resource path. Used internally to load scenes on demand." % resolved_key_class)
	lines.append("\tpublic static readonly Godot.Collections.Dictionary<StringName, string> %s = new()" % dict_name)
	lines.append("\t{")
	for id in entries.keys():
		var path: String = entries[id]
		lines.append('\t\t[%s.%s] = "%s",' % [resolved_key_class, id.to_pascal_case(), path])
	lines.append("\t};")
	lines.append("")

static func write_file(path: String, lines: Array) -> void:
	var file = FileAccess.open(path, FileAccess.WRITE)
	if file == null:
		push_error("IdWriter: could not write to '%s'" % path)
		return
	file.store_string("\n".join(lines))
	file.close()
	print("IdWriter: generated '%s'" % path)
