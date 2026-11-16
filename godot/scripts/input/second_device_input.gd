class_name SecondDeviceInput
extends RefCounted
## TrainingDeviceInput(second player): the 2P keyboard on the numpad (from TrainingInputConfig).
## Move 8/5/4/6, slots X Y Z = 7 9 -, J K L = 1 2 3, Hold = /, TRC = numpad Enter, Dash = 0.
## No pad, no T1/T2, no Mania (as in Unity).

var _hub
var _slots: Array = []        # control name per slot id (or "")
var _hold := ""
var _trc := ""
var held := 0
var pressed := 0
var commands: Array = []
var movement := Vector2.ZERO
var dash := false
var dash_pressed := false
var neutral := false

func _init(hub) -> void:
	_hub = hub
	var c: Dictionary = GameData.system().input
	for i in 9:
		var v := String(c.second_slot_keyboard[i])
		_slots.append("" if v.is_empty() else v.substr(v.find("/") + 1))
	_hold = String(c.second_throw_keyboard).substr(String(c.second_throw_keyboard).find("/") + 1)
	_trc = String(c.second_trc_keyboard).substr(String(c.second_trc_keyboard).find("/") + 1)
	if _hub != null:
		_hub.control_changed.connect(_on_control)
		_hub.devices_reset.connect(clear)

func detach() -> void:
	if _hub != null:
		_hub.control_changed.disconnect(_on_control)
		_hub.devices_reset.disconnect(clear)
		_hub = null

func _on_control(device: String, control: String, is_pressed: bool) -> void:
	if device != "kb":
		return
	var slot := _slots.find(control)
	if slot >= 0:
		var bit := 1 << slot
		if is_pressed:
			if (held & bit) == 0:
				pressed |= bit
			held |= bit
		else:
			held &= ~bit
	if control == _hold and is_pressed:
		commands.append(Sk.Cmd.THROW)
	if control == _trc and is_pressed:
		commands.append(Sk.Cmd.CANCEL)
	if control == "numpad0":
		dash = is_pressed
		if is_pressed:
			dash_pressed = true
	if control in ["numpad8", "numpad5", "numpad4", "numpad6"]:
		var v := Vector2((1.0 if _hub.pressed("kb", "numpad6") else 0.0) - (1.0 if _hub.pressed("kb", "numpad4") else 0.0),
			(1.0 if _hub.pressed("kb", "numpad8") else 0.0) - (1.0 if _hub.pressed("kb", "numpad5") else 0.0))
		if v.y > 0.5 and movement.y <= 0.5:
			commands.append(Sk.Cmd.JUMP)
		movement = v

func any_held() -> bool:
	for c in _slots + [_hold, _trc, "numpad0", "numpad8", "numpad5", "numpad4", "numpad6"]:
		if not c.is_empty() and _hub.pressed("kb", c):
			return true
	return false

func read(_training: bool) -> InputFrame:
	if neutral:
		var h := any_held()
		_clear_pending()
		if not h:
			neutral = false
		return InputFrame.new()
	var f := InputFrame.new()
	f.movement = movement
	f.dash = dash
	f.dash_pressed = dash_pressed
	# ModifierPressOwnership without modifiers: a new press of a held slot is an attack edge.
	f.attack_pressed_mask = pressed & 511
	f.attack_held_mask = held
	pressed = 0
	f.commands = commands.duplicate()
	commands.clear()
	dash_pressed = false
	return f

func _clear_pending() -> void:
	movement = Vector2.ZERO
	commands.clear()
	held = 0
	pressed = 0
	dash = false
	dash_pressed = false

func clear() -> void:
	_clear_pending()
	neutral = true
