class_name LocalFile
## LocalProfileFile: explicit saves only. Writes a temp file, keeps "<file>.bak" of the
## previous version, limit 4 MiB. Reading never rewrites the file.

const MAX_BYTES := 4194304

static func read_json(path: String) -> Dictionary:
	if not FileAccess.file_exists(path):
		return {"status": "No saved file", "value": null}
	var f := FileAccess.open(path, FileAccess.READ)
	if f == null:
		return {"status": "Read fallback: open failed", "value": null}
	if f.get_length() > MAX_BYTES:
		return {"status": "Read fallback: too large", "value": null}
	var value = JSON.parse_string(f.get_as_text())
	return {"status": "Empty data" if value == null else "Loaded", "value": value}

static func write_json(path: String, data) -> String:
	var dir := path.get_base_dir()
	if DirAccess.make_dir_recursive_absolute(dir) != OK and not DirAccess.dir_exists_absolute(dir):
		return "Save failed: directory"
	var temp := path + ".tmp"
	var f := FileAccess.open(temp, FileAccess.WRITE)
	if f == null:
		return "Save failed: " + error_string(FileAccess.get_open_error())
	f.store_string(JSON.stringify(data, "  "))
	f.close()
	if FileAccess.file_exists(path):
		var bak := path + ".bak"
		if FileAccess.file_exists(bak):
			DirAccess.remove_absolute(bak)
		if DirAccess.rename_absolute(path, bak) != OK:
			return "Save failed: backup"
	if DirAccess.rename_absolute(temp, path) != OK:
		return "Save failed: replace"
	return ""
