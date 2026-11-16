class_name Fighter
extends RefCounted
## Port of TrainingFighter (+ partial classes AirDash, BackGuard, CombatV01, FD, MotionAction,
## Options, Spatial, Throw). Only the v0.1 combat path is kept (the Unity Training scene always
## ran with CombatTestV01); legacy branches that the v0.1 path still reaches (throws, knockdown)
## are kept. Positions are meters, y up. One tick() = Unity FixedUpdate at 60 Hz.

const P := Sk.Phase
const C := Sk.Cmd
const L := Sk.Lock

var name := ""
var data: GameData.CharacterDef
var def: Dictionary = {}       # TrainingCharacterDefinition (runtime copy)
var sys: Dictionary
var combat                     # CombatTest (Match.CombatTest)
var session                    # TrainingSession or null
var camera_bounds              # CameraBounds or null
var opponent: Fighter
var input_source               # object with read_input() / clear()
var body := Motor.new()

var input_buffer := CommandBuffer.new()
var tension := Tension.new()
var air_clock := AirClock.new()

# --- state ---
var phase := P.IDLE
var facing := 1
var health := 0.0
var stun_remaining := 0.0
var hitstop := 0.0
var timer := 0.0
var horizontal_speed := 0.0
var vertical_speed := 0.0
var walk_time := 0.0
var idle_time := 0.0
var locomotion_distance := 0.0
var movement := Vector2.ZERO
var dash := false
var previous_dash := false
var grounded := false
var air_from_jump := false
var dash_inertia := false
var attack_connected := false
var is_crouching := false
var crouch_pose_progress := 0.0
var current_move: GameData.MoveDef = null
var air_combat_active := false
var air_candidate_name := ""
var air_candidate: Dictionary = {}
var guarding := false           # training dummy guard toggle
var guard_held := false
var fd_held := false
var last_result := "None"
var last_level_difference := 0.0
var hit_count := 0
var guard_count := 0
var chain_count := 0
var cancel_count := 0
var completed_jumps := 0
var completed_attacks := 0
var last_wall_side := 0
var contact_sequence := 0
var last_resolved_contact: Dictionary = {}
var last_contact_hurtbox_id := -1
var option_input_suspended := false
var pending_knockdown := false
var down_elapsed := 0.0
var down_followup_count := 0
var spawn := Vector2.ZERO

# air dash
var is_air_dashing := false
var air_dash_used := false
var air_dash_count := 0
var _air_dash_remaining := 0
var _air_dash_direction := 1
var _air_dash_speed := 0.0
var _air_dash_residual_ratio := 0.0
var _air_dash_residual_deceleration := 0.0
var _air_dash_residual := false

# v0.1 combat
var v01_last_contact: CombatFormula.Result = null
var _v01_frozen_this_tick := false
var _v01_jg_start := -1
var _v01_guard_direction := 0
var _v01_stop := 0
var _v01_stun := 0
var _v01_jg_continuous := false
var _v01_block := false
var _v01_push: Array = []  # [{distance, frame, total, direction}]

# FD / TRC
var fd_guard_count := 0
var last_fd_cost := 0.0
var last_fd_push_distance := 0.0
var last_trc_cost := 0.0
var last_trc_cost_scale := 1.0
var last_trc_base_cost := 0.0

# throw
var throw_invulnerable := false
var throw_partner: Fighter = null
var _throw_resolved := false
var _caught_level := 0.0
var _caught_defense := 0.0
var last_throw_result := "None"
var throw_count := 0

# motion sampling (TrainingFighter.MotionAction)
var _sampled_motion := ""
var _motion_start := 0.0

# hurtboxes: [{id, region, offset: Vector2, size: Vector2, enabled}]
var hurtboxes: Array = []
var throw_hurtbox := {"offset": Vector2.ZERO, "size": Vector2.ZERO, "enabled": true}
var throwbox := {"offset": Vector2.ZERO, "size": Vector2.ZERO, "enabled": false}

signal trc_performed(fighter)

func _init(character: GameData.CharacterDef, system: Dictionary, label: String) -> void:
	data = character
	sys = system
	name = label
	def = character.raw.definition.duplicate()
	for k in def.keys():
		def[k] = Sk.f(float(def[k]))
	_apply_locomotion(character.raw.locomotion)
	var m: Dictionary = character.raw.motor
	body.height = float(m.height)
	body.radius = float(m.radius)
	body.center = Vector2(0, float(m.center_y))
	body.skin_width = float(m.skin_width)
	tension.suppress_gains = true
	for r in character.raw.hurtboxes:
		hurtboxes.append({"id": int(r.id), "region": r, "offset": Vector2.ZERO, "size": Vector2.ZERO, "enabled": true})

## ApprovedLocomotionTuning.Commit (canonical ApprovedLocomotionProfile values).
func _apply_locomotion(s: Dictionary) -> void:
	var step := Sk.fdt()
	def.backstepSeconds = Sk.f(Sk.f(int(s.backstepFrames) * step) - 0.000001)
	def.backstepSpeed = Sk.f(float(s.backstepDistance) / Sk.f(int(s.backstepFrames) * step))
	var t := Sk.f(int(s.jumpAirFrames) * step)
	def.gravity = Sk.f(-8.0 * float(s.jumpHeight) / Sk.f(t * t))
	def.jumpVelocity = Sk.f(Sk.f(4.0 * float(s.jumpHeight) / t) - Sk.f(Sk.f(0.5 * def.gravity) * step))

func locomotion() -> Dictionary:
	return data.raw.locomotion

# ---------------------------------------------------------------- properties
func position() -> Vector2:
	return body.position

func walk_speed() -> float: return def.walkSpeed
func dash_speed() -> float: return def.dashSpeed

func attack_startup() -> float:
	return Sk.f(def.attackStartup if current_move == null else Sk.f(current_move.startup_frames / 60.0))
func attack_active() -> float:
	return Sk.f(def.attackActive if current_move == null else Sk.f(current_move.active_frames / 60.0))
func attack_recovery() -> float:
	return Sk.f(def.attackRecovery if current_move == null else Sk.f(current_move.recovery_frames / 60.0))
func attack_level() -> float:
	if current_move == null:
		return def.attackLevel
	return current_move.attack_level_at(timer) if phase == P.ATTACK else current_move.attack_level

func is_down() -> bool:
	return pending_knockdown or phase == P.KNOCKDOWN or phase == P.DOWN or phase == P.GET_UP
func is_throw_state() -> bool:
	return phase == P.THROW_ATTEMPT or phase == P.THROW_HOLD or phase == P.THROWN
func is_throw_linked() -> bool:
	return throw_partner != null
func is_air_combat_attack() -> bool:
	return air_combat_active
func training_recovery_busy() -> bool:
	return stun_remaining > 0 or hitstop > 0 or is_down() or is_throw_state() or is_throw_linked()

func throw_settings() -> Dictionary:
	return data.raw.throw
func _throw_attempt_seconds() -> float:
	var t := throw_settings()
	return Sk.f(Sk.f(Sk.f(float(t.startup) + float(t.active)) + float(t.recovery)) / 60.0)
func _throw_hold_seconds() -> float:
	return Sk.f(maxf(1.0, float(throw_settings().hold)) / 60.0)
func _throw_startup_seconds() -> float:
	return Sk.f(float(throw_settings().startup) / 60.0)
func _throw_active_end_seconds() -> float:
	var t := throw_settings()
	return Sk.f(Sk.f(float(t.startup) + float(t.active)) / 60.0)
func throw_active() -> bool:
	return phase == P.THROW_ATTEMPT and timer >= _throw_startup_seconds() and timer < _throw_active_end_seconds()
func _throw_lock_seconds() -> float:
	if phase == P.THROW_ATTEMPT:
		return maxf(0.0, Sk.f(_throw_attempt_seconds() - timer))
	if phase == P.THROW_HOLD:
		return maxf(0.0, Sk.f(_throw_hold_seconds() - timer))
	return 0.0

func v01_block_paused() -> bool:
	return _v01_block and _v01_stun > 0

func lock_kind() -> int:
	if phase == P.THROW_ATTEMPT:
		return L.THROW_STARTUP if timer < _throw_startup_seconds() else (L.THROW_ACTIVE if throw_active() else L.THROW_RECOVERY)
	if phase == P.THROW_HOLD: return L.THROW_HOLD
	if phase == P.THROWN: return L.THROWN
	if phase == P.KNOCKDOWN: return L.KNOCKDOWN
	if phase == P.DOWN: return L.DOWN
	if phase == P.GET_UP: return L.GET_UP
	if stun_remaining > 0.0:
		return L.BLOCKSTUN if v01_block_paused() else (L.GUARD_STUN if last_result == "Guard" else L.HITSTUN)
	if phase == P.BACKSTEP: return L.BACKSTEP
	if phase == P.TAKEOFF: return L.TAKEOFF
	if phase == P.LANDING: return L.LANDING
	if phase == P.ATTACK:
		var s := attack_startup()
		return L.STARTUP if timer < s else (L.ACTIVE if timer < Sk.f(s + attack_active()) else L.RECOVERY)
	if air_clock.is_busy(): return L.AIR_ACTION
	return L.NONE

func remaining_lock_frames() -> float:
	var m := maxf(stun_remaining, _throw_lock_seconds())
	if phase == P.BACKSTEP: m = maxf(m, maxf(0.0, Sk.f(def.backstepSeconds - timer)))
	if phase == P.ATTACK: m = maxf(m, maxf(0.0, Sk.f(Sk.f(Sk.f(attack_startup() + attack_active()) + attack_recovery()) - timer)))
	if phase == P.TAKEOFF: m = maxf(m, maxf(0.0, Sk.f(def.takeoffSeconds - timer)))
	if phase == P.LANDING: m = maxf(m, maxf(0.0, Sk.f(def.landingSeconds - timer)))
	if air_clock.is_busy(): m = maxf(m, Sk.f(air_clock.duration - air_clock.elapsed))
	return Sk.f(m * 60.0)

func hitbox_active() -> bool:
	if throw_active():
		return true
	if phase == P.ATTACK:
		var s := attack_startup()
		if timer >= s and timer < Sk.f(s + attack_active()):
			return true
	return air_combat_active and phase == P.AIR and air_clock.can_apply_hit()

func hitbox_center() -> Vector2:
	if phase == P.THROW_ATTEMPT:
		return position() + throwbox.offset
	if air_combat_active and not air_candidate.is_empty():
		return position() + Sk.from_legacy(air_candidate.contact_center, facing)
	return position() + Sk.from_legacy([0, .95, .65] if current_move == null else current_move.center, facing)

func hitbox_half_size() -> Vector2:
	if phase == P.THROW_ATTEMPT:
		return throwbox.size * 0.5
	if air_combat_active and not air_candidate.is_empty():
		return Sk.half_size(air_candidate.half_size)
	return Sk.half_size([.32, .3, .4] if current_move == null else current_move.half_size)

func current_attack_class() -> int:
	if is_throw_state():
		return Sk.AttackClass.THROW
	return Sk.AttackClass.MID if current_move == null else current_move.attack_class

# --- guard (BackGuard / CombatV01) ---
func is_guard_threat_motion() -> bool:
	return phase == P.ATTACK or (phase == P.AIR and air_combat_active and air_clock.is_busy())

func opponent_direction_for_guard() -> int:
	if opponent == null:
		return facing
	var delta := opponent.position().x - position().x
	return (1 if delta > 0 else -1) if absf(delta) > 0.0001 else facing

func relative_back_held() -> bool:
	return opponent != null and movement.x * opponent_direction_for_guard() < -0.1

func back_guard_intent() -> bool:
	return relative_back_held() and opponent.is_guard_threat_motion()

func guard_available() -> bool:
	return not is_down() and (stun_remaining <= 0 or last_result == "Guard") and (phase == P.IDLE or phase == P.MOVE or phase == P.CROUCH or phase == P.AIR or phase == P.GUARD_STUN)

func is_guarding() -> bool:
	return guarding or (back_guard_intent() and guard_available())

func fd_active() -> bool:
	return fd_held and relative_back_held() and is_guarding() and guard_available() and not is_throw_state() and not is_throw_linked()

func v01_can_fd(direction: Vector2) -> bool:
	return opponent != null and opponent.is_guard_threat_motion() and direction.x * opponent_direction_for_guard() < -0.1 and guard_available()

func v01_jg_age() -> int:
	return -1 if _v01_jg_start < 0 else input_buffer.frame - _v01_jg_start

func v01_jg_reduction() -> int:
	if session != null:
		var forced: int = session.dummy_jg_for(self)
		if forced >= 0:
			return forced
	return CombatFormula.jg_reduction(v01_jg_age(), _v01_jg_continuous, sys)

func v01_matching_guard(kind: int) -> bool:
	return is_guarding() and (kind != Sk.AttackClass.LOW or is_crouching) and (kind != Sk.AttackClass.HIGH or not is_crouching)

func v01_defense(part: Dictionary, kind: int) -> float:
	var r: Dictionary = part.region
	var base := float(r.high) if kind == Sk.AttackClass.HIGH else (float(r.low) if kind == Sk.AttackClass.LOW else float(r.mid))
	return Sk.f(maxf(0.0, base) * maxf(0.0, float(r.correction)))

## DefenseFor (v0.1 path: definition defense level, used by throws and the frame bar)
func defense_for(_kind: int) -> float:
	return maxf(0.0, def.defenseLevel)

func v01_can_contact() -> bool:
	return health > 0 and not option_input_suspended and hitbox_active() and not attack_connected and not _v01_frozen_this_tick and stun_remaining <= 0 and hitstop <= 0 and not is_throw_state()

func trc_recovery_cost_scale() -> float:
	if grounded and phase == P.ATTACK and stun_remaining <= 0.0 and current_move != null:
		return current_move.timed_trc_recovery_scale_at(timer)
	return 1.0

# ---------------------------------------------------------------- reset
func reset_training() -> void:
	_reset_air_dash()
	_reset_local()
	if opponent != null:
		opponent._reset_local()
	refresh_facing()
	if opponent != null:
		opponent.refresh_facing()

func refresh_facing() -> void:
	facing = 1 if opponent == null or opponent.position().x > position().x else -1

func _reset_local() -> void:
	last_trc_cost_scale = 1.0
	last_trc_base_cost = 0.0
	_reset_fd()
	_release_throw()
	throw_count = 0
	last_throw_result = "None"
	_throw_resolved = false
	body.enabled = false
	_set_crouching(false)
	body.position = spawn
	body.enabled = true
	phase = P.IDLE
	air_clock.reset()
	air_candidate = {}
	air_candidate_name = ""
	current_move = null
	air_combat_active = false
	if input_source != null and input_source.has_method("clear"):
		input_source.clear()
	throw_invulnerable = false
	last_contact_hurtbox_id = -1
	input_buffer.reset()
	tension.reset()
	chain_count = 0
	cancel_count = 0
	guard_held = false
	fd_held = false
	timer = 0.0
	vertical_speed = 0.0
	horizontal_speed = 0.0
	hitstop = 0.0
	walk_time = 0.0
	idle_time = 0.0
	locomotion_distance = 0.0
	pending_knockdown = false
	down_elapsed = 0.0
	down_followup_count = 0
	update_hurtboxes()
	crouch_pose_progress = 0.0
	attack_connected = false
	movement = Vector2.ZERO
	dash = false
	previous_dash = false
	completed_jumps = 0
	completed_attacks = 0
	facing = 1 if opponent == null or opponent.position().x > position().x else -1
	grounded = body.has_floor_support()
	refresh_throw_volumes()
	air_from_jump = false
	dash_inertia = false
	last_wall_side = 0
	health = def.maxHealth
	hit_count = 0
	guard_count = 0
	last_level_difference = 0.0
	last_resolved_contact = {}
	stun_remaining = 0.0
	last_result = "None"
	_v01_reset()

func _v01_reset() -> void:
	_v01_stop = 0
	_v01_stun = 0
	_v01_block = false
	_v01_jg_start = -1
	_v01_guard_direction = 0
	_v01_jg_continuous = false
	_v01_push.clear()
	v01_last_contact = null
	if combat != null:
		health = float(combat.hp)
		tension.set_test_gauge(combat.tg_for(self))

func _reset_fd() -> void:
	fd_held = false
	fd_guard_count = 0
	last_fd_cost = 0.0
	last_fd_push_distance = 0.0
	last_trc_cost = 0.0

func set_training_hp(hp: float) -> void:
	health = clampf(hp, 0.0, def.maxHealth)
	if session != null:
		session.hp_assigned(self)

func clear_input_boundary() -> void:
	if input_source != null and input_source.has_method("clear"):
		input_source.clear()
	input_buffer.clear_pending()
	movement = Vector2.ZERO
	dash = false
	previous_dash = false
	guard_held = false
	fd_held = false

func set_input_source(source) -> void:
	if input_source != null and input_source.has_method("clear"):
		input_source.clear()
	_release_throw()
	input_source = source
	guard_held = false
	fd_held = false
	if input_source != null and input_source.has_method("clear"):
		input_source.clear()
	movement = Vector2.ZERO
	dash = false
	input_buffer.clear_pending()

func set_option_input_suspended(suspended: bool) -> void:
	option_input_suspended = suspended
	if input_source != null and input_source.has_method("clear") and not input_source.has_method("try_peek_reaction"):
		input_source.clear()
	input_buffer.clear_pending()
	movement = Vector2.ZERO
	dash = false
	previous_dash = false
	guard_held = false
	fd_held = false

# ---------------------------------------------------------------- tick
func tick() -> void:
	if option_input_suspended:
		return
	var dt := Sk.fdt()
	var dash_pressed := false
	_v01_frozen_this_tick = false
	input_buffer.advance(dt)
	if input_source != null or session != null:
		var input: InputFrame = session.read_for(self) if session != null else input_source.read_input()
		input.fd_held = input.fd_held or input.force_held
		movement = input.movement
		dash = input.dash
		fd_held = input.fd_held
		dash_pressed = input.dash_pressed or (dash and not previous_dash)
		if input.reset:
			if session != null:
				session.reset_configured()
				return
			combat.reset_test()
			return
		_capture_commands(input)
		if input.toggle_opponent_guard and opponent != null:
			opponent.guarding = not opponent.guarding
	_v01_update_guard()
	_execute_buffered()
	previous_dash = dash
	if dash_pressed:
		_try_start_air_dash()
	if dash_pressed and grounded and hitstop <= 0 and stun_remaining <= 0 and movement.y >= -0.5 and (phase == P.IDLE or phase == P.MOVE or phase == P.CROUCH):
		refresh_facing()
		if movement.x * facing < -0.1:
			_set_crouching(false)
			phase = P.BACKSTEP
			timer = 0.0
			horizontal_speed = Sk.f(-facing * def.backstepSpeed)
			dash_inertia = false
	if hitstop > 0.0:
		air_clock.hold(AirClock.Pause.HITSTOP)
		_v01_tick_stop()
		if hitstop == 0.0:
			_execute_buffered()
		return
	if _v01_tick_block():
		return
	if _tick_throw(dt):
		return
	var was_stunned := stun_remaining > 0.0
	if stun_remaining > 0.0 and not is_down():
		_v01_tick_stun()
		if stun_remaining == 0.0 and (phase == P.HITSTUN or phase == P.GUARD_STUN):
			phase = P.IDLE
		if stun_remaining == 0.0:
			_execute_buffered(true)
	var floor_before := body.has_floor_support()
	if phase != P.AIR and not floor_before:
		_enter_air(false)
	if not was_stunned and grounded and (phase == P.IDLE or phase == P.MOVE or phase == P.CROUCH):
		if opponent != null and absf(opponent.position().x - position().x) > 0.08:
			facing = 1 if opponent.position().x > position().x else -1
		_set_crouching(movement.y < -0.5)
		if is_crouching:
			phase = P.CROUCH
		else:
			phase = P.MOVE if absf(movement.x) > 0.1 else P.IDLE
	if is_down() and grounded:
		_advance_down(dt)
	if not was_stunned:
		timer = Sk.f(timer + dt)
	# Acceleration belongs to the floor. Air input never changes stored inertia.
	if phase != P.AIR and phase != P.BACKSTEP:
		var hold_position: bool = session != null and session.hold_dummy_position(self)
		var can_move := not hold_position and health > 0 and not was_stunned and (phase == P.IDLE or phase == P.MOVE or phase == P.TAKEOFF)
		var target := 0.0
		if can_move:
			target = Sk.f(movement.x * (def.dashSpeed if dash and movement.x * facing > 0.0 else def.walkSpeed))
		if can_move and dash and movement.x * facing > 0.1:
			dash_inertia = true
		var reversing := can_move and absf(movement.x) > 0.1 and horizontal_speed * movement.x < 0.0
		var slowing_dash := dash_inertia and not dash and not reversing and phase != P.CROUCH and absf(target) < absf(horizontal_speed)
		var rate: float = maxf(0.1, def.dashDeceleration) if slowing_dash else def.acceleration
		horizontal_speed = Sk.f(Sk.move_toward(horizontal_speed, target, Sk.f(rate * dt)))
		if reversing or absf(horizontal_speed) < 0.001 or (not dash and can_move and absf(movement.x) > 0.1 and absf(horizontal_speed) <= def.walkSpeed):
			dash_inertia = false
	if phase == P.TAKEOFF and timer >= def.takeoffSeconds:
		_enter_air(true)
	# All unsupported motion uses the same gravity, including walking off an edge.
	vertical_speed = Sk.f(vertical_speed + Sk.f(def.gravity * dt)) if phase == P.AIR else -2.0
	_prepare_air_dash()
	var motion_start_x := position().x
	body.move(Vector2(horizontal_speed, vertical_speed) * dt)
	_v01_tick_push()
	_finish_air_dash_step()
	_resolve_opponent_overlap()
	# Keep the simulation inside the training floor / camera walls.
	var p := position()
	var left: float = -6.0 if camera_bounds == null else camera_bounds.left
	var right: float = 6.0 if camera_bounds == null else camera_bounds.right
	last_wall_side = -1 if p.x < left else (1 if p.x > right else 0)
	p.x = clampf(p.x, left, right) if camera_bounds == null else camera_bounds.limit_position(motion_start_x, p.x)
	if last_wall_side != 0 and horizontal_speed * last_wall_side > 0.0:
		_stop_air_dash()
		_air_dash_residual = false
		horizontal_speed = 0.0
		dash_inertia = false
	body.teleport(p)
	if phase == P.MOVE:
		locomotion_distance += absf(p.x - motion_start_x)
	var floor_after := body.has_floor_support() and vertical_speed <= 0.0
	if phase == P.AIR and floor_after:
		_land_air_dash()
		# Resolve the landing side before recovery can consume a buffered jump.
		if not pending_knockdown:
			refresh_facing()
		if pending_knockdown:
			_begin_down()
		else:
			phase = P.LANDING
		vertical_speed = 0.0
		timer = 0.0
		grounded = true
		if air_from_jump:
			completed_jumps += 1
		if air_combat_active:
			air_clock.end(AirClock.Finish.LANDING)
			air_combat_active = false
			current_move = null
			input_buffer.clear_pending()
	elif phase != P.AIR and not floor_after:
		_enter_air(false)
	elif phase != P.AIR:
		grounded = true
	if phase == P.LANDING and timer >= def.landingSeconds:
		phase = P.IDLE
		timer = 0.0
		_execute_buffered(true)
	if phase == P.BACKSTEP and timer >= def.backstepSeconds:
		phase = P.IDLE
		timer = 0.0
		horizontal_speed = 0.0
		_execute_buffered(true)
	if air_clock.is_busy():
		if phase != P.AIR and not air_clock.ground_recovery_allowed:
			air_clock.hold(AirClock.Pause.LANDING)
		elif was_stunned:
			air_clock.hold(AirClock.Pause.STUN)
		else:
			air_clock.advance(dt)
	if air_combat_active and not air_clock.is_busy():
		air_combat_active = false
		current_move = null
		completed_attacks += 1
		_execute_buffered(true)
	update_hurtboxes()
	refresh_throw_volumes()
	_resolve_throw_attempt()
	refresh_throw_volumes()
	if phase == P.ATTACK and timer >= Sk.f(Sk.f(attack_startup() + attack_active()) + attack_recovery()):
		var low := _is_crouch_attack()
		phase = P.IDLE
		timer = 0.0
		completed_attacks += 1
		current_move = null
		if low:
			crouch_pose_progress = 1.0
		_execute_buffered(true)
	if phase == P.MOVE:
		walk_time += dt * (-1.0 if movement.x * facing < 0.0 else 1.0)
	if phase == P.BACKSTEP:
		walk_time -= dt
	if phase == P.IDLE or phase == P.CROUCH:
		idle_time += dt
	update_hurtboxes()
	var interrupts := is_throw_state() or phase == P.TAKEOFF or phase == P.AIR or phase == P.LANDING or (phase == P.ATTACK and not _is_crouch_attack())
	crouch_pose_progress = 0.0 if interrupts else Sk.move_toward(crouch_pose_progress, 1.0 if is_crouching else 0.0, dt / maxf(0.02, def.crouchTransitionSeconds))

# ---------------------------------------------------------------- commands
func input_character() -> GameData.CharacterDef:
	return data

func _capture_commands(input: InputFrame) -> void:
	input_buffer.record_sample(input, lock_kind(), facing, not grounded)
	var resolved: Array = KeyConfig.resolve_commands(data, input.attack_pressed_mask, int(CommandHistory.direction(input.movement, facing)))
	if not resolved.is_empty():
		var list := input.commands.duplicate()
		list.append_array(resolved)
		input.commands = list
	if input.fd_held and v01_can_fd(input.movement):
		input_buffer.remove_pending(func(c): return c != C.CANCEL and c != C.JUMP)
		input.commands = input.commands.filter(func(c): return c == C.CANCEL or c == C.JUMP)
	for command in input.commands:
		input_buffer.push(_directional_command(command), lock_kind())

func _directional_command(command: int) -> int:
	var f := facing
	if grounded and (phase == P.IDLE or phase == P.MOVE or phase == P.CROUCH) and opponent != null:
		f = 1 if opponent.position().x > position().x else -1
	if phase == P.AIR or phase == P.TAKEOFF:
		if command == C.STRONG_KICK:
			return C.AIR_DOWN_STRONG_KICK if movement.y < -0.5 else C.AIR_STRONG_KICK
		if command == C.HEAVY:
			return C.AIR_STRONG_PUNCH
		if command == C.SPECIAL2:
			return C.AIR_SPECIAL2
	if grounded and movement.y > -0.5 and movement.x * f > 0.1 and command == C.SPECIAL:
		return C.FORWARD_SPECIAL1
	if command == C.STRONG_KICK and grounded and movement.y < -0.5 and movement.x * f > 0.1:
		return C.SWEEP
	if grounded and movement.y < -0.5:
		match command:
			C.LIGHT: return C.CROUCH_WEAK_PUNCH
			C.MEDIUM: return C.CROUCH_MEDIUM_PUNCH
			C.HEAVY: return C.CROUCH_STRONG_PUNCH
			C.SLASH: return C.CROUCH_WEAK_KICK
			C.HEAVY_SLASH: return C.CROUCH_MEDIUM_KICK
			C.STRONG_KICK: return C.CROUCH_STRONG_KICK
	return command

static func is_crouch_move(c: int) -> bool:
	return c == C.CROUCH_WEAK_KICK or c == C.CROUCH_MEDIUM_KICK or c == C.CROUCH_STRONG_KICK or c == C.CROUCH_WEAK_PUNCH or c == C.CROUCH_MEDIUM_PUNCH or c == C.CROUCH_STRONG_PUNCH

func _is_crouch_attack() -> bool:
	return phase == P.ATTACK and current_move != null and is_crouch_move(current_move.command)

func _can_start_throw() -> bool:
	return grounded and not is_down() and not is_throw_state() and hitstop <= 0 and stun_remaining <= 0 and (phase == P.IDLE or phase == P.MOVE or phase == P.CROUCH)

func _can_accept(command: int) -> bool:
	if health <= 0:
		return false
	if command == C.CANCEL:
		return true
	if stun_remaining > 0.0 or is_down():
		return false
	if hitstop > 0.0:
		return false
	if command == C.THROW:
		return _can_start_throw()
	if phase == P.ATTACK:
		if command == C.JUMP or current_move == null:
			return false
		if not (attack_connected or (current_move.command == C.LIGHT and timer >= Sk.f(attack_startup() + attack_active()))):
			return false
		var chain := C.SPECIAL if command == C.FORWARD_SPECIAL1 else (C.STRONG_KICK if command == C.SWEEP else command)
		return current_move.can_chain(chain) and data.find(command) != null
	if phase == P.AIR:
		return not air_clock.is_busy() and not _air_candidate_for(command).is_empty()
	if not grounded or (movement.y < -0.5 and command != C.SWEEP and not is_crouch_move(command)) or (phase != P.IDLE and phase != P.MOVE and phase != P.CROUCH):
		return false
	return command == C.JUMP or command == C.LIGHT or data.find(command) != null

func _air_candidate_for(c: int) -> String:
	match c:
		C.SLASH: return "flyingKick"
		C.HEAVY_SLASH: return "dropKick"
		C.AIR_STRONG_KICK: return "airStrongKick"
		C.AIR_DOWN_STRONG_KICK: return "airDownStrongKick"
		C.AIR_STRONG_PUNCH: return "airStrongPunch"
		C.AIR_SPECIAL2: return "airSpecial2"
	return ""

func _air_move_for(candidate: String) -> GameData.MoveDef:
	match candidate:
		"airStrongKick": return data.roles.get("airStrongKickMove")
		"airDownStrongKick": return data.roles.get("airDownStrongKickMove")
		"airStrongPunch": return data.roles.get("airStrongPunchMove")
		"airSpecial2": return data.roles.get("airSpecial2Move")
	return null

func _try_begin_air_attack(candidate: String) -> bool:
	if phase != P.AIR or hitstop > 0.0 or stun_remaining > 0.0 or candidate.is_empty():
		return false
	var c: Dictionary = data.candidates[candidate]
	var move := _air_move_for(candidate)
	var dur := Sk.f(float(c.duration)) if move == null else move.duration()
	if dur <= 0.0:
		return false
	var startup := Sk.f(0.25 * dur) if move == null else Sk.f(move.startup_frames / 60.0)
	var active := Sk.f(0.25 * dur) if move == null else Sk.f(move.active_frames / 60.0)
	if not air_clock.try_begin(dur, startup, active):
		return false
	_stop_air_dash()
	air_candidate = c
	air_candidate_name = candidate
	return true

func _execute_buffered(ends_lock: bool = false) -> void:
	_try_queue_configured_reaction()
	var command := input_buffer.try_take(_can_accept)
	if command < 0:
		if ends_lock:
			input_buffer.clear_pending()
		return
	# A selected command ends this hold; newer invalid commands cannot leak into the next state.
	input_buffer.clear_pending()
	if command == C.CANCEL:
		perform_trc()
		input_buffer.confirm_selected(true)
		return
	if command == C.THROW:
		_begin_throw()
		input_buffer.confirm_selected(true)
		return
	if phase == P.AIR:
		var candidate := _air_candidate_for(command)
		var began := _try_begin_air_attack(candidate)
		if began:
			air_combat_active = true
			current_move = _air_move_for(candidate)
			attack_connected = false
		input_buffer.confirm_selected(began)
		return
	if command == C.JUMP:
		_set_crouching(false)
		phase = P.TAKEOFF
		timer = 0.0
		input_buffer.confirm_selected(true)
		return
	if phase == P.ATTACK:
		chain_count += 1
	refresh_facing()
	_set_crouching(is_crouch_move(command))
	current_move = data.find(command)
	phase = P.ATTACK
	timer = 0.0
	attack_connected = false
	input_buffer.confirm_selected(true)

## Options.TryQueueConfiguredReaction: dummy counter attack at the first actionable frame.
func _try_queue_configured_reaction() -> void:
	if session != null and session.suppress_dummy_reaction(self):
		return
	if option_input_suspended or input_source == null or not input_source.has_method("try_peek_reaction"):
		return
	var input: InputFrame = input_source.try_peek_reaction(self)
	if input == null:
		return
	var previous := movement
	movement = input.movement
	var command := _directional_command(input.commands[0])
	if not _can_accept(command):
		movement = previous
		return
	input_buffer.push(command, lock_kind())
	input_source.confirm_reaction()

func perform_trc() -> void:
	_stop_air_dash()
	var low_cancel := _is_crouch_attack()
	if input_source != null and input_source.has_method("clear_pending_chord_input"):
		input_source.clear_pending_chord_input()
	var lock_frames := remaining_lock_frames()
	last_trc_cost_scale = trc_recovery_cost_scale()
	var cost := lock_frames * last_trc_cost_scale + hitstop * 60.0
	last_trc_base_cost = lock_frames + hitstop * 60.0
	if stun_remaining > 0.0 or is_down():
		cost *= 2.0
		last_trc_base_cost *= 2.0
	tension.spend(Sk.f(cost), float(data.raw.legacy_rules.tensionLevelGainRate))
	last_trc_cost = tension.last_cost
	_release_throw()
	air_clock.end()
	air_combat_active = false
	current_move = null
	hitstop = 0.0
	stun_remaining = 0.0
	timer = 0.0
	attack_connected = false
	_v01_stop = 0
	_v01_stun = 0
	_v01_block = false
	tension.set_test_gauge(tension.gauge)
	pending_knockdown = false
	down_elapsed = 0.0
	down_followup_count = 0
	phase = P.IDLE if grounded else P.AIR
	_set_crouching(low_cancel and grounded and movement.y < -0.5)
	crouch_pose_progress = 1.0 if low_cancel else 0.0
	if absf(horizontal_speed) > 0.001:
		dash_inertia = true
	input_buffer.clear_pending()
	cancel_count += 1
	if grounded and movement.y > 0.5:
		phase = P.TAKEOFF
		timer = 0.0
	update_hurtboxes()
	trc_performed.emit(self)

func _set_crouching(value: bool) -> void:
	is_crouching = value
	var standing := float(data.raw.motor.height)
	body.height = clampf(def.crouchHeight, body.radius * 2.0, standing) if value else standing
	# Keep the capsule bottom in place: center = standing center - (standing - height) / 2.
	body.center = Vector2(0, float(data.raw.motor.center_y) - (standing - body.height) * 0.5)

func _enter_air(jump: bool) -> void:
	_set_crouching(false)
	phase = P.AIR
	timer = 0.0
	grounded = false
	air_from_jump = jump
	crouch_pose_progress = 0.0
	vertical_speed = def.jumpVelocity if jump else 0.0

func _down_duration() -> float:
	var r: Dictionary = data.raw.legacy_rules
	return Sk.f(Sk.f(Sk.f(float(r.knockdownSeconds)) + Sk.f(float(r.downHoldSeconds))) + Sk.f(float(r.getUpSeconds)))

func _begin_down() -> void:
	pending_knockdown = true
	down_elapsed = 0.0
	down_followup_count = 0
	phase = P.KNOCKDOWN
	timer = 0.0
	stun_remaining = _down_duration()
	horizontal_speed = 0.0
	dash_inertia = false
	_set_crouching(false)
	current_move = null
	air_combat_active = false
	air_clock.end()
	input_buffer.clear_pending()
	update_hurtboxes()

func _advance_down(dt: float) -> void:
	var r: Dictionary = data.raw.legacy_rules
	down_elapsed = Sk.f(down_elapsed + dt)
	stun_remaining = maxf(0.0, Sk.f(_down_duration() - down_elapsed))
	var k := Sk.f(float(r.knockdownSeconds))
	var dh := Sk.f(k + Sk.f(float(r.downHoldSeconds)))
	phase = P.KNOCKDOWN if down_elapsed < k else (P.DOWN if down_elapsed < dh else P.GET_UP)
	if stun_remaining <= 0.0:
		pending_knockdown = false
		phase = P.IDLE
		timer = 0.0
		update_hurtboxes()
		_execute_buffered(true)

func _resolve_opponent_overlap() -> void:
	if opponent == null or not opponent.body.enabled:
		return
	var oc := opponent.body.bounds_center()
	var oe := opponent.body.bounds_extents()
	var mc := body.bounds_center()
	var me := body.bounds_extents()
	# Fighters do not support each other; this shared side pushbox separates horizontally, never upwards.
	if mc.y - me.y >= oc.y + oe.y - 0.01 or mc.y + me.y <= oc.y - oe.y + 0.01:
		return
	if grounded and not opponent.grounded:
		return
	var gap := body.radius + oe.x + 0.015
	var difference := position().x - opponent.position().x
	if absf(difference) >= gap:
		return
	var side := signf(difference) if absf(difference) > 0.005 else (signf(horizontal_speed) if absf(horizontal_speed) > 0.01 else float(-facing))
	if horizontal_speed * -side > 0.01 and opponent.hitstop <= 0.0:
		var push := maxf(0.0, gap - absf(difference))
		var low := -6.0
		var high := 6.0
		if camera_bounds != null:
			var lim: Vector2 = camera_bounds.movement_limits(opponent)
			low = lim.x
			high = lim.y
		var destination_other := clampf(opponent.position().x - side * push, low, high)
		opponent.body.move(Vector2(destination_other - opponent.position().x, 0))
	var destination := opponent.position().x + side * gap
	var minimum := -6.0
	var maximum := 6.0
	if camera_bounds != null:
		var lim2: Vector2 = camera_bounds.movement_limits(self)
		minimum = lim2.x
		maximum = lim2.y
	if destination < minimum or destination > maximum:
		destination = clampf(destination, minimum, maximum)
		var other_low := minimum
		var other_high := maximum
		if camera_bounds != null:
			var lim3: Vector2 = camera_bounds.movement_limits(opponent)
			other_low = lim3.x
			other_high = lim3.y
		var corrected_other := clampf(destination - side * gap, other_low, other_high)
		opponent.body.move(Vector2(corrected_other - opponent.position().x, 0))
	body.move(Vector2(destination - position().x, 0))

# ---------------------------------------------------------------- air dash
func _try_start_air_dash() -> bool:
	var a: Dictionary = data.raw.air_dash
	if phase != P.AIR or grounded or air_dash_used or is_air_dashing or air_combat_active or air_clock.is_busy() or stun_remaining > 0 or hitstop > 0 or is_down() or movement.x * facing < -0.1:
		return false
	air_dash_used = true
	is_air_dashing = true
	air_dash_count += 1
	_air_dash_remaining = int(a.duration_frames)
	_air_dash_speed = Sk.f(Sk.f(float(a.distance) * 60.0) / int(a.duration_frames))
	_air_dash_direction = facing
	_air_dash_residual_ratio = float(a.post_dash_speed_ratio)
	_air_dash_residual_deceleration = float(a.post_dash_deceleration)
	_air_dash_residual = false
	horizontal_speed = _air_dash_direction * _air_dash_speed
	if a.reset_vertical_velocity_on_start:
		vertical_speed = 0.0
	return true

func _stop_air_dash() -> void:
	if not is_air_dashing:
		return
	is_air_dashing = false
	_air_dash_remaining = 0
	_air_dash_residual = true
	horizontal_speed = Sk.f(_air_dash_direction * _air_dash_speed * _air_dash_residual_ratio)

func _reset_air_dash() -> void:
	_stop_air_dash()
	_air_dash_residual = false
	air_dash_used = false
	air_dash_count = 0

func _prepare_air_dash() -> void:
	if not is_air_dashing:
		if _air_dash_residual and phase == P.AIR:
			horizontal_speed = Sk.f(Sk.move_toward(horizontal_speed, 0.0, Sk.f(_air_dash_residual_deceleration * Sk.fdt())))
			if absf(horizontal_speed) < 0.00001:
				_air_dash_residual = false
		return
	if phase != P.AIR or stun_remaining > 0 or air_combat_active or is_down() or _air_dash_remaining <= 0:
		_stop_air_dash()
		return
	horizontal_speed = _air_dash_direction * _air_dash_speed

func _finish_air_dash_step() -> void:
	if is_air_dashing:
		_air_dash_remaining -= 1

func _land_air_dash() -> void:
	_stop_air_dash()
	_air_dash_residual = false
	air_dash_used = false

# ---------------------------------------------------------------- v0.1 combat
func _v01_update_guard() -> void:
	var direction := 0
	if relative_back_held():
		direction = 1 if movement.y < -0.5 else (7 if movement.y > 0.5 else 4)
	var raw := (-1 if movement.x < -0.1 else (1 if movement.x > 0.1 else 0)) * 10 + (-1 if movement.y < -0.5 else (1 if movement.y > 0.5 else 0))
	if direction == 0:
		_v01_jg_start = -1
	elif raw != _v01_guard_direction:
		_v01_jg_start = input_buffer.frame
		_v01_jg_continuous = last_result == "Guard" and stun_remaining > 0
	_v01_guard_direction = raw
	guard_held = direction != 0

func _v01_tick_stop() -> void:
	_v01_frozen_this_tick = true
	_v01_stop = maxi(0, _v01_stop - 1)
	hitstop = Sk.f(_v01_stop / 60.0)

func _v01_tick_stun() -> void:
	_v01_frozen_this_tick = true
	_v01_stun = maxi(0, _v01_stun - 1)
	stun_remaining = Sk.f(_v01_stun / 60.0)
	if _v01_stun == 0:
		_v01_block = false

func _v01_tick_block() -> bool:
	if not v01_block_paused():
		return false
	_v01_tick_stun()
	_v01_tick_push()
	update_hurtboxes()
	return true

func _v01_tick_push() -> void:
	for i in range(_v01_push.size() - 1, -1, -1):
		var push: Dictionary = _v01_push[i]
		var delta: float = CombatFormula.push_step(push.distance, push.frame, push.total) * push.direction
		push.frame += 1
		_move_fd_actor(self, delta)
		if push.frame >= push.total:
			_v01_push.remove_at(i)

func v01_add_push(distance: float, direction: float) -> void:
	if distance > 0:
		_v01_push.append({"distance": distance, "frame": 0, "total": int(combat.push_frames), "direction": direction})

func v01_stop_for(frames: int) -> void:
	_v01_stop = maxi(_v01_stop, frames)
	hitstop = Sk.f(_v01_stop / 60.0)

func v01_connect() -> void:
	attack_connected = true

func v01_receive(result: CombatFormula.Result, attacker: Fighter, part: Dictionary) -> void:
	if health <= 0:
		return
	v01_last_contact = result
	last_contact_hurtbox_id = part.id
	last_level_difference = Sk.f(result.H - result.B)
	health = maxf(0.0, health - result.damage)
	last_result = Sk.OUTCOME_NAMES[result.outcome]
	_v01_stun = result.stun_frames
	stun_remaining = Sk.f(_v01_stun / 60.0)
	_v01_block = result.outcome == Sk.Outcome.BLOCK
	if result.outcome == Sk.Outcome.HIT:
		_stop_air_dash()
		_release_throw()
		air_clock.end()
		air_combat_active = false
		current_move = null
		input_buffer.clear_pending()
		phase = P.HITSTUN if grounded else P.AIR
		timer = 0.0
		hit_count += 1
	elif result.outcome == Sk.Outcome.GUARD:
		guard_count += 1
		if grounded:
			phase = P.GUARD_STUN
		timer = 0.0
		if result.fd:
			fd_guard_count += 1
			last_fd_cost = 0.0
			last_fd_push_distance = result.attacker_push
	if _v01_stun == 0 and (phase == P.GUARD_STUN or phase == P.HITSTUN):
		phase = P.IDLE if grounded else P.AIR
	contact_sequence += 1
	last_resolved_contact = {"sequence": contact_sequence, "part": part.id, "raw_attack": result.A, "defense": result.D,
		"final": result.H, "threshold": result.B, "hit": result.outcome == Sk.Outcome.HIT, "damage": result.damage,
		"stun_frames": result.stun_frames, "hitstop_frames": result.hitstop_frames}
	var away := float(-attacker.facing) if position().x == attacker.position().x else signf(position().x - attacker.position().x)
	v01_add_push(result.self_push, away)
	attacker.v01_add_push(result.attacker_push, -away)
	v01_stop_for(result.hitstop_frames)
	attacker.v01_stop_for(result.hitstop_frames)
	update_hurtboxes()

## World-X displacement only; stored velocities and the wall snapshot are preserved.
static func _move_fd_actor(actor: Fighter, displacement: float) -> float:
	if not actor.body.enabled:
		return 0.0
	var before := actor.position().x
	var after: float = clampf(before + displacement, -6.0, 6.0) if actor.camera_bounds == null else actor.camera_bounds.limit_position(before, before + displacement)
	actor.body.move(Vector2(after - before, 0))
	return actor.position().x - before

# ---------------------------------------------------------------- legacy contact (throws)
func _resolve_attack(level: float, incoming_damage: float, source_level: float, attack_class: int, knocks_down: bool, defense_override: float = -1.0) -> bool:
	_stop_air_dash()
	if is_throw_linked() or is_throw_state():
		_release_throw()
	if is_down():
		return false  # allowDownFollowup is off in the shared legacy rules
	air_clock.hold(AirClock.Pause.STUN)
	var defense := defense_override if defense_override >= 0.0 else defense_for(attack_class)
	var final_result := CombatFormula.legacy_result(level, defense)
	var continued := last_result == "Hit" and stun_remaining > 0
	var health_before := health
	var base_level := level if source_level < 0.0 else source_level
	last_level_difference = CombatFormula.legacy_result(level, defense)
	if final_result > 0.0:
		health = maxf(0.0, health - incoming_damage)
		hit_count += 1
		last_result = "Hit"
		stun_remaining = Sk.f(maxf(0.0, CombatFormula.legacy_hitstun_frames(base_level, last_level_difference)) / 60.0)
		if grounded:
			phase = P.HITSTUN
		if knocks_down:
			pending_knockdown = true
			stun_remaining = _down_duration()
			air_combat_active = false
			air_clock.end()
			current_move = null
			input_buffer.clear_pending()
			if grounded:
				_begin_down()
	else:
		health = maxf(0.0, health - incoming_damage * 0.0)
		guard_count += 1
		last_result = "Guard"
		stun_remaining = Sk.f(CombatFormula.legacy_guardstun_frames(base_level, defense) / 60.0)
		if grounded:
			phase = P.GUARD_STUN
	hitstop = Sk.f(CombatFormula.legacy_hitstop_frames(base_level) / 60.0)
	timer = 0.0
	contact_sequence += 1
	last_resolved_contact = {"sequence": contact_sequence, "part": last_contact_hurtbox_id, "raw_attack": base_level, "defense": defense,
		"final": final_result, "threshold": 0.0, "hit": last_result == "Hit", "continued": continued, "damage": health_before - health,
		"stun_frames": stun_remaining * 60.0, "hitstop_frames": hitstop * 60.0}
	update_hurtboxes()
	return true

# ---------------------------------------------------------------- throw
func refresh_throw_volumes() -> void:
	var th: Dictionary = data.raw.throw_hurtbox
	throw_hurtbox.offset = Sk.from_legacy(th.center)
	throw_hurtbox.size = Sk.half_size(th.size)
	throw_hurtbox.enabled = not throw_invulnerable
	var t := throw_settings()
	throwbox.offset = Sk.from_legacy(t.box_center, facing)
	throwbox.size = Sk.half_size(t.box_size)
	throwbox.enabled = throw_active()

func _begin_throw() -> void:
	refresh_facing()
	_set_crouching(false)
	crouch_pose_progress = 0.0
	current_move = null
	air_combat_active = false
	phase = P.THROW_ATTEMPT
	timer = 0.0
	_throw_resolved = false
	last_throw_result = "Attempt"

## The holder advances one shared hold clock; the victim is held but TRC input is read first.
func _tick_throw(dt: float) -> bool:
	if phase == P.THROWN:
		return true
	if phase != P.THROW_HOLD:
		return false
	if throw_partner == null:
		_release_throw()
		return true
	timer = Sk.f(timer + dt)
	throw_partner.stun_remaining = maxf(0.0, Sk.f(_throw_hold_seconds() - timer))
	if timer >= _throw_hold_seconds():
		var victim := throw_partner
		var level := _caught_level
		var defense := _caught_defense
		var t := throw_settings()
		_release_throw()
		victim._resolve_attack(level, float(t.damage), float(t.attack_level), Sk.AttackClass.THROW, true, defense)
		hitstop = Sk.f(Sk.f(float(t.attack_level) * Sk.f(0.2)) / 60.0)
		tension.gain(float(t.hit_gain), float(data.raw.legacy_rules.tensionLevelGainRate))
		throw_count += 1
		last_throw_result = "Hit"
		victim.last_throw_result = "Received"
	return true

func _throwbox_matches(target: Fighter) -> bool:
	if not throwbox.enabled or target == self or not target.throw_hurtbox.enabled or target.throw_invulnerable:
		return false
	var category := 1 if target.grounded else 2
	return (int(throw_settings().targets) & category) != 0

func _resolve_throw_attempt() -> void:
	if phase != P.THROW_ATTEMPT:
		return
	if throw_active() and not _throw_resolved:
		var target := opponent
		var t := throw_settings()
		if target != null and not target.is_down() and not target.is_throw_linked() and target.stun_remaining <= 0 and target.hitstop <= 0:
			refresh_throw_volumes()
			target.refresh_throw_volumes()
			if _throwbox_matches(target) and Sk.aabb_overlap(position() + throwbox.offset, throwbox.size * 0.5,
					target.position() + target.throw_hurtbox.offset, target.throw_hurtbox.size * 0.5):
				if target.phase == P.THROW_ATTEMPT and not target._throw_resolved and target.timer <= target._throw_active_end_seconds():
					_throw_resolved = true
					target._throw_resolved = true
					last_throw_result = "Clash"
					target.last_throw_result = "Clash"
					timer = _throw_active_end_seconds()
					target.timer = target._throw_active_end_seconds()
					target.refresh_throw_volumes()
				else:
					_throw_resolved = true
					_caught_level = Sk.f(float(t.attack_level) * tension.bonus())
					_caught_defense = target.defense_for(Sk.AttackClass.THROW)
					if _caught_level <= _caught_defense:
						target._resolve_attack(_caught_level, 0.0, float(t.attack_level), Sk.AttackClass.THROW, false)
						hitstop = Sk.f(Sk.f(float(t.attack_level) * Sk.f(0.2)) / 60.0)
						tension.gain(float(t.hit_gain), float(data.raw.legacy_rules.tensionLevelGainRate))
						last_throw_result = "Resisted"
					else:
						throw_partner = target
						target.throw_partner = self
						phase = P.THROW_HOLD
						target.phase = P.THROWN
						timer = 0.0
						target.timer = 0.0
						target.stun_remaining = _throw_hold_seconds()
						target.last_result = "ThrowHold"
						target._set_crouching(false)
						target.crouch_pose_progress = 0.0
						target.current_move = null
						target.air_combat_active = false
						target.air_clock.end()
						target.input_buffer.clear_pending()
						target.update_hurtboxes()
						last_throw_result = "Holding"
						target.last_throw_result = "Caught"
	if phase == P.THROW_ATTEMPT and timer >= _throw_attempt_seconds():
		if not _throw_resolved:
			last_throw_result = "Whiff"
		phase = P.IDLE
		timer = 0.0
		completed_attacks += 1
		_execute_buffered(true)

func _release_throw() -> void:
	var partner := throw_partner
	throw_partner = null
	if phase == P.THROW_ATTEMPT:
		phase = P.IDLE if grounded else P.AIR
		timer = 0.0
		_throw_resolved = true
		last_throw_result = "Canceled"
		input_buffer.clear_pending()
	if phase == P.THROWN or phase == P.THROW_HOLD:
		phase = P.IDLE if grounded else P.AIR
		timer = 0.0
		stun_remaining = 0.0
		current_move = null
		input_buffer.clear_pending()
		last_throw_result = "Released"
	update_hurtboxes()
	refresh_throw_volumes()
	if partner != null and partner.throw_partner == self:
		partner.throw_partner = null
		if partner.phase == P.THROWN or partner.phase == P.THROW_HOLD:
			partner.phase = P.IDLE if partner.grounded else P.AIR
			partner.timer = 0.0
			partner.stun_remaining = 0.0
			partner.current_move = null
			partner.input_buffer.clear_pending()
			partner.last_throw_result = "Released"
			partner.update_hurtboxes()
			partner.refresh_throw_volumes()

# ---------------------------------------------------------------- hurtboxes / motion
## TrainingFighter.MotionAction (name in awari.json "motions")
func motion_action() -> String:
	if stun_remaining > 0 or is_guarding() or is_down() or is_throw_state() or is_air_combat_attack():
		return ""
	if is_air_dashing: return "airDash"
	if phase == P.AIR or phase == P.TAKEOFF: return "jump"
	if phase == P.BACKSTEP: return "backstep"
	if not grounded: return ""
	if is_crouching: return "crouch" if phase == P.CROUCH else ""
	if phase == P.IDLE: return "idle"
	if phase == P.MOVE:
		if horizontal_speed * facing < -0.01: return "backwardWalk"
		if dash_inertia or absf(horizontal_speed) > def.walkSpeed + 0.1: return "groundDash"
		return "forwardWalk"
	return ""

static func backstep_sequence_index(elapsed_seconds: float, key2: float, key3: float) -> int:
	var f := maxf(0.0, elapsed_seconds * 60.0 + 0.0001)
	if f < key2:
		return clampi(int(floor(f * 4.0 / maxf(0.0001, key2))), 0, 3)
	if f < key3:
		return 4 + clampi(int(floor((f - key2) * 4.0 / maxf(0.0001, key3 - key2))), 0, 3)
	return 8

## Returns the selected pose dictionary {sprite, hold, offset, hurt} or {}.
func motion_pose() -> Dictionary:
	var action := motion_action()
	if action != _sampled_motion or timer < _motion_start:
		_sampled_motion = action
		_motion_start = timer
	if action.is_empty():
		return {}
	var m: Dictionary = data.motion(action)
	var poses: Array = m.get("poses", [])
	if poses.is_empty():
		return {}
	var playback := int(m.playback)
	if playback == 1 and poses.size() >= 2:
		return poses[0 if phase == P.TAKEOFF or vertical_speed > 0 else 1]
	if playback == 2 and poses.size() == 9:
		var loco := locomotion()
		var elapsed := Sk.f(def.backstepSeconds - Sk.f(remaining_lock_frames() / 60.0))
		return poses[backstep_sequence_index(elapsed, float(loco.pose1EndFrame), float(loco.pose2EndFrame))]
	var frame := maxi(0, int(floor(Sk.f(timer - _motion_start) * 60.0 + 0.0001)))
	var total := 0
	for p in poses:
		total += int(p.hold)
	var f := frame % total if bool(m.loop) else mini(frame, total - 1)
	for p in poses:
		if f < int(p.hold):
			return p
		f -= int(p.hold)
	return poses[poses.size() - 1]

func _motion_hurt_geometry(id: int) -> Array:
	var pose := motion_pose()
	if pose.is_empty():
		return []
	for h in pose.hurt:
		if int(h.part) == id:
			if h.inherit_body:
				return []
			return [Vector2(float(h.center[0]) * facing, float(h.center[1])), Vector2(float(h.size[0]), float(h.size[1]))]
	return []

func update_hurtboxes() -> void:
	var down := is_down() and grounded
	for hb in hurtboxes:
		var r: Dictionary = hb.region
		var geometry: Array = [] if down else _motion_hurt_geometry(hb.id)
		if not geometry.is_empty():
			hb.offset = geometry[0]
			hb.size = geometry[1]
		elif int(r.shape) == 2 and not down:
			hb.offset = Sk.from_legacy(r.center, facing)
			hb.size = Sk.half_size(r.size)
		else:
			var h := 0.35 if down else body.height
			var split := h * 0.5 if down else minf(0.75, h * 0.6)
			var bottom := 0.0 if down else body.center.y - body.height * 0.5
			var whole := int(r.shape) == 3
			var upper := int(r.shape) == 0
			var part_height := h if whole else (h - split if upper else split)
			hb.offset = Vector2(0, bottom + (split if (not whole and upper) else 0.0) + part_height * 0.5)
			hb.size = Vector2(1.2 if down else body.radius * 2.0, part_height)
		hb.enabled = not is_down()

## SelectHurtboxContacts for the v0.1 path: the opponent's enabled hurtbox overlapping the
## hitbox with the highest priority, then the highest H, then the lowest id.
func select_hurtbox_contact(target: Fighter) -> Dictionary:
	var center := hitbox_center()
	var half := hitbox_half_size()
	var chosen: Dictionary = {}
	var before := 0.0
	var kind := current_attack_class()
	for hb in target.hurtboxes:
		if not hb.enabled:
			continue
		if not Sk.aabb_overlap(center, half, target.position() + hb.offset, hb.size * 0.5):
			continue
		var value := CombatFormula.hit_level(attack_level(), target.v01_defense(hb, kind), 0, 0, sys)
		if chosen.is_empty() or CombatFormula.prefer(int(hb.region.priority), value, hb.id, int(chosen.region.priority), before, chosen.id):
			chosen = hb
			before = value
	return chosen
