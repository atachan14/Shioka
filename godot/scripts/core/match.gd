class_name Match
extends RefCounted
## One Training match: two fighters plus the systems that ran around them in Unity.
## step() reproduces Unity's FixedUpdate order (DefaultExecutionOrder):
##   TrainingCameraBounds(-1000) -> TrainingSession(-700) -> 1P / 2P TrainingFighter(0)
##   -> CombatV01ContactBatch(500) -> TrainingFrameBar(1000) -> MatchHud(1600)
##   -> TrainingSessionAfterTick(1800)
## The two fighters tick 1P first (Unity's order between equal-order scripts is unspecified).

## CombatTestV01: the v0.1 contact batch and the authored HP / TG.
class CombatTest:
	extends RefCounted
	var sys: Dictionary
	var hp := 2300.0
	var push_frames := 10
	var player_tg := 0.0
	var opponent_tg := 0.0
	var player: Fighter
	var opponent: Fighter
	var last: CombatFormula.Result = null
	var status := ""

	func _init(system: Dictionary) -> void:
		sys = system
		var t: Dictionary = system.combat_test
		hp = float(t.hp)
		push_frames = int(t.push_frames)
		player_tg = float(t.player_tg)
		opponent_tg = float(t.opponent_tg)

	func tg_for(actor: Fighter) -> float:
		return player_tg if actor == player else opponent_tg

	func reset_test() -> void:
		last = null
		player.def.maxHealth = hp
		opponent.def.maxHealth = hp
		player.reset_training()
		status = "Reset HP and selected TG."

	func resolve_batch() -> void:
		var pending: Array = []
		for actor: Fighter in [player, opponent]:
			if not actor.v01_can_contact():
				continue
			var part: Dictionary = actor.select_hurtbox_contact(actor.opponent)
			if part.is_empty() or actor.opponent.health <= 0:
				continue
			var target: Fighter = actor.opponent
			var move: GameData.MoveDef = actor.current_move
			pending.append({"from": actor, "to": target, "part": part,
				"a": actor.attack_level(), "d": target.v01_defense(part, actor.current_attack_class()),
				"guard": target.v01_matching_guard(actor.current_attack_class()), "fd": target.fd_active(),
				"jg": target.v01_jg_reduction(), "age": target.v01_jg_age(),
				"damage": 1.0 if move == null else move.damage_multiplier, "chip": 0.1 if move == null else move.chip_multiplier,
				"push": 0.001 if move == null else move.pushback_scale})
			actor.v01_connect()
		if pending.is_empty():
			return
		# Both TG values are fixed before any contact of this frame is applied.
		var tg0 := player.tension.gauge
		var tg1 := opponent.tension.gauge
		for c in pending:
			var r := CombatFormula.resolve(c.a, c.d, tg0 if c.from == player else tg1, tg0 if c.to == player else tg1,
				c.guard, c.fd, c.jg, c.damage, c.chip, c.push, sys)
			r.jg_age = c.age
			c.to.v01_receive(r, c.from, c.part)
			last = r
			status = "Part %d | %s%s%s" % [c.part.id, Sk.OUTCOME_NAMES[r.outcome], " FD" if r.fd else "", " KO" if c.to.health <= 0 else ""]

var sys: Dictionary
var player: Fighter
var dummy_fighter: Fighter
var combat: CombatTest
var camera: CameraBounds
var session: TrainingSession
var frame_bar := FrameBar.new()
var first_attack := ContactState.new()   # 1P's attacks (observes 2P)
var second_attack := ContactState.new()  # 2P's attacks (observes 1P)
var display: Dictionary
var option_visible := false
var after_tick: Callable

func _init(player_id: String, cpu_id: String) -> void:
	sys = GameData.system()
	display = sys.display
	player = Fighter.new(GameData.character(player_id), sys, "1P")
	dummy_fighter = Fighter.new(GameData.character(cpu_id), sys, "2P")
	player.opponent = dummy_fighter
	dummy_fighter.opponent = player
	combat = CombatTest.new(sys)
	combat.player = player
	combat.opponent = dummy_fighter
	for f: Fighter in [player, dummy_fighter]:
		f.combat = combat
		f.def.maxHealth = combat.hp
		var mid := float(f.data.raw.hurtboxes[0].mid)
		f.def.defenseLevel = mid
		f.def.guardDefenseLevel = mid
	# Scene placement of the Unity Training scene (CombatFormulaComparison.unity).
	player.spawn = Vector2(-1.2, 0.02)
	dummy_fighter.spawn = Vector2(1.2, 0.02)
	player.body.position = player.spawn
	dummy_fighter.body.position = dummy_fighter.spawn
	camera = CameraBounds.new(display, player, dummy_fighter)
	player.camera_bounds = camera
	dummy_fighter.camera_bounds = camera
	frame_bar.configure(player, dummy_fighter)
	combat.reset_test()
	first_attack.observe(dummy_fighter, 0.0, float(display.completedComboSeconds))
	second_attack.observe(player, 0.0, float(display.completedComboSeconds))

## Breaks the reference cycles (fighters <-> opponent / session / camera) so nothing leaks.
func dispose() -> void:
	for f: Fighter in [player, dummy_fighter]:
		if f != null:
			f.opponent = null
			f.session = null
			f.combat = null
			f.camera_bounds = null
			f.input_source = null
			f.throw_partner = null
	if combat != null:
		combat.player = null
		combat.opponent = null
	if camera != null:
		camera.first = null
		camera.second = null
	frame_bar.first = null
	frame_bar.second = null
	if session != null:
		session.dispose()
	session = null

func attach_session(s: TrainingSession) -> void:
	session = s
	player.session = s
	dummy_fighter.session = s

func step() -> void:
	if option_visible:
		return
	var dt := Sk.fdt()
	if not player.option_input_suspended and not dummy_fighter.option_input_suspended:
		camera.advance_simulation(dt)
	if session != null:
		session.fixed_update()
	player.tick()
	dummy_fighter.tick()
	combat.resolve_batch()
	frame_bar.capture()
	var hold := float(display.completedComboSeconds)
	first_attack.observe(dummy_fighter, dt, hold)
	second_attack.observe(player, dt, hold)
	if session != null:
		session.after_tick()
