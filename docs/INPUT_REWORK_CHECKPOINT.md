# Input rework — latest no-wait design

The earlier 30ms and trial 2F simultaneous-input confirmation waits are retired. Input sources sample on normal 60Hz ticks. A down event maps immediately to an assigned slot/move; the ordinary 10F command buffer and original move startup are unchanged. No elapsed-time subtraction or retroactive startup exists. Hitstop still advances the existing input clock; menus pause it.

## What is connected
- Nine stable logical slots, displayed ABCJKLXYZ. IDs A/B/C/X/Y/Z retain old values; J/K/L are additional, currently unassigned. Existing six move labels are LP/MP/HP/LK/MK/HK in each character's TrainingApprovedCharacter.attackSlots data. Different characters can supply different names/move IDs.
- CharacterCommandResolver is now immediate mapping, not a chord recognizer. LP+HP and LK+HK no longer request dedicated Special inputs. Existing underlying special move assets/execution IDs are retained, but their new nine-slot assignments are undecided and were not invented.
- TechniqueCategory (Default, Conditional, Command, Delirium) is move metadata. Input conditions live separately in contextualMoves. Undefined direction+slot falls back to its default assigned slot; a defined move's runtime eligibility failure is not converted into a new fallback policy.
- Mania is an independent modifier. Internal ForceHeld/Force enum identifiers remain compatibility names; UI uses Mania. Current prior back+modifier FD behavior remains; no new Mania enhancement, TG cost, Delirium activation, flash or ultimate move is implemented. Delirium has no dedicated button.
- Raw InputSystem events preserve short taps between ticks. Per-device T1/T2 ownership resolves at the input boundary: modifier before/same tick wins; later modifier cannot reinterpret the earlier press. No grace timer. Release never creates a normal attack. Training-only modified pulses are inert elsewhere, not fallback attacks. T1+T2 has no invented action.

## Provisional compatible controls, not a final nine-button layout
Keyboard existing six: J/K/L/M/comma/period => A/B/C/X/Y/Z. Added J/K/L logical slots have no physical key or move assignment. I Throw, semicolon TRC, either Shift Mania, Space Dash, R Record, P Play, either Ctrl Reset, Esc Options/back. WASD/arrows retained.
The prior six-slot pad layout is only compatibility data pending the user's final pad decision. No new nine-button pad layout was selected. Existing T1=L2, T2 empty and prior modifier actions are configurable data; T2 has no default action. Menu circle confirms, cross cancels, Options opens/back. D-pad/stick directions remain. A second pad/device-assignment UI is not completed.

## Editing and limits
Active config: Assets/Shioka/調整Data/共通System/Resources/TrainingInputConfig.asset. Version3 uses slotGamepadBindings, extraKeyboardBindings, modifier1/2 bindings and action tables. Legacy fields remain serialized for recovery but chord timing is hidden/zero and not used.
Options > Input settings edits six existing plus three unassigned key/pad paths and T1/T2 as a validated runtime draft. Apply replaces maps while keeping menu suspension/neutral-release gating; Discard reloads current runtime values. No disk saving, interactive press-to-rebind, full profile/history or online implementation. T2 action table is Inspector data. Character mapping is in AwariCharacter.asset near Data; no unapproved new move assignment.
Training recording stores logical slot masks and Mania state, excluding Record/Play/Reset/menu keys. Existing recordings are memory-only and restart with the session; no old recording-file format needs migration.

## Verification
See task input-nine-* reports. Edit-mode virtual events were not reaching game action buffers; actual Play-mode virtual devices do. Keep this distinction from physical keyboard/pad or mouse testing. A known UnityEditor.Search index startup exception is separate from game runtime. Old G and the public working copy remain untouched; no push/deploy.

## Final verification (2026-10-26)
- Final Play-mode virtual Keyboard/Gamepad checks: 86 pass, 0 fail. Includes all retained provisional bindings, immediate slots, removed U and basic FD/Throw pairs, modifier before/same/after tick, both release orders, neutral gating, disconnect, inactive Training chords, optional data-driven T2, conflict rejection and menu/text-field routing.
- Foundation ownership/data checks: 59 pass, 0 fail. Existing portable scene/reference and combat formula regressions: 146 pass, 0 fail.
- Actual production TrainingControls -> TrainingSession -> Fighter Play path with virtual Keyboard: 28 pass, exit0. Includes sampled-tick LP queue/start without confirmation wait, unchanged startup, movement/jump/landing, relative-back Mania FD on both sides and 1/4/7 directions, menu held-button leak suppression, record/countdown/play/reset and no held-reset repetition.
- Ordinary buffer expires at frame10 exactly as before. No combat formula, movement tuning, scene, package or ProjectSettings change. Only active Input config changes layoutVersion2->3 and obsolete chordWindowSeconds.03->0.
- Physical keyboard/pad/arcade stick/mouse and visual layout were not manually tested. Batches still log the preexisting UnityEditor.Search.SearchDatabase startup index exception, separate from game code. No standalone build or online validation is claimed.
