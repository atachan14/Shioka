# Input rework checkpoint — not connected to live controls

Baseline: 300d06d. Current live mappings/router/UI/recorder are intentionally unchanged at this checkpoint.

Added independent foundation:
- BattleAttackSlot ABCXYZ and editable CharacterAttackSlot mapping data (Awari defaults LP/MP/HP/LK/MK/HK). Not yet attached to a character asset or used by history.
- ModifierPressOwnership: per-device frame-boundary resolution, no time grace; stable press owner, no late modifier reinterpretation, one-shot pulses, held Force, inert training chords outside Training, optional empty T2, no invented T1+T2 action.
- Binding conflict checks currently compare normalized exact binding strings; integration must also address equivalent/overlapping device control paths. Not a complete rebinding UI.
- InputFoundationChecks tests this pure logic; not a physical or virtual InputSystem integration test.

Approved destination remains: keyboard J K L M comma period; Throw I; TRC semicolon; Force either Shift; Dash Space; record R/play P/reset either Ctrl; Esc Options/back. Pad square/triangle/R1/cross/circle/R2, TRC L1, T1 L2, empty T2; modified triangle Throw/R1 Force/R2 Dash/square Record/circle Play/cross Reset; Options opens menu. No profile/history/online expansion.

Blocking decision before connecting live routing:
Existing Special1(A+C) and Special2(X+Z) use the same 30ms grace buffer as the removed FD/Throw pairs. All normal attacks must now have zero grace, but unrelated Special behavior must not be changed without approval. Removing only FD/Throw does not eliminate the delay. Requested clarification: retire Special multi-button chords and retain dedicated paths, or explicitly retain old delay for these Special-participating buttons (an exception to zero-delay). Neither option has been selected by implementation.

Still pending: InputSystem device/context adapter, live character slot resolution/history labels, Force frame/recording support, training action routing, menu pad navigation/suppression, config UI updates, disconnect/focus integration and full virtual-device/Play tests. No claim of finished controls or saved key settings. No source projects or remotes changed.
