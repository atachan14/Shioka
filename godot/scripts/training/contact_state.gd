class_name ContactState
extends RefCounted
## MatchHud.ContactState: last contact, combo count and damage of one attacker,
## observed on the defender every tick.

var count := 0
var total_damage := 0.0
var last_damage := 0.0
var last: Dictionary = {}
var has_contact := false
var active := false
var display_left := 0.0
var _sequence := 0
var _reset := -1
var _hp := 0.0

func observe(defender: Fighter, dt: float, hold: float) -> void:
	if defender == null:
		return
	if _reset != defender.input_buffer.reset_version:
		clear()
		_reset = defender.input_buffer.reset_version
		_sequence = defender.contact_sequence
		_hp = defender.health
		return
	var changed := _sequence != defender.contact_sequence
	if changed:
		_sequence = defender.contact_sequence
		last = defender.last_resolved_contact
		has_contact = true
		last_damage = maxf(0.0, _hp - defender.health)
		if last.get("hit", false):
			if not active:
				count = 0
				total_damage = 0.0
			count += 1
			total_damage += last_damage
			active = true
			display_left = hold
		else:
			active = false
	if not changed and defender.stun_remaining <= 0 and defender.hitstop <= 0:
		active = false
	if not active:
		display_left = maxf(0.0, display_left - dt)
	_hp = defender.health

func rebase_hp(value: float) -> void:
	_hp = value

func clear() -> void:
	count = 0
	total_damage = 0.0
	last_damage = 0.0
	has_contact = false
	active = false
	display_left = 0.0
	last = {}
