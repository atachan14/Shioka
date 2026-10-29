# 画面の流れと保存

根拠はコード(`Runtime/Flow/`、`Runtime/Menus/`、`Runtime/Profiles/`、`Runtime/Persistence/`)。

## 画面の流れ

入口は `Assets/Shioka/Scenes/Title.unity`(ビルドの最初のシーン)。Editor のメニュー `Shioka > Play > Open title scene` でも開ける。`Play-Shioka.cmd` は正式なプロジェクトを開く。Training のシーン `CombatFormulaComparison.unity` を直接 Play することもできる。

- **Title**: 確定済みの Player 名、FighterSelect、Training、CommandList(未実装)、Option。Online / Offline は見出しで、Rank / Exhibition / Arcade / VS CPU は「(未実装)」として灰色で表示され、選べない。
- **FighterSelect**: 共通の選択画面を Player モードで開く。確定すると Player だけを保存して Title に戻る。
- **Training**: 同じ選択画面を CPU(ダミー)モードで開く。左上に確定済みの Player、右上に選択中の CPU。確定すると CPU だけを保存し、両方の確定した ID で Training に入る。
- 選択画面は、左右それぞれ2行×3列の12枠と、中央の Random。今は「ぁゎり」だけが選べる。Random は、実装済みで Training につながっているキャラだけが対象。
- カーソル移動・プレビュー・取消では保存しない。Player / CPU それぞれ、前回確定したキャラから始まる。保存できないときは、確定も画面の移動もしない。
- 操作は WASD / 方向キー / スティックと、メニューの決定・取消。Esc は、選択画面では Title に戻り、Title では Option を開く。マウスのクリックとホバーも同じように使える。
- **Option**: 製品の KeyConfig 画面を使う。Title から開くときは、Training のシーンを一度だけ読み込み、最初の戦闘フレームの前で止めて Option を開く。閉じると Title に戻る。Training 中の Option の「Title へ戻る」も使える。決定・戻るのボタンを押したままでも、戦闘には漏れない。

## 保存するもの

| ファイル(`Application.persistentDataPath/Profiles/` の下) | 中身 |
|---|---|
| `PlayerPreferences-v1.json` | 選んだキャラ。version 2。Player は `selectedFighterId`(旧形式との互換のため名前はそのまま)、CPU は `cpuFighterId`。Training のダミーと将来の VS CPU は、同じ CPU の選択を使う |
| `KeyConfig-v1.json` | キー設定([input.md](input.md)) |
| 対戦履歴 | 明示的に保存したローカルの対戦結果。最大1000件、同じ matchId は拒否。Training は対象外。実際の対戦結果を自動で集める仕組みはまだない |

- 保存は、明示的に確定・保存したときだけ。書き込みは一時ファイルを使って置き換え、`.bak` を残す。上限は4MiB。
- 読み込むだけではファイルを書き換えない。壊れた JSON・知らないバージョン・登録されていない ID のときは、表示用の安全な代わりの値を使い、元のファイルは消さない。
- 旧 version 1(Player だけ)は、CPU を既定のキャラで補って読み、次に確定したときに version 2 で保存する。
- 個人の設定は、調整用のアセット(ScriptableObject)には書き込まない。
