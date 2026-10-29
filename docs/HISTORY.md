# 旧資料と docs の経緯

## docs の経緯

- 旧非公開リポジトリ(3D の時代を含む)には docs が数十本あった。2026-10-25 に、今の公開リポジトリを旧リポジトリと履歴を共有しない形で作り直したとき、docs はほとんど引き継がれなかった。
- その後の docs は、作業ごとの記録が中心に積み重なった。2026-10-29 に Dot がコンセプト・現行仕様・状態の入口を復元した。
- 同じ日、Claude Opus 5.5 が、それまでの docs 一式を `docs/archive/20261029/` に移し、今のコードと旧資料とユーザーの確認をもとに、docs を作り直した(今の docs)。

## 旧資料の場所

| 場所 | 中身 |
|---|---|
| `docs/archive/20261029/` | 2026-10-29 までの docs 一式(作業記録、検証記録、Dot がまとめた入口を含む)。書き換えない。採用時の会話の索引(Slack のメッセージ ID)は、その中の `DOCS_HISTORY.md` にある |
| 非公開リポジトリ `Shioka-archive`(GitHub) | 旧リポジトリ。ブランチ `cleanup/obsolete-3d-20261025` に docs 54本(下の表)。`main` は初期の版 |
| ローカルの退役保管 `C:\Users\user\Documents\Codex\2026-10-22\task\Shioka-retirement-20261026` | 旧リポジトリの `old-history.bundle`、ソースと設定の `old-source-and-settings.zip`、`RECOVERY.txt`、`SHA256.txt` |

`Shioka-archive` の主な docs(ブランチ `cleanup/obsolete-3d-20261025`):

| ファイル | 中身 | 扱い |
|---|---|---|
| `docs/concept.md` | 初期のコンセプト | 意図の根拠。古い数式は今の仕様ではない |
| `docs/imported/shioka2/` | Shioka2 時代の原案(concept、combat_model、v0.1) | 歴史資料。旧名称・旧操作を含む |
| `docs/COMBAT_TEST_V01.md` | v0.1 の戦闘の合意 | 式の根拠。入力とパスはその後変わった |
| `docs/TRAINING_SESSION.md` | Training の詳細 | キー名は今の入力と照らし合わせる |
| `docs/MORNING_REQUIREMENTS_REVIEW.md` | 当時の要件と未決の整理 | 当時の記録 |
| `docs/*_TRIAL.md` など | 試作と検証の記録 | 当時の記録。今の正本ではない |

## 読むときの注意

- 旧資料は歴史資料として読む。旧リポジトリの履歴を今の公開リポジトリに混ぜない。
- 旧資料の「未実装」や検証の件数は、その時点の記録。今の状態は [STATUS.md](STATUS.md) を見る。
- 古い案と今のコードが違うだけで、「旧案は撤回された」と決めつけない。迷ったらユーザーに確認する。
