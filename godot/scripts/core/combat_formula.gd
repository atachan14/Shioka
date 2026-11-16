class_name CombatFormula
## CombatV01Formula (doubles, as in Unity) and the legacy TrainingHitFormula (float32)
## still used by the throw path. Spec: docs/spec/combat.md.

class Result:
	extends RefCounted
	var A := 0.0
	var D := 0.0
	var H := 0.0
	var B := 0.0
	var attack_tg := 0.0
	var defense_tg := 0.0
	var jg_reduction := 0.0
	var raw_stun := 0.0
	var surplus := 0.0
	var damage := 0.0
	var self_push := 0.0
	var attacker_push := 0.0
	var jg_age := -1
	var hitstop_frames := 0
	var stun_frames := 0
	var fd := false
	var outcome := Sk.Outcome.HIT

## system: the "guard" / "tg" / "combat" dictionaries of data/system.json
static func tg_multiplier(tg: float, sys: Dictionary) -> float:
	var t: Dictionary = sys.tg
	return 1.0 + tg / float(t.positiveDivisor) if tg >= 0 else 1.0 / (1.0 + absf(tg) / float(t.negativeDivisor))

static func hit_level(a: float, d: float, attack_tg: float, defense_tg: float, sys: Dictionary) -> float:
	return maxf(0.0, a) * tg_multiplier(attack_tg, sys) / tg_multiplier(defense_tg, sys) / (1.0 + maxf(0.0, d) / float(sys.combat.defenseDivisor))

static func frames(duration: float) -> int:
	return int(ceil(maxf(0.0, duration)))

static func jg_reduction(age: int, continuous: bool, sys: Dictionary) -> int:
	var g: Dictionary = sys.guard
	var window := int(g.continuousWindow if continuous else g.normalWindow)
	if age < 0 or age >= window:
		return 0
	var decay := int(g.continuousDecayFrames if continuous else g.normalDecayFrames)
	return maxi(0, int(g.maxReduction) - int(age / decay))

static func resolve(a: float, d: float, attack_tg: float, defense_tg: float, matching_guard: bool, fd: bool, jg: int,
		damage_multiplier: float, chip_multiplier: float, push_scale: float, sys: Dictionary) -> Result:
	var g: Dictionary = sys.guard
	var c: Dictionary = sys.combat
	a = maxf(0.0, a)
	d = maxf(0.0, d)
	damage_multiplier = maxf(0.0, damage_multiplier)
	chip_multiplier = maxf(0.0, chip_multiplier)
	push_scale = maxf(0.0, push_scale)
	var r := Result.new()
	r.A = a
	r.D = d
	r.B = d
	r.H = hit_level(a, d, attack_tg, defense_tg, sys)
	r.attack_tg = attack_tg
	r.defense_tg = defense_tg
	r.outcome = Sk.Outcome.GUARD if matching_guard else (Sk.Outcome.BLOCK if r.H <= r.B else Sk.Outcome.HIT)
	r.fd = matching_guard and fd
	r.jg_reduction = float(maxi(0, mini(int(g.maxReduction), jg))) if matching_guard else 0.0
	r.raw_stun = r.H / float(c.stunDivisor) - r.jg_reduction
	r.surplus = maxf(0.0, -r.raw_stun)
	r.stun_frames = frames(maxf(0.0, r.raw_stun))
	r.hitstop_frames = frames(a * tg_multiplier(attack_tg, sys) / tg_multiplier(defense_tg, sys) / float(c.hitstopDivisor))
	r.damage = 0.0 if r.fd else r.H * damage_multiplier * (1.0 if r.outcome == Sk.Outcome.HIT else chip_multiplier)
	var k := r.H * push_scale
	var j := r.jg_reduction / maxf(1.0, float(g.maxReduction))
	if r.outcome == Sk.Outcome.HIT:
		r.self_push = float(c.hitPushRatio) * k
	elif r.outcome == Sk.Outcome.BLOCK:
		r.self_push = k / float(c.blockPushDivisor)
	elif r.fd:
		r.self_push = k
	else:
		r.self_push = k * (1.0 - float(g.jgSelfPushReduction) * j)
	r.attacker_push = k * float(g.fdAttackerPush) * (1.0 + float(g.fdJGBonus) * j) if r.fd else 0.0
	return r

static func push_step(distance: float, completed: int, total: int = 10) -> float:
	if completed < 0 or completed >= total or total < 1:
		return 0.0
	var a := float(completed) / total
	var b := float(completed + 1) / total
	return maxf(0.0, distance) * ((2 * b - b * b) - (2 * a - a * a))

# --- legacy TrainingHitFormula (float32) ---
static func legacy_result(attack: float, defense: float) -> float:
	return Sk.f(attack - defense)

static func legacy_hitstun_frames(source: float, result: float) -> float:
	return Sk.f(Sk.f(source * Sk.f(0.1)) + Sk.f(result * Sk.f(0.4)))

static func legacy_guardstun_frames(source: float, defense: float) -> float:
	return Sk.f(Sk.f(source * Sk.f(0.4)) * Sk.f(pow(2.0, Sk.f(-defense / 100.0))))

static func legacy_hitstop_frames(source: float) -> float:
	return Sk.f(source * Sk.f(0.2))

## TrainingContactOrder.Prefer
static func prefer(priority: int, result: float, id: int, previous_priority: int, previous_result: float, previous_id: int) -> bool:
	return priority > previous_priority or (priority == previous_priority and (result > previous_result or (result == previous_result and id < previous_id)))
