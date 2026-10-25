# Public snapshot scope

Prepared from the adopted2D development state on2026-10-25. The old private repository and its history remain separate. This tree has no shared Git ancestry with that repository.

Included: current combat runtime/data, approved active character sprites, shared Physics2D scene, spatial editing scene/tools, render/input settings and pinned Unity registry dependencies.

Excluded: all old3D source models/animation assets; historical and test scenes; personal QA harnesses/outputs; local setup executables/scripts and paths; internal correspondence/documents/provenance Downloads paths; Unity-generated caches and cloud project binding; old exploratory sprite trials and template tutorial content.

Portable-only adjustments: Editor project guards use scene presence rather than a workstation path; local QA writes/environment paths removed; obsolete scene recreation fallback removed. Gameplay numerical data, approved PNG pixels/import settings,60Hz logic and formulas are preserved. The disabled exploratory Sprite trial component is removed. The old StrongKick fallback image is replaced by the existing approved Provisional21/5HK reference, avoiding legacy model-derived exploratory art.

Verification from the cleaned development state includes ground/air/throw coverage129, native movement/selection67, training409, input13, landing50, HUD113, combat96 and authoring16 checks. These were scripted Unity checks, not physical keyboard/mouse or standalone-build proof. This exported tree passed Runtime/Editor and non-Editor C# compilation, a fresh Unity6000.4.1f1 import, playable/editing scene and dependency checks, canonical data/build-scene validation, and146 formula assertions. The fresh verifier exits0. No physical-input smoke, standalone executable build, GitHub CI or Web build success is implied.

The serialized initial renderer sprite is the approved Neutral sprite, replacing an unused exploratory trial image; runtime pose selection remains unchanged.
