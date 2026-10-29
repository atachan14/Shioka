# Shioka docs の読み方

更新基準: 2026-10-29、実装基準 `0b93ebf`。この整理はゲーム変更ではありません。

初めて読む人・AI は次の順で確認してください。

1. [コンセプト](CONCEPT.md): 何を面白さの中心にするか。初期案と現行仕様を区別。
2. [現行の暫定仕様](CURRENT_SPEC.md): 戦闘式・入力・Training の重要条件。
3. [実装状況と未決](STATUS.md): 今できること、勝手に補ってはいけないこと。
4. [調整入口](EDITING_FRONTWALK_5LP.md)、[Inspector 操作](AUTHORING_INSPECTOR_EDITING.md)、[共通調整](SHARED_SYSTEM_TUNING.md)。
5. [docs 移行経緯と旧資料の参照](DOCS_HISTORY.md)。旧資料を調べる場合もここから。

## 実装を調べる入口

- 構造: [Assets 配置](ASSETS_LAYOUT.md)、[製品・開発・Editor の境界](DEVELOPMENT_BOUNDARIES.md)。
- キャラと技: [Prefab](AWARI_PREFAB.md)、[移動性能の同一化](MOVEMENT_PARITY.md)、[動作データ](MOTION_ACTIONS.md)、[技データ](SKILL_DATA.md)。
- 画像: [技画像列](SKILL_SPRITE_SEQUENCE.md)、[FrontWalk](FRONTWALK_CYCLE.md)、[弱P v14 導入記録](WEAKP_V14_IMPORT.md)。
- 操作と保存: [入力仕様](INPUT_REWORK_CHECKPOINT.md)、[KeyConfig](KEYCONFIG.md)、[Profile](PROFILES_AND_FLOW.md)、[Title / Training](TRAINING_FLOW.md)。
- [2D 整理](TWO_D_EDITOR_CLEANUP.md)、[旧作業ログ](WORK_CHECKPOINT_20261026.md)。

## 情報の扱い

採用した設計と実装済みは別です。過去のテスト件数は当時のコードに対する記録で、今のコードを再検証した証明ではありません。各文書の追記には古い「未実装」も残るため、現在状態は STATUS を先に確認してください。矛盾は出典と時点を示して保留し、古い案を復活させないでください。コードは実挙動、会話は採用意図の根拠であり、片方だけで他方を断定しません。
