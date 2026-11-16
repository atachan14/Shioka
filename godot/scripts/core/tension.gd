class_name Tension
extends RefCounted
## TrainingTension (TG). Gains are suppressed in the v0.1 combat rules; TRC still spends.

var suppress_gains := false
var fixed := false
var gauge := 0.0
var level := 0.0
var last_cost := 0.0

## Legacy multiplier (used by the throw path and the frame bar).
func bonus() -> float:
	return Sk.f(2.0 - 100.0 / (gauge + 100.0) if gauge >= 0 else 100.0 / (absf(gauge) + 100.0))

func gain(amount: float, level_rate: float) -> void:
	if fixed or suppress_gains:
		return
	gauge += amount * (1.0 + level / (level + 100.0))
	level += absf(amount) * level_rate

func spend(amount: float, level_rate: float) -> void:
	if fixed:
		last_cost = 0.0
		return
	last_cost = maxf(0.0, amount)
	gauge -= last_cost
	level += last_cost * level_rate

func set_test_gauge(value: float) -> void:
	if not fixed:
		gauge = value

func set_training_gauge(value: float) -> void:
	gauge = value
	last_cost = 0.0

func reset() -> void:
	if fixed:
		return
	gauge = 0.0
	level = 0.0
	last_cost = 0.0
