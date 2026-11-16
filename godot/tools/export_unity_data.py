"""One-time export of Shioka's Unity tuning data into the Godot project.

Run from anywhere:  python godot/tools/export_unity_data.py
Reads ../Assets (the Unity project, kept for reference) and writes:
  godot/data/system.json          shared rules (Guard, TG, Combat, CombatTestV01, ...)
  godot/data/awari.json           the character (motor, motions, moves, hurtboxes, ...)
  godot/data/sprites.json         per-sprite pivot / pixels-per-unit (from Unity .meta)
  godot/assets/awari/**.png       copies of every referenced sprite

After the migration the JSON files are the source of truth (edit them directly);
running this again overwrites them from the Unity assets.
"""
import json, os, shutil, struct, sys
sys.path.insert(0, os.path.dirname(__file__))
from unity_yaml import ASSETS, load_asset, load_ref, path_of, meta, hex_ints

GODOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
DATA = os.path.join(ASSETS, 'Shioka', '調整Data')
AWARI = os.path.join(DATA, 'Fighter', 'Awari')
SYSTEM = os.path.join(DATA, '共通System')

sprites = {}


def v3(v):
    return [float(v.get('x', 0)), float(v.get('y', 0)), float(v.get('z', 0))]


def v2(v):
    return [float(v.get('x', 0)), float(v.get('y', 0))]


def png_size(p):
    with open(p, 'rb') as h:
        head = h.read(24)
    return struct.unpack('>II', head[16:24])


ALIGN = {0: (.5, .5), 1: (0, 1), 2: (.5, 1), 3: (1, 1), 4: (0, .5), 5: (1, .5), 6: (0, 0), 7: (.5, 0), 8: (1, 0)}


def sprite(ref):
    src = path_of(ref)
    if src is None:
        return None
    rel = os.path.relpath(src, AWARI) if src.startswith(AWARI) else 'misc/' + os.path.basename(src)
    rel = rel.replace('\\', '/')
    dest = 'res://assets/awari/' + rel
    if dest not in sprites:
        m = meta(src)['TextureImporter']
        w, h = png_size(src)
        align = int(m.get('alignment', 0))
        pivot = v2(m['spritePivot']) if align == 9 else list(ALIGN.get(align, (.5, .5)))
        sprites[dest] = {'size': [w, h], 'pivot': pivot, 'ppu': float(m.get('spritePixelsToUnits', 100)),
                         'source': os.path.relpath(src, ASSETS).replace('\\', '/')}
        out = os.path.join(GODOT, 'assets', 'awari', rel)
        os.makedirs(os.path.dirname(out), exist_ok=True)
        shutil.copyfile(src, out)
    return dest


def slope(v):
    # Infinite tangents are Unity's stepped keys; exported as null.
    v = float(v)
    return None if v != v or v in (float('inf'), float('-inf')) else v


def curve(c):
    return [[float(k['time']), float(k['value']), slope(k.get('inSlope', 0)), slope(k.get('outSlope', 0))] for k in (c or {}).get('m_Curve', [])]


def move(d, role):
    seq = (d.get('spriteSequence') or {}).get('frames') or []
    st = d['strength']
    return {
        'role': role,
        'command': int(d['command']), 'attack_class': int(d['attackClass']), 'causes_knockdown': bool(int(d['causesKnockdown'])),
        'state': d.get('state', ''),
        'startup': float(d['startupFrames']), 'active': float(d['activeFrames']), 'recovery': float(d['recoveryFrames']),
        'attack_level': float(d['attackLevel']), 'damage': float(d['damage']), 'hit_gain': float(d['hitGain']),
        'ground_hit_knockback_speed': float(d.get('groundHitKnockbackSpeed', 0)),
        'damage_multiplier': float(d.get('combatDamageMultiplier', 1)), 'chip_multiplier': float(d.get('combatChipMultiplier', .1)),
        'pushback_scale': float(d.get('combatPushbackScale', .001)),
        'center': v3(d['center']), 'half_size': v3(d['halfSize']), 'contact_normalized': float(d.get('contactNormalized', .5)),
        'chains': hex_ints(d.get('chains')),
        'strength': {'enabled': bool(int(st['enabled'])), 'weak_point': v3(st['weakPoint']), 'strong_point': v3(st['strongPoint']),
                     'minimum_level': float(st['minimumLevel']), 'maximum_level': float(st['maximumLevel'])},
        'use_level_curves': bool(int(d.get('useLevelCurves', 0))),
        'attack_level_curve': curve(d.get('attackLevelCurve')), 'defense_level_curve': curve(d.get('defenseLevelCurve')),
        'use_timed_trc_recovery': bool(int(d.get('useTimedTdcRecovery', 0))),
        'timed_trc_recovery_cost_curve': curve(d.get('timedTdcRecoveryCostCurve')),
        'spatial_profile': None if path_of(d.get('spatialProfile')) is None else 'unsupported',
        'sprites': [{'sprite': sprite(f['sprite']), 'hold': int(f['holdFrames'])} for f in seq],
    }


def motion(ref):
    d = load_ref(ref)
    poses = []
    for p in d['poses']:
        poses.append({'sprite': sprite(p['sprite']), 'hold': int(p.get('holdFrames', 1)), 'offset': v2(p.get('visualOffset', {})),
                      'hurt': [{'part': int(h['partId']), 'inherit_body': bool(int(h.get('inheritBodyShape', 0) or 0)),
                                'center': v2(h['center']), 'size': v2(h['size'])} for h in (p.get('hurt') or [])]})
    return {'playback': int(d.get('playback', 0) or 0), 'loop': bool(int(d.get('loop', 1))), 'poses': poses}


def candidate(ref):
    d = load_ref(ref)
    assert path_of(d.get('spatialProfile')) is None
    return {'duration': float(d['standaloneDurationSeconds']), 'contact_center': v3(d['measuredContactCenter']),
            'half_size': v3(d['proposedHalfSize']), 'pose_offset': v3(d['poseOffset'])}


def values(path):
    return load_asset(path)['values']


def export():
    character = load_asset(os.path.join(AWARI, 'Data', 'AwariCharacter.asset'))
    shared = load_asset(os.path.join(AWARI, 'Data', 'SharedCharacter.asset'))
    body = load_ref(character['bodyCalibration'])
    loco = load_asset(os.path.join(AWARI, 'Move', 'ApprovedLocomotionProfile.asset'))['defaults']
    airdash = load_asset(os.path.join(AWARI, 'Move', 'AirDashProfile.asset'))
    test = load_asset(os.path.join(SYSTEM, 'CombatTestV01.asset'))
    moveset = load_ref(test['moves'])
    hurt = load_ref(test['hurtboxes'])
    throw = load_ref(moveset['throwSettings'])
    fd = load_ref(moveset['fdSettings'])
    legacy = load_ref(moveset['sharedLegacyRules'])['values']
    throw_hurt = load_asset(os.path.join(AWARI, 'Compatibility', 'Resources', 'TrainingThrowHurtboxProfile.asset'))
    provisional = load_ref(character['provisionalPoses'])
    inputcfg = load_asset(os.path.join(SYSTEM, 'Resources', 'TrainingInputConfig.asset'))
    display = load_asset(os.path.join(SYSTEM, 'Resources', 'MatchDisplayProfile.asset'))

    definition = {k: float(v) for k, v in shared.items() if isinstance(v, (int, float)) and not k.startswith('m_')}
    moves = []
    for r in moveset['catalog']:
        moves.append(move(load_ref(r['asset'])['data'], r['role']))

    awari = {
        'id': character['characterId'], 'name': 'ぁゎり',
        'definition': definition,
        'locomotion': {k: (float(v) if '.' in str(v) else int(v)) for k, v in loco.items()},
        'air_dash': {'duration_frames': int(airdash['durationFrames']), 'distance': float(airdash['distance']),
                     'reset_vertical_velocity_on_start': bool(int(airdash['resetVerticalVelocityOnStart'])),
                     'post_dash_speed_ratio': float(airdash['postDashSpeedRatio']), 'post_dash_deceleration': float(airdash['postDashDeceleration'])},
        # Awari.prefab: CapsuleCollider2D offset (0,0.775) size (0.52,1.55); TrainingMotor2D.skinWidth 0.01.
        'motor': {'center_y': .775, 'height': 1.55, 'radius': .26, 'skin_width': .01},
        'body': {'standing_height': float(body['standingHeight']), 'reference_sprite': sprite(body['referenceSprite']),
                 'skull_pixel': v2(body['skullPixel']), 'sole_pixel': v2(body['solePixel'])},
        'attack_slots': [{'slot': int(s['slot']), 'assigned': bool(int(s['assigned'])), 'command': int(s['command']),
                          'display_name': s['displayName'], 'operation_id': s.get('operationId') or ''} for s in character['attackSlots']],
        'contextual_moves': [],
        'hurtboxes': [{'id': int(r['stableId']), 'correction': float(r['combatPositionCorrection']), 'priority': int(r['selectionPriority']),
                       'shape': int(r['shape']), 'center': v3(r['center']), 'size': v3(r['size']),
                       'high': float(r['highDefense']), 'mid': float(r['midDefense']), 'low': float(r['lowDefense'])} for r in hurt['regions']],
        'throw': {'startup': float(throw['startupFrames']), 'active': float(throw['activeFrames']), 'recovery': float(throw['recoveryFrames']),
                  'hold': float(throw['holdFrames']), 'targets': int(throw['targets']), 'box_center': v3(throw['boxCenter']),
                  'box_size': v3(throw['boxSize']), 'attack_level': float(throw['attackLevel']), 'damage': float(throw['damage']),
                  'hit_gain': float(throw['hitGain'])},
        'throw_hurtbox': {'center': v3(throw_hurt['center']), 'size': v3(throw_hurt['size'])},
        'fd': {'cost_per_attack_level': float(fd['costPerAttackLevel']), 'push_distance': float(fd['pushDistance'])},
        'legacy_rules': {k: float(v) for k, v in legacy.items()},
        'poses': {k: sprite(character[k]) for k in ['strongKick', 'weakPunch', 'mediumPunch', 'strongPunch', 'standingGuard']},
        'air_dash_visual_offset': v3(character['airDashVisualOffset']),
        'provisional': [{'id': e['id'], 'sprite': sprite(e['sprite']), 'offset_y': float(e['offsetY']), 'preview_only': bool(int(e['previewOnly']))}
                        for e in provisional['entries']],
        'motions': {k: motion(character[k + 'Action']) for k in ['idle', 'forwardWalk', 'backwardWalk', 'crouch', 'groundDash', 'backstep', 'airDash', 'jump']},
        'moves': moves,
        'air_candidates': {k: candidate(moveset[k]) for k in ['flyingKick', 'dropKick', 'airStrongKick', 'airDownStrongKick', 'airStrongPunch', 'airSpecial2']},
    }

    def actions(v):
        return [0] * 9 if not v else hex_ints(v)
    system = {
        'guard': {k: float(v) for k, v in values(os.path.join(SYSTEM, 'Guard.asset')).items()},
        'tg': {k: float(v) for k, v in values(os.path.join(SYSTEM, 'TG.asset')).items()},
        'combat': {k: float(v) for k, v in values(os.path.join(SYSTEM, 'Combat.asset')).items()},
        'combat_test': {'player_tg': float(test['playerTG']), 'opponent_tg': float(test['opponentTG']), 'hp': float(test['hp']),
                        'push_frames': int(test['pushFrames'])},
        'display': {k: (v if isinstance(v, dict) else float(v)) for k, v in display.items() if not k.startswith('m_')},
        'input': {
            'modifier1_actions': actions(inputcfg.get('modifier1Actions')), 'modifier2_actions': actions(inputcfg.get('modifier2Actions')),
            'modifier1_binding': inputcfg.get('modifier1Binding') or '', 'modifier2_binding': inputcfg.get('modifier2Binding') or '',
            'slot_keyboard': [x or '' for x in inputcfg['slotKeyboardBindings']],
            'slot_gamepad': [x or '' for x in inputcfg['slotGamepadBindings']],
            'second_slot_keyboard': [x or '' for x in inputcfg['secondSlotKeyboardBindings']],
            'trc_gamepad': inputcfg.get('trcGamepadBinding') or '', 'mania_gamepad': inputcfg.get('maniaGamepadBinding') or '',
            'record_gamepad': inputcfg.get('recordGamepadBinding') or '', 'play_gamepad': inputcfg.get('playGamepadBinding') or '',
            'first_throw_keyboard': inputcfg.get('firstThrowKeyboardBinding') or '', 'second_throw_keyboard': inputcfg.get('secondThrowKeyboardBinding') or '',
            'second_trc_keyboard': inputcfg.get('secondTrcKeyboardBinding') or '',
            'double_tap_seconds': float(inputcfg.get('doubleTapSeconds', .25)),
        },
    }
    os.makedirs(os.path.join(GODOT, 'data'), exist_ok=True)
    for name, obj in [('awari', awari), ('system', system), ('sprites', sprites)]:
        with open(os.path.join(GODOT, 'data', name + '.json'), 'w', encoding='utf-8', newline='\n') as h:
            json.dump(obj, h, ensure_ascii=False, indent=1)
    print('exported', len(sprites), 'sprites')


if __name__ == '__main__':
    export()
