class_name PlayerControls
extends RefCounted
## TrainingControls (1P) / TrainingSecondPlayerControls (2P): wraps the device input with the
## option-menu block, "release everything first" after menus, and the double-tap dash.

var second := false
var device                     # ProfileDeviceInput or SecondDeviceInput
var _hub
var _option_blocked := false
var _await_neutral := true  # TrainingSceneEntryGate: release everything before the first input
var _double_tap_direction := 0
var _last_tap_direction := 0
var _last_tap_time := -100.0
var _tap_dash_pressed := false
var double_tap_seconds := 0.25
var enabled := true
var _tap_keys := {}   # direction -> [controls]

func _init(hub, second_player: bool) -> void:
	_hub = hub
	second = second_player
	double_tap_seconds = float(GameData.system().input.double_tap_seconds)
	_tap_keys = {-1: ["numpad4"], 1: ["numpad6"]} if second else {-1: ["a", "leftArrow"], 1: ["d", "rightArrow"]}
	_configure()
	if _hub != null:
		_hub.control_changed.connect(_on_control)

func _configure() -> void:
	device = SecondDeviceInput.new(_hub) if second else ProfileDeviceInput.new(_hub)

func detach() -> void:
	if device != null:
		device.detach()
	if _hub != null:
		_hub.control_changed.disconnect(_on_control)
		_hub = null

func reload_bindings() -> void:
	if device != null:
		device.detach()
	_configure()
	clear()
	_await_neutral = true

func _on_control(dev: String, control: String, is_pressed: bool) -> void:
	if dev != "kb" or not is_pressed or _option_blocked:
		return
	for direction in _tap_keys.keys():
		var keys: Array = _tap_keys[direction]
		if not keys.has(control):
			continue
		# The tap action is one button with two bindings: only the first key down performs it.
		var others := 0
		for k in keys:
			if _hub.pressed("kb", k):
				others += 1
		if others > 1:
			return
		var now := Time.get_ticks_usec() / 1000000.0
		if _last_tap_direction == direction and now - _last_tap_time <= double_tap_seconds:
			_double_tap_direction = direction
			_tap_dash_pressed = true
		_last_tap_direction = direction
		_last_tap_time = now

func set_option_blocked(blocked: bool) -> void:
	_option_blocked = blocked
	clear()
	if not blocked:
		_await_neutral = true

func _suppress_option_input() -> bool:
	if _option_blocked:
		clear()
		return true
	if not _await_neutral:
		return false
	clear()
	if device.any_held() or _any_tap_held():
		return true
	_await_neutral = false
	return true

func _any_tap_held() -> bool:
	for keys in _tap_keys.values():
		for k in keys:
			if _hub.pressed("kb", k):
				return true
	return false

func clear() -> void:
	_tap_dash_pressed = false
	if device != null:
		device.clear()
	_double_tap_direction = 0
	_last_tap_direction = 0
	_last_tap_time = -100.0

func clear_pending_chord_input() -> void:
	if device != null:
		device.clear()

func read_input() -> InputFrame:
	if not enabled:
		clear()
		return InputFrame.new()
	if _suppress_option_input():
		return InputFrame.new()
	var frame: InputFrame = device.read(true)
	if frame.movement.x * _double_tap_direction <= 0.1:
		_double_tap_direction = 0
	frame.dash = frame.dash or (_double_tap_direction != 0 and frame.movement.x * _double_tap_direction > 0.1)
	frame.dash_pressed = frame.dash_pressed or _tap_dash_pressed
	_tap_dash_pressed = false
	return frame
