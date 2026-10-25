# Shioka

Unity 2D fighting-game training prototype. Development snapshot; combat balance and artwork are provisional.

## Open and play

1. Open this directory with Unity **6000.4.1f1** (Universal Render Pipeline).
2. Allow Package Manager to restore the versions in `Packages/manifest.json` and `packages-lock.json`.
3. Open `Assets/Scenes/CombatFormulaComparison.unity`, press Play, then click Game view. Use a 16:9 Game view of960x540 or larger.

A/D move, W jump, S crouch. J/K/L punches; M/comma/period kicks. Forward double-tap or Space dashes; backward double-tap backsteps. The same forward dash command works once per airborne period. Hold back during an opponent attack to guard. Back+U (or weakP+weakK) requests FD. Semicolon performs the existing TRC. Esc opens training options; WASD selects, J confirms, K cancels. R records dummy input after3seconds; R stops, P plays once, Ctrl resets configured training state.

## Authoring

- `Assets/CombatPrototype/CombatV01/CombatTestV01.asset` references the adopted move/hurtbox data.
- Edit `Assets/CombatPrototype/CombatV01/Moves.asset` in Inspector for frame/attack data.
- `Shioka > Combat Test v0.1 > Edit move geometry`: select action/frame, edit detached Scene handles, explicitly Save authoritative SO. Unsaved changes can be discarded; preview does not execute combat.
- Locomotion defaults: `Assets/CombatPrototype/ApprovedLocomotionProfile.asset`; air dash: `Assets/CombatPrototype/AirDashProfile.asset`.
- Both fighters share `Assets/CombatPrototype/ApprovedLocomotion/AwariCharacter.asset`, calibrated body height1.55m.

The runtime uses60Hz combat ticks and native XY Unity Physics2D. Sprite poses are selected from combat state; artwork count does not determine gameplay duration. Frame values still include historical fractional values. No rollback/netcode, final wall-down rules or complete move roster is claimed.

## Snapshot scope

This public repository begins with a new root commit. Prior private history, old3D model/animation data, local QA output, absolute machine paths, personal records and generated caches are excluded. The ordinary Unity package dependencies are restored from their registry. See `ASSET_NOTICE.md` for rights scope. No separate software or unrestricted artwork license is granted by this publication.
