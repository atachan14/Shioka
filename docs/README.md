# Shioka docs

2D対戦格闘ゲーム。**2026-11-16 に Unity から Godot 4(GDScript)に移した。今後は Godot 版(`godot/`)が正。** Unity のプロジェクト(`Assets/` など)は参照用に残している。

- Godot での開き方・動かし方・調整: [guides/godot.md](guides/godot.md)
- Unity 版との対応・違い・未確認の点: [UNITY_TO_GODOT.md](UNITY_TO_GODOT.md)

docs は 2026-10-29 に作り直したもの(コード `106c62f` 時点)を、移行に合わせて Godot 版の内容に直した。

## 読む順番

1. [CONCEPT.md](CONCEPT.md) — 何を面白さの中心にするか
2. [STATUS.md](STATUS.md) — 今できること、未決のこと
3. 仕様(`spec/`)
   - [combat.md](spec/combat.md) — 接触・TG・ガード・押し返しの式
   - [input.md](spec/input.md) — ボタンの枠、既定の割り当て、同時押し、KeyConfig
   - [training.md](spec/training.md) — Training の設定・録画と再生
   - [flow-and-save.md](spec/flow-and-save.md) — 画面の流れと保存ファイル
   - [character-data.md](spec/character-data.md) — キャラのデータの持ち方とプロジェクトの構成
4. 手順(`guides/`)
   - [godot.md](guides/godot.md) — Godot での開き方・動かし方・テスト・調整データの編集
   - [UNITY_TO_GODOT.md](UNITY_TO_GODOT.md) — Unity 版との対応表、違う点、確かめられなかった点
5. そのほか
   - [USER_NOTES.md](USER_NOTES.md) — ユーザーに確認した作業上の扱い
   - [QUESTIONS.md](QUESTIONS.md) — 確認待ちの質問
   - [HISTORY.md](HISTORY.md) — docs の経緯と旧資料の場所

## この docs の書き方

- `CONCEPT`、`STATUS`、`spec/`、`guides/` は、いつも**今の状態**に書き直す。古くなった記述は追記で上書きせず、消して書き直す(履歴は Git に残る)。
- 作業の記録や検証の細かい結果は、ここには書かない。
- 作り直しや大きな整理のときは、docs を消さずに `docs/archive/<日付>/` に移す。
