class_name CameraBounds
extends RefCounted
## TrainingCameraBounds. The authoritative wall state (left/right) advances only on the
## combat tick; render smoothing consumes that state and never moves fighters.

const ASPECT := 16.0 / 9.0
const STAGE_LEFT := -6.0
const STAGE_RIGHT := 6.0

var display: Dictionary   # MatchDisplayProfile values (system.json "display")
var first: Fighter
var second: Fighter
var left := STAGE_LEFT
var right := STAGE_RIGHT
var width := 0.0
var center := 0.0
var _zoom_velocity := [0.0]
var _pan_velocity := [0.0]
var _ready := false
var render_width := 0.0
var render_center := 0.0
var _render_zoom_velocity := [0.0]
var _render_pan_velocity := [0.0]
var _render_ready := false

func _init(display_settings: Dictionary, a: Fighter, b: Fighter) -> void:
	display = display_settings
	first = a
	second = b

static func _margin(actor: Fighter) -> float:
	return 0.35 if actor == null else absf(actor.body.center.x) + actor.body.radius + actor.body.skin_width + 0.02

func body_margin() -> float:
	return maxf(_margin(first), _margin(second))

func snap_framing() -> void:
	_ready = false
	_render_ready = false
	advance_simulation(0.0, true)
	render_frame(0.0, true)

func advance_simulation(dt: float, snap: bool = false) -> void:
	if first == null or second == null:
		return
	var minimum := maxf(0.1, float(display.minimumWidth))
	var maximum := maxf(minimum, float(display.maximumWidth))
	var a := first.position().x
	var b := second.position().x
	var gap := absf(b - a)
	var separating := maxf(0.0, signf(b - a) * (second.horizontal_speed - first.horizontal_speed))
	var look := maxf(0.0, float(display.lookAheadSeconds))
	var padding := maxf(0.1, float(display.fitPadding))
	var anchor := Sk.clamp01(float(display.groundAnchor))
	var top := maxf(first.position().y + first.body.height + maxf(0.0, first.vertical_speed) * look,
		second.position().y + second.body.height + maxf(0.0, second.vertical_speed) * look) + maxf(0.0, float(display.airPadding))
	var target := clampf(maxf(gap + padding * 2.0 + separating * look, top / maxf(0.5, 1.0 + anchor - 0.12) * 2.0 * ASPECT), minimum, maximum)
	dt = clampf(dt, 0.0, 0.1)
	if not _ready or snap:
		width = target
		center = (a + b) * 0.5
		_zoom_velocity = [0.0]
		_pan_velocity = [0.0]
		_ready = true
	elif dt > 0.0:
		width = Sk.smooth_damp(width, target, _zoom_velocity, maxf(0.01, float(display.zoomSmoothSeconds)), dt)
		center = Sk.smooth_damp(center, (a + b) * 0.5, _pan_velocity, maxf(0.01, float(display.panSmoothSeconds)), dt)
	width = clampf(maxf(width, gap + body_margin() * 2.0), minimum, maximum)
	var half := width * 0.5
	var stage_low := STAGE_LEFT + half
	var stage_high := STAGE_RIGHT - half
	if stage_low <= stage_high:
		center = clampf(center, stage_low, stage_high)
	var low := maxf(a, b) + body_margin() - half
	var high := minf(a, b) - body_margin() + half
	if low <= high:
		center = clampf(center, low, high)
	left = maxf(STAGE_LEFT, center - half + body_margin())
	right = minf(STAGE_RIGHT, center + half - body_margin())

## Returns Vector3(render_center_x, camera_y, half_height) for the view.
func render_frame(dt: float, snap: bool = false) -> Vector3:
	if not _ready:
		return Vector3(0, 2.13, 2.6)
	dt = clampf(dt, 0.0, 0.1)
	if not _render_ready or snap:
		render_width = width
		render_center = center
		_render_zoom_velocity = [0.0]
		_render_pan_velocity = [0.0]
		_render_ready = true
	elif dt > 0.0:
		render_width = Sk.smooth_damp(render_width, width, _render_zoom_velocity, 0.08, dt)
		render_center = Sk.smooth_damp(render_center, center, _render_pan_velocity, 0.08, dt)
	var minimum := float(display.minimumWidth)
	var maximum := float(display.maximumWidth)
	var a := first.position().x
	var b := second.position().x
	render_width = clampf(maxf(render_width, absf(a - b) + 2.0 * body_margin()), minimum, maximum)
	var low := maxf(a, b) + body_margin() - render_width * 0.5
	var high := minf(a, b) - body_margin() + render_width * 0.5
	if low <= high:
		render_center = clampf(render_center, low, high)
	var half_height := render_width / (2.0 * ASPECT)
	return Vector3(render_center, half_height * float(display.groundAnchor), half_height)

func movement_limits(actor: Fighter) -> Vector2:
	var x := actor.position().x
	return Vector2(minf(left, x), maxf(right, x))

func limit_position(start: float, desired: float) -> float:
	return clampf(desired, minf(left, start), maxf(right, start))
