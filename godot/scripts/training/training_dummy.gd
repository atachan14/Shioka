class_name TrainingDummy
extends RefCounted
## TrainingDummy input source: posture (stand / crouch / jump) and the counter command
## issued at the first actionable frame after being hit.

enum Posture { STAND, CROUCH, JUMP }

var fighter: Fighter
var posture := Posture.STAND
var counter_command := ""
var counter_error := ""
var reactions_issued := 0
var _parsed: Dictionary = {}   # {command, direction} or {}
var _observed_hits := 0
var _pending := false

func _init(f: Fighter) -> void:
	fighter = f
	_observed_hits = f.hit_count

func set_posture(mode: int) -> void:
	posture = mode

func set_counter_command(value: String) -> bool:
	counter_command = value
	var r := parse(value)
	counter_error = r.error
	_parsed = r.result
	if not r.valid or _parsed.is_empty():
		_pending = false
	return r.valid

func clear() -> void:
	_pending = false
	_observed_hits = fighter.hit_count

func read_input() -> InputFrame:
	var f := fighter
	if f.hit_count < _observed_hits:
		_pending = false
		_observed_hits = f.hit_count
	if f.hit_count > _observed_hits:
		_observed_hits = f.hit_count
		_pending = not _parsed.is_empty()
	var frame := InputFrame.new()
	if posture == Posture.CROUCH:
		frame.movement = Vector2(0, -1)
	if not _pending and posture == Posture.JUMP and f.grounded and not f.is_down() and f.stun_remaining <= 0 and f.hitstop <= 0 \
			and (f.phase == Sk.Phase.IDLE or f.phase == Sk.Phase.MOVE or f.phase == Sk.Phase.LANDING):
		frame.commands = [Sk.Cmd.JUMP]
	return frame

func try_peek_reaction(f: Fighter) -> InputFrame:
	if not _pending or _parsed.is_empty():
		return null
	var facing := f.facing if f.opponent == null else (1 if f.opponent.position().x >= f.position().x else -1)
	var d: int = _parsed.direction
	var x := -1.0 if d in [1, 4, 7] else (1.0 if d in [3, 6, 9] else 0.0)
	var y := -1.0 if d <= 3 else (1.0 if d >= 7 else 0.0)
	var frame := InputFrame.new()
	frame.movement = Vector2(x * facing, y)
	frame.commands = [_parsed.command]
	return frame

func confirm_reaction() -> void:
	_pending = false
	reactions_issued += 1

## TrainingDummyCommand.TryParse. Returns {valid, result: {command, direction} or {}, error}.
static func parse(text: String) -> Dictionary:
	var s := _normalize(text).strip_edges().to_upper().replace(" ", "")
	if s.is_empty():
		return {"valid": true, "result": {}, "error": ""}
	var direction := 5
	var first := s.unicode_at(0)
	if first >= 49 and first <= 57:
		direction = first - 48
		s = s.substr(1)
		while s.begins_with("+"):
			s = s.substr(1)
	var legacy_sweep := direction == 3 and (s == "強" or s == "HEAVY" or s == "L")
	var command := -1
	match s:
		"弱P", "LP", "弱", "J", "LIGHT": command = Sk.Cmd.LIGHT
		"中P", "MP", "中", "K", "MEDIUM": command = Sk.Cmd.MEDIUM
		"強P", "HP", "強", "L", "HEAVY": command = Sk.Cmd.HEAVY
		"弱K", "LK", "S", "M", "SLASH": command = Sk.Cmd.SLASH
		"中K", "MK", "HS", ",", "HEAVYSLASH": command = Sk.Cmd.HEAVY_SLASH
		"強K", "HK", ".": command = Sk.Cmd.STRONG_KICK
		"SPECIAL2", "特殊2": command = Sk.Cmd.SPECIAL2
		"投げ", "I", "THROW": command = Sk.Cmd.THROW
		"SPECIAL1", "特殊1", "特殊", "O", "SPECIAL": command = Sk.Cmd.SPECIAL
		"足払い", "SWEEP":
			command = Sk.Cmd.STRONG_KICK
			direction = 3
		"ジャンプ", "JUMP": command = Sk.Cmd.JUMP
		_:
			return {"valid": false, "result": {}, "error": "不正コマンド。例: 弱 / 中 / 強 / 3強 / S / HS / 投げ / 特殊"}
	if legacy_sweep:
		command = Sk.Cmd.STRONG_KICK  # input layout v3
	if direction <= 3:
		if command == Sk.Cmd.SLASH:
			command = Sk.Cmd.CROUCH_WEAK_KICK
		elif command == Sk.Cmd.HEAVY_SLASH:
			command = Sk.Cmd.CROUCH_MEDIUM_KICK
		elif command == Sk.Cmd.STRONG_KICK and direction != 3:
			command = Sk.Cmd.CROUCH_STRONG_KICK
		elif not (direction == 3 and command == Sk.Cmd.STRONG_KICK):
			return {"valid": false, "result": {}, "error": "しゃがみ攻撃は弱K・中K・強K、3強Kは足払いです。"}
	if direction >= 7 and command != Sk.Cmd.JUMP:
		return {"valid": false, "result": {}, "error": "上方向との組合せはジャンプのみです。空中技は弱K・中Kを指定します。"}
	return {"valid": true, "result": {"command": command, "direction": direction}, "error": ""}

## Approximates Unicode NFKC for the characters this field accepts (full-width ASCII).
static func _normalize(text: String) -> String:
	var out := ""
	for i in text.length():
		var c := text.unicode_at(i)
		if c >= 0xFF01 and c <= 0xFF5E:
			c -= 0xFEE0
		elif c == 0x3000:
			c = 0x20
		out += char(c)
	return out
