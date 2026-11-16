extends Node2D
## Training scene (Unity: CombatFormulaComparison.unity + TrainingOverlay world parts).
## Drives Match.step() at 60 Hz in _physics_process and renders the fighters, the floor,
## debug boxes (hurtbox / hitbox / throw receiver) and the TRC red ring.

const PPM := 100.0  # Godot pixels per meter in world space
const BACKGROUND := Color(0.19215687, 0.3019608, 0.4745098)

var match_: Match
var session: TrainingSession
var player_controls: PlayerControls
var second_controls: PlayerControls
var dummy: TrainingDummy
var second_player_controlled := false
var option_visible := false
var commands_first := CommandHistory.new()
var commands_second := CommandHistory.new()
var ui
var camera: Camera2D
var _sprites := {}        # Fighter -> Sprite2D
var _textures := {}
var _rings: Array = []    # {center, age}
var _overlay: Node2D
var _background: Node2D
var level_colors := {"minus100": Color(.68, .6, .38, .85), "zero": Color(.76, .94, .32, .19607843),
	"hundred": Color(.52, .42, .96, .38), "twoHundred": Color(.45, .44, .6, .85)}

func _ready() -> void:
	var player_id: String = Flow.player_fighter_id if not Flow.player_fighter_id.is_empty() else Flow.default_fighter_id()
	var cpu_id: String = Flow.cpu_fighter_id if not Flow.cpu_fighter_id.is_empty() else Flow.default_fighter_id()
	Flow.awaiting_training_entry = false
	match_ = Match.new(player_id, cpu_id)
	player_controls = PlayerControls.new(InputHub, false)
	second_controls = PlayerControls.new(InputHub, true)
	second_controls.enabled = false
	dummy = TrainingDummy.new(match_.dummy_fighter)
	match_.player.input_source = player_controls
	match_.dummy_fighter.input_source = dummy
	session = TrainingSession.new(match_, self, dummy)
	for f: Fighter in [match_.player, match_.dummy_fighter]:
		f.trc_performed.connect(_on_trc)
	_background = Node2D.new()
	_background.z_index = -10
	_background.draw.connect(_draw_background.bind(_background))
	add_child(_background)
	for f: Fighter in [match_.dummy_fighter, match_.player]:
		var s := Sprite2D.new()
		s.centered = false
		add_child(s)
		_sprites[f] = s
	_overlay = Node2D.new()
	_overlay.z_index = 10
	_overlay.draw.connect(_draw_overlay.bind(_overlay))
	add_child(_overlay)
	camera = Camera2D.new()
	camera.anchor_mode = Camera2D.ANCHOR_MODE_DRAG_CENTER
	add_child(camera)
	camera.make_current()
	match_.camera.snap_framing()
	ui = preload("res://scripts/ui/training_ui.gd").new()
	var layer := CanvasLayer.new()
	layer.layer = 5
	add_child(layer)
	layer.add_child(ui)
	ui.setup(self)
	if Flow.open_options_from_title:
		Flow.open_options_from_title = false
		ui.return_to_title_on_close = true
		set_option_visible(true)
		ui.show_page("keys")
	_update_view(0.0)

func _exit_tree() -> void:
	player_controls.detach()
	second_controls.detach()
	match_.dispose()

func _physics_process(_delta: float) -> void:
	if option_visible:
		return
	match_.step()

func _process(delta: float) -> void:
	_update_view(delta)
	for i in range(_rings.size() - 1, -1, -1):
		_rings[i].age += delta
		if _rings[i].age >= 0.22:
			_rings.remove_at(i)
	_overlay.queue_redraw()
	_background.queue_redraw()

func _update_view(delta: float) -> void:
	var view: Vector3 = match_.camera.render_frame(0.0 if option_visible else delta)
	camera.position = Vector2(view.x * PPM, -view.y * PPM)
	var zoom := 1600.0 / (2.0 * view.z * CameraBounds.ASPECT * PPM)
	camera.zoom = Vector2(zoom, zoom)
	for f: Fighter in _sprites.keys():
		var p := FighterView.present(f)
		var s: Sprite2D = _sprites[f]
		var path := String(p.sprite)
		if path.is_empty():
			s.visible = false
			continue
		s.visible = true
		if not _textures.has(path):
			_textures[path] = load(path)
		s.texture = _textures[path]
		var info: Dictionary = GameData.sprites().get(path, {"size": [1, 1], "pivot": [.5, 0], "ppu": 100})
		var w := float(info.size[0])
		var h := float(info.size[1])
		s.offset = Vector2(-float(info.pivot[0]) * w, -(1.0 - float(info.pivot[1])) * h)
		var k: float = PPM / float(info.ppu) * float(p.scale)
		s.scale = Vector2(-k if p.flip else k, k)
		var world: Vector2 = f.position() + p.offset
		s.position = Vector2(world.x * PPM, -world.y * PPM)

static func to_screen(v: Vector2) -> Vector2:
	return Vector2(v.x * PPM, -v.y * PPM)

func _px() -> float:
	return 1.0 / maxf(0.0001, camera.zoom.x)

func _draw_background(node: Node2D) -> void:
	var view: Vector3 = match_.camera.render_frame(0.0)
	var half_w := view.z * CameraBounds.ASPECT
	var r := Rect2(to_screen(Vector2(view.x - half_w - 1, view.y + view.z + 1)), Vector2((half_w + 1) * 2 * PPM, (view.z + 1) * 2 * PPM))
	node.draw_rect(r, BACKGROUND)
	# 2D floor display: gray block below y = 0 over the stage (-7..7).
	node.draw_rect(Rect2(to_screen(Vector2(-7, 0)), Vector2(14 * PPM, 10 * PPM)), Color(.38, .38, .38))
	var tick_color := Color(.2, .3, .35)
	for x in range(-6, 7):
		var top := 3.0 if x % 2 == 0 else 0.25
		node.draw_line(to_screen(Vector2(x, 0)), to_screen(Vector2(x, top)), tick_color, (0.035 if x == 0 else 0.015) * PPM)
	node.draw_line(to_screen(Vector2(-0.015, -0.02)), to_screen(Vector2(0.015, -0.02)), Color(.65, .7, .75), 0.02 * PPM)

func level_color(level: float) -> Color:
	var a: Color = level_colors.minus100
	var z: Color = level_colors.zero
	var h: Color = level_colors.hundred
	var t: Color = level_colors.twoHundred
	if level <= -100: return a
	if level < 0: return a.lerp(z, (level + 100) / 100.0)
	if level < 100: return z.lerp(h, level / 100.0)
	if level < 200: return h.lerp(t, (level - 100) / 100.0)
	return t

func _box(node: Node2D, center: Vector2, half: Vector2, color: Color, width: float) -> void:
	var tl := to_screen(center + Vector2(-half.x, half.y))
	node.draw_rect(Rect2(tl, half * 2.0 * PPM), color, false, width)

func _draw_overlay(node: Node2D) -> void:
	var px := _px()
	for f: Fighter in [match_.player, match_.dummy_fighter]:
		for hb in f.hurtboxes:
			if not hb.enabled:
				continue
			var chosen: bool = f.last_contact_hurtbox_id == hb.id
			var color := Color.YELLOW if chosen else (Color.GREEN if f == match_.player else Color.CYAN)
			_box(node, f.position() + hb.offset, hb.size * 0.5, color, maxf(1.0, px))
		if f.throw_hurtbox.enabled:
			_box(node, f.position() + f.throw_hurtbox.offset, f.throw_hurtbox.size * 0.5, Color(.3, .6, 1), maxf(1.0, px))
		if f.hitbox_active():
			var c := Color(1, .65, 0) if f.current_attack_class() == Sk.AttackClass.THROW else level_color(f.attack_level())
			c.a = 1.0
			_box(node, f.hitbox_center(), f.hitbox_half_size(), c, maxf(1.5, 1.3 * px))
	for ring in _rings:
		var t := Sk.clamp01(ring.age / 0.22)
		var radius := lerpf(0.12, 0.9, 1.0 - (1.0 - t) * (1.0 - t))
		node.draw_arc(to_screen(ring.center), radius * PPM, 0, TAU, 48, Color(1, .045, .025, (1 - t) * .72), 0.11 * PPM)

func _on_trc(f: Fighter) -> void:
	if _rings.size() >= 4:
		_rings.remove_at(0)
	_rings.append({"center": f.body.bounds_center(), "age": 0.0})

# ---------------------------------------------------------------- called by session / UI
func unblock_controls() -> void:
	player_controls.set_option_blocked(false)
	second_controls.set_option_blocked(false)

func capture_command_displays() -> void:
	commands_first.capture(match_.player)
	commands_second.capture(match_.dummy_fighter)

func set_second_player_control(controlled: bool) -> void:
	second_controls.clear()
	second_controls.enabled = controlled
	match_.dummy_fighter.set_input_source(second_controls if controlled else dummy)
	second_player_controlled = controlled

func set_option_visible(visible: bool) -> void:
	if option_visible == visible:
		return
	if visible:
		session.menu_opened()
	option_visible = visible
	match_.option_visible = visible
	for f: Fighter in [match_.player, match_.dummy_fighter]:
		f.set_option_input_suspended(visible)
	player_controls.set_option_blocked(visible)
	second_controls.set_option_blocked(visible)
	ui.on_option_visible(visible)
