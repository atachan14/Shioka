extends SceneTree
## Headless checks of the ported rules and frame data.
## Run: Godot_v4.7.2-stable_win64_console.exe --headless --path godot --script res://tests/run_tests.gd
## Expected numbers come from the Unity C# rules (docs/spec/combat.md) and from replaying
## Unity's float32 time accumulation (e.g. 5HK lasts 57 ticks, not 56).

var failures := 0
var matches: Array = []
var checks := 0

class Script_:
	extends RefCounted
	var frames := {}   # tick -> InputFrame
	var tick := 0
	var hold_move := Vector2.ZERO
	func at(t: int) -> InputFrame:
		if not frames.has(t):
			frames[t] = InputFrame.new()
		return frames[t]
	func read_input() -> InputFrame:
		tick += 1
		var f: InputFrame = frames.get(tick, InputFrame.new()).copy()
		if f.movement == Vector2.ZERO:
			f.movement = hold_move
		return f
	func clear() -> void:
		pass

func expect(cond: bool, label: String) -> void:
	checks += 1
	if not cond:
		failures += 1
		print("FAIL: ", label)
	else:
		print("ok   ", label)

func eq(a, b, label: String) -> void:
	expect(a == b, "%s (got %s, expected %s)" % [label, str(a), str(b)])

func _init() -> void:
	test_formula()
	test_5lp_hit()
	test_guard()
	test_5hk_length()
	test_jump_and_backstep()
	test_trc()
	test_throw()
	test_key_config()
	test_dummy_command()
	for each in matches:
		each.dispose()
	matches.clear()
	print("\n%d checks, %d failures" % [checks, failures])
	quit(1 if failures > 0 else 0)

func new_match(gap: float) -> Array:
	var m := Match.new("awari", "awari")
	matches.append(m)
	var a := Script_.new()
	var b := Script_.new()
	m.player.input_source = a
	m.dummy_fighter.input_source = b
	m.player.body.position = Vector2(-gap / 2, 0.02)
	m.dummy_fighter.body.position = Vector2(gap / 2, 0.02)
	m.camera.snap_framing()
	for i in 3:  # settle on the floor
		m.step()
	a.tick = 0
	b.tick = 0
	return [m, a, b]

func press_slot(s: Script_, t: int, slot: int) -> void:
	s.at(t).attack_pressed_mask |= 1 << slot
	s.at(t).attack_held_mask |= 1 << slot

func test_formula() -> void:
	var sys := GameData.system()
	eq(CombatFormula.tg_multiplier(100, sys), 1.5, "M(100) = 1.5")
	eq(CombatFormula.tg_multiplier(-100, sys), 0.5, "M(-100) = 0.5")
	for pair in [[260, 130, 13, 7], [360, 180, 18, 9], [460, 230, 23, 12]]:
		var r := CombatFormula.resolve(pair[0], 100, 0, 0, false, false, 0, 1, .1, .001, sys)
		eq(snappedf(r.H, 0.0001), float(pair[1]), "H for A=%d" % pair[0])
		eq(r.stun_frames, pair[2], "stun for A=%d" % pair[0])
		eq(r.hitstop_frames, pair[3], "hitstop for A=%d" % pair[0])
		eq(r.outcome, Sk.Outcome.HIT, "outcome Hit for A=%d" % pair[0])
	var low := CombatFormula.resolve(150, 100, 0, 0, false, false, 0, 1, .1, .001, sys)
	eq(low.outcome, Sk.Outcome.BLOCK, "H <= B is Block")
	eq(CombatFormula.jg_reduction(0, false, sys), 10, "JG age 0")
	eq(CombatFormula.jg_reduction(29, false, sys), 1, "JG age 29")
	eq(CombatFormula.jg_reduction(30, false, sys), 0, "JG age 30")
	eq(CombatFormula.jg_reduction(19, true, sys), 1, "JG continuous age 19")
	eq(CombatFormula.jg_reduction(20, true, sys), 0, "JG continuous age 20")
	var g := CombatFormula.resolve(260, 100, 0, 0, true, false, 4, 1, .1, .001, sys)
	eq(g.stun_frames, 9, "Guard stun = 13 - JG 4")
	eq(snappedf(g.damage, 0.0001), 13.0, "chip = H x 0.1")

func test_5lp_hit() -> void:
	var r := new_match(1.0)
	var m: Match = r[0]
	press_slot(r[1], 1, Sk.Slot.J)
	var contact_tick := -1
	var attacker_idle := -1
	var defender_idle := -1
	var seq := m.dummy_fighter.contact_sequence
	for t in range(1, 60):
		m.step()
		if contact_tick < 0 and m.dummy_fighter.contact_sequence != seq:
			contact_tick = t
			eq(m.dummy_fighter.last_result, "Hit", "5LP hits an idle dummy")
			eq(m.dummy_fighter.v01_last_contact.stun_frames, 13, "5LP hitstun 13F")
			eq(m.dummy_fighter.v01_last_contact.hitstop_frames, 7, "5LP hitstop 7F")
			eq(snappedf(m.dummy_fighter.health, 0.001), 2300.0 - 130.0, "5LP damage 130")
		if contact_tick > 0 and attacker_idle < 0 and m.player.phase == Sk.Phase.IDLE:
			attacker_idle = t
		if contact_tick > 0 and defender_idle < 0 and m.dummy_fighter.phase == Sk.Phase.IDLE and m.dummy_fighter.stun_remaining == 0:
			defender_idle = t
	eq(contact_tick, 6, "5LP startup: contact on the 6th tick")
	eq(attacker_idle, 26, "5LP ends at tick 19 + 7 hitstop")
	eq(defender_idle, 26, "dummy leaves hitstun at tick 6 + 7 + 13")

func test_guard() -> void:
	var r := new_match(1.0)
	var m: Match = r[0]
	m.dummy_fighter.guarding = true
	press_slot(r[1], 1, Sk.Slot.J)
	for t in range(1, 8):
		m.step()
	eq(m.dummy_fighter.last_result, "Guard", "guarding dummy guards 5LP")
	eq(m.dummy_fighter.v01_last_contact.stun_frames, 13, "guard stun 13F without just guard")
	expect(m.dummy_fighter.health < 2300.0 and m.dummy_fighter.health > 2280.0, "chip damage applied")

func test_5hk_length() -> void:
	var r := new_match(4.0)
	var m: Match = r[0]
	press_slot(r[1], 1, Sk.Slot.Z)
	var end := -1
	for t in range(1, 80):
		m.step()
		if t == 1:
			eq(m.player.current_move.role, "move:5", "Z = HK (5HK)")
		if end < 0 and t > 1 and m.player.phase == Sk.Phase.IDLE:
			end = t
	eq(end, 57, "5HK whiff lasts 57 ticks (Unity float32 timing)")

func test_jump_and_backstep() -> void:
	var r := new_match(4.0)
	var m: Match = r[0]
	var s: Script_ = r[1]
	s.at(1).commands = [Sk.Cmd.JUMP]
	var air := -1
	var landing := -1
	var idle := -1
	for t in range(1, 120):
		m.step()
		if air < 0 and m.player.phase == Sk.Phase.AIR:
			air = t
		if air > 0 and landing < 0 and m.player.phase == Sk.Phase.LANDING:
			landing = t
		if landing > 0 and idle < 0 and m.player.phase == Sk.Phase.IDLE:
			idle = t
	eq(air, 6, "takeoff 6 ticks")
	print("     jump: air at %d, landing at %d (airtime %d), idle at %d" % [air, landing, landing - air, idle])
	expect(landing - air >= 53 and landing - air <= 56, "airtime about 54F (jumpAirFrames)")
	eq(idle - landing, 9, "landing recovery 9 ticks (0.14 s)")
	r = new_match(4.0)
	m = r[0]
	s = r[1]
	s.at(1).dash_pressed = true
	s.at(1).movement = Vector2(-1, 0)
	var start := m.player.position().x
	var end := -1
	for t in range(1, 60):
		m.step()
		if t == 1:
			eq(m.player.phase, Sk.Phase.BACKSTEP, "dash + back = backstep")
		if end < 0 and t > 1 and m.player.phase == Sk.Phase.IDLE:
			end = t
	eq(end, 25, "backstep 25 ticks")
	print("     backstep distance %.3f" % (start - m.player.position().x))

func test_trc() -> void:
	var r := new_match(4.0)
	var m: Match = r[0]
	var s: Script_ = r[1]
	press_slot(s, 1, Sk.Slot.J)
	s.at(12).commands = [Sk.Cmd.CANCEL]
	for t in range(1, 13):
		m.step()
		if t == 11:
			eq(m.player.lock_kind(), Sk.Lock.RECOVERY, "5LP in recovery at tick 11")
	eq(m.player.phase, Sk.Phase.IDLE, "TRC returns to neutral")
	eq(m.player.cancel_count, 1, "TRC performed")
	# At tick 12 before TRC: timer 11/60, remaining 19 - 11 = 8 frames.
	eq(snappedf(m.player.tension.gauge, 0.01), -8.0, "TRC cost = remaining lock frames")

func test_throw() -> void:
	var r := new_match(0.6)
	var m: Match = r[0]
	var s: Script_ = r[1]
	s.at(1).commands = [Sk.Cmd.THROW]
	var held := -1
	var down := -1
	for t in range(1, 90):
		m.step()
		if held < 0 and m.player.phase == Sk.Phase.THROW_HOLD:
			held = t
		if held > 0 and down < 0 and m.dummy_fighter.phase == Sk.Phase.KNOCKDOWN:
			down = t
	eq(held, 6, "throw catches on the 6th tick (startup 6)")
	eq(down, 30, "throw resolves after the 24F hold")
	eq(snappedf(m.dummy_fighter.health, 0.001), 2300.0 - 16.0, "throw damage 16")

func test_key_config() -> void:
	var p := KeyConfig.make_defaults()
	eq(KeyConfig.validate(p), "", "default KeyConfig is valid")
	eq(KeyConfig.row(p, "battle.Dash").pad, "T1+<Gamepad>/buttonSouth", "pad Dash = T1 + ×")
	eq(KeyConfig.row(p, "battle.Hold").pad, "T1+<Gamepad>/buttonEast", "pad Hold = T1 + ○")
	eq(KeyConfig.row(p, "battle.A").pad, "T1+<Gamepad>/buttonWest", "pad A = T1 + □")
	eq(KeyConfig.row(p, "battle.J").keyboard, "<Keyboard>/j", "keyboard J")
	var t := KeyConfig.try_binding(p, "battle.J", false, "<Keyboard>/k", false)
	eq(t.conflict, "battle.K", "binding K to J conflicts with K")
	t = KeyConfig.try_binding(p, "battle.J", false, "<Keyboard>/k", true)
	expect(t.ok and KeyConfig.row(p, "battle.K").keyboard == "<Keyboard>/j", "approved swap exchanges J and K")
	t = KeyConfig.try_binding(p, "battle.J", false, "<Keyboard>/w", false)
	expect(not t.ok, "direction keys are reserved")

func test_dummy_command() -> void:
	var a := TrainingDummy.parse("3強")
	expect(a.valid and a.result.command == Sk.Cmd.STRONG_KICK and a.result.direction == 3, "3強 = sweep input")
	var b := TrainingDummy.parse("２弱Ｋ")
	expect(b.valid and b.result.command == Sk.Cmd.CROUCH_WEAK_KICK, "full-width 2弱K = crouch LK")
	var c := TrainingDummy.parse("8弱")
	expect(not c.valid, "up + attack is rejected")
