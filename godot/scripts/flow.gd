extends Node
## Autoload "Flow": TrainingFlowSession + PlayerPreferencesStore.
## Title -> FighterSelect (Player) -> Title; Title -> Training (CPU select) -> Training.

const TITLE_SCENE := "res://scenes/title.tscn"
const TRAINING_SCENE := "res://scenes/training.tscn"
const PREFS_PATH := "user://Profiles/PlayerPreferences-v1.json"

var awaiting_training_entry := false
var open_options_from_title := false
var player_fighter_id := ""
var cpu_fighter_id := ""

func _ready() -> void:
	process_mode = Node.PROCESS_MODE_ALWAYS

func default_fighter_id() -> String:
	return GameData.fighter_ids()[0]

static func _id_valid(id) -> bool:
	return typeof(id) == TYPE_STRING and not String(id).strip_edges().is_empty() and String(id).length() <= 128

## Returns {player, cpu, status}. Version 1 (Player only) fills CPU with the default fighter;
## unknown ids fall back to the catalog default without overwriting the file.
func load_preferences() -> Dictionary:
	var r := LocalFile.read_json(PREFS_PATH)
	var fallback := default_fighter_id()
	var v = r.value
	var status: String = r.status
	if typeof(v) == TYPE_DICTIONARY and (int(v.get("version", 0)) == 1 or int(v.get("version", 0)) == 2) and _id_valid(v.get("selectedFighterId")) \
			and (not v.has("cpuFighterId") or String(v.get("cpuFighterId", "")).is_empty() or _id_valid(v.get("cpuFighterId"))):
		var player: String = v.selectedFighterId
		var cpu: String = String(v.get("cpuFighterId", ""))
		if cpu.is_empty():
			cpu = fallback
		return {"player": resolve(player), "cpu": resolve(cpu), "status": status}
	return {"player": fallback, "cpu": fallback, "status": status + "; default selection (file preserved)"}

func resolve(id: String) -> String:
	return id if GameData.fighter_ids().has(id) else default_fighter_id()

func save_preferences(player: String, cpu: String) -> String:
	if not _id_valid(player) or not _id_valid(cpu):
		return "Save failed: supported version and stable fighter IDs required"
	return LocalFile.write_json(PREFS_PATH, {"version": 2, "selectedFighterId": player, "cpuFighterId": cpu})

func go_title() -> void:
	get_tree().paused = false
	get_tree().change_scene_to_file(TITLE_SCENE)

func start_training(player: String, cpu: String) -> void:
	player_fighter_id = player
	cpu_fighter_id = cpu
	awaiting_training_entry = true
	open_options_from_title = false
	get_tree().change_scene_to_file(TRAINING_SCENE)

func open_options() -> void:
	var prefs := load_preferences()
	player_fighter_id = prefs.player
	cpu_fighter_id = prefs.cpu
	open_options_from_title = true
	get_tree().change_scene_to_file(TRAINING_SCENE)
