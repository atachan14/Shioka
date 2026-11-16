class_name CommandBuffer
extends RefCounted
## TrainingCommandBuffer: the 10F input buffer. Input time runs through hitstop.

enum Outcome { QUEUED, SELECTED, EXECUTED, REJECTED, EXPIRED, REPLACED, CLEARED }
const OUTCOME_NAMES := ["Queued", "Selected", "Executed", "Rejected", "Expired", "Replaced", "Cleared"]

class Trace:
	extends RefCounted
	var command: int
	var issued_frame: int
	var expires_frame: int
	var lock_at_issue: int
	var outcome := 0
	var resolved_frame := -1

class Sample:
	extends RefCounted
	var frame := 0
	var movement := Vector2.ZERO
	var attack_pressed_mask := 0
	var facing := 1
	var airborne := false
	var dash := false
	var guard_held := false
	var fd_held := false
	var force_held := false
	var lock_kind := 0
	var presses := ""

var window_frames := 10
var frame := 0
var reset_version := 0
var _fraction := 0.0
var pending: Array = []   # Trace (shared with the trace list)
var trace: Array = []     # last 180 traces
var samples: Array = []   # last 180 samples
var _selected: Trace = null

func count() -> int:
	return pending.size()

func record_sample(input: InputFrame, kind: int, facing: int, airborne: bool) -> void:
	var s := Sample.new()
	s.frame = frame
	s.movement = input.movement
	s.attack_pressed_mask = input.attack_pressed_mask
	s.facing = -1 if facing < 0 else 1
	s.airborne = airborne
	s.dash = input.dash
	s.guard_held = input.guard_held
	s.fd_held = input.fd_held
	s.force_held = input.force_held
	s.lock_kind = kind
	s.presses = input.presses_text()
	samples.append(s)
	if samples.size() > 180:
		samples.remove_at(0)

func _resolve(t: Trace, status: int) -> void:
	t.outcome = status
	t.resolved_frame = frame

func advance(seconds: float) -> void:
	_fraction += snappedf(seconds * 60.0, 0.000001)
	var elapsed := int(floor(_fraction + 0.00001))
	frame += elapsed
	_fraction -= elapsed
	for i in range(pending.size() - 1, -1, -1):
		if frame >= pending[i].expires_frame:
			_resolve(pending[i], Outcome.EXPIRED)
			pending.remove_at(i)

func push(c: int, kind: int = 0) -> void:
	var t := Trace.new()
	t.command = c
	t.issued_frame = frame
	t.expires_frame = frame + maxi(1, window_frames)
	t.lock_at_issue = kind
	pending.append(t)
	trace.append(t)
	if pending.size() > 32:
		_resolve(pending[0], Outcome.REPLACED)
		pending.remove_at(0)
	if trace.size() > 180:
		trace.remove_at(0)

## Newest-first selection; older entries are replaced. Returns -1 when nothing is accepted.
func try_take(allowed: Callable) -> int:
	for i in range(pending.size() - 1, -1, -1):
		var t: Trace = pending[i]
		if not allowed.call(t.command):
			continue
		_selected = t
		_resolve(t, Outcome.SELECTED)
		for j in range(i):
			_resolve(pending[j], Outcome.REPLACED)
		pending = pending.slice(i + 1)
		return t.command
	return -1

func confirm_selected(executed: bool) -> void:
	if _selected != null:
		_resolve(_selected, Outcome.EXECUTED if executed else Outcome.REJECTED)
		_selected = null

func remove_pending(matcher: Callable) -> void:
	for i in range(pending.size() - 1, -1, -1):
		if matcher.call(pending[i].command):
			_resolve(pending[i], Outcome.CLEARED)
			pending.remove_at(i)

func clear_pending() -> void:
	for t in pending:
		_resolve(t, Outcome.CLEARED)
	pending.clear()

func reset() -> void:
	reset_version += 1
	pending.clear()
	trace.clear()
	samples.clear()
	_selected = null
	frame = 0
	_fraction = 0.0
