# キャラクターのデータとプロジェクトの構成

根拠は Godot 版のコード(`godot/scripts/`)とデータ(`godot/data/`)。データは 2026-11-16 に Unity のアセットから書き出したもので、値は Unity 版と同じ。Unity 版でどのアセットだったかは、下の表の「Unity での場所」(`Assets/Shioka/調整Data/` の下)。

## データの持ち方(ぁゎり)

すべて `godot/data/awari.json` の中。

| 項目 | 中身 | Unity での場所 |
|---|---|---|
| `definition` | 歩き・ダッシュの速度、加速、ジャンプの移行・着地の硬直、しゃがみの高さなど | `Fighter/Awari/Data/SharedCharacter.asset` |
| `locomotion` | ジャンプ(高さ 2m、滞空 54F)とバックステップ(25F、1.5m、画像の切り替え 10F / 15F)の採用値。ここから重力・初速・速度を計算する | `Move/ApprovedLocomotionProfile.asset` |
| `air_dash` | 空中ダッシュ(12F で 1.8m、後の減速) | `Move/AirDashProfile.asset` |
| `motor` | 体の大きさ(高さ 1.55m、幅 0.52m) | `Awari.prefab` の CapsuleCollider2D |
| `body` | 画像の縮尺の基準(身長 1.55m、頭頂と足裏のピクセル) | `Data/ShiokaBody155.asset` |
| `attack_slots` | 技と枠の対応([input.md](input.md)) | `Data/AwariCharacter.asset` |
| `motions` | 動作ごとの画像・表示フレーム数・表示の位置・食らい判定(idle、forwardWalk、backwardWalk、crouch、groundDash、backstep、airDash、jump) | `Move/<動作>/<動作>.asset` |
| `moves` | 技ごとのデータ(発生・持続・硬直、攻撃の強さ、倍率、判定の位置、キャンセル先、画像列)。`role` が `move:0`〜`move:11` は立ち・しゃがみの通常技、ほかは足払い(`sweep`)、空中技(`airStrongKickMove` など)、未使用の特殊技 | `Skill/<技>/Data.asset`、`Skill/Moves.asset` |
| `air_candidates` | 空中技の判定と時間(jLK = `flyingKick`、jMK = `dropKick` は技のデータを持たず、ここだけで動く) | `Skill/Compatibility/*Candidate.asset` など |
| `hurtboxes` | 食らい判定の部位ごとの防御の強さ(今は全身1つ、100)・優先度・補正 | `Hurtboxes.asset` |
| `throw`、`throw_hurtbox` | 投げ(Hold)の時間・判定・強さ、投げられ判定 | `Compatibility/Resources/TrainingThrow*.asset` |
| `fd`、`legacy_rules` | FD の旧設定、ダウンの時間など旧仕様の共通値(投げのダウンで使う) | `共通System/Resources/TrainingFdSettings.asset`、`共通System/Compatibility/LegacyBattleRules.asset` |
| `poses`、`provisional` | 攻撃中・ガード中の画像、仮の画像(ヒット・ガード・空中技・しゃがみ技など) | `Data/AwariCharacter.asset`、`Data/Provisional21.asset` |

- 画像は `godot/assets/awari/` にある。画像ごとの基準点と 1m あたりのピクセル数(PPU)は `godot/data/sprites.json`(Unity の取り込み設定から写したもの)。
- 技: 立ち(5LP〜5HK)、しゃがみ(2LP〜2HK)、足払い(3HK)、空中(jLK、jMK、jHP、jHK、j2HK)、投げ(Hold)。空中投げは画像だけで、技はない。
- **同じキャラは同じ性能**: 1P と 2P は同じデータを使う(2026-10-27 に統一、ユーザー承認)。入力の主体と左右の向きは別。HP などは1人ずつ持つ。
- **見た目と性能は分ける**: 画像の枚数や表示時間を変えても、技の発生・持続・硬直や移動の速度は変わらない。画像の位置、本体の移動、攻撃判定、食らい判定も、それぞれ別に持つ。
- 技の画像列は技の開始から再生する。短ければ、残りは動作や仮の画像で表示する。長ければ技の終わりで打ち切る。画像の合計時間で技の時間は変わらない。

## 共通のデータ

`godot/data/system.json`: `guard`(直前ガード・FD)、`tg`(TG の倍率)、`combat`(戦闘の共通係数。[combat.md](combat.md))、`combat_test`(HP 2300、開始時の TG、押し返しのフレーム数)、`display`(カメラの幅・HUD)、`input`(物理入力の既定)。Unity では `共通System/` の `Guard.asset`、`TG.asset`、`Combat.asset`、`CombatTestV01.asset`、`Resources/MatchDisplayProfile.asset`、`Resources/TrainingInputConfig.asset`。

## コードの構成

`godot/` の下。詳しくは [../guides/godot.md](../guides/godot.md)、Unity 版のクラスとの対応は [../UNITY_TO_GODOT.md](../UNITY_TO_GODOT.md)。

| フォルダ | 役割 |
|---|---|
| `scripts/core/` | 戦闘・キャラ・カメラの壁・表示する画像の選択・データの読み込み |
| `scripts/training/` | Training の設定・ダミー・録画・フレームバー・入力履歴 |
| `scripts/input/` | 入力、KeyConfig、メニュー入力 |
| `scripts/ui/` | Training の画面・HUD・Option、Title とキャラ選択 |
| `scenes/` | `title.tscn`(入口)、`training.tscn` |
| `tests/` | ヘッドレスのテスト、スクリーンショット |

- 2D の表示、正投影のカメラ。60Hz の固定ロジック(`_physics_process`)。1フレームの処理の順番は Unity 版と同じ(`scripts/core/match.gd`)。
- 画像の原本(採用前の素材や旧版)は、Unity 版と同じくリポジトリの `SourceArt/` にある。
