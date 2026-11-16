class_name KeyCapture
extends RefCounted
## KeyBindingCapture (settings only): release everything, press one button (optionally while
## holding T1/T2 on the same pad), release to register. Sticks are ignored.

var _hub
var _profile: Dictionary
var _trigger := false
var _armed := false
var _candidate := ""
var value := ""
var pad := false
var complete := false
var status := "すべてのボタンを離してください"

func _init(hub, profile: Dictionary, trigger_row: bool) -> void:
	_hub = hub
	_profile = profile
	_trigger = trigger_row
	_hub.control_changed.connect(_on_control)

func dispose() -> void:
	if _hub != null:
		_hub.control_changed.disconnect(_on_control)
		_hub = null

func _matches(device: String, control: String, binding: String) -> bool:
	var pc := KeyConfig.path_controls(binding)
	return not KeyConfig.normalize(binding).is_empty() and pc.pad == (device != "kb") and pc.controls.has(control)

func _on_control(device: String, control: String, is_pressed: bool) -> void:
	if not _armed or complete or not _candidate.is_empty() or not is_pressed:
		return
	var is_pad := device != "kb"
	var t1 := KeyConfig.row(_profile, "trigger.T1")
	var t2 := KeyConfig.row(_profile, "trigger.T2")
	var t1v: String = t1.pad if is_pad else t1.keyboard
	var t2v: String = t2.pad if is_pad else t2.keyboard
	if not _trigger and (_matches(device, control, t1v) or _matches(device, control, t2v)):
		status = "修飾を押したまま、組み合わせるボタンを押してください"
		return
	var mod := 0
	if not _trigger:
		for c in _hub.controls.get(device, {}).keys():
			if _matches(device, c, t1v):
				mod |= 1
			if _matches(device, c, t2v):
				mod |= 2
		if mod == 3:
			status = "T1+T2の同時使用は登録できません"
			return
	var relative := control
	if not is_pad and (relative == "leftShift" or relative == "rightShift"):
		relative = "shift"
	if not is_pad and (relative == "leftCtrl" or relative == "rightCtrl"):
		relative = "ctrl"
	var root := "<Gamepad>/" if is_pad else "<Keyboard>/"
	_candidate = device + ":" + control
	pad = is_pad
	value = ("T1+" if mod == 1 else ("T2+" if mod == 2 else "")) + root + relative
	status = "離すと登録: " + value

func tick() -> void:
	if complete:
		return
	if not _armed:
		if not _hub.any_pressed():
			_armed = true
			status = "Keyboard / Pad を入力（取消は画面のボタン）"
		return
	if not _candidate.is_empty() and not _hub.any_pressed():
		complete = true
		status = "入力を取得しました"
