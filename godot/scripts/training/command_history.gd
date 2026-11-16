class_name CommandHistory
extends RefCounted
## TrainingCommandHistory: input display. Numpad directions are facing-relative
## (6 forward, 4 back, 5 neutral); facing and airborne belong to the input's tick.

var _rows: Array = []
var _seen := {}
var _reset := -1
var _frame := -1

func capture(fighter: Fighter) -> void:
	if fighter == null:
		return
	var buffer := fighter.input_buffer
	if _reset != buffer.reset_version:
		_rows.clear()
		_seen.clear()
		_frame = -1
		_reset = buffer.reset_version
	if buffer.frame == _frame:
		return
	_frame = buffer.frame
	var sample: CommandBuffer.Sample = CommandBuffer.Sample.new() if buffer.samples.is_empty() else buffer.samples[buffer.samples.size() - 1]
	var action_event := false
	var entries: Array = []
	var available := {}
	for t in buffer.trace:
		available[t] = true
	for k in _seen.keys():
		if not available.has(k):
			_seen.erase(k)
	for t in buffer.trace:
		if t.outcome == CommandBuffer.Outcome.EXECUTED and not _seen.has(t):
			_seen[t] = true
			var action := command_name(fighter, t.command)
			if action.is_empty():
				continue
			var issued: CommandBuffer.Sample = sample
			for i in range(buffer.samples.size() - 1, -1, -1):
				if buffer.samples[i].frame == t.issued_frame:
					issued = buffer.samples[i]
					break
			action_event = true
			if entries.size() < 8:
				entries.append(format(issued.movement, issued.facing, issued.airborne, action))
	if sample.attack_pressed_mask != 0:
		var slots := KeyConfig.resolve_slots(fighter.data)
		for id in Sk.SLOT_DISPLAY_ORDER:
			if (sample.attack_pressed_mask & (1 << id)) == 0:
				continue
			for slot in slots:
				if int(slot.slot) == id and not String(slot.display_name).is_empty():
					var entry := format(sample.movement, sample.facing, sample.airborne, slot.display_name)
					if not entries.has(entry):
						entries.append(entry)
					action_event = true
					break
	if entries.is_empty():
		entries.append(format(sample.movement, sample.facing, sample.airborne, "Mania" if sample.force_held else ("FD" if sample.fd_held else ("D" if sample.dash else ""))))
	var text := "/".join(entries)
	if action_event or _rows.is_empty() or _rows[_rows.size() - 1] != text:
		_rows.append(text)
		if _rows.size() > 64:
			_rows.remove_at(0)

func text(visible: int = 24) -> String:
	var lines: Array = []
	for i in range(_rows.size() - 1, maxi(0, _rows.size() - visible) - 1, -1):
		lines.append(_rows[i])
	return "\n".join(lines)

static func direction(move: Vector2, facing: int = 1) -> String:
	var s := -1 if facing < 0 else 1
	var x := -1 if move.x * s < -0.1 else (1 if move.x * s > 0.1 else 0)
	var y := -1 if move.y < -0.1 else (1 if move.y > 0.1 else 0)
	return str(5 + x + 3 * y)

static func format(move: Vector2, facing: int, airborne: bool, action: String) -> String:
	return direction(move, facing) + ("" if action.is_empty() else ("J" if airborne else "") + action)

static func _base(c: int) -> int:
	match c:
		Sk.Cmd.CROUCH_WEAK_PUNCH: return Sk.Cmd.LIGHT
		Sk.Cmd.CROUCH_MEDIUM_PUNCH: return Sk.Cmd.MEDIUM
		Sk.Cmd.CROUCH_STRONG_PUNCH, Sk.Cmd.AIR_STRONG_PUNCH: return Sk.Cmd.HEAVY
		Sk.Cmd.CROUCH_WEAK_KICK: return Sk.Cmd.SLASH
		Sk.Cmd.CROUCH_MEDIUM_KICK: return Sk.Cmd.HEAVY_SLASH
		Sk.Cmd.CROUCH_STRONG_KICK, Sk.Cmd.AIR_STRONG_KICK, Sk.Cmd.AIR_DOWN_STRONG_KICK: return Sk.Cmd.STRONG_KICK
		Sk.Cmd.FORWARD_SPECIAL1: return Sk.Cmd.SPECIAL
		Sk.Cmd.AIR_SPECIAL2: return Sk.Cmd.SPECIAL2
	return c

static func command_name(actor: Fighter, c: int) -> String:
	if actor != null:
		var basic := _base(c)
		for slot in actor.data.raw.attack_slots:
			if slot.assigned and int(slot.command) == basic:
				return slot.display_name
	match c:
		Sk.Cmd.JUMP: return ""
		Sk.Cmd.CANCEL: return "TRC"
		Sk.Cmd.THROW: return "Hold"
	return Sk.CMD_NAMES[c]
