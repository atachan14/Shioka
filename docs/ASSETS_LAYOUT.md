# Assets layout — stage 1

Local development project: G:/source/repos/unity/Shioka. Based on public main 78241cc; old private history and old G project remain separate. No push.

- Assets/Shioka/調整Data/共通System: existing test system profile and compatibility Resources settings. Camera/Display are still one existing SO. Input defaults remain unchanged, pending user clarification.
- 調整Data/Fighter/Awari: existing Data, Move profiles, per-action movement art, per-skill art, reactions, and existing Moves.asset/Hurtboxes.asset. Skill/Compatibility and Compatibility/Resources retain the distinct legacy/fallback data without pretending they are the v0.1 canonical Moves. No values merged.
- Runtime/Battle: shared Fighter, motor, combat, presentation, HUD. Runtime/Training: product training session, recorder, dummy and current overlay. Runtime/Input: unchanged device mapping/input logic.
- Editor/Authoring and Editor/Validation: existing authoring inspectors/adoption and portable setup/check tools.
- Development/Runtime/Authoring: Scene-attached Spatial authoring host, still guarded by UNITY_EDITOR, not blindly placed in Editor. Development/Runtime/Comparison and Development/Data retain developer formula comparison, already guarded for development at runtime. A Development folder alone does not exclude builds. Mixed overlay partials and canonical motion application remain in runtime until a later dependency-aware split.
- Scenes/CombatFormulaComparison.unity: playable entry. Development/Scenes/MoveSpatialAuthoring.unity: editor-only workflow; not in build scenes.
- Shared: materials/shaders, including Resources/TrainingCancelRing for unchanged shader loading.
- Empty Profiles/History/Persistence/Flow/Menus/Online/Rules/Tests folders are reserved destinations, not implemented features or Test Runner suites.

Open the playable scene, press Play and click Game for ordinary keyboard input. Existing bindings unchanged. Spatial authoring menu still opens editing scene and saves explicit SO drafts. The menu and literal asset paths follow the new layout.

Not done in this stage: per-move SO conversion, consolidated Motion SO, individual movement animation/hurtbox SOs, Fighter Prefab, shared formula SO extraction, complete runtime/developer architecture split, menus/network/save/rebinding functionality. No new input behavior. Existing Resources.Load names are retained; file .meta GUIDs and serialized numeric asset bytes are preserved.

Old G-only QA, source exploration images, legacy preview scenes and guides were not silently imported into the public history. They remain recoverable in old G. Public ASSET_NOTICE.md and PUBLICATION_SCOPE.md retained. No new root README.

## Verification (2026-10-26)
- Unity 6000.4.1f1 new-project import and C# compilation completed; playable dependency closure, both approved presentations, persistent motion references, build scene and authoring scene passed. Formula checks: 146 pass, 0 fail.
- Bounded logical-input Play smoke: movement, jump, landing, standing light punch and nonmissing sprites passed over 3.216667 simulation seconds. This injects ITrainingInputSource commands; physical keyboard/mouse and full visual layout were not verified. Batch virtual-keyboard attempt did not reach gameplay and is not counted as input success. Input sources remain byte-identical to source.
- A UnityEditor.Search.SearchDatabase startup index exception occurred during batch Play, outside gameplay code. No migration workaround or package change made. Full manual Editor smoke remains advisable.
- Original file asset/script GUIDs retained; no duplicate Assets GUIDs. Serialized SO values/art and all adopted image files preserved. BuildSettings repaired using the relocated scene GUID and clean-source bytes; only its path differs.
- New tests are Editor-only explicit entry points, idle during ordinary Play. This is not a claim that every old private QA suite was transferred or run.

## Current follow-up state
The sections above record the stage-1 migration. The canonical path is now G:/source/repos/unity/Shioka; the old checkout is recoverable in the Recycle Bin and retirement archive, not a second active project. Profiles/Persistence now implement KeyConfig drafts and explicit user JSON save/load; Menus includes Option navigation. Flow/History/Online remain future placeholders. See KEYCONFIG.md. Individual Idle/ForwardWalk motion assets are now implemented; other motion/per-skill SO conversion and Fighter Prefab remain future work.

## Subsequent input work
The stage-1 input-unchanged statement describes the layout commit only. See INPUT_REWORK_CHECKPOINT.md for the later approved nine-slot/no-confirmation-wait input revision and its remaining undecided bindings.
