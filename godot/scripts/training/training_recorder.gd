class_name TrainingRecorder
extends RefCounted
## TrainingRecorder: R = 3 s countdown then record the player's logical input as the dummy
## (max 10 s, one slot, memory only). P = play it back once.

enum Mode { IDLE, COUNTDOWN, RECORDING, PLAYBACK }
const FPS := 60
const COUNTDOWN_FRAMES := 180
const MAXIMUM_FRAMES := 600

var state := Mode.IDLE
var remaining := 0
var position := 0
var _saved: Array = []
var _draft: Array = []
var status := "R record | P play | Ctrl reset"

func saved_count() -> int: return _saved.size()
func draft_count() -> int: return _draft.size()
func countdown_number() -> int: return int(ceil(remaining / float(FPS)))

## Only logical slots, Mania and movement are recorded (menu / training keys removed).
static func logical(f: InputFrame) -> InputFrame:
	var c := f.copy()
	c.reset = false
	c.toggle_opponent_guard = false
	c.record_pressed = false
	c.play_pressed = false
	c.commands = c.commands.slice(0, mini(32, c.commands.size()))
	return c

func record_key() -> void:
	if state == Mode.PLAYBACK:
		status = "Playback busy; Ctrl cancels"
		return
	if state == Mode.IDLE:
		remaining = COUNTDOWN_FRAMES
		position = 0
		_draft.clear()
		state = Mode.COUNTDOWN
		status = "Recording countdown"
	elif state == Mode.COUNTDOWN:
		cancel(false)
	else:
		_finish()

func play_key() -> bool:
	if state != Mode.IDLE:
		status = "Recorder busy"
		return false
	if _saved.is_empty():
		status = "No saved recording"
		return false
	state = Mode.PLAYBACK
	position = 0
	status = "Playing dummy (one shot)"
	return true

func _finish() -> void:
	if not _draft.is_empty():
		_saved = _draft.duplicate()
	_draft.clear()
	state = Mode.IDLE
	position = 0
	status = "Recording saved: %dF" % _saved.size()

func cancel(save_partial: bool) -> void:
	if save_partial and state == Mode.RECORDING:
		_finish()
		return
	_draft.clear()
	state = Mode.IDLE
	remaining = 0
	position = 0
	status = "Stopped; previous recording retained"

func tick(live: InputFrame) -> InputFrame:
	match state:
		Mode.COUNTDOWN:
			remaining -= 1
			if remaining <= 0:
				state = Mode.RECORDING
				position = 0
				status = "RECORDING dummy | R stop"
			return InputFrame.new()
		Mode.RECORDING:
			var frame := logical(live)
			_draft.append(frame)
			position += 1
			if _draft.size() >= MAXIMUM_FRAMES:
				_finish()
			return frame.copy()
		Mode.PLAYBACK:
			if position >= _saved.size():
				state = Mode.IDLE
				position = 0
				status = "Playback finished"
				return InputFrame.new()
			var f := logical(_saved[position])
			position += 1
			return f
	return InputFrame.new()
