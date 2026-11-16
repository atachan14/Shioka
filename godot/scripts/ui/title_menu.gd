extends Control
## TrainingFrontMenu: Title and the shared fighter-select screen (Player / CPU modes).
## Unity laid this out on a 1280 x 720 canvas (center origin, y up); positions are scaled
## by 1.25 onto the 1600 x 900 viewport.

enum Mode { PLAYER, CPU }
const S := 1.25

var menu: MenuInput
var selection_visible := false
var mode := Mode.PLAYER
var player_id := ""
var cpu_id := ""
var selected_id := ""
var status := ""
var starting := false
var _cells: Array = []   # {button, row, column, fighter_id, random}
var _focus := 0
var _preview: Label
var _font: Font
var _root: Control

func _ready() -> void:
	_font = SystemFont.new()
	_font.font_names = PackedStringArray(["Yu Gothic UI", "Yu Gothic", "Meiryo", "Noto Sans CJK JP", "Arial"])
	theme = Theme.new()
	theme.default_font = _font
	var bg := ColorRect.new()
	bg.color = Color(.035, .055, .065)
	bg.size = Vector2(1600, 900)
	bg.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(bg)
	_root = Control.new()
	_root.size = Vector2(1600, 900)
	_root.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(_root)
	for action in ["ui_up", "ui_down", "ui_left", "ui_right", "ui_accept", "ui_select", "ui_cancel", "ui_focus_next", "ui_focus_prev"]:
		if InputMap.has_action(action):
			InputMap.action_erase_events(action)
	menu = MenuInput.new(InputHub)
	_load()
	_build()

func _exit_tree() -> void:
	menu.dispose()

func _load() -> void:
	var prefs: Dictionary = Flow.load_preferences()
	status = prefs.status
	player_id = prefs.player
	cpu_id = prefs.cpu
	selected_id = cpu_id if mode == Mode.CPU else player_id

func _name_of(id: String) -> String:
	if not GameData.fighter_ids().has(id):
		return "FighterName"
	return GameData.character(id).name

static func _available(id: String) -> bool:
	return GameData.fighter_ids().has(id)

func _place(c: Control, pos: Vector2, dim: Vector2) -> void:
	c.size = dim * S
	c.position = Vector2(800 + pos.x * S, 450 - pos.y * S) - c.size / 2

func _label(text: String, font_size: int, pos: Vector2, dim: Vector2) -> Label:
	var l := Label.new()
	l.text = text
	l.add_theme_font_size_override("font_size", int(font_size * S))
	l.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	l.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	l.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_place(l, pos, dim)
	_root.add_child(l)
	return l

func _box(pos: Vector2, dim: Vector2) -> void:
	var r := ColorRect.new()
	r.color = Color(.07, .11, .14, .85)
	r.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_place(r, pos, dim)
	_root.add_child(r)

func _style(b: Button, focused: bool) -> void:
	for state in ["normal", "hover", "pressed", "focus", "disabled"]:
		var s := StyleBoxFlat.new()
		s.bg_color = Color(.15, .48, .48) if focused else Color(.1, .19, .2)
		if state == "disabled":
			s.bg_color = Color(.23, .26, .28)
		if state == "focus":
			s.draw_center = false
		b.add_theme_stylebox_override(state, s)

func _add(name_: String, text: String, action: Callable, enabled: bool, row: int, column: int, pos: Vector2, dim: Vector2, fighter_id := "", random := false) -> void:
	var b := Button.new()
	b.name = name_
	b.text = text
	b.disabled = not enabled
	b.focus_mode = Control.FOCUS_NONE
	b.add_theme_font_size_override("font_size", int((23 if enabled else 19) * S))
	b.add_theme_color_override("font_color", Color.WHITE)
	b.add_theme_color_override("font_disabled_color", Color(.55, .58, .6))
	_place(b, pos, dim)
	var cell := {"button": b, "row": row, "column": column, "fighter_id": fighter_id, "random": random}
	_cells.append(cell)
	b.pressed.connect(func():
		if b.disabled or starting:
			return
		_set_focus(_cells.find(cell))
		if action.is_valid():
			action.call())
	b.mouse_entered.connect(func():
		if not b.disabled:
			_set_focus(_cells.find(cell)))
	_root.add_child(b)

func _menu_binding(id: String) -> String:
	var r := KeyConfig.row(KeyConfig.ensure(), id)
	var parts: Array = []
	for v in [r.keyboard, r.pad]:
		if not KeyConfig.normalize(v).is_empty():
			parts.append(KeyConfig.binding_text(v))
	return " / ".join(parts)

func _build() -> void:
	for child in _root.get_children():
		_root.remove_child(child)
		child.queue_free()
	_cells.clear()
	_focus = 0
	_preview = null
	if not selection_visible:
		_build_title()
	else:
		_build_selection()
	_label("WASD / Dpad: 選択　決定: " + _menu_binding("menu.Confirm") + "　戻る: " + _menu_binding("menu.Cancel") + " / Esc　Mouse: click", 16, Vector2(0, -321), Vector2(1200, 38))
	if status.begins_with("Save failed"):
		_label("保存できませんでした。選択は未確定です。", 18, Vector2(0, 303), Vector2(1000, 35))
	if selection_visible:
		for i in _cells.size():
			if _cells[i].fighter_id == selected_id and not _cells[i].button.disabled:
				_focus = i
	if not _cells.is_empty():
		_set_focus(_focus)
	menu.clear()

func _build_title() -> void:
	_label("Shioka", 36, Vector2(0, 282), Vector2(700, 65))
	_box(Vector2(0, 135), Vector2(500, 210))
	_label("ArtGraphic（準備中）", 19, Vector2(0, 135), Vector2(480, 180))
	_label(_name_of(player_id), 30, Vector2(-420, 30), Vector2(350, 50))
	_add("FighterSelect", "FighterSelect", func(): _open_select(Mode.PLAYER), true, 0, 0, Vector2(-420, -35), Vector2(350, 54))
	_add("Training", "Training", func(): _open_select(Mode.CPU), true, 1, 0, Vector2(-420, -105), Vector2(350, 54))
	_add("CommandList", "CommandList（未実装）", Callable(), false, 2, 0, Vector2(-420, -175), Vector2(350, 54))
	_add("Option", "Option", _open_options, true, 3, 0, Vector2(-420, -245), Vector2(350, 54))
	_label("Online", 27, Vector2(0, -35), Vector2(340, 54))
	_label("Offline", 27, Vector2(420, -35), Vector2(340, 54))
	_add("Rank", "Rank（未実装）", Callable(), false, 1, 1, Vector2(0, -120), Vector2(350, 54))
	_add("Exhibition", "Exhibition（未実装）", Callable(), false, 2, 1, Vector2(0, -200), Vector2(350, 54))
	_add("Arcade", "Arcade（未実装）", Callable(), false, 1, 2, Vector2(420, -120), Vector2(350, 54))
	_add("VS CPU", "VS CPU（未実装）", Callable(), false, 2, 2, Vector2(420, -200), Vector2(350, 54))

func _build_selection() -> void:
	var cpu := mode == Mode.CPU
	if cpu:
		_box(Vector2(-310, 180), Vector2(500, 220))
		_box(Vector2(310, 180), Vector2(500, 220))
		_label("Player: " + _name_of(player_id), 28, Vector2(-310, 50), Vector2(580, 50))
		_preview = _label("CPU: " + _name_of(selected_id), 28, Vector2(310, 50), Vector2(580, 50))
	else:
		_box(Vector2(0, 180), Vector2(600, 220))
		_preview = _label(_name_of(selected_id), 30, Vector2(0, 50), Vector2(1000, 50))
	_box(Vector2(0, -150), Vector2(1220, 300))
	_label("Dummy（CPU）" if cpu else "Player", 24, Vector2(-440, -28), Vector2(300, 42))
	var implemented: Array = GameData.fighter_ids().filter(func(id): return _available(id))
	# Awari occupies the third top-left cell, as in the supplied wireframe.
	var order := [2, 0, 1, 6, 7, 8, 3, 4, 5, 9, 10, 11]
	var ids := {}
	for i in mini(12, implemented.size()):
		ids[order[i]] = implemented[i]
	for i in 12:
		var row := i / 6
		var col := i % 6
		var x := -530 + col * 156 if col < 3 else 218 + (col - 3) * 156
		var id: String = ids.get(i, "")
		_add("FighterSlot%d" % i, "（未実装）" if id.is_empty() else _name_of(id), func(): _confirm_fighter(id), not id.is_empty(), row, col if col < 3 else col + 1, Vector2(x, -101 - row * 108), Vector2(142, 94), id)
	_add("Random", "Random", _confirm_random, implemented.size() > 0, 0, 3, Vector2(0, -155), Vector2(136, 104), "", true)
	_add("Back", "戻る", _back, true, 2, 0, Vector2(-510, -278), Vector2(170, 34))

func _set_focus(index: int) -> void:
	if index < 0 or index >= _cells.size() or _cells[index].button.disabled:
		return
	_focus = index
	var selected: Dictionary = _cells[index]
	if selection_visible:
		if not String(selected.fighter_id).is_empty():
			selected_id = selected.fighter_id
		if _preview != null:
			_preview.text = ("CPU: " if mode == Mode.CPU else "") + ("Random" if selected.random else _name_of(selected_id))
	for c in _cells:
		_style(c.button, c == selected)

func _navigate(direction: Vector2) -> void:
	if _cells.is_empty():
		return
	var current: Dictionary = _cells[_focus]
	var candidates: Array = _cells.filter(func(c): return not c.button.disabled and c != current)
	var next = null
	if direction.y != 0:
		var sign_ := -1 if direction.y > 0 else 1
		var best := []
		for c in candidates:
			if (c.row - current.row) * sign_ > 0:
				best.append(c)
		best.sort_custom(func(a, b):
			var ra: int = absi(a.row - current.row)
			var rb: int = absi(b.row - current.row)
			if ra != rb:
				return ra < rb
			return absi(a.column - current.column) < absi(b.column - current.column))
		if not best.is_empty():
			next = best[0]
	elif direction.x != 0:
		var sign2 := 1 if direction.x > 0 else -1
		var best2 := []
		for c in candidates:
			if c.row == current.row and (c.column - current.column) * sign2 > 0:
				best2.append(c)
		best2.sort_custom(func(a, b): return absi(a.column - current.column) < absi(b.column - current.column))
		if not best2.is_empty():
			next = best2[0]
	if next != null:
		_set_focus(_cells.find(next))

func _process(_delta: float) -> void:
	var f := menu.read()
	if starting:
		return
	if f.direction != Vector2.ZERO:
		_navigate(f.direction)
	if f.confirm and _focus < _cells.size() and not _cells[_focus].button.disabled:
		_cells[_focus].button.pressed.emit()
	elif f.cancel and selection_visible:
		_back()
	elif f.option:
		if selection_visible:
			_back()
		else:
			_open_options()

func _open_select(m: int) -> void:
	mode = m
	selection_visible = true
	_load()
	_build()

func _back() -> void:
	selection_visible = false
	_load()
	_build()

func _confirm_fighter(id: String) -> void:
	if id.is_empty() or not _available(id):
		return
	var player := id if mode == Mode.PLAYER else player_id
	var cpu := id if mode == Mode.CPU else cpu_id
	var error: String = Flow.save_preferences(player, cpu)
	if not error.is_empty():
		status = error
		_build()
		return
	if mode == Mode.CPU:
		starting = true
		Flow.start_training(player, cpu)
	else:
		_back()

func _confirm_random() -> void:
	var list: Array = GameData.fighter_ids().filter(func(id): return _available(id))
	if not list.is_empty():
		_confirm_fighter(list[randi() % list.size()])

func _open_options() -> void:
	starting = true
	Flow.open_options()
