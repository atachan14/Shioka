class_name GameData
## Loads data/*.json (exported once from the Unity assets; now the editable source).
## system.json: shared rules. awari.json: the character. sprites.json: pivot / PPU per image.

static var _system: Dictionary = {}
static var _characters: Dictionary = {}
static var _sprites: Dictionary = {}

static func load_json(path: String) -> Variant:
	var text := FileAccess.get_file_as_string(path)
	if text.is_empty():
		push_error("Missing data file: " + path)
		return {}
	return JSON.parse_string(text)

static func system() -> Dictionary:
	if _system.is_empty():
		_system = load_json("res://data/system.json")
	return _system

static func sprites() -> Dictionary:
	if _sprites.is_empty():
		_sprites = load_json("res://data/sprites.json")
	return _sprites

static func character(id: String) -> CharacterDef:
	if not _characters.has(id):
		var d: Dictionary = load_json("res://data/%s.json" % id)
		_characters[id] = CharacterDef.new(d)
	return _characters[id]

## Implemented fighters (FighterCatalog). Training uses the same list.
static func fighter_ids() -> Array:
	return ["awari"]

static func reload() -> void:
	_system = {}
	_characters = {}
	_sprites = {}


class MoveDef:
	extends RefCounted
	var role := ""
	var command := 0
	var attack_class := 0
	var causes_knockdown := false
	var state := ""
	var startup_frames := 0.0
	var active_frames := 0.0
	var recovery_frames := 0.0
	var attack_level := 0.0
	var damage := 0.0
	var hit_gain := 0.0
	var ground_hit_knockback_speed := 0.0
	var damage_multiplier := 1.0
	var chip_multiplier := 0.1
	var pushback_scale := 0.001
	var center: Array = [0, .95, .65]
	var half_size: Array = [.32, .3, .4]
	var contact_normalized := 0.5
	var chains: Array = []
	var strength: Dictionary = {}
	var use_level_curves := false
	var attack_level_curve: Array = []
	var defense_level_curve: Array = []
	var use_timed_trc_recovery := false
	var timed_trc_recovery_cost_curve: Array = []
	var sprites: Array = []  # [{sprite, hold}]

	func _init(d: Dictionary) -> void:
		role = d.role
		command = int(d.command)
		attack_class = int(d.attack_class)
		causes_knockdown = bool(d.causes_knockdown)
		state = String(d.state)
		startup_frames = Sk.f(float(d.startup))
		active_frames = Sk.f(float(d.active))
		recovery_frames = Sk.f(float(d.recovery))
		attack_level = Sk.f(float(d.attack_level))
		damage = float(d.damage)
		hit_gain = float(d.hit_gain)
		ground_hit_knockback_speed = float(d.ground_hit_knockback_speed)
		damage_multiplier = float(d.damage_multiplier)
		chip_multiplier = float(d.chip_multiplier)
		pushback_scale = float(d.pushback_scale)
		center = d.center
		half_size = d.half_size
		contact_normalized = float(d.contact_normalized)
		# JSON numbers load as float, and Array.has(int) never matches a float.
		chains = d.chains.map(func(c): return int(c))
		strength = d.strength
		use_level_curves = bool(d.use_level_curves)
		attack_level_curve = d.attack_level_curve
		defense_level_curve = d.defense_level_curve
		use_timed_trc_recovery = bool(d.use_timed_trc_recovery)
		timed_trc_recovery_cost_curve = d.timed_trc_recovery_cost_curve
		sprites = d.sprites

	func duration() -> float:
		return Sk.f(Sk.f(Sk.f(startup_frames + active_frames) + recovery_frames) / 60.0)

	func level_scale(curve: Array, elapsed: float) -> float:
		if not use_level_curves or curve.is_empty():
			return 1.0
		var dur := duration()
		return clampf(Sk.eval_curve(curve, 0.0 if dur <= 0 else Sk.clamp01(elapsed / dur)), 0.0, 4.0)

	func attack_level_at(elapsed: float) -> float:
		return Sk.f(attack_level * level_scale(attack_level_curve, elapsed))

	func defense_scale_at(elapsed: float) -> float:
		return level_scale(defense_level_curve, elapsed)

	func timed_trc_recovery_scale_at(elapsed: float) -> float:
		if not use_timed_trc_recovery or timed_trc_recovery_cost_curve.is_empty() or recovery_frames <= 0:
			return 1.0
		var recovery := elapsed * 60.0 - startup_frames - active_frames
		if recovery < 0.0 or recovery >= recovery_frames:
			return 1.0
		return clampf(Sk.eval_curve(timed_trc_recovery_cost_curve, Sk.clamp01(recovery / recovery_frames)), 0.25, 1.0)

	func can_chain(next: int) -> bool:
		return chains.has(next)

	func sequence_total() -> int:
		var total := 0
		for s in sprites:
			total += clampi(int(s.hold), 0, 36000)
		return total

	## TrainingMove.SequenceSpriteAt (spriteSequence.Sample)
	func sequence_sprite_at(frame: float) -> String:
		if sprites.is_empty() or frame < 0.0 or frame >= Sk.f(duration() * 60.0) - 0.0001:
			return ""
		var f := maxf(0.0, frame)
		var nearest := roundf(f)
		if absf(f - nearest) < 0.0001:
			f = nearest
		var end := 0
		for s in sprites:
			end += int(s.hold)
			if f < end:
				return s.sprite
		return sprites[sprites.size() - 1].sprite


class CharacterDef:
	extends RefCounted
	var raw: Dictionary
	var id := ""
	var name := ""
	var moves: Array = []           # MoveDef in catalog order ("move:N")
	var roles: Dictionary = {}      # role -> MoveDef
	var candidates: Dictionary = {} # flyingKick ... -> Dictionary
	var provisional: Dictionary = {}

	func _init(d: Dictionary) -> void:
		raw = d
		id = d.id
		name = d.name
		for m in d.moves:
			var md := MoveDef.new(m)
			roles[md.role] = md
			if md.role.begins_with("move:"):
				moves.append(md)
		candidates = d.air_candidates
		for e in d.provisional:
			provisional[e.id] = e

	## TrainingMoveSet.Find
	func find(command: int) -> MoveDef:
		if command == Sk.Cmd.SPECIAL:
			return roles.get("special")
		if command == Sk.Cmd.SPECIAL2:
			return roles.get("special2")
		if command == Sk.Cmd.FORWARD_SPECIAL1:
			return roles.get("forwardSpecial1")
		if command == Sk.Cmd.SWEEP:
			return roles.get("sweep")
		for m in moves:
			if m.command == command:
				return m
		return null

	func motion(name_: String) -> Dictionary:
		return raw.motions.get(name_, {})
