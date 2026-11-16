class_name TrainingSession
extends RefCounted
## TrainingSession: routes 1P / dummy inputs, recording and playback, Reset, HP recovery.

enum StartPosition { LEFT, CENTER, RIGHT }
enum DummyGuard { NONE, GUARD, FD, ALWAYS_FD }

class PlayerSettings:
	extends RefCounted
	var hp_percent := 100
	var tg := 0
	var recover := true
	var fixed_tg := false

## TrainingRecoveryClock: HP returns 3 simulation seconds after the last damage, once not busy.
class RecoveryClock:
	extends RefCounted
	var _previous := 0.0
	var _age := 0.0
	var _initialized := false
	func rebase(hp: float) -> void:
		_previous = hp
		_age = 0.0
		_initialized = true
	func tick(hp: float, target: float, enabled: bool, busy: bool, seconds: float) -> bool:
		if not _initialized:
			rebase(hp)
		if hp < _previous - 0.00001:
			_age = 0.0
		else:
			_age += maxf(0.0, seconds)
		_previous = hp
		if not enabled or busy or _age + 1e-7 < 3.0 or hp >= target:
			return false
		_previous = target
		return true

var match_: Match
var owner   # TrainingScene (option_visible, second_player_controlled, capture_command_displays)
var first: Fighter
var second: Fighter
var dummy: TrainingDummy
var first_settings := PlayerSettings.new()
var second_settings := PlayerSettings.new()
var start_position := StartPosition.CENTER
var guard_mode := DummyGuard.NONE
var dummy_jg := 0
var recorder := TrainingRecorder.new()
var _recovery1 := RecoveryClock.new()
var _recovery2 := RecoveryClock.new()
var _first_frame := InputFrame.new()
var _second_frame := InputFrame.new()
var _previous_mode := TrainingRecorder.Mode.IDLE
var _routing_controlled := false
var _reset_pending := false
var _record_pending := false
var _play_pending := false
var resetting := false
var notice := ""

func _init(m: Match, scene, dummy_source: TrainingDummy) -> void:
	match_ = m
	owner = scene
	first = m.player
	second = m.dummy_fighter
	dummy = dummy_source
	m.attach_session(self)
	_recovery1.rebase(first.health)
	_recovery2.rebase(second.health)

func dispose() -> void:
	match_ = null
	owner = null
	first = null
	second = null
	if dummy != null:
		dummy.fighter = null
	dummy = null

func settings(player: int) -> PlayerSettings:
	return first_settings if player == 0 else second_settings

func _second_player_controlled() -> bool:
	return owner != null and owner.second_player_controlled

func auto_dummy() -> bool:
	return second.input_source == dummy and not _second_player_controlled() and not _routing_controlled \
		and recorder.state != TrainingRecorder.Mode.RECORDING and recorder.state != TrainingRecorder.Mode.PLAYBACK

func hold_dummy_position(actor: Fighter) -> bool:
	return actor == second and auto_dummy() and _second_frame.fd_held

func suppress_dummy_reaction(actor: Fighter) -> bool:
	return actor == second and not auto_dummy()

## -1 = not forced (use the fighter's own just-guard timing)
func dummy_jg_for(actor: Fighter) -> int:
	return dummy_jg if actor == second and auto_dummy() and guard_mode != DummyGuard.NONE else -1

func hp_assigned(actor: Fighter) -> void:
	if actor == first:
		_recovery1.rebase(actor.health)
		match_.second_attack.rebase_hp(actor.health)
	elif actor == second:
		_recovery2.rebase(actor.health)
		match_.first_attack.rebase_hp(actor.health)

func set_hp(player: int, percent: int) -> void:
	var s := settings(player)
	s.hp_percent = clampi(percent, 1, 100)
	var f := first if player == 0 else second
	f.set_training_hp(f.def.maxHealth * s.hp_percent / 100.0)
	(_recovery1 if player == 0 else _recovery2).rebase(f.health)

func set_tg(player: int, value: int) -> void:
	var s := settings(player)
	s.tg = clampi(value, -200, 200)
	(first if player == 0 else second).tension.set_training_gauge(s.tg)
	notice = ""

func set_fixed(player: int, value: bool) -> void:
	settings(player).fixed_tg = value
	(first if player == 0 else second).tension.fixed = value

func request_reset() -> void: _reset_pending = true
func request_record() -> void: _record_pending = true
func request_play() -> void: _play_pending = true

func _boundary() -> void:
	first.clear_input_boundary()
	second.clear_input_boundary()
	if owner != null:
		owner.unblock_controls()
	_first_frame = InputFrame.new()
	_second_frame = InputFrame.new()

func menu_opened() -> void:
	_record_pending = false
	_play_pending = false
	_reset_pending = false
	recorder.cancel(true)
	_boundary()

func reset_configured() -> void:
	resetting = true
	recorder.cancel(false)
	_record_pending = false
	_play_pending = false
	_reset_pending = false
	_boundary()
	match_.combat.reset_test()
	var middle := -3.7 if start_position == StartPosition.LEFT else (3.7 if start_position == StartPosition.RIGHT else 0.0)
	first.body.teleport(Vector2(middle - 0.8, 0.01))
	second.body.teleport(Vector2(middle + 0.8, 0.01))
	first.refresh_facing()
	second.refresh_facing()
	match_.camera.snap_framing()
	set_hp(0, first_settings.hp_percent)
	set_hp(1, second_settings.hp_percent)
	set_tg(0, first_settings.tg)
	set_tg(1, second_settings.tg)
	set_fixed(0, first_settings.fixed_tg)
	set_fixed(1, second_settings.fixed_tg)
	notice = "Reset configured position / HP / TG"
	resetting = false

func read_for(fighter: Fighter) -> InputFrame:
	return (_first_frame if fighter == first else _second_frame).copy()

func fixed_update() -> void:
	if owner != null and owner.option_visible:
		return
	var live: InputFrame = first.input_source.read_input() if first.input_source != null else InputFrame.new()
	if live.record_pressed:
		_record_pending = true
	if live.play_pressed:
		_play_pending = true
	if _reset_pending or live.reset:
		reset_configured()
		return
	if _record_pending:
		recorder.record_key()
		_record_pending = false
	if _play_pending:
		recorder.play_key()
		_play_pending = false
	var mode := recorder.state
	if _previous_mode != mode:
		_boundary()
		_previous_mode = mode
	if live.toggle_opponent_guard:
		guard_mode = DummyGuard.GUARD if guard_mode == DummyGuard.NONE else DummyGuard.NONE
	live = TrainingRecorder.logical(live)
	_routing_controlled = mode == TrainingRecorder.Mode.RECORDING or mode == TrainingRecorder.Mode.PLAYBACK
	var recorded := recorder.tick(live)
	_first_frame = InputFrame.new() if mode == TrainingRecorder.Mode.COUNTDOWN or mode == TrainingRecorder.Mode.RECORDING else live
	if mode == TrainingRecorder.Mode.RECORDING or mode == TrainingRecorder.Mode.PLAYBACK:
		_second_frame = recorded
	elif mode == TrainingRecorder.Mode.COUNTDOWN:
		_second_frame = InputFrame.new()
	else:
		_second_frame = second.input_source.read_input() if second.input_source != null else InputFrame.new()
	if auto_dummy():
		second.guarding = guard_mode != DummyGuard.NONE
		var fd := guard_mode == DummyGuard.ALWAYS_FD or (guard_mode == DummyGuard.FD and first.is_guard_threat_motion())
		if fd:
			_second_frame.movement.x = -second.opponent_direction_for_guard()
			_second_frame.fd_held = true
	elif mode == TrainingRecorder.Mode.RECORDING or mode == TrainingRecorder.Mode.PLAYBACK:
		second.guarding = false

func after_tick() -> void:
	if owner != null and owner.option_visible:
		return
	_recover(first, first_settings, _recovery1)
	_recover(second, second_settings, _recovery2)
	if owner != null:
		owner.capture_command_displays()

static func _recover(actor: Fighter, s: PlayerSettings, clock: RecoveryClock) -> void:
	var hp: float = actor.def.maxHealth * s.hp_percent / 100.0
	if clock.tick(actor.health, hp, s.recover, actor.training_recovery_busy(), Sk.fdt()):
		actor.set_training_hp(hp)
