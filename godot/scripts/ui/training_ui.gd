extends Control
## TrainingOverlay HUD + Option screens (Training settings / ダミー設定 / KeyConfig) and MatchHud.
## Layout uses the same 1600 x 900 logical coordinates as Unity's 16:9 viewport.

const PANEL := Color(.025, .045, .065)
const BTN := Color(.14, .22, .20)
const BTN_ON := Color(.25, .48, .36)
const TEXT := Color(.9, .94, .92)
const OPTION_W := 1080.0
const OPTION_H := 700.0

var scene
var m: Match
var menu: MenuInput
var return_to_title_on_close := false
var frames_visible := true
var damage_visible := true
var commands_visible := true
var _font: Font
var _ui_block_frames := 0

# HUD
var _hud := {}
var _frame_strip: Control
# Option
var _option_root: ColorRect
var _panel: Control
var _pages := {}
var _page := "training"
var _nav := {}
var _values: Array = []        # value editors {button, label, min, max, get, set}
var _editing = null
var _edit_draft := 0
var _posture_buttons: Array = []
var _guard_buttons: Array = []
var _jg_buttons: Array = []
var _position_buttons: Array = []
var _toggles := {}
var _counter_field: LineEdit
var _counter_status: Label
# KeyConfig
var _key_draft: Dictionary = {}
var _key_capture: KeyCapture = null
var _capture_id := ""
var _key_rows := {}            # id -> {button, name, keyboard, pad}
var _key_list: VBoxContainer
var _key_scroll: ScrollContainer
var _key_status: Label
var _key_dialog: Control
var _key_dialog_text: Label
var _key_character_mode := false
var _pending := {}             # {id, pad, value} or {operation, slot}
var _pending_operation := ""
var _operation_rows: Array = []
var _slot_choices: Array = []
var _basic_tab: Button
var _char_tab: Button
var _key_note: Label

func setup(training_scene) -> void:
	scene = training_scene
	m = training_scene.match_
	size = Vector2(1600, 900)
	mouse_filter = Control.MOUSE_FILTER_IGNORE
	_font = SystemFont.new()
	_font.font_names = PackedStringArray(["Yu Gothic UI", "Yu Gothic", "Meiryo", "Noto Sans CJK JP", "Arial"])
	theme = Theme.new()
	theme.default_font = _font
	theme.default_font_size = 15
	_disable_builtin_navigation()
	menu = MenuInput.new(InputHub)
	_build_hud()
	_build_options()

func _exit_tree() -> void:
	if menu != null:
		menu.dispose()
	_cancel_capture()

static func _disable_builtin_navigation() -> void:
	# Menus use KeyConfig (confirm J / ○, cancel K / ×, WASD / Dpad) like Unity; drop Godot's
	# built-in ui_* bindings so the pad's × is not "accept".
	for action in ["ui_up", "ui_down", "ui_left", "ui_right", "ui_accept", "ui_select", "ui_cancel", "ui_focus_next", "ui_focus_prev"]:
		if InputMap.has_action(action):
			InputMap.action_erase_events(action)

# ---------------------------------------------------------------- helpers
func _rect(parent: Control, r: Rect2, color: Color) -> ColorRect:
	var c := ColorRect.new()
	c.position = r.position
	c.size = r.size
	c.color = color
	c.mouse_filter = Control.MOUSE_FILTER_IGNORE
	parent.add_child(c)
	return c

func _label(parent: Control, r: Rect2, text: String, size_: int = 15, align := HORIZONTAL_ALIGNMENT_LEFT) -> Label:
	var l := Label.new()
	l.position = r.position
	l.size = r.size
	l.text = text
	l.add_theme_font_size_override("font_size", size_)
	l.add_theme_color_override("font_color", TEXT)
	l.horizontal_alignment = align
	l.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	l.clip_text = true
	l.autowrap_mode = TextServer.AUTOWRAP_OFF
	l.mouse_filter = Control.MOUSE_FILTER_IGNORE
	parent.add_child(l)
	return l

func _button(parent: Control, r: Rect2, text: String, action: Callable, enabled := true) -> Button:
	var b := Button.new()
	b.position = r.position
	b.size = r.size
	b.text = text
	b.disabled = not enabled
	b.focus_mode = Control.FOCUS_ALL
	b.add_theme_font_size_override("font_size", 13)
	_style_button(b, false)
	if action.is_valid():
		b.pressed.connect(action)
	parent.add_child(b)
	return b

func _style_button(b: Button, on: bool) -> void:
	for state in ["normal", "hover", "pressed", "focus", "disabled"]:
		var s := StyleBoxFlat.new()
		s.bg_color = BTN_ON if on else BTN
		if state == "hover":
			s.bg_color = s.bg_color.lightened(0.25)
		if state == "disabled":
			s.bg_color = Color(.2, .22, .22)
		if state == "focus":
			s.draw_center = false
			s.border_color = Color(.65, .95, .85)
			s.set_border_width_all(2)
		b.add_theme_stylebox_override(state, s)
	b.add_theme_color_override("font_color", TEXT)
	b.add_theme_color_override("font_disabled_color", Color(.55, .58, .6))

# ---------------------------------------------------------------- HUD
func _build_hud() -> void:
	var hud := Control.new()
	hud.size = Vector2(1600, 900)
	hud.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(hud)
	for i in 2:
		var n := str(i + 1)
		var one := i == 0
		var x := 26.0 if one else 924.0
		_hud["hp" + n] = _rect(hud, Rect2(x, 35, 650, 24), Color(.02, .035, .05, .85))
		_hud["hpfill" + n] = _rect(hud, Rect2(x, 35, 650, 24), Color(.25, .86, .58) if one else Color(.27, .68, .98))
		_hud["name" + n] = _label(hud, Rect2(x + 6, 9, 638, 24), "", 18, HORIZONTAL_ALIGNMENT_LEFT if one else HORIZONTAL_ALIGNMENT_RIGHT)
		var gx := 130.0 if one else 924.0
		_hud["tg" + n] = _rect(hud, Rect2(gx, 850, 546, 26), Color(.02, .035, .05, .85))
		_hud["tgfill" + n] = _rect(hud, Rect2(gx, 850, 546, 26), Color.WHITE)
		_hud["numberbg" + n] = _rect(hud, Rect2(26 if one else 1476, 842, 98, 42), Color(.02, .035, .05, .85))
		_hud["number" + n] = _label(hud, Rect2(26 if one else 1476, 842, 98, 42), "", 18, HORIZONTAL_ALIGNMENT_CENTER)
		_hud["band" + n] = _label(hud, Rect2(gx + 6, 877, 534, 18), "", 14, HORIZONTAL_ALIGNMENT_LEFT if one else HORIZONTAL_ALIGNMENT_RIGHT)
		_hud["combo" + n] = _label(hud, Rect2(38 if one else 1262, 350, 300, 60), "", 34, HORIZONTAL_ALIGNMENT_LEFT if one else HORIZONTAL_ALIGNMENT_RIGHT)
	var opacity := float(m.display.panelOpacity)
	_hud.status = _rect(hud, Rect2(480, 104, 640, 182), Color(PANEL, opacity))
	_hud.status1 = _label(hud, Rect2(492, 108, 296, 174), "", 15)
	_hud.status2 = _label(hud, Rect2(812, 108, 296, 174), "", 15)
	for k in ["status1", "status2"]:
		_hud[k].vertical_alignment = VERTICAL_ALIGNMENT_TOP
		_hud[k].clip_text = false
	_hud.recordbg = _rect(hud, Rect2(680, 4, 240, 54), Color(PANEL, .7))
	_hud.record = _label(hud, Rect2(680, 4, 240, 54), "", 14, HORIZONTAL_ALIGNMENT_CENTER)
	for i in 2:
		var cx := 14.0 if i == 0 else 1448.0
		_hud["commandsbg%d" % i] = _rect(hud, Rect2(cx, 116, 138, 616), Color(PANEL, .42))
		var l := _label(hud, Rect2(cx + 4, 118, 130, 612), "", 15)
		l.vertical_alignment = VERTICAL_ALIGNMENT_TOP
		l.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
		_hud["commands%d" % i] = l
	_hud.frame_title = _label(hud, Rect2(8, 744, 1584, 32), "1P top / 2P bottom | latest 60F (120F retained) | start yellow, active red, end orange, guard cyan, hurt purple", 11)
	_hud.frame_bg = _rect(hud, Rect2(4, 780, 1592, 36), Color(0, 0, 0, .4))
	_frame_strip = Control.new()
	_frame_strip.position = Vector2(4, 780)
	_frame_strip.size = Vector2(1592, 36)
	_frame_strip.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_frame_strip.draw.connect(_draw_frames)
	hud.add_child(_frame_strip)
	_hud.frame_info = _label(hud, Rect2(8, 818, 1584, 28), "", 11)
	_hud.option_button = _button(hud, Rect2(1600 - 14 - 92, 14, 92, 28), "Option", func(): scene.set_option_visible(true))
	_hud.option_button.focus_mode = Control.FOCUS_NONE

func _draw_frames() -> void:
	var span := 60
	var cell := _frame_strip.size.x / span
	var row_h := _frame_strip.size.y / 2.0
	for row in 2:
		var history: FrameBar.History = m.frame_bar.first_history if row == 0 else m.frame_bar.second_history
		for x in span:
			var age := span - 1 - x
			var c := Color(.06, .075, .1) if age >= history.count else FrameBar.color_for(history.newest(age))
			_frame_strip.draw_rect(Rect2(x * cell, row * row_h, cell, row_h), c)

static func tg_color(display: Dictionary, v: float) -> Color:
	var key := "negativeHigh" if v < -100 else ("negativeLow" if v < 0 else ("zero" if v == 0 else ("positiveLow" if v <= 100 else "positiveHigh")))
	var c: Dictionary = display[key]
	return Color(float(c.r), float(c.g), float(c.b), float(c.a))

static func tg_band(v: float) -> String:
	return "-200..-101" if v < -100 else ("-100..-1" if v < 0 else ("0" if v == 0 else ("1..100" if v <= 100 else "101..200")))

## Unity "0.##" / "0.#" formatting.
static func num(v: float, digits: int = 2) -> String:
	var s: String = ("%." + str(digits) + "f") % v
	if s.contains("."):
		s = s.rstrip("0").rstrip(".")
	return "0" if s == "-0" else s

func _refresh_hud() -> void:
	for i in 2:
		var n := str(i + 1)
		var one := i == 0
		var f: Fighter = m.player if one else m.dummy_fighter
		var x := 26.0 if one else 924.0
		var hp := Sk.clamp01(f.health / f.def.maxHealth) if f.def.maxHealth > 0 else 0.0
		var fill: ColorRect = _hud["hpfill" + n]
		fill.position = Vector2(x if one else x + 650 * (1 - hp), 35)
		fill.size = Vector2(650 * hp, 24)
		_hud["name" + n].text = "%sP  %s   HP %s / %s" % [n, f.data.name, num(f.health), num(f.def.maxHealth)]
		var v := f.tension.gauge
		var tgf := Sk.clamp01(absf(v) / 200.0)
		var gx := 130.0 if one else 924.0
		var tgfill: ColorRect = _hud["tgfill" + n]
		tgfill.position = Vector2(gx if one else gx + 546 * (1 - tgf), 850)
		tgfill.size = Vector2(546 * tgf, 26)
		tgfill.color = tg_color(m.display, v)
		_hud["number" + n].text = "TG " + num(v)
		var cue := Sk.clamp01((1.0 - f.trc_recovery_cost_scale()) / 0.5) if f.hitstop <= 0 else 0.0
		_hud["number" + n].add_theme_color_override("font_color", Color.WHITE.lerp(Color(.65, .95, .85), cue))
		_hud["band" + n].text = tg_band(v) + ("  | outside display scale" if absf(v) > 200 else "")
		_hud["band" + n].add_theme_color_override("font_color", tg_color(m.display, v))
		var combo: ContactState = m.first_attack if one else m.second_attack
		_hud["combo" + n].text = ("%d HIT" % combo.count) if combo.count > 0 and (combo.active or combo.display_left > 0) else ""
		var target: Fighter = m.dummy_fighter if one else m.player
		var head := "%sP → %dP  LAST CONTACT\n" % [n, 2 if one else 1]
		if combo.has_contact:
			var c := combo.last
			_hud["status" + n].text = head + "A %s  D %s\nH %s  %s\nDamage %s\nCombo %d / total %s\nScaling: 未実装\nStop %sF / stun %sF" % [
				num(c.raw_attack), num(c.defense), num(c.final), "HIT" if c.hit else target.last_result, num(combo.last_damage),
				combo.count, num(combo.total_damage), num(float(c.hitstop_frames), 1), num(float(c.stun_frames), 1)]
		else:
			_hud["status" + n].text = head + "A —  D —\nDamage —\nCombo 0 / total 0\nScaling: 未実装"
	for k in ["status", "status1", "status2"]:
		_hud[k].visible = damage_visible
	for i in 2:
		_hud["commandsbg%d" % i].visible = commands_visible
		_hud["commands%d" % i].visible = commands_visible
		_hud["commands%d" % i].text = ("1P" if i == 0 else "2P") + " INPUT / newest ↑\n" + (scene.commands_first if i == 0 else scene.commands_second).text(24)
	for k in ["frame_title", "frame_bg", "frame_info"]:
		_hud[k].visible = frames_visible
	_frame_strip.visible = frames_visible
	if frames_visible:
		var a := FrameBar.snapshot(m.player)
		var b := FrameBar.snapshot(m.dummy_fighter)
		_hud.frame_info.text = "F %d/%d  Lock %s/%sF  Stop %s/%sF  Est adv %sF  Atk %s/%s  Def %s/%s" % [a.frame, b.frame,
			num(a.remaining, 1), num(b.remaining, 1), num(a.stop, 1), num(b.stop, 1), num(m.frame_bar.estimated_advantage(), 1),
			num(a.attack, 1), num(b.attack, 1), num(a.defense, 1), num(b.defense, 1)]
		_frame_strip.queue_redraw()
	var r := scene.session.recorder as TrainingRecorder
	match r.state:
		TrainingRecorder.Mode.COUNTDOWN:
			_hud.record.text = "REC IN %d" % r.countdown_number()
			_hud.record.add_theme_font_size_override("font_size", 28)
		TrainingRecorder.Mode.RECORDING:
			_hud.record.text = "REC 2P %.1f / 10s | R stop" % (r.draft_count() / 60.0)
			_hud.record.add_theme_font_size_override("font_size", 14)
		TrainingRecorder.Mode.PLAYBACK:
			_hud.record.text = "PLAY 2P %d / %dF" % [r.position, r.saved_count()]
			_hud.record.add_theme_font_size_override("font_size", 14)
		_:
			_hud.record.text = "R Record | P Play\nCtrl Reset | saved %dF" % r.saved_count()
			_hud.record.add_theme_font_size_override("font_size", 14)

# ---------------------------------------------------------------- Option screens
func _build_options() -> void:
	_option_root = ColorRect.new()
	_option_root.color = Color(0, 0, 0, .78)
	_option_root.size = Vector2(1600, 900)
	_option_root.mouse_filter = Control.MOUSE_FILTER_STOP
	add_child(_option_root)
	_panel = ColorRect.new()
	_panel.color = Color(.045, .065, .06, 1)
	_panel.position = Vector2((1600 - OPTION_W) / 2, (900 - OPTION_H) / 2)
	_panel.size = Vector2(OPTION_W, OPTION_H)
	_option_root.add_child(_panel)
	var left := clampf(OPTION_W * .25, 130, 190)
	var x := left + 20
	var right := OPTION_W - x - 18
	_label(_panel, Rect2(18, 12, 200, 30), "Option", 21)
	_button(_panel, Rect2(OPTION_W - 145, 12, 126, 30), "Esc / 閉じる", func(): _close_option())
	var navs := [["training", "Training settings"], ["dummy", "ダミー設定"], ["keys", "KeyConfig"]]
	for i in navs.size():
		var id: String = navs[i][0]
		_nav[id] = _button(_panel, Rect2(18, 62 + i * 39, left - 24, 32), navs[i][1], func(): show_page(id))
	_nav.title = _button(_panel, Rect2(18, OPTION_H - 82, left - 24, 32), "Titleへ戻る", func(): _back_to_title())
	_label(_panel, Rect2(18, OPTION_H - 30, OPTION_W - 36, 25), "WASD: select | J: confirm / edit | K: back | Enter: finish text | Esc: close | Mouse: click", 12)
	_build_training_page(x, right)
	_build_dummy_page(x, right)
	_build_keys_page(x, right)
	_option_root.visible = false
	show_page("training")

func _page_root(id: String, x: float, right: float) -> Control:
	var c := Control.new()
	c.position = Vector2(x, 52)
	c.size = Vector2(right, OPTION_H - 94)
	_panel.add_child(c)
	_pages[id] = c
	return c

func _value(parent: Control, r: Rect2, label: String, lo: int, hi: int, getter: Callable, setter: Callable) -> void:
	var entry := {"label": label, "min": lo, "max": hi, "get": getter, "set": setter}
	var b := _button(parent, r, "", func(): _toggle_value_edit(entry))
	b.alignment = HORIZONTAL_ALIGNMENT_LEFT
	entry.button = b
	_values.append(entry)

func _build_training_page(x: float, right: float) -> void:
	var p := _page_root("training", x, right)
	var s: TrainingSession = scene.session
	_label(p, Rect2(0, 10, right, 28), "TRAINING | settings apply to this Play session", 16)
	_label(p, Rect2(0, 46, 120, 30), "Reset position", 15)
	for i in 3:
		var n := i
		_position_buttons.append(_button(p, Rect2(124 + i * (right - 132) / 3, 46, (right - 140) / 3, 30), ["Left", "Center", "Right"][i], func(): s.start_position = n))
	_toggles.frames = _button(p, Rect2(0, 86, (right - 16) / 3, 32), "", func(): frames_visible = not frames_visible)
	_toggles.damage = _button(p, Rect2((right - 8) / 3, 86, (right - 16) / 3, 32), "", func(): damage_visible = not damage_visible)
	_toggles.commands = _button(p, Rect2(2 * (right - 8) / 3, 86, (right - 16) / 3, 32), "", func(): commands_visible = not commands_visible)
	for i in 2:
		var n := i
		var y := 132 + i * 118
		_label(p, Rect2(0, y, right, 24), "%dP" % (i + 1), 16)
		_value(p, Rect2(0, y + 26, right * .62 - 8, 34), "%dP HP" % (i + 1), 1, 100, func(): return s.settings(n).hp_percent, func(v): s.set_hp(n, v))
		_toggles["recover%d" % i] = _button(p, Rect2(right * .62, y + 26, right * .38 - 10, 34), "", func(): s.settings(n).recover = not s.settings(n).recover)
		_value(p, Rect2(0, y + 68, right * .62 - 8, 34), "%dP TG" % (i + 1), -200, 200, func(): return s.settings(n).tg, func(v): s.set_tg(n, v))
		_toggles["fixed%d" % i] = _button(p, Rect2(right * .62, y + 68, right * .38 - 10, 34), "", func(): s.set_fixed(n, not s.settings(n).fixed_tg))
	_toggles.second = _button(p, Rect2(0, 372, right * .5, 30), "", func(): scene.set_second_player_control(not scene.second_player_controlled))
	var info := _label(p, Rect2(0, 410, right - 12, 190), "R: 3-second countdown → control dummy; R again stops. Max 10s.\nP: one-shot playback. Ctrl: reset configured HP/TG/positions.\nOpening menu stops playback; saves a nonempty partial recording.\nRelease held controls at routing boundaries before operating the new actor.\nHP: J edit, W/S ±10, A/D ±1, J commit, K discard.\nHP recovery waits 3 simulation seconds after last HP damage and all stun/stop ends.\nTG fixed blocks every gameplay gain/spend; Ctrl applies the configured value.\nExisting v0.1 TG earning remains deferred even with fixed OFF.\n2P keyboard (Godot): numpad 8/5/4/6 move, 7 9 - kicks, 1 2 3 punches, / Hold, Enter TRC, 0 Dash.", 13)
	info.vertical_alignment = VERTICAL_ALIGNMENT_TOP
	info.autowrap_mode = TextServer.AUTOWRAP_WORD

func _build_dummy_page(x: float, right: float) -> void:
	var p := _page_root("dummy", x, right)
	var s: TrainingSession = scene.session
	_label(p, Rect2(0, 13, right, 25), "姿勢", 15)
	for i in 3:
		var n := i
		_posture_buttons.append(_button(p, Rect2(i * (right / 3), 44, right / 3 - 5, 32), ["立ち", "しゃがみ", "ジャンプ"][i], func(): scene.dummy.set_posture(n)))
	_label(p, Rect2(0, 88, right, 25), "ガード", 15)
	for i in 4:
		var n := i
		_guard_buttons.append(_button(p, Rect2((i % 2) * (right / 2), 122 + (i / 2) * 35, right / 2 - 5, 30), ["None", "Guard", "FD", "Always FD"][i], func(): s.guard_mode = n))
	_label(p, Rect2(0, 194, right, 20), "JG reduction (not input age)", 13)
	for i in 11:
		var n := i
		_jg_buttons.append(_button(p, Rect2(i * right / 11, 218, right / 11 - 3, 29), "-%dF" % i, func(): s.dummy_jg = n))
	_label(p, Rect2(0, 258, right, 25), "被弾後の反撃コマンド", 15)
	_counter_field = LineEdit.new()
	_counter_field.position = Vector2(0, 287)
	_counter_field.size = Vector2(right, 34)
	_counter_field.max_length = 40
	_counter_field.placeholder_text = "空欄: 反撃なし / 例: 3強"
	_counter_field.text_changed.connect(func(t): scene.dummy.set_counter_command(t))
	_counter_field.text_submitted.connect(func(_t): _counter_field.release_focus(); _jg_buttons[0].grab_focus())
	p.add_child(_counter_field)
	_counter_status = _label(p, Rect2(0, 327, right, 34), "", 12)
	var note := _label(p, Rect2(0, 367, right, 54), "FD: during opponent attack. Always FD: maintained while idle too. JG buttons set guard-stun reduction directly (0..10F), only for the automated dummy. Player JG is unchanged.", 12)
	note.autowrap_mode = TextServer.AUTOWRAP_WORD

func _refresh_options() -> void:
	var s: TrainingSession = scene.session
	for i in 3:
		_style_button(_position_buttons[i], int(s.start_position) == i)
		_style_button(_posture_buttons[i], int(scene.dummy.posture) == i)
	for i in 4:
		_style_button(_guard_buttons[i], int(s.guard_mode) == i)
	for i in 11:
		_style_button(_jg_buttons[i], s.dummy_jg == i)
		_jg_buttons[i].disabled = i > int(m.sys.guard.maxReduction)
	_toggles.frames.text = "Frames: " + ("ON" if frames_visible else "OFF")
	_toggles.damage.text = "Damage: " + ("ON" if damage_visible else "OFF")
	_toggles.commands.text = "Commands: " + ("ON" if commands_visible else "OFF")
	_toggles.second.text = "2P: " + ("keyboard (numpad)" if scene.second_player_controlled else "dummy")
	for i in 2:
		_toggles["recover%d" % i].text = "Recovery: " + ("ON" if s.settings(i).recover else "OFF")
		_toggles["fixed%d" % i].text = "TG fixed: " + ("ON" if s.settings(i).fixed_tg else "OFF")
	for v in _values:
		if v == _editing:
			v.button.text = "%s: %d  (W/S ±10, A/D ±1, J 決定, K 取消)" % [v.label, _edit_draft]
		else:
			v.button.text = "%s: %d" % [v.label, v.get.call()]
	var d: TrainingDummy = scene.dummy
	if d.counter_error.is_empty():
		_counter_status.text = "反撃なし" if d.counter_command.strip_edges().is_empty() else "設定済み: " + d.counter_command
		_counter_status.add_theme_color_override("font_color", Color(.6, .8, .7))
	else:
		_counter_status.text = d.counter_error
		_counter_status.add_theme_color_override("font_color", Color(1, .55, .4))
	for id in ["training", "dummy", "keys"]:
		_style_button(_nav[id], _page == id)

func show_page(id: String) -> void:
	if _page == "keys" and id != "keys":
		_cancel_capture()
	if id == "keys" and _page != "keys":
		_open_key_draft()
	_page = id
	for k in _pages:
		_pages[k].visible = k == id
	_editing = null

func on_option_visible(visible: bool) -> void:
	_option_root.visible = visible
	_hud.option_button.visible = not visible
	if visible:
		_counter_field.text = scene.dummy.counter_command
		_ui_block_frames = 2
		menu.clear()
		_nav[_page].grab_focus()
	else:
		if _page == "keys":
			_open_key_draft()
		_cancel_capture()
		_editing = null
		get_viewport().gui_release_focus()

func _close_option() -> void:
	var to_title := return_to_title_on_close
	scene.set_option_visible(false)
	if to_title:
		Flow.go_title()

func _back_to_title() -> void:
	scene.set_option_visible(false)
	Flow.go_title()

func _toggle_value_edit(entry: Dictionary) -> void:
	if _editing == entry:
		entry.set.call(_edit_draft)
		_editing = null
	else:
		_editing = entry
		_edit_draft = int(entry.get.call())

# ---------------------------------------------------------------- KeyConfig
func _build_keys_page(x: float, right: float) -> void:
	var p := _page_root("keys", x, right)
	var view_h := OPTION_H - 94 - 90
	_key_scroll = ScrollContainer.new()
	_key_scroll.position = Vector2(0, 0)
	_key_scroll.size = Vector2(right, view_h)
	_key_scroll.horizontal_scroll_mode = ScrollContainer.SCROLL_MODE_DISABLED
	var bg := StyleBoxFlat.new()
	bg.bg_color = Color(.05, .09, .12, .9)
	_key_scroll.add_theme_stylebox_override("panel", bg)
	p.add_child(_key_scroll)
	_key_list = VBoxContainer.new()
	_key_list.custom_minimum_size = Vector2(right - 16, 0)
	_key_list.add_theme_constant_override("separation", 4)
	_key_scroll.add_child(_key_list)
	var cell := (right - 12) / 5
	var names := [["保存して反映", _save_keys], ["取消 / 再読込", func(): _open_key_draft()], ["既定値", _default_keys], ["戻る", _keys_back], ["入力捕捉を取消", func(): _cancel_capture(); _key_status.text = "入力登録を取り消しました"]]
	for i in names.size():
		_button(p, Rect2(i * cell, view_h + 6, cell - 4, 30), names[i][0], names[i][1])
	_key_status = _label(p, Rect2(3, view_h + 40, right - 6, 42), "", 12)
	_key_status.autowrap_mode = TextServer.AUTOWRAP_WORD
	_key_dialog = ColorRect.new()
	(_key_dialog as ColorRect).color = Color(.06, .1, .14, .98)
	_key_dialog.position = Vector2(10, maxf(40, view_h / 2 - 70))
	_key_dialog.size = Vector2(right - 20, 140)
	p.add_child(_key_dialog)
	_key_dialog_text = _label(_key_dialog, Rect2(8, 8, right - 36, 54), "", 14)
	_button(_key_dialog, Rect2(8, 76, (right - 48) / 2, 40), "はい", _confirm_swap)
	_button(_key_dialog, Rect2(24 + (right - 48) / 2, 76, (right - 48) / 2, 40), "いいえ / 取消", _cancel_swap)
	_key_dialog.visible = false

func _key_header(text: String) -> void:
	var l := Label.new()
	l.text = text
	l.add_theme_font_size_override("font_size", 14)
	l.custom_minimum_size = Vector2(0, 22)
	_key_list.add_child(l)

func _key_row_button(text_cells: Array, action: Callable) -> Button:
	var b := Button.new()
	b.custom_minimum_size = Vector2(0, 28)
	b.focus_mode = Control.FOCUS_ALL
	_style_button(b, false)
	b.pressed.connect(action)
	var w := _key_list.custom_minimum_size.x
	var widths := [w * .36, w * .32, w * .32]
	var xs := [8.0, w * .36, w * .68]
	for i in text_cells.size():
		var l := _label(b, Rect2(xs[i], 0, widths[i] - 8, 28), text_cells[i], 12)
		l.name = "c%d" % i
	_key_list.add_child(b)
	return b

static func _op_name(name_: String) -> String:
	match name_:
		"LP": return "弱P"
		"MP": return "中P"
		"HP": return "強P"
		"LK": return "弱K"
		"MK": return "中K"
		"HK": return "強K"
	return name_

func _row_name(id: String) -> String:
	if id.begins_with("battle.") and Sk.SLOT_NAMES.has(id.substr(7)):
		var slot := Sk.SLOT_NAMES.find(id.substr(7))
		var c := m.player.data
		for op in c.raw.attack_slots:
			if KeyConfig.slot_for(_key_draft, c, op) == slot:
				return Sk.SLOT_NAMES[slot] + "〔" + _op_name(op.display_name) + "〕"
		return Sk.SLOT_NAMES[slot]
	return id.substr(id.find(".") + 1)

func _rebuild_key_list() -> void:
	for child in _key_list.get_children():
		_key_list.remove_child(child)
		child.queue_free()
	_key_rows.clear()
	_operation_rows.clear()
	_slot_choices.clear()
	for group in ["Trigger", "Menu", "Training"]:
		_key_header(group + " / 共通")
		for r in _key_draft.bindings:
			if r.context == group or (group == "Menu" and r.context == "Global"):
				_add_key_row(r)
	_key_header("Battle")
	var tabs := HBoxContainer.new()
	_basic_tab = Button.new()
	_basic_tab.text = "基本キー設定"
	_char_tab = Button.new()
	_char_tab.text = m.player.data.name
	for t in [_basic_tab, _char_tab]:
		t.custom_minimum_size = Vector2(_key_list.custom_minimum_size.x / 2 - 4, 30)
		t.focus_mode = Control.FOCUS_ALL
		tabs.add_child(t)
	_style_button(_basic_tab, not _key_character_mode)
	_style_button(_char_tab, _key_character_mode)
	_basic_tab.pressed.connect(func(): _set_key_tab(false))
	_char_tab.pressed.connect(func(): _set_key_tab(true))
	_key_list.add_child(tabs)
	_key_note = Label.new()
	_key_note.add_theme_font_size_override("font_size", 12)
	_key_note.text = "キャラ操作のslotだけを入れ替えます。物理bindingは参照のみです。" if _key_character_mode else "基本bindingは全キャラ共通です。行を選択しKeyboard / Padを入力。"
	_key_list.add_child(_key_note)
	if not _key_character_mode:
		var header := _key_row_button(["項目", "Keyboard", "Pad"], func(): pass)
		header.disabled = true
		header.focus_mode = Control.FOCUS_NONE
		for r in _key_draft.bindings:
			if r.context == "Battle":
				_add_key_row(r)
	else:
		var c := m.player.data
		for i in c.raw.attack_slots.size():
			var op: Dictionary = c.raw.attack_slots[i]
			var slot := KeyConfig.slot_for(_key_draft, c, op)
			var binding := KeyConfig.row(_key_draft, KeyConfig.slot_id(slot))
			var index: int = i
			var b := _key_row_button(["%s → %s" % [_op_name(op.display_name), Sk.SLOT_NAMES[slot]], KeyConfig.binding_text(binding.keyboard), KeyConfig.binding_text(binding.pad)], func(): _select_operation(index))
			_operation_rows.append(b)
		if not _pending_operation.is_empty():
			var row := HBoxContainer.new()
			for slot in Sk.SLOT_DISPLAY_ORDER:
				var sl: int = slot
				var b2 := Button.new()
				b2.text = Sk.SLOT_NAMES[slot]
				b2.custom_minimum_size = Vector2(_key_list.custom_minimum_size.x / 9 - 4, 28)
				b2.focus_mode = Control.FOCUS_ALL
				_style_button(b2, false)
				b2.pressed.connect(func(): _choose_slot(sl))
				row.add_child(b2)
				_slot_choices.append(b2)
			_key_list.add_child(row)

func _add_key_row(r: Dictionary) -> void:
	var id: String = r.id
	var b := _key_row_button([_row_name(id), KeyConfig.binding_text(r.keyboard), KeyConfig.binding_text(r.pad)], func(): _start_capture(id))
	_key_rows[id] = b

func _open_key_draft() -> void:
	_cancel_capture()
	_key_draft = KeyConfig.copy(KeyConfig.ensure())
	_pending_operation = ""
	_pending = {}
	if _key_dialog != null:
		_key_dialog.visible = false
	if _key_status != null:
		_key_status.text = KeyConfig.status + " / 保存するまでゲームは変わりません"
	_rebuild_key_list()
	_flush_menu()

func _set_key_tab(character: bool) -> void:
	_cancel_capture()
	_key_character_mode = character
	_pending_operation = ""
	_rebuild_key_list()
	(_char_tab if character else _basic_tab).grab_focus()

func _start_capture(id: String) -> void:
	_cancel_capture()
	_capture_id = id
	_key_capture = KeyCapture.new(InputHub, _key_draft, KeyConfig.row(_key_draft, id).context == "Trigger")
	_key_status.text = _row_name(id) + ": ボタンを離してから入力。Escも登録可能です"
	_flush_menu()

func _cancel_capture() -> void:
	if _key_capture != null:
		_key_capture.dispose()
		_key_capture = null
		_flush_menu()

func _propose_binding(id: String, pad: bool, value: String) -> void:
	var r := KeyConfig.try_binding(_key_draft, id, pad, value, false)
	if r.ok:
		_key_status.text = "draftを変更しました（未保存）"
		_rebuild_key_list()
		return
	if not String(r.conflict).is_empty() and String(r.error).is_empty():
		_pending = {"id": id, "pad": pad, "value": value}
		_pending_operation = ""
		_show_swap(_row_name(r.conflict) + " と入れ替えますか？")
	else:
		_key_status.text = r.error

func _show_swap(text: String) -> void:
	_key_dialog.visible = true
	_key_dialog_text.text = text
	_key_dialog.get_child(2).grab_focus()
	_flush_menu()

func _confirm_swap() -> void:
	if not _pending_operation.is_empty() and _pending.has("slot"):
		KeyConfig.swap_operation(_key_draft, m.player.data, _pending_operation, _pending.slot)
		_key_status.text = "キャラ配置draftを入れ替えました"
	elif _pending.has("id"):
		var r := KeyConfig.try_binding(_key_draft, _pending.id, _pending.pad, _pending.value, true)
		_key_status.text = "binding draftを交換しました" if r.ok else r.error
	_pending_operation = ""
	_pending = {}
	_key_dialog.visible = false
	_rebuild_key_list()
	_flush_menu()

func _cancel_swap() -> void:
	_pending_operation = ""
	_pending = {}
	_key_dialog.visible = false
	_rebuild_key_list()
	_flush_menu()

func _select_operation(index: int) -> void:
	var c := m.player.data
	_pending_operation = KeyConfig.op_id(c.raw.attack_slots[index])
	_key_status.text = _op_name(c.raw.attack_slots[index].display_name) + ": 移動先slotを選択してください"
	_rebuild_key_list()
	if not _slot_choices.is_empty():
		_slot_choices[0].grab_focus()

func _choose_slot(slot: int) -> void:
	if _pending_operation.is_empty():
		return
	var c := m.player.data
	var other := ""
	for op in c.raw.attack_slots:
		if KeyConfig.slot_for(_key_draft, c, op) == slot:
			other = _op_name(op.display_name)
	_pending = {"slot": slot}
	_show_swap(("このslot" if other.is_empty() else other) + " と配置を入れ替えますか？")

func _save_keys() -> void:
	_cancel_capture()
	var error := KeyConfig.save(_key_draft)
	if error.is_empty():
		_key_status.text = "保存して反映しました"
		scene.player_controls.reload_bindings()
		scene.second_controls.reload_bindings()
		menu.dispose()
		menu = MenuInput.new(InputHub)
		_flush_menu()
	else:
		_key_status.text = error

func _default_keys() -> void:
	_cancel_capture()
	_key_draft = KeyConfig.defaults()
	_key_status.text = "既定値のdraftです。保存するまで反映しません"
	_rebuild_key_list()

func _keys_back() -> void:
	if return_to_title_on_close:
		_close_option()
		return
	_open_key_draft()
	show_page("training")
	_nav.training.grab_focus()

func _flush_menu() -> void:
	if menu != null:
		menu.clear()
	_ui_block_frames = 2

# ---------------------------------------------------------------- per frame
func _process(_delta: float) -> void:
	_refresh_hud()
	if _ui_block_frames > 0:
		_ui_block_frames -= 1
	var frame := menu.read()
	if not scene.option_visible:
		if frame.option:
			scene.set_option_visible(true)
		return
	_refresh_options()
	if _key_capture != null:
		_key_capture.tick()
		_key_status.text = _key_capture.status
		if _key_capture.complete:
			var id := _capture_id
			var value := _key_capture.value
			var pad := _key_capture.pad
			_cancel_capture()
			_propose_binding(id, pad, value)
		return
	if _ui_block_frames > 0:
		return
	if _counter_field.has_focus():
		if frame.option:
			_close_option()
		return
	if frame.option:
		_close_option()
		return
	if _editing != null:
		var d: Vector2 = frame.direction
		if d.y != 0:
			_edit_draft = clampi(_edit_draft + int(d.y) * 10, _editing.min, _editing.max)
		elif d.x != 0:
			_edit_draft = clampi(_edit_draft + int(d.x), _editing.min, _editing.max)
		if frame.confirm:
			_toggle_value_edit(_editing)
		elif frame.cancel:
			_editing = null
		return
	if frame.direction != Vector2.ZERO:
		_navigate(frame.direction)
	if frame.confirm:
		var focus := get_viewport().gui_get_focus_owner()
		if focus is Button and not focus.disabled:
			focus.pressed.emit()
		elif focus is LineEdit and focus.has_method("edit"):
			focus.call("edit")
	elif frame.cancel:
		if _key_dialog.visible:
			_cancel_swap()
		elif not _pending_operation.is_empty():
			_pending_operation = ""
			_rebuild_key_list()
		else:
			var focus2 := get_viewport().gui_get_focus_owner()
			if focus2 != null and focus2 != _nav[_page] and _pages[_page].is_ancestor_of(focus2):
				_nav[_page].grab_focus()
			else:
				_close_option()

func _navigate(direction: Vector2) -> void:
	var focus := get_viewport().gui_get_focus_owner()
	if focus == null or not _option_root.is_ancestor_of(focus):
		_nav[_page].grab_focus()
		return
	var candidates: Array = []
	_collect_focusables(_key_dialog if _key_dialog.visible else _panel, candidates)
	var here := focus.get_global_rect().get_center()
	var best: Control = null
	var best_score := INF
	for c: Control in candidates:
		if c == focus:
			continue
		var delta := c.get_global_rect().get_center() - here
		var forward := delta.x * direction.x + (-delta.y) * direction.y
		if forward <= 1.0:
			continue
		var side := absf(delta.x * direction.y) + absf(delta.y * direction.x)
		var score := forward + side * 2.5
		if score < best_score:
			best_score = score
			best = c
	if best != null:
		best.grab_focus()
		if _key_scroll.is_ancestor_of(best):
			_key_scroll.ensure_control_visible(best)

func _collect_focusables(node: Node, out: Array) -> void:
	for child in node.get_children():
		if child is Control and not child.is_visible_in_tree():
			continue
		if (child is Button and not child.disabled and child.focus_mode != Control.FOCUS_NONE) or child is LineEdit:
			out.append(child)
		_collect_focusables(child, out)
