extends Node
## Autoload "InputHub". Turns Godot input events into Unity Input System style control
## changes: device "kb" / "pad<N>", control names as in Unity paths ("j", "leftShift",
## "buttonSouth", "leftTrigger", "dpad/up", ...). Listeners get every change immediately
## (like Unity action callbacks); each one samples at its own 60 Hz boundary.

signal control_changed(device: String, control: String, pressed: bool)
signal stick_changed(device: String, value: Vector2)
signal devices_reset()

const TRIGGER_PRESS := 0.5
const TRIGGER_RELEASE := 0.375
const STICK_DEADZONE_MIN := 0.125
const STICK_DEADZONE_MAX := 0.925

var controls := {}   # device -> {control: true}
var sticks := {}     # device -> Vector2 (left stick, deadzoned, y up)

const KEY_NAMES := {
	KEY_SPACE: "space", KEY_ENTER: "enter", KEY_TAB: "tab", KEY_BACKSPACE: "backspace", KEY_ESCAPE: "escape",
	KEY_COMMA: "comma", KEY_PERIOD: "period", KEY_SEMICOLON: "semicolon", KEY_SLASH: "slash", KEY_BACKSLASH: "backslash",
	KEY_APOSTROPHE: "quote", KEY_BRACKETLEFT: "leftBracket", KEY_BRACKETRIGHT: "rightBracket", KEY_MINUS: "minus",
	KEY_EQUAL: "equals", KEY_QUOTELEFT: "backquote", KEY_UP: "upArrow", KEY_DOWN: "downArrow", KEY_LEFT: "leftArrow",
	KEY_RIGHT: "rightArrow", KEY_KP_0: "numpad0", KEY_KP_1: "numpad1", KEY_KP_2: "numpad2", KEY_KP_3: "numpad3",
	KEY_KP_4: "numpad4", KEY_KP_5: "numpad5", KEY_KP_6: "numpad6", KEY_KP_7: "numpad7", KEY_KP_8: "numpad8",
	KEY_KP_9: "numpad9", KEY_KP_ADD: "numpadPlus", KEY_KP_SUBTRACT: "numpadMinus", KEY_KP_MULTIPLY: "numpadMultiply",
	KEY_KP_DIVIDE: "numpadDivide", KEY_KP_PERIOD: "numpadPeriod", KEY_KP_ENTER: "numpadEnter", KEY_INSERT: "insert",
	KEY_DELETE: "delete", KEY_HOME: "home", KEY_END: "end", KEY_PAGEUP: "pageUp", KEY_PAGEDOWN: "pageDown",
}
const PAD_BUTTONS := {
	JOY_BUTTON_A: "buttonSouth", JOY_BUTTON_B: "buttonEast", JOY_BUTTON_X: "buttonWest", JOY_BUTTON_Y: "buttonNorth",
	JOY_BUTTON_BACK: "select", JOY_BUTTON_START: "start", JOY_BUTTON_LEFT_STICK: "leftStickPress",
	JOY_BUTTON_RIGHT_STICK: "rightStickPress", JOY_BUTTON_LEFT_SHOULDER: "leftShoulder",
	JOY_BUTTON_RIGHT_SHOULDER: "rightShoulder", JOY_BUTTON_DPAD_UP: "dpad/up", JOY_BUTTON_DPAD_DOWN: "dpad/down",
	JOY_BUTTON_DPAD_LEFT: "dpad/left", JOY_BUTTON_DPAD_RIGHT: "dpad/right",
}

func _ready() -> void:
	process_mode = Node.PROCESS_MODE_ALWAYS
	Input.joy_connection_changed.connect(_on_joy_changed)

static func key_name(event: InputEventKey) -> String:
	var code := event.physical_keycode if event.physical_keycode != 0 else event.keycode
	if code >= KEY_A and code <= KEY_Z:
		return char(code).to_lower()
	if code >= KEY_0 and code <= KEY_9:
		return char(code)
	if code >= KEY_F1 and code <= KEY_F12:
		return "f%d" % (code - KEY_F1 + 1)
	if code == KEY_SHIFT:
		return "rightShift" if event.location == KEY_LOCATION_RIGHT else "leftShift"
	if code == KEY_CTRL:
		return "rightCtrl" if event.location == KEY_LOCATION_RIGHT else "leftCtrl"
	if code == KEY_ALT:
		return "rightAlt" if event.location == KEY_LOCATION_RIGHT else "leftAlt"
	return KEY_NAMES.get(code, "")

func _set_control(device: String, control: String, pressed: bool) -> void:
	if control.is_empty():
		return
	var d: Dictionary = controls.get(device, {})
	if bool(d.get(control, false)) == pressed:
		return
	if pressed:
		d[control] = true
	else:
		d.erase(control)
	controls[device] = d
	control_changed.emit(device, control, pressed)

func _input(event: InputEvent) -> void:
	if event is InputEventKey:
		if event.echo:
			return
		_set_control("kb", key_name(event), event.pressed)
	elif event is InputEventJoypadButton:
		_set_control("pad%d" % event.device, PAD_BUTTONS.get(event.button_index, ""), event.pressed)
	elif event is InputEventJoypadMotion:
		var device := "pad%d" % event.device
		if event.axis == JOY_AXIS_TRIGGER_LEFT or event.axis == JOY_AXIS_TRIGGER_RIGHT:
			var name := "leftTrigger" if event.axis == JOY_AXIS_TRIGGER_LEFT else "rightTrigger"
			var held := pressed(device, name)
			if not held and event.axis_value >= TRIGGER_PRESS:
				_set_control(device, name, true)
			elif held and event.axis_value < TRIGGER_RELEASE:
				_set_control(device, name, false)
		elif event.axis == JOY_AXIS_LEFT_X or event.axis == JOY_AXIS_LEFT_Y:
			var joy := event.device
			var raw := Vector2(Input.get_joy_axis(joy, JOY_AXIS_LEFT_X), -Input.get_joy_axis(joy, JOY_AXIS_LEFT_Y))
			var value := deadzone(raw)
			if sticks.get(device, Vector2.ZERO) != value:
				sticks[device] = value
				stick_changed.emit(device, value)

## Unity StickDeadzoneProcessor (radial, min 0.125, max 0.925).
static func deadzone(v: Vector2) -> Vector2:
	var m := v.length()
	if m < STICK_DEADZONE_MIN:
		return Vector2.ZERO
	var scaled := clampf((m - STICK_DEADZONE_MIN) / (STICK_DEADZONE_MAX - STICK_DEADZONE_MIN), 0.0, 1.0)
	return v / m * scaled

func pressed(device: String, control: String) -> bool:
	return bool(controls.get(device, {}).get(control, false))

func any_pressed() -> bool:
	for d in controls.values():
		if not d.is_empty():
			return true
	for v in sticks.values():
		if v.length_squared() > 0.01:
			return true
	return false

func devices() -> Array:
	var list: Array = ["kb"]
	for joy in Input.get_connected_joypads():
		list.append("pad%d" % joy)
	return list

func _on_joy_changed(_device: int, _connected: bool) -> void:
	var device := "pad%d" % _device
	controls.erase(device)
	sticks.erase(device)
	devices_reset.emit()

func _notification(what: int) -> void:
	if what == NOTIFICATION_APPLICATION_FOCUS_OUT:
		controls.clear()
		sticks.clear()
		devices_reset.emit()
