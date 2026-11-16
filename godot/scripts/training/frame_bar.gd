class_name FrameBar
extends RefCounted
## TrainingFrameBar + TrainingFrameHistory: 120 retained frames per player, newest last.

const CAPACITY := 120

class History:
	extends RefCounted
	var items: Array = []
	var _next := 0
	var _last_reset := -2147483648
	var _last_trc := 0
	var _last_fd := 0
	var _last_frame := -1
	var count := 0
	var version := 0
	func _init() -> void:
		items.resize(CAPACITY)
	func newest(age: int = 0) -> Dictionary:
		return items[(_next - 1 - age + CAPACITY) % CAPACITY]
	func capture(s: Dictionary, reset_version: int, trc_count: int, fd_count: int) -> void:
		if reset_version != _last_reset or s.frame < _last_frame:
			count = 0
			_next = 0
			_last_trc = 0
			_last_fd = 0
			_last_frame = -1
			_last_reset = reset_version
			version += 1
		if s.frame <= _last_frame:
			return
		var first: int = s.frame if _last_frame < 0 else maxi(_last_frame + 1, s.frame - CAPACITY + 1)
		for frame in range(first, s.frame + 1):
			var item := s.duplicate()
			item.frame = frame
			item.trc = maxi(0, trc_count - _last_trc) if frame == s.frame else 0
			item.fd = maxi(0, fd_count - _last_fd) if frame == s.frame else 0
			items[_next] = item
			_next = (_next + 1) % CAPACITY
			count = mini(CAPACITY, count + 1)
		_last_frame = s.frame
		_last_trc = trc_count
		_last_fd = fd_count
		version += 1

var first_history := History.new()
var second_history := History.new()
var first: Fighter
var second: Fighter

func configure(p: Fighter, o: Fighter) -> void:
	first = p
	second = o

func estimated_advantage() -> float:
	return 0.0 if first == null or second == null else second.stun_remaining * 60.0 - first.remaining_lock_frames()

static func snapshot(f: Fighter) -> Dictionary:
	var attack := float(f.throw_settings().attack_level) if f.current_attack_class() == Sk.AttackClass.THROW else f.attack_level()
	var opponent_class := Sk.AttackClass.MID if f.opponent == null else f.opponent.current_attack_class()
	return {"frame": f.input_buffer.frame, "phase": f.phase, "lock": f.lock_kind(), "stop": f.hitstop * 60.0,
		"remaining": f.remaining_lock_frames(), "attack": attack, "defense": f.defense_for(opponent_class),
		"difference": f.last_level_difference, "trc": 0, "fd": 0}

func capture() -> void:
	if first != null:
		first_history.capture(snapshot(first), first.input_buffer.reset_version, first.cancel_count, first.fd_guard_count)
	if second != null:
		second_history.capture(snapshot(second), second.input_buffer.reset_version, second.cancel_count, second.fd_guard_count)

static func color_for(s: Dictionary) -> Color:
	if s.trc > 0:
		return Color.WHITE
	if s.fd > 0:
		return Color(.1, .9, .65)
	var c := Color(.28, .32, .38)
	match int(s.lock):
		Sk.Lock.STARTUP, Sk.Lock.THROW_STARTUP: c = Color.YELLOW
		Sk.Lock.ACTIVE, Sk.Lock.THROW_ACTIVE: c = Color(1, .2, .1)
		Sk.Lock.RECOVERY, Sk.Lock.THROW_RECOVERY: c = Color(1, .55, .12)
		Sk.Lock.BLOCKSTUN: c = Color(.3, .6, 1)
		Sk.Lock.GUARD_STUN: c = Color.CYAN
		Sk.Lock.HITSTUN, Sk.Lock.KNOCKDOWN, Sk.Lock.DOWN, Sk.Lock.GET_UP, Sk.Lock.THROW_HOLD, Sk.Lock.THROWN: c = Color(.7, .3, .95)
		_:
			if int(s.phase) == Sk.Phase.MOVE:
				c = Color(.3, .7, .35)
			elif int(s.phase) == Sk.Phase.AIR:
				c = Color(.3, .55, .95)
	return c.lerp(Color.GRAY, .5) if float(s.stop) > 0 else c
