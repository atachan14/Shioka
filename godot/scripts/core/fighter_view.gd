class_name FighterView
## ArmoredStaticPoseView.Present + TrainingProvisionalPoses: picks one sprite per frame and
## its offset. Presentation only; reads the fighter state, never changes the simulation
## (except the motion-sampling clock that Unity's MotionPose getter also advances).

const C := Sk.Cmd
const P := Sk.Phase

## Returns {sprite, offset: Vector2 (meters, relative to the fighter root), scale, flip, name}
static func present(f: Fighter) -> Dictionary:
	var raw: Dictionary = f.data.raw
	var poses: Dictionary = raw.poses
	var m: Dictionary = raw.motions
	var move := f.current_move
	var kick := move != null and (move.command == C.STRONG_KICK or move.command == C.AIR_STRONG_KICK) and (f.phase == P.ATTACK or f.is_air_combat_attack())
	var idle := _pose_sprite(m.idle, 0)
	var selected := idle
	var name := "Idle"
	var punch := _active_punch(f)
	if not punch.is_empty():
		selected = punch
		name = "Active %s P" % Sk.CMD_NAMES[move.command]
	elif kick:
		selected = poses.strongKick
		name = "StrongKick"
	elif f.phase == P.BACKSTEP:
		var elapsed := Sk.f(f.def.backstepSeconds - Sk.f(f.remaining_lock_frames() / 60.0))
		var loco := f.locomotion()
		var fr := elapsed * 60.0 + 0.0001
		var index := 0 if fr < float(loco.pose1EndFrame) else (1 if fr < float(loco.pose2EndFrame) else 2)
		selected = _pose_sprite(m.backstep, [0, 4, 8][index])
		name = "Backstep%d" % (index + 1)
	elif f.is_air_dashing:
		selected = _pose_sprite(m.airDash, 0)
		name = "AirDash"
	elif f.phase == P.TAKEOFF or f.phase == P.AIR:
		var rising := f.phase == P.TAKEOFF or f.vertical_speed > 0
		selected = _pose_sprite(m.jump, 0 if rising else 1)
		name = "JumpAscent" if rising else "JumpDescent"
	elif f.is_crouching:
		selected = _pose_sprite(m.crouch, 0)
		name = "Crouch"
	elif _standing_guard_visible(f):
		selected = poses.standingGuard
		name = "StandingGuard"
	elif f.phase == P.MOVE:
		var running: bool = f.horizontal_speed * f.facing > 0.01 and (f.dash_inertia or absf(f.horizontal_speed) > f.def.walkSpeed + 0.1)
		var retreating: bool = f.horizontal_speed * f.facing < -0.01
		selected = _pose_sprite(m.backwardWalk, 0) if retreating else (_pose_sprite(m.groundDash, 0) if running else _pose_sprite(m.forwardWalk, 0))
		name = "BackwardWalk" if retreating else ("Dash" if running else "Walk")
	var key := provisional_key(f)
	var extra: Dictionary = f.data.provisional.get(key, {}) if not key.is_empty() else {}
	if not extra.is_empty() and extra.get("sprite") != null:
		selected = extra.sprite
		name = "Provisional " + key
	var motion: Dictionary = f.motion_pose() if extra.is_empty() else {}
	if not motion.is_empty():
		selected = motion.sprite
	var skill := ""
	if (f.phase == P.ATTACK or f.is_air_combat_attack()) and move != null:
		skill = move.sequence_sprite_at(Sk.f(f.air_clock.elapsed * 60.0) if f.phase == P.AIR else Sk.f(f.timer * 60.0))
	if not skill.is_empty():
		selected = skill
		motion = {}
		extra = {}
		name = "Skill sequence " + Sk.CMD_NAMES[move.command]
	if selected == null or String(selected).is_empty():
		selected = idle
	var offset := Vector2(0, float(extra.offset_y) if not extra.is_empty() else 0.0)
	if motion.is_empty() and f.is_air_dashing and selected == _pose_sprite(m.airDash, 0):
		offset += Sk.from_legacy(raw.air_dash_visual_offset, f.facing)
	if not motion.is_empty():
		offset += Vector2(float(motion.offset[0]) * f.facing, float(motion.offset[1]))
	return {"sprite": selected, "offset": offset, "scale": visual_scale(f), "flip": f.facing < 0, "name": name}

static func _pose_sprite(action: Dictionary, index: int) -> String:
	var list: Array = action.get("poses", [])
	return list[index].sprite if index < list.size() else ""

static func _active_punch(f: Fighter) -> String:
	if f.phase != P.ATTACK or not f.hitbox_active() or f.current_move == null:
		return ""
	var poses: Dictionary = f.data.raw.poses
	match f.current_move.command:
		C.LIGHT: return poses.weakPunch
		C.MEDIUM: return poses.mediumPunch
		C.HEAVY: return poses.strongPunch
	return ""

static func _standing_guard_visible(f: Fighter) -> bool:
	return f.grounded and not f.is_crouching and (f.phase == P.GUARD_STUN or ((f.phase == P.IDLE or f.phase == P.MOVE) and f.is_guarding()))

## TrainingBodyCalibration.VisualScale
static func visual_scale(f: Fighter) -> float:
	var b: Dictionary = f.data.raw.body
	var info: Dictionary = GameData.sprites().get(b.reference_sprite, {})
	var ppu := float(info.get("ppu", 700))
	return float(b.standing_height) * ppu / (float(b.sole_pixel[1]) - float(b.skull_pixel[1]))

## TrainingProvisionalPoses.StateKey
static func provisional_key(f: Fighter) -> String:
	if f.phase == P.DOWN or f.phase == P.KNOCKDOWN:
		return "Down"
	if f.stun_remaining > 0 and f.last_result == "Hit":
		return "jHit" if not f.grounded else ("2Hit" if f.is_crouching else "5Hit")
	var guard := f.phase == P.GUARD_STUN or f.is_guarding()
	if guard and ((f.stun_remaining > 0 and f.last_result == "Guard") or (not f.is_air_combat_attack() and f.phase != P.ATTACK)) and (not f.grounded or f.is_crouching):
		return "2Guard" if f.grounded else "jGuard"
	if f.phase == P.THROW_ATTEMPT or f.phase == P.THROW_HOLD:
		return "Throw"
	if f.is_air_combat_attack() and f.air_clock.in_active_window():
		if f.current_move != null:
			match f.current_move.command:
				C.AIR_STRONG_PUNCH: return "jHP"
				C.AIR_STRONG_KICK, C.AIR_DOWN_STRONG_KICK: return "jHK"
		if f.air_candidate_name == "flyingKick":
			return "jLK"
		if f.air_candidate_name == "dropKick":
			return "jMK"
	if f.phase == P.ATTACK and f.hitbox_active() and f.current_move != null:
		match f.current_move.command:
			C.CROUCH_WEAK_PUNCH: return "2LP"
			C.CROUCH_MEDIUM_PUNCH: return "2MP"
			C.CROUCH_STRONG_PUNCH: return "2HP"
			C.CROUCH_WEAK_KICK: return "2LK"
			C.CROUCH_MEDIUM_KICK: return "2MK"
			C.CROUCH_STRONG_KICK, C.SWEEP: return "2HK"
			C.SLASH: return "5LK"
			C.HEAVY_SLASH: return "5MK"
			C.STRONG_KICK: return "5HK"
	return ""
