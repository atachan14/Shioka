# Shioka docs

Unity で作っている2D対戦格闘ゲーム。2026-10-29 に作り直した docs(コード `106c62f` 時点)。

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
   - [editing.md](guides/editing.md) — Unity Editor での技・動作・共通パラメータの調整
5. そのほか
   - [USER_NOTES.md](USER_NOTES.md) — ユーザーに確認した作業上の扱い
   - [QUESTIONS.md](QUESTIONS.md) — 確認待ちの質問
   - [HISTORY.md](HISTORY.md) — docs の経緯と旧資料の場所

## この docs の書き方

- `CONCEPT`、`STATUS`、`spec/`、`guides/` は、いつも**今の状態**に書き直す。古くなった記述は追記で上書きせず、消して書き直す(履歴は Git に残る)。
- 作業の記録や検証の細かい結果は、ここには書かない。
- 作り直しや大きな整理のときは、docs を消さずに `docs/archive/<日付>/` に移す。
