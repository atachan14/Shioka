# Godot での開き方・動かし方・調整

Shioka は 2026-11-16 から Godot 4(GDScript)が正。プロジェクトは `godot/`。Unity のプロジェクト(`Assets/` など)は参照用に残している。

## 用意するもの

- Godot 4.7.2(普通の版。.NET 版ではない)。このPCでは `G:\source\tools\Godot`。
  - エディタ: `Godot_v4.7.2-stable_win64.exe`
  - コマンドから使うとき: `Godot_v4.7.2-stable_win64_console.exe`
- .NET SDK は要らない(C# は使っていない)。

## 開く・遊ぶ

1. エディタを起動し、「インポート」で `godot/project.godot` を選ぶ(2回目からはプロジェクト一覧に出る)。
2. F5(メインシーンを実行)で Title から始まる。Training だけを試すなら `scenes/training.tscn` を開いて F6。

コマンドから直接起動するとき(リポジトリの直下で):

```bash
/g/source/tools/Godot/Godot_v4.7.2-stable_win64.exe --path godot
```

操作は Unity 版と同じ([../spec/input.md](../spec/input.md))。キーボードは WASD / 方向キーで移動、J K L で弱中強P、M , . で弱中強K、Shift で Mania、; で TRC、Space で Dash、H で Hold(投げ)、Esc で Option。

## 確認(ヘッドレス)

リポジトリの直下で実行する。

```bash
/g/source/tools/Godot/Godot_v4.7.2-stable_win64_console.exe --headless --path godot --import
```

```bash
/g/source/tools/Godot/Godot_v4.7.2-stable_win64_console.exe --headless --path godot --quit-after 120
```

```bash
/g/source/tools/Godot/Godot_v4.7.2-stable_win64_console.exe --headless --path godot --script res://tests/run_tests.gd
```

- 1つ目: 画像の取り込みとスクリプトの読み込み。
- 2つ目: Title を120フレーム動かす(`res://scenes/training.tscn` を最後に付ければ Training)。エラーが出なければよい。
- 3つ目: ルールとフレーム数のテスト(式、5LP の発生・硬直、5HK の全体、ジャンプ、バックステップ、TRC のコスト、投げ、KeyConfig、ダミーの反撃コマンド)。最後に `N checks, 0 failures` と出ればよい。

画面の確認用に `tests/screenshot.tscn` がある(ウィンドウが開く。ヘッドレスでは動かない)。決まった操作をして、スクリーンショットを引数のフォルダに保存する。

```bash
/g/source/tools/Godot/Godot_v4.7.2-stable_win64_console.exe --path godot res://tests/screenshot.tscn -- <保存先のフォルダ>
```

## フォルダの構成(`godot/`)

| 場所 | 中身 |
|---|---|
| `project.godot` | 設定。物理(固定ロジック)60Hz、画面 1600×900 の論理座標(16:9 で拡大縮小) |
| `scenes/` | `title.tscn`(入口)、`training.tscn` |
| `scripts/core/` | 戦闘とキャラの中身。`fighter.gd`(状態・入力の先行・ガード・TRC・投げ・空中ダッシュ)、`combat_formula.gd`(式)、`match.gd`(1フレームの処理順)、`camera_bounds.gd`(画面の端)、`fighter_view.gd`(表示する画像の選択)、`game_data.gd`(データの読み込み) |
| `scripts/training/` | Training のセッション、録画と再生、ダミー、フレームバー、入力履歴、コンボの表示 |
| `scripts/input/` | キーボードとパッド(`input_hub.gd`)、KeyConfig、T1 の同時押し、メニュー入力、キーの登録 |
| `scripts/ui/` | Training の画面・HUD・Option、Title とキャラ選択 |
| `data/` | 調整データ(JSON)。下の「調整のしかた」 |
| `assets/awari/` | ぁゎりの画像(Unity の `Assets/Shioka/調整Data/Fighter/Awari/` から、使っているものだけをコピー) |
| `tests/` | ヘッドレスのテストと、スクリーンショット |
| `tools/` | Unity のアセットから `data/` を書き出したスクリプト(移行に使ったもの) |

Unity 版との対応は [../UNITY_TO_GODOT.md](../UNITY_TO_GODOT.md)。

## 調整のしかた

Unity 版の Inspector とシーン上のハンドルによる編集の道具は、Godot には移していない(手順は `docs/archive/20261116/guides/editing.md`)。今は JSON を直接書き換える。保存したら、Godot で遊び直せば反映される(エディタの再起動は要らない)。

| ファイル | 中身 |
|---|---|
| `data/awari.json` の `moves` | 技ごとの値。`startup` / `active` / `recovery`(発生・持続・硬直のフレーム)、`attack_level`(攻撃の強さ A)、`center` / `half_size`(判定。`[厚み, 高さ, 前方]` の順で、使うのは高さと前方)、`chains`(キャンセルできる技)、`damage_multiplier` / `chip_multiplier` / `pushback_scale`、`sprites`(画像列と各画像の表示フレーム数 `hold`) |
| `data/awari.json` の `motions` | 動作(Idle、歩き、しゃがみ、ダッシュ、バックステップ、空中ダッシュ、ジャンプ)の画像・表示フレーム数・表示の位置・食らい判定 |
| `data/awari.json` の `definition` / `locomotion` / `air_dash` | 歩き・ダッシュの速度や加速、ジャンプの高さと滞空、バックステップ |
| `data/awari.json` の `hurtboxes` / `throw` / `provisional` | 食らい判定の防御の強さ、投げ、仮の画像(ヒット・ガード・空中技など) |
| `data/system.json` | 共通のルール。`guard`(直前ガード・FD の押し返し)、`tg`、`combat`(割る数・押し返しの比率)、`combat_test`(HP 2300、開始時の TG)、`display`(カメラと HUD)、`input`(既定のキー) |
| `data/sprites.json` | 画像ごとの基準点(pivot)と、1m あたりのピクセル数(PPU)。Unity の取り込み設定から写したもの |

- `command` や `attack_class` などの数字は、Unity の enum と同じ番号(`scripts/core/sk.gd` の `Cmd`、`AttackClass`)。
- 画像を足すときは `assets/awari/` に PNG を置き、`data/sprites.json` に基準点と PPU を足し、技の `sprites` か動作の `poses` に書く。
- `tools/export_unity_data.py` をもう一度実行すると、`data/` が Unity のアセットの値で上書きされる。Godot で調整を始めたら実行しない。

## 保存ファイル

ユーザーの設定は Godot の `user://` に保存する。Windows では `%APPDATA%\Godot\app_userdata\Shioka\Profiles\`。

- `KeyConfig-v1.json`(キー設定)、`PlayerPreferences-v1.json`(選んだキャラ)。中身の形は Unity 版と同じ。
- Unity 版の保存ファイルは引き継がない(必要ならファイルをコピーすれば読める)。
