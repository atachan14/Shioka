class_name Motor
extends RefCounted
## TrainingMotor2D: kinematic capsule against the Training floor (top at y = 0).
## Unity used Physics2D capsule casts; the Training stage has only the flat floor
## (fighters ignore each other here; their pushbox is resolved by Fighter), so the
## cast is solved analytically. Separation = Physics2D.defaultContactOffset*0.5 + 0.001.

enum Contact { NONE = 0, SIDES = 1, ABOVE = 2, BELOW = 4 }
const SEPARATION := 0.006
const FLOOR_Y := 0.0
const FLOOR_LEFT := -7.0
const FLOOR_RIGHT := 7.0

var position := Vector2.ZERO
var height := 1.55
var radius := 0.26
var center := Vector2(0, 0.775)
var skin_width := 0.01
var enabled := true

func bottom() -> float:
	return position.y + center.y - height * 0.5

func bounds_center() -> Vector2:
	return position + center

func bounds_extents() -> Vector2:
	return Vector2(radius, height * 0.5)

func teleport(p: Vector2) -> void:
	position = p

func _on_floor_x(x: float) -> bool:
	return x + radius >= FLOOR_LEFT and x - radius <= FLOOR_RIGHT

func move(displacement: Vector2) -> int:
	if not enabled:
		return Contact.NONE
	var result := Contact.NONE
	var remaining := displacement
	for _pass in range(4):
		if remaining.length_squared() <= 1e-12:
			break
		var length := remaining.length()
		var direction := remaining / length
		var hit := false
		var distance := 0.0
		if direction.y < -0.00001 and _on_floor_x(position.x):
			var gap := bottom() - FLOOR_Y
			distance = maxf(0.0, gap / -direction.y) if gap >= 0.0 else 0.0
			hit = distance <= length + SEPARATION
		if not hit:
			position += remaining
			remaining = Vector2.ZERO
			break
		var travel := clampf(distance - SEPARATION, 0.0, length)
		position += direction * travel
		result |= Contact.BELOW
		remaining -= direction * travel
		var into := remaining.y  # dot(remaining, floor normal (0, 1))
		if into < 0.0:
			remaining.y = 0.0
	return result

func has_floor_support() -> bool:
	return _on_floor_x(position.x) and bottom() - FLOOR_Y <= 0.04
