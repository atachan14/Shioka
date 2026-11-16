class_name InputFrame
extends RefCounted
## TrainingInputFrame. In C# this is a struct: call copy() wherever Unity copied by value.

var movement := Vector2.ZERO
var attack_pressed_mask := 0
var attack_held_mask := 0
var force_held := false
var record_pressed := false
var play_pressed := false
var dash := false
var dash_pressed := false
var reset := false
var toggle_opponent_guard := false
var guard_held := false
var fd_held := false
var commands: Array = []  # Sk.Cmd ints

func copy() -> InputFrame:
	var c := InputFrame.new()
	c.movement = movement
	c.attack_pressed_mask = attack_pressed_mask
	c.attack_held_mask = attack_held_mask
	c.force_held = force_held
	c.record_pressed = record_pressed
	c.play_pressed = play_pressed
	c.dash = dash
	c.dash_pressed = dash_pressed
	c.reset = reset
	c.toggle_opponent_guard = toggle_opponent_guard
	c.guard_held = guard_held
	c.fd_held = fd_held
	c.commands = commands.duplicate()
	return c

func presses_text() -> String:
	var names: Array = []
	for c in commands:
		names.append(Sk.CMD_NAMES[c])
	return ",".join(names)
