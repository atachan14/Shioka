class_name KeyConfig
## KeyConfigProfile + KeyConfigService + KeyConfigStore. Bindings keep the Unity Input System
## path strings ("<Keyboard>/j", "T1+<Gamepad>/buttonWest") so the saved file has the same
## shape as Unity's KeyConfig-v1.json. Saved at user://Profiles/KeyConfig-v1.json.

const SYSTEM_IDS := ["battle.Hold", "battle.Mania", "battle.TRC", "battle.Dash", "training.Record", "training.Play",
	"training.Reset", "menu.Confirm", "menu.Cancel", "common.Options", "trigger.T1", "trigger.T2"]
const SAVE_PATH := "user://Profiles/KeyConfig-v1.json"
const RESERVED_KEYS := ["w", "a", "s", "d", "upArrow", "downArrow", "leftArrow", "rightArrow"]

static var _current: Dictionary = {}
static var _defaults: Dictionary = {}
static var status := ""
static var revision := 0

static func slot_id(slot: int) -> String:
	return "battle." + Sk.SLOT_NAMES[slot]

static func copy(p: Dictionary) -> Dictionary:
	return p.duplicate(true)

static func row(p: Dictionary, id: String) -> Dictionary:
	for r in p.bindings:
		if r.id == id:
			return r
	return {}

static func _action_id(a: int) -> String:
	match a:
		Sk.Mod.A: return "battle.A"
		Sk.Mod.B: return "battle.B"
		Sk.Mod.C: return "battle.C"
		Sk.Mod.X: return "battle.X"
		Sk.Mod.Y: return "battle.Y"
		Sk.Mod.Z: return "battle.Z"
		Sk.Mod.J: return "battle.J"
		Sk.Mod.K: return "battle.K"
		Sk.Mod.L: return "battle.L"
		Sk.Mod.THROW: return "battle.Hold"
		Sk.Mod.FORCE: return "battle.Mania"
		Sk.Mod.TRC: return "battle.TRC"
		Sk.Mod.DASH: return "battle.Dash"
		Sk.Mod.RECORD: return "training.Record"
		Sk.Mod.PLAY: return "training.Play"
		Sk.Mod.RESET: return "training.Reset"
	return ""

## KeyConfigProfile.Defaults(TrainingInputConfig)
static func make_defaults() -> Dictionary:
	var c: Dictionary = GameData.system().input
	var rows: Array = []
	for slot in Sk.SLOT_DISPLAY_ORDER:
		rows.append({"id": slot_id(slot), "context": "Battle", "keyboard": c.slot_keyboard[slot], "pad": c.slot_gamepad[slot]})
	var add := func(id: String, context: String, key: String, pad: String) -> void:
		rows.append({"id": id, "context": context, "keyboard": key, "pad": pad})
	add.call("battle.Hold", "Battle", c.first_throw_keyboard, "")
	add.call("battle.Mania", "Battle", "<Keyboard>/shift", c.mania_gamepad)
	add.call("battle.TRC", "Battle", "<Keyboard>/semicolon", c.trc_gamepad)
	add.call("battle.Dash", "Battle", "<Keyboard>/space", "")
	add.call("training.Record", "Training", "<Keyboard>/r", c.record_gamepad)
	add.call("training.Play", "Training", "<Keyboard>/p", c.play_gamepad)
	add.call("training.Reset", "Training", "<Keyboard>/ctrl", "")
	add.call("menu.Confirm", "Menu", "<Keyboard>/j", "<Gamepad>/buttonEast")
	add.call("menu.Cancel", "Menu", "<Keyboard>/k", "<Gamepad>/buttonSouth")
	add.call("common.Options", "Global", "<Keyboard>/escape", "<Gamepad>/start")
	add.call("trigger.T1", "Trigger", "", c.modifier1_binding)
	add.call("trigger.T2", "Trigger", "", c.modifier2_binding)
	for m in [1, 2]:
		var table: Array = c.modifier1_actions if m == 1 else c.modifier2_actions
		for i in 9:
			if String(c.slot_gamepad[i]).is_empty():
				continue
			var id := _action_id(int(table[i]))
			for r in rows:
				if r.id == id:
					r.pad = "T%d+%s" % [m, c.slot_gamepad[i]]
	return {"version": 1, "bindings": rows, "characters": []}

static func normalize(v) -> String:
	return String("" if v == null else v).strip_edges()

static func path_part(v) -> String:
	var s := normalize(v)
	return s.substr(3) if s.to_upper().begins_with("T1+") or s.to_upper().begins_with("T2+") else s

static func modifier(v) -> int:
	var s := normalize(v).to_upper()
	return 1 if s.begins_with("T1+") else (2 if s.begins_with("T2+") else 0)

static func same(a, b) -> bool:
	return normalize(a).to_lower() == normalize(b).to_lower()

static func path_same(a, b) -> bool:
	a = path_part(a)
	b = path_part(b)
	if same(a, b):
		return true
	for f in ["shift", "ctrl"]:
		var root := "<Keyboard>/"
		if (same(a, root + f) and (same(b, root + "left" + f) or same(b, root + "right" + f))) or \
				(same(b, root + f) and (same(a, root + "left" + f) or same(a, root + "right" + f))):
			return true
	return false

static func overlap(a: Dictionary, b: Dictionary) -> bool:
	return a.context == "Trigger" or b.context == "Trigger" or a.context == "Global" or b.context == "Global" or a.context == b.context or (a.context != "Menu" and b.context != "Menu")

static func _reserved(p: String) -> bool:
	for x in RESERVED_KEYS:
		if same(p, "<Keyboard>/" + x):
			return true
	var l := p.to_lower()
	return l.begins_with("<gamepad>/dpad") or same(p, "<Gamepad>/leftStick") or l.begins_with("<gamepad>/leftstick/") or l.begins_with("<joystick>/stick")

static func _expected_context(id: String) -> String:
	if id.begins_with("battle."): return "Battle"
	if id.begins_with("training."): return "Training"
	if id.begins_with("menu."): return "Menu"
	if id.begins_with("trigger."): return "Trigger"
	return "Global"

static func validate(p: Dictionary) -> String:
	if int(p.get("version", 0)) != 1 or not p.has("bindings") or not p.has("characters") or p.bindings.size() != 21:
		return "Unsupported or incomplete KeyConfig"
	var ids := {}
	for r in p.bindings:
		var valid_id: bool = SYSTEM_IDS.has(r.id)
		for s in Sk.SLOT_DISPLAY_ORDER:
			if slot_id(s) == r.id:
				valid_id = true
		if ids.has(r.id) or not valid_id:
			return "Invalid/duplicate action ID"
		ids[r.id] = true
		if r.context != _expected_context(r.id):
			return "Invalid context"
		for pad in [false, true]:
			var value := normalize(r.pad if pad else r.keyboard)
			if value.is_empty():
				continue
			var path := path_part(value)
			var l := path.to_lower()
			if value.length() > 160 or path.contains("+") or not (l.begins_with("<gamepad>/" if pad else "<keyboard>/") or (pad and l.begins_with("<joystick>/"))):
				return "Invalid control path"
			if r.context == "Trigger" and modifier(value) != 0:
				return "Trigger must be a single button"
			if _reserved(path):
				return "方向入力として使用中です"
	for pad in [false, true]:
		for i in p.bindings.size():
			for j in range(i + 1, p.bindings.size()):
				var a: Dictionary = p.bindings[i]
				var b: Dictionary = p.bindings[j]
				var av := normalize(a.pad if pad else a.keyboard)
				var bv := normalize(b.pad if pad else b.keyboard)
				if av.is_empty() or bv.is_empty() or not overlap(a, b):
					continue
				if (a.context == "Trigger" or b.context == "Trigger") and path_same(av, bv):
					return "T1/T2は通常項目と重複できません"
				if modifier(av) == modifier(bv) and path_same(av, bv):
					return "Duplicate binding: %s / %s" % [a.id, b.id]
	var chars := {}
	for c in p.characters:
		if String(c.get("characterId", "")).is_empty() or chars.has(c.characterId) or c.operations.size() != 9 or c.slots.size() != 9:
			return "Invalid character layout"
		chars[c.characterId] = true
		var seen_ops := {}
		var seen_slots := {}
		for o in c.operations:
			seen_ops[o] = true
		for s in c.slots:
			if int(s) < 0 or int(s) > 8:
				return "Invalid character layout"
			seen_slots[int(s)] = true
		if seen_ops.size() != 9 or seen_slots.size() != 9:
			return "Invalid character layout"
	return ""

## Returns {ok, conflict, error}; with approve_swap the conflicting row receives the old value.
static func try_binding(p: Dictionary, id: String, pad: bool, candidate: String, approve_swap: bool) -> Dictionary:
	var r := row(p, id)
	if r.is_empty():
		return {"ok": false, "conflict": "", "error": "Unknown action"}
	candidate = normalize(candidate)
	var old := normalize(r.pad if pad else r.keyboard)
	if same(old, candidate):
		candidate = ""
	if not candidate.is_empty():
		for x in p.bindings:
			var xv := normalize(x.pad if pad else x.keyboard)
			if x != r and (x.context == "Trigger" or r.context == "Trigger") and overlap(r, x) and not xv.is_empty() and path_same(candidate, xv):
				return {"ok": false, "conflict": "", "error": "T1/T2は同時利用する通常項目と交換できません"}
	var conflicts: Array = []
	for x in p.bindings:
		var xv := normalize(x.pad if pad else x.keyboard)
		if x != r and overlap(r, x) and not candidate.is_empty() and modifier(candidate) == modifier(xv) and path_same(candidate, xv) and not xv.is_empty():
			conflicts.append(x.id)
	if conflicts.size() > 1:
		return {"ok": false, "conflict": "", "error": "複数の項目と重複します。別の入力を選んでください"}
	var conflict := ""
	if conflicts.size() == 1:
		conflict = conflicts[0]
		if not approve_swap:
			return {"ok": false, "conflict": conflict, "error": ""}
	var trial := copy(p)
	var target := row(trial, id)
	target["pad" if pad else "keyboard"] = candidate
	if not conflict.is_empty():
		row(trial, conflict)["pad" if pad else "keyboard"] = old
	var error := validate(trial)
	if not error.is_empty():
		return {"ok": false, "conflict": "", "error": error}
	p.bindings = trial.bindings
	return {"ok": true, "conflict": "", "error": ""}

static func op_id(op: Dictionary) -> String:
	var id := String(op.get("operation_id", ""))
	return "origin." + Sk.SLOT_NAMES[int(op.slot)] if id.is_empty() else id

static func layout(p: Dictionary, c: GameData.CharacterDef, create: bool) -> Dictionary:
	for x in p.characters:
		if x.characterId == c.id:
			return x
	if not create:
		return {}
	var ops: Array = []
	var slots: Array = []
	for op in c.raw.attack_slots:
		ops.append(op_id(op))
		slots.append(int(op.slot))
	var entry := {"characterId": c.id, "operations": ops, "slots": slots}
	p.characters.append(entry)
	return entry

static func slot_for(p: Dictionary, c: GameData.CharacterDef, op: Dictionary) -> int:
	var l := layout(p, c, false)
	if l.is_empty():
		return int(op.slot)
	var i: int = l.operations.find(op_id(op))
	return int(op.slot) if i < 0 else int(l.slots[i])

static func swap_operation(p: Dictionary, c: GameData.CharacterDef, operation: String, target: int) -> void:
	var l := layout(p, c, true)
	var i: int = l.operations.find(operation)
	var j: int = -1
	for k in l.slots.size():
		if int(l.slots[k]) == target:
			j = k
	if i < 0 or j < 0:
		return
	var old: int = l.slots[i]
	l.slots[i] = target
	l.slots[j] = old

# ---------------------------------------------------------------- service
static func ensure() -> Dictionary:
	if not _current.is_empty():
		return _current
	_defaults = make_defaults()
	_current = _load()
	return _current

static func defaults() -> Dictionary:
	ensure()
	return copy(_defaults)

static func _load() -> Dictionary:
	if not FileAccess.file_exists(SAVE_PATH):
		status = "既定値（保存ファイルなし）"
		return copy(_defaults)
	var f := FileAccess.open(SAVE_PATH, FileAccess.READ)
	if f == null or f.get_length() > 131072:
		status = "保存設定を読めないため既定値"
		return copy(_defaults)
	var parsed = JSON.parse_string(f.get_as_text())
	if typeof(parsed) != TYPE_DICTIONARY or not validate(parsed).is_empty():
		status = "保存設定を読めないため既定値: invalid data"
		return copy(_defaults)
	status = "保存設定を読み込みました"
	return parsed

static func save(draft: Dictionary) -> String:
	var error := validate(draft)
	if not error.is_empty():
		return error
	error = LocalFile.write_json(SAVE_PATH, draft)
	if not error.is_empty():
		return error
	_current = copy(draft)
	revision += 1
	status = "保存して反映しました"
	return ""

## KeyConfigService.ResolveSlots: the character's operations placed on the saved slots.
static func resolve_slots(c: GameData.CharacterDef) -> Array:
	var p := ensure()
	var result: Array = []
	var used := {}
	for op in c.raw.attack_slots:
		var s := slot_for(p, c, op)
		used[s] = true
		result.append({"operation_id": op_id(op), "slot": s, "assigned": op.assigned, "command": int(op.command), "display_name": op.display_name})
	if used.size() != 9:
		for i in result.size():
			result[i].slot = int(c.raw.attack_slots[i].slot)
	return result

## CharacterCommandResolver.Sample: immediate mapping of pressed slots to the character's moves.
static func resolve_commands(c: GameData.CharacterDef, pressed: int, _direction: int) -> Array:
	var out: Array = []
	if pressed == 0:
		return out
	for slot in resolve_slots(c):
		if slot.assigned and (pressed & (1 << int(slot.slot))) != 0:
			out.append(slot.command)
	return out

## Display text for a binding (KeyConfig screen / help).
static func binding_text(value) -> String:
	var v := normalize(value)
	if v.is_empty():
		return "—"
	var path := path_part(v)
	var text := path.substr(path.find("/") + 1)
	var names := {"buttonWest": "□", "buttonNorth": "△", "buttonEast": "○", "buttonSouth": "×", "leftShoulder": "L1",
		"rightShoulder": "R1", "leftTrigger": "L2", "rightTrigger": "R2", "leftStickPress": "L3", "rightStickPress": "R3", "start": "Options"}
	if names.has(text):
		text = names[text]
	elif path.begins_with("<Keyboard>"):
		text = text.to_upper()
	var m := modifier(v)
	return ("" if m == 0 else "T%d+" % m) + text

## Control names a path matches on its device kind: {"pad": bool, "controls": [..]}
static func path_controls(path: String) -> Dictionary:
	var p := path_part(path)
	if p.is_empty():
		return {"pad": false, "controls": []}
	var pad := not p.to_lower().begins_with("<keyboard>")
	var name := p.substr(p.find("/") + 1)
	if not pad and name == "shift":
		return {"pad": false, "controls": ["leftShift", "rightShift"]}
	if not pad and name == "ctrl":
		return {"pad": false, "controls": ["leftCtrl", "rightCtrl"]}
	if not pad and name == "alt":
		return {"pad": false, "controls": ["leftAlt", "rightAlt"]}
	return {"pad": pad, "controls": [name]}
