# Skill data ownership

> 2026-10-29 現在: 現在は個別技SOとv14画像列導入済みです。本文の新弱P未導入・素材未受領は旧checkpointの記録です。 現在状態は [STATUS](STATUS.md)、全体入口は [README](README.md)。
Each adopted TrainingMove definition now lives in Skill/<move>/Data.asset near its Sprite folder. Moves.asset is a reference catalog; its20 migrated definitions preserve all serialized values/curves/references exactly. Compatibility definitions retain technical names rather than inventing D1-D3 assignments. Throw settings and airborne candidate metadata remain their existing types for now.

Select Data.asset to edit timing/intensity/etc outside Play. Open spatial editing for this skill opens the existing authoring scene and selects the command. Save authoritative SO now creates the spatial profile as a subasset in that same Data.asset for adopted skills. Catalog values are never manually synchronized. Legacy/fallback catalogs still read old serialized fields via FormerlySerializedAs.

Play resolves detached runtime copies; JSON reset clears caches and reloads source references. Individual skill authoring never receives developer comparison mutations. Verification: same-file spatial save/reload and untouched catalog; runtime clone/source isolation; motionPlay24, input73/combat146 regression passed. Native mouse interaction is unverified. Existing character presentation/gallery still owns the active key-pose references; images are physically adjacent but this step does not claim new weakP animation material was found.

## 共有互換ルール
Moves.assetの9個の旧共有数値は共通System/Compatibility/LegacyBattleRules.assetへ同値移行。カタログは技と共有ルールの参照を持つ。新しいダウン／TG獲得仕様を有効化したものではない。Play中の変更は複製値だけに適用される。

## Sprite sequence follow-up
Data.asset now edits a private whole-skill draft: Save same skill asset adopts it; Cancel/reload or deselection discards it. Save or cancel before opening Spatial. Optional spriteSequence uses shared action-clock frame selection in runtime and Spatial preview. Empty sequences retain character-owned legacy key-pose references. See SKILL_SPRITE_SEQUENCE.md. No v14 art or move-timing changes were adopted.

## Folder detection
Editor detects Sprite subfolders next to each Data.asset; explicit Sync only edits a private draft. Natural filename order, GUID/local-ID hold retention, tombstones for restored images, no runtime disk search. Original-only art stays outside Sprite. Existing twenty Sprites folders were renamed with image bytes and .meta GUID/values preserved. No new weakP source was imported.
