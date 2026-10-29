# Current input rules — chord establishment edges

> 2026-10-29 現在: KeyConfig UI/個人保存は後続で実装済みです。「今回範囲外」はそのcheckpoint時点の記録。後押しT1拒否と同時押し待ちは撤回済み。 現在状態は [STATUS](STATUS.md)、全体入口は [README](README.md)。

Latest approved revision after 195d6b4. No simultaneous-input grace or command confirmation wait. Normal 10F buffering/startup and battle eligibility remain unchanged.

## Modifier edges
T1 alone is inert. Modifier first or same sampled tick: chord only. Normal button first: normal input immediately; later T1 emits a NEW chord input once. The earlier input is neither removed nor rewound. Whether the new move executes, cancels or waits/expires is decided by existing battle/buffer rules. Held chord does not repeat. T1 release does not issue a normal attack. Any break and re-establishment (either button released/repressed) is a fresh chord edge. T1+T2 has no invented action. Per-device state clears on focus/context/disconnection. Mania is independent hold state, not part of this edge ownership.

## Approved defaults
| Slot/action | Keyboard | Pad |
|---|---|---|
| A / D1 | U | T1 + square |
| B / D2 | I | T1 + triangle |
| C / D3 | O | T1 + R1 |
| J / LP | J | square |
| K / MP | K | triangle |
| L / HP | L | R1 |
| X / LK | M | cross |
| Y / MK | comma | circle |
| Z / HK | period | R2 |
| Hold | H | T1 + circle |
| Mania | either Shift | L2 |
| TRC | semicolon | T1 + R2 |
| Dash | Space | T1 + cross |
| Record | R | L3 |
| Play | P | R3 |
| Reset | either Ctrl | unassigned |
| Options | Esc | Options |
T1=L1; T2 unassigned. Directions and menu confirm J/circle, cancel K/cross are unchanged. Training actions remain inert outside Training, and menu suspension/neutral release prevents leaking into Battle.

## Two data layers
Basic settings: Assets/Shioka/調整Data/共通System/Resources/TrainingInputConfig.asset. slotKeyboardBindings and direct slotGamepadBindings plus modifier1/2 action tables produce logical slot masks/system inputs. These physical settings do not name character techniques. The character's AwariCharacter.asset maps operations/move IDs and display names to slots. JKL now point to the existing LP/MP/HP; XYZ retain LK/MK/HK. ABC display D1/D2/D3 but remain assigned=false: no super move or new D3 technique was invented. Input history displays those raw character labels even when no technique is assigned.
Hold is the display/input rename of the existing Throw mechanic; internal IDs remain for compatibility. Mania retains prior back+modifier FD behavior only; no new enhancement or TG cost. Delirium remains move-category metadata with no dedicated button. Undefined directional override falls back to the default slot; no new fallback for an existing move's failed TG/eligibility.

Input settings currently edits runtime direct bindings and T1/T2 paths; modifier action tables are Inspector data. The direct ABC pad paths are empty because their inputs come from the T1 table. This is not a complete chord-rebinding UI. Disk saving, interactive capture rebinding, profiles/history persistence and second-pad pairing remain outside this change. Old serialized binding fields are hidden, not deleted. Approved defaults and character mapping were explicitly saved with value-preservation checks. Record/replay stores logical slot masks and Mania, not physical keys.

## Final verification
Final Unity compilation and scene/dependency checks passed. Foundation: 73 pass; existing formula regressions: 146 pass; Play-mode virtual Keyboard/Gamepad: 72 pass; actual production source/session/fighter Play: 36 pass, exit0. Includes HP first then fixture-only late D3, held/reformed edges, unchanged startup, ordinary buffering, Mania with ABC, character history, both-facing FD, menu nonleak and record/play/reset. The fixture uses detached character data and restores it; real D1/D2/D3 stay unassigned.
A failed live fixture was diagnosed: GamepadState.WithButton mutates its struct; the HP-only test state accidentally acquired L1. Independent state construction fixed it. No production workaround was made for that failure. Physical devices/mouse and visual layout are unverified. The preexisting UnityEditor.Search startup index exception remains separate from game code.
Old G, public copy, remote and published history remain unchanged; no push/deploy.

KeyConfig UIとユーザー保存は後続実装済み。現仕様・検証限界は docs/KEYCONFIG.md を参照。
