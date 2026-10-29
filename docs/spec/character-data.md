# キャラクターのデータとプロジェクトの構成

根拠はコードと `Assets/Shioka/` のフォルダ・アセット。

## データの持ち方(ぁゎり)

すべて `Assets/Shioka/調整Data/Fighter/Awari/` の下。

| 場所 | 中身 |
|---|---|
| `Awari.prefab` | 2D の体、表示、キャラの定義、技・食らい判定のカタログへの参照。Training の2人は、この Prefab のインスタンス |
| `Data/AwariCharacter.asset` | キャラの定義。技と枠の対応([input.md](input.md))、動作アセットへの参照 |
| `Data/SharedCharacter.asset` | 歩き・ダッシュの速度、加速、ジャンプの移行・着地の硬直など |
| `Move/ApprovedLocomotionProfile.asset` | ジャンプとバックステップの採用値 |
| `Move/AirDashProfile.asset` | 空中ダッシュの設定 |
| `Move/<動作>/<動作>.asset` | 動作ごとの画像・表示の位置・食らい判定(Idle、ForwardWalk、BackwardWalk、Crouch、GroundDash、AirDash、Jump、Backstep) |
| `Skill/<技>/Data.asset` | 技ごとのデータ(発生・持続・硬直、攻撃の強さ、倍率、判定の位置)。同じフォルダの `Sprite/` に、その技の画像列を置く |
| `Skill/Moves.asset` | 技のカタログ(各 Data.asset への参照と、共通ルールへの参照) |
| `Hurtboxes.asset` | 食らい判定の部位ごとの防御の強さ・優先度・補正 |
| `Compatibility/` | 旧形式との互換のためのデータ。今の調整の入口ではない |

- 技: 立ち(5LP〜5HK)、しゃがみ(2LP〜2HK)、空中(jLP〜jHK、j2HK)、投げ(Throw)、空中投げ(AirThrow)。
- **同じキャラは同じ性能**: 1P と 2P は、同じ Prefab と同じ調整データを使う(2026-10-27 に統一、ユーザー承認)。入力の主体と左右の向きは別。Play 中は1人ずつ複製を使うので、HP などは共有しない。
- **見た目と性能は分ける**: 画像の枚数や表示時間を変えても、技の発生・持続・硬直や移動の速度は変わらない。画像の位置、本体の移動、攻撃判定、食らい判定も、それぞれ別に編集する。

## 共通のデータ

`Assets/Shioka/調整Data/共通System/`: `Guard.asset`、`TG.asset`、`Combat.asset`(戦闘の共通係数。[combat.md](combat.md))、`CombatTestV01.asset`(HP、開始時の TG)、`Resources/TrainingInputConfig.asset`(物理入力の既定)、`Compatibility/LegacyBattleRules.asset`(旧仕様の共通値。新しいダウンや TG 獲得の仕様を有効にしたものではない)。

## コードの構成

| フォルダ(`Assets/Shioka/` の下) | 役割 |
|---|---|
| `Runtime/Battle/` | 戦闘・キャラ・表示・HUD(製品) |
| `Runtime/Training/` | Training の設定・ダミー・録画・入力履歴(製品) |
| `Runtime/Input/`、`Menus/`、`Flow/`、`Profiles/`、`Persistence/`、`History/` | 入力、メニュー、画面の流れ、保存 |
| `Development/Runtime/` | 開発用の比較・調整 UI。`UNITY_EDITOR` か Development Build のときだけコンパイルされる |
| `Editor/Authoring/` | Inspector とシーン上の編集ツール |
| `Editor/Validation/` | 導入・参照・ビルドの検査 |
| `Tests/EditMode/Editor/`、`Tests/PlayMode/Editor/` | Editor から明示的に実行する検証 |

- シーン: `Scenes/Title.unity`(入口)、`Scenes/CombatFormulaComparison.unity`(Training)、`Development/Scenes/MoveSpatialAuthoring.unity`(編集用。ビルドに含めない)。
- 2D の表示と Physics2D、正投影のカメラ。60Hz の固定ロジック。
- `Development` というフォルダ名だけではビルドから除外されない。除外はコンパイル条件と Editor フォルダで行っている。
- 空の `Online/`、`Rules/` などのフォルダは、将来のための場所で、実装はない。
- 画像の原本(採用前の素材や旧版)は `SourceArt/` に置く。`Assets/` の外なので、ビルドや自動検出には入らない。
