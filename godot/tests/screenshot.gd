extends Node
## Windowed helper (not headless): renders the Training scene after a few scripted actions
## and saves PNGs. Run:
##   Godot_v4.7.2-stable_win64_console.exe --path godot res://tests/screenshot.tscn -- <out_dir>

var out_dir := "user://"
var frame := 0
var scene

func _ready() -> void:
	var args := OS.get_cmdline_user_args()
	if args.size() > 0:
		out_dir = args[0]
	scene = load("res://scenes/training.tscn").instantiate()
	add_child(scene)

func _process(_delta: float) -> void:
	frame += 1
	if frame > 260:
		if frame == 270:
			_shot("title")
			get_tree().quit()
		return
	var m: Match = scene.match_
	match frame:
		30:
			_shot("idle")
			m.player.body.position.x = -0.5
			m.dummy_fighter.body.position.x = 0.5
			m.player.input_buffer.push(Sk.Cmd.LIGHT)
		36:
			_shot("5lp_active")
		70:
			m.player.input_buffer.push(Sk.Cmd.JUMP)
		95:
			_shot("jump")
		150:
			scene.dummy.set_posture(TrainingDummy.Posture.CROUCH)
			m.player.input_buffer.push(Sk.Cmd.STRONG_KICK)
		166:
			_shot("5hk_vs_crouch")
		240:
			scene.set_option_visible(true)
		245:
			_shot("option")
			scene.ui.show_page("keys")
		250:
			_shot("keyconfig")
			scene.ui.show_page("dummy")
		255:
			_shot("dummy_settings")
			scene.ui.show_page("training")
		260:
			_shot("training_settings")
			scene.queue_free()
			add_child(load("res://scenes/title.tscn").instantiate())
		270:
			_shot("title")
			get_tree().quit()

func _shot(name_: String) -> void:
	var img := get_viewport().get_texture().get_image()
	img.save_png(out_dir.path_join(name_ + ".png"))
	print("saved ", name_)
