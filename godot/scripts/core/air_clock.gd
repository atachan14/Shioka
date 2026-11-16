class_name AirClock
extends RefCounted
## TrainingAirActionClock: action time of an air attack (float32 seconds, as in Unity).

enum State { IDLE, RUNNING, HELD, FINISHED }
enum Pause { NONE, HITSTOP, STUN, LANDING }
enum Finish { NONE, COMPLETED, CANCELLED, LANDING, RESET }

var state := State.IDLE
var pause_reason := Pause.NONE
var finish_reason := Finish.NONE
var elapsed := 0.0
var duration := 0.0
var needs_landing_decision := false
var ground_recovery_allowed := false
var _startup := 0.0
var _active := 0.0

func is_busy() -> bool:
	return state == State.RUNNING or state == State.HELD

func normalized() -> float:
	return Sk.clamp01(elapsed / duration) if duration > 0.0 else 0.0

func in_active_window() -> bool:
	return is_busy() and elapsed >= _startup and elapsed < Sk.f(_startup + _active)

func can_apply_hit() -> bool:
	return state == State.RUNNING and in_active_window()

func try_begin(dur: float, startup_seconds: float, active_seconds: float) -> bool:
	if is_busy() or dur <= 0.0 or startup_seconds < 0.0 or active_seconds < 0.0 or Sk.f(startup_seconds + active_seconds) > dur:
		return false
	duration = dur
	_startup = startup_seconds
	_active = active_seconds
	elapsed = 0.0
	needs_landing_decision = false
	ground_recovery_allowed = false
	state = State.RUNNING
	pause_reason = Pause.NONE
	finish_reason = Finish.NONE
	return true

func hold(reason: int) -> void:
	if not is_busy():
		return
	if reason == Pause.LANDING:
		needs_landing_decision = true
	state = State.HELD
	pause_reason = Pause.LANDING if needs_landing_decision else reason

func advance(seconds: float) -> void:
	if not is_busy() or needs_landing_decision or seconds <= 0.0:
		return
	state = State.RUNNING
	pause_reason = Pause.NONE
	elapsed = minf(duration, Sk.f(elapsed + seconds))
	if elapsed >= Sk.f(duration - 0.000001):
		elapsed = duration
		end(Finish.COMPLETED)

func end(reason: int = Finish.CANCELLED) -> void:
	state = State.FINISHED
	pause_reason = Pause.NONE
	needs_landing_decision = false
	ground_recovery_allowed = false
	finish_reason = reason

func reset() -> void:
	state = State.IDLE
	pause_reason = Pause.NONE
	finish_reason = Finish.RESET
	elapsed = 0.0
	duration = 0.0
	_startup = 0.0
	_active = 0.0
	needs_landing_decision = false
	ground_recovery_allowed = false
