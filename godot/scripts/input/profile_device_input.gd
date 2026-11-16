class_name ProfileDeviceInput
extends RefCounted
## ProfileDeviceInput (1P): watches every Battle/Training binding of the saved KeyConfig per
## device. T1/T2 chords have no grace window; Read() is the 60 Hz logical-frame boundary.

const KB_MOVE := ["w", "a", "s", "d", "upArrow", "downArrow", "leftArrow", "rightArrow"]
const PAD_MOVE := ["dpad/up", "dpad/down", "dpad/left", "dpad/right"]

var _hub
var watches: Array = []    # {id, context, modifier, pad, controls, states: {device: {...}}}
var triggers: Array = []   # {number, pad, controls}
var devices := {}          # device -> {m1, m2, seen1, seen2}
var commands: Array = []
var movement := Vector2.ZERO
var neutral := false

func _init(hub) -> void:
	_hub = hub
	var profile := KeyConfig.ensure()
	for m in [1, 2]:
		var r := KeyConfig.row(profile, "trigger.T%d" % m)
		for pad in [false, true]:
			var value := KeyConfig.normalize(r.pad if pad else r.keyboard)
			if not value.is_empty():
				var pc := KeyConfig.path_controls(value)
				triggers.append({"number": m, "pad": pc.pad, "controls": pc.controls})
	for r in profile.bindings:
		if r.context != "Battle" and r.context != "Training":
			continue
		for pad in [false, true]:
			var value := KeyConfig.normalize(r.pad if pad else r.keyboard)
			if value.is_empty():
				continue
			var pc := KeyConfig.path_controls(value)
			watches.append({"id": r.id, "context": r.context, "modifier": KeyConfig.modifier(value), "pad": pc.pad,
				"controls": pc.controls, "states": {}})
	if _hub != null:
		_hub.control_changed.connect(_on_control)
		_hub.stick_changed.connect(_on_stick)
		_hub.devices_reset.connect(clear)

func detach() -> void:
	if _hub != null:
		_hub.control_changed.disconnect(_on_control)
		_hub.stick_changed.disconnect(_on_stick)
		_hub.devices_reset.disconnect(clear)
		_hub = null

func _device(id: String) -> Dictionary:
	if not devices.has(id):
		devices[id] = {"m1": false, "m2": false, "seen1": false, "seen2": false}
	return devices[id]

func _any(device: String, controls: Array) -> bool:
	for c in controls:
		if _hub.pressed(device, c):
			return true
	return false

func _on_control(device: String, control: String, _pressed: bool) -> void:
	var is_pad := device != "kb"
	for t in triggers:
		if t.pad == is_pad and t.controls.has(control):
			var d := _device(device)
			var held := _any(device, t.controls)
			if t.number == 1:
				d.m1 = held
				d.seen1 = d.seen1 or held
			else:
				d.m2 = held
				d.seen2 = d.seen2 or held
	for w in watches:
		if w.pad != is_pad or not w.controls.has(control):
			continue
		var d2 := _device(device)
		if not w.states.has(device):
			w.states[device] = {"held": false, "pressed": false, "at1": false, "at2": false, "previous": false}
		var state: Dictionary = w.states[device]
		var held2 := _any(device, w.controls)
		if held2 and not state.held:
			state.pressed = true
			state.at1 = state.at1 or d2.m1
			state.at2 = state.at2 or d2.m2
		state.held = held2
	if (not is_pad and KB_MOVE.has(control)) or (is_pad and PAD_MOVE.has(control)):
		_update_movement()

func _on_stick(_device: String, _value: Vector2) -> void:
	_update_movement()

static func _digital(up: bool, down: bool, left: bool, right: bool) -> Vector2:
	return Vector2((1.0 if right else 0.0) - (1.0 if left else 0.0), (1.0 if up else 0.0) - (1.0 if down else 0.0))

func _current_movement() -> Vector2:
	var candidates: Array = [
		_digital(_hub.pressed("kb", "w"), _hub.pressed("kb", "s"), _hub.pressed("kb", "a"), _hub.pressed("kb", "d")),
		_digital(_hub.pressed("kb", "upArrow"), _hub.pressed("kb", "downArrow"), _hub.pressed("kb", "leftArrow"), _hub.pressed("kb", "rightArrow")),
	]
	for device in _hub.devices():
		if device == "kb":
			continue
		candidates.append(_hub.sticks.get(device, Vector2.ZERO))
		candidates.append(_digital(_hub.pressed(device, "dpad/up"), _hub.pressed(device, "dpad/down"), _hub.pressed(device, "dpad/left"), _hub.pressed(device, "dpad/right")))
	var best := Vector2.ZERO
	for v in candidates:
		if v.length_squared() > best.length_squared():
			best = v
	return best

func _update_movement() -> void:
	var v := _current_movement()
	if v == movement:
		return
	if v.y > 0.5 and movement.y <= 0.5:
		commands.append(Sk.Cmd.JUMP)
	movement = v

func any_held() -> bool:
	for w in watches:
		for device in _hub.devices():
			if (device != "kb") == w.pad and _any(device, w.controls):
				return true
	for t in triggers:
		for device in _hub.devices():
			if (device != "kb") == t.pad and _any(device, t.controls):
				return true
	return _current_movement().length_squared() > 0.01

static func _try_slot(id: String) -> int:
	if not id.begins_with("battle."):
		return -1
	return Sk.SLOT_NAMES.find(id.substr(7))

func read(training: bool) -> InputFrame:
	if neutral:
		var held := any_held()
		_drop()
		if not held:
			neutral = false
		return InputFrame.new()
	var frame := InputFrame.new()
	frame.movement = movement
	for w in watches:
		for device in w.states.keys():
			var state: Dictionary = w.states[device]
			var d := _device(device)
			var one: bool = d.m1 or d.seen1 or state.at1
			var two: bool = d.m2 or d.seen2 or state.at2
			var independent: bool = w.id == "battle.Mania" or w.context == "Training"
			var m: int = w.modifier
			var decision: bool = (independent or (not one and not two)) if m == 0 else ((one and not two) if m == 1 else (two and not one))
			var active: bool = state.held and ((independent or (not d.m1 and not d.m2)) if m == 0 else ((d.m1 and not d.m2) if m == 1 else (d.m2 and not d.m1)))
			var edge: bool
			if m == 0:
				edge = state.pressed and decision
			else:
				edge = (state.held or state.pressed) and decision and (not state.previous or state.pressed or (d.seen1 if m == 1 else d.seen2))
			state.previous = active
			if w.context != "Training" or training:
				if w.id == "battle.Mania":
					frame.force_held = frame.force_held or active
				elif w.id == "battle.Dash":
					frame.dash = frame.dash or active
				var slot := _try_slot(w.id)
				if slot >= 0:
					if active:
						frame.attack_held_mask |= 1 << slot
					if edge:
						frame.attack_pressed_mask |= 1 << slot
				elif edge:
					match w.id:
						"battle.Hold": commands.append(Sk.Cmd.THROW)
						"battle.TRC": commands.append(Sk.Cmd.CANCEL)
						"battle.Dash":
							frame.dash_pressed = true
							frame.dash = true
						"training.Record": frame.record_pressed = true
						"training.Play": frame.play_pressed = true
						"training.Reset": frame.reset = true
			state.pressed = false
			state.at1 = false
			state.at2 = false
	for d in devices.values():
		d.seen1 = false
		d.seen2 = false
	frame.commands = commands.duplicate()
	commands.clear()
	return frame

func _drop() -> void:
	movement = Vector2.ZERO
	commands.clear()
	devices.clear()
	for w in watches:
		w.states.clear()

func clear() -> void:
	_drop()
	neutral = true
