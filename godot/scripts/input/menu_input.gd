class_name MenuInput
extends RefCounted
## TrainingMenuInput: Options / Confirm / Cancel from the KeyConfig (with T1/T2 rules) and a
## discrete navigation direction from WASD / pad Dpad / stick. read() is cached per frame.
## Starts in "release everything first" mode.

var _hub
var _watches: Array = []    # {name, modifier, pad, controls, states: {device: {...}}}
var _triggers: Array = []
var _mods := {}
var _pending := {}
var _current := {}
var _last_frame := -1
var _previous := Vector2.ZERO
var _neutral := true

func _init(hub) -> void:
	_hub = hub
	var p := KeyConfig.ensure()
	for m in [1, 2]:
		var r := KeyConfig.row(p, "trigger.T%d" % m)
		for pad in [false, true]:
			var v := KeyConfig.normalize(r.pad if pad else r.keyboard)
			if not v.is_empty():
				var pc := KeyConfig.path_controls(v)
				_triggers.append({"number": m, "pad": pc.pad, "controls": pc.controls})
	for id in ["common.Options", "menu.Confirm", "menu.Cancel"]:
		var r := KeyConfig.row(p, id)
		for pad in [false, true]:
			var v := KeyConfig.normalize(r.pad if pad else r.keyboard)
			if v.is_empty():
				continue
			var pc := KeyConfig.path_controls(v)
			_watches.append({"name": id, "modifier": KeyConfig.modifier(v), "pad": pc.pad, "controls": pc.controls, "states": {}})
	_pending = _empty()
	_current = _empty()
	_hub.control_changed.connect(_on_control)
	_hub.stick_changed.connect(_on_stick)
	_hub.devices_reset.connect(clear)

func dispose() -> void:
	if _hub != null:
		_hub.control_changed.disconnect(_on_control)
		_hub.stick_changed.disconnect(_on_stick)
		_hub.devices_reset.disconnect(clear)
		_hub = null

static func _empty() -> Dictionary:
	return {"option": false, "confirm": false, "cancel": false, "cancel_from_pad": false, "direction": Vector2.ZERO}

func _mod(device: String) -> Dictionary:
	if not _mods.has(device):
		_mods[device] = {"one": false, "two": false, "seen1": false, "seen2": false}
	return _mods[device]

func _any(device: String, controls: Array) -> bool:
	for c in controls:
		if _hub.pressed(device, c):
			return true
	return false

func _on_control(device: String, control: String, _is_pressed: bool) -> void:
	var is_pad := device != "kb"
	for t in _triggers:
		if t.pad == is_pad and t.controls.has(control):
			var m := _mod(device)
			var held := _any(device, t.controls)
			if t.number == 1:
				m.one = held
				m.seen1 = m.seen1 or held
			else:
				m.two = held
				m.seen2 = m.seen2 or held
	for w in _watches:
		if w.pad != is_pad or not w.controls.has(control):
			continue
		if not w.states.has(device):
			w.states[device] = {"held": false, "pressed": false, "previous": false, "at1": false, "at2": false}
		var s: Dictionary = w.states[device]
		var m2 := _mod(device)
		var held2 := _any(device, w.controls)
		if held2 and not s.held:
			s.pressed = true
			s.at1 = s.at1 or m2.one
			s.at2 = s.at2 or m2.two
		s.held = held2
	_update_direction()

func _on_stick(_d: String, _v: Vector2) -> void:
	_update_direction()

func _update_direction() -> void:
	var v := Vector2.ZERO
	var kb := Vector2((1.0 if _hub.pressed("kb", "d") else 0.0) - (1.0 if _hub.pressed("kb", "a") else 0.0),
		(1.0 if _hub.pressed("kb", "w") else 0.0) - (1.0 if _hub.pressed("kb", "s") else 0.0))
	if kb.length_squared() > v.length_squared():
		v = kb
	for device in _hub.devices():
		if device == "kb":
			continue
		var dp := Vector2((1.0 if _hub.pressed(device, "dpad/right") else 0.0) - (1.0 if _hub.pressed(device, "dpad/left") else 0.0),
			(1.0 if _hub.pressed(device, "dpad/up") else 0.0) - (1.0 if _hub.pressed(device, "dpad/down") else 0.0))
		var st: Vector2 = _hub.sticks.get(device, Vector2.ZERO)
		for c in [dp, st]:
			if c.length_squared() > v.length_squared():
				v = c
	v = Vector2(-1 if v.x < -0.5 else (1 if v.x > 0.5 else 0), -1 if v.y < -0.5 else (1 if v.y > 0.5 else 0))
	if v != _previous and v != Vector2.ZERO:
		_pending.direction = v
	_previous = v

func read() -> Dictionary:
	var frame := Engine.get_process_frames()
	if _last_frame == frame:
		return _current
	_last_frame = frame
	if _neutral:
		var held: bool = _hub.any_pressed()
		_drop()
		if not held:
			_neutral = false
		_current = _empty()
		return _current
	for w in _watches:
		for device in w.states.keys():
			var s: Dictionary = w.states[device]
			var m := _mod(device)
			var one: bool = m.one or m.seen1 or s.at1
			var two: bool = m.two or m.seen2 or s.at2
			var mod: int = w.modifier
			var decision: bool = (not one and not two) if mod == 0 else ((one and not two) if mod == 1 else (two and not one))
			var active: bool = s.held and ((not m.one and not m.two) if mod == 0 else ((m.one and not m.two) if mod == 1 else (m.two and not m.one)))
			var edge: bool = (s.pressed and decision) if mod == 0 else ((s.held or s.pressed) and decision and (not s.previous or s.pressed or (m.seen1 if mod == 1 else m.seen2)))
			s.previous = active
			if edge:
				if w.name == "common.Options":
					_pending.option = true
				elif w.name == "menu.Confirm":
					_pending.confirm = true
				else:
					_pending.cancel = true
					_pending.cancel_from_pad = _pending.cancel_from_pad or device != "kb"
			s.pressed = false
			s.at1 = false
			s.at2 = false
	for m in _mods.values():
		m.seen1 = false
		m.seen2 = false
	_current = _pending
	_pending = _empty()
	return _current

func _drop() -> void:
	_pending = _empty()
	_current = _empty()
	_previous = Vector2.ZERO
	_mods.clear()
	for w in _watches:
		w.states.clear()

func clear() -> void:
	_drop()
	_last_frame = -1
	_neutral = true
