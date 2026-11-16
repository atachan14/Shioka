class_name Sk
## Shared enums and small helpers. Enum orders match the Unity version
## (TrainingCommand, TrainingLockKind, TrainingFighter.Phase, ...) so the
## exported data (ints) and saved files stay compatible.

enum Cmd { JUMP, LIGHT, MEDIUM, HEAVY, SLASH, HEAVY_SLASH, CANCEL, SWEEP, THROW, SPECIAL, STRONG_KICK, SPECIAL2,
	CROUCH_WEAK_KICK, CROUCH_MEDIUM_KICK, CROUCH_STRONG_KICK, AIR_STRONG_KICK, AIR_DOWN_STRONG_KICK, AIR_STRONG_PUNCH,
	FORWARD_SPECIAL1, AIR_SPECIAL2, CROUCH_WEAK_PUNCH, CROUCH_MEDIUM_PUNCH, CROUCH_STRONG_PUNCH }
const CMD_NAMES := ["Jump", "Light", "Medium", "Heavy", "Slash", "HeavySlash", "Cancel", "Sweep", "Throw", "Special", "StrongKick", "Special2",
	"CrouchWeakKick", "CrouchMediumKick", "CrouchStrongKick", "AirStrongKick", "AirDownStrongKick", "AirStrongPunch",
	"ForwardSpecial1", "AirSpecial2", "CrouchWeakPunch", "CrouchMediumPunch", "CrouchStrongPunch"]

enum Lock { NONE, TAKEOFF, LANDING, STARTUP, ACTIVE, RECOVERY, HITSTUN, GUARD_STUN, AIR_ACTION, BACKSTEP, KNOCKDOWN, DOWN, GET_UP,
	THROW_STARTUP, THROW_ACTIVE, THROW_RECOVERY, THROW_HOLD, THROWN, BLOCKSTUN }

enum Phase { IDLE, MOVE, TAKEOFF, AIR, LANDING, ATTACK, HITSTUN, GUARD_STUN, CROUCH, BACKSTEP, KNOCKDOWN, DOWN, GET_UP,
	THROW_ATTEMPT, THROW_HOLD, THROWN }
const PHASE_NAMES := ["Idle", "Move", "Takeoff", "Air", "Landing", "Attack", "Hitstun", "GuardStun", "Crouch", "Backstep",
	"Knockdown", "Down", "GetUp", "ThrowAttempt", "ThrowHold", "Thrown"]

enum AttackClass { MID, HIGH, LOW, THROW }

## BattleAttackSlot ids (stable numeric ids) and display order.
enum Slot { A = 0, B = 1, C = 2, X = 3, Y = 4, Z = 5, J = 6, K = 7, L = 8 }
const SLOT_NAMES := ["A", "B", "C", "X", "Y", "Z", "J", "K", "L"]
const SLOT_DISPLAY_ORDER := [0, 1, 2, 6, 7, 8, 3, 4, 5]

## ModifiedInputAction (T1/T2 chord actions)
enum Mod { NONE, A, B, C, X, Y, Z, THROW, TRC, FORCE, DASH, RECORD, PLAY, RESET, J, K, L }

enum Outcome { GUARD, BLOCK, HIT }
const OUTCOME_NAMES := ["Guard", "Block", "Hit"]

const DT := 1.0 / 60.0

static var _f32 := PackedFloat32Array([0.0])

## Round to IEEE float32 (Unity's float). Used wherever the Unity code
## accumulates or compares float seconds so frame boundaries land identically.
static func f(x: float) -> float:
	_f32[0] = x
	return _f32[0]

## float32 1/60 (Unity's Time.fixedDeltaTime after ApprovedLocomotionTuning).
static func fdt() -> float:
	return f(1.0 / 60.0)

static func move_toward(current: float, target: float, max_delta: float) -> float:
	if absf(target - current) <= max_delta:
		return target
	return current + signf(target - current) * max_delta

static func clamp01(x: float) -> float:
	return clampf(x, 0.0, 1.0)

static func repeat(t: float, length: float) -> float:
	return clampf(t - floorf(t / length) * length, 0.0, length)

## Unity Mathf.SmoothDamp
static func smooth_damp(current: float, target: float, velocity: Array, smooth_time: float, dt: float) -> float:
	smooth_time = maxf(0.0001, smooth_time)
	var omega := 2.0 / smooth_time
	var x := omega * dt
	var e := 1.0 / (1.0 + x + 0.48 * x * x + 0.235 * x * x * x)
	var change := current - target
	var original_to := target
	var temp: float = (velocity[0] + omega * change) * dt
	velocity[0] = (velocity[0] - omega * temp) * e
	var output: float = target + (change + temp) * e
	if (original_to - current > 0.0) == (output > original_to):
		output = original_to
		velocity[0] = (output - original_to) / dt
	return output

## Unity legacy (x, y=height, z=forward) -> 2D (forward*facing, height)
static func from_legacy(v: Array, facing: int = 1) -> Vector2:
	return Vector2(float(v[2]) * facing, float(v[1]))

static func half_size(v: Array) -> Vector2:
	return Vector2(absf(float(v[2])), absf(float(v[1])))

static func aabb_overlap(c1: Vector2, h1: Vector2, c2: Vector2, h2: Vector2) -> bool:
	return absf(c1.x - c2.x) < h1.x + h2.x and absf(c1.y - c2.y) < h1.y + h2.y

## Evaluate a Unity AnimationCurve exported as [[time, value, inSlope, outSlope], ...] (Hermite).
static func eval_curve(keys: Array, t: float) -> float:
	if keys.is_empty():
		return 1.0
	if keys.size() == 1 or t <= float(keys[0][0]):
		return float(keys[0][1])
	if t >= float(keys[keys.size() - 1][0]):
		return float(keys[keys.size() - 1][1])
	for i in range(keys.size() - 1):
		var a: Array = keys[i]
		var b: Array = keys[i + 1]
		if t <= float(b[0]):
			var dt := float(b[0]) - float(a[0])
			if dt <= 0.0:
				return float(b[1])
			if a[3] == null or b[2] == null:
				return float(a[1])  # stepped (infinite tangent)
			var s := (t - float(a[0])) / dt
			var s2 := s * s
			var s3 := s2 * s
			return (2 * s3 - 3 * s2 + 1) * float(a[1]) + (s3 - 2 * s2 + s) * dt * float(a[3]) + (-2 * s3 + 3 * s2) * float(b[1]) + (s3 - s2) * dt * float(b[2])
	return float(keys[keys.size() - 1][1])
