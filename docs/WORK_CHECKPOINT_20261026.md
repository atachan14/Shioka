# 採用構造への移行・素材反映チェックポイント

現役 G:/source/repos/unity/Shioka。Play-Shioka.cmd → CombatFormulaComparison。pushなし。

## 完了
- 980aec2: 承認FrontWalk4枚を原本hash照合して導入。4Fずつ16Fループは暫定、歩行性能不変。
- 4be0909: Guard/TG/Combat共有調整asset、旧計算の同値移行。
- 47246b0: 8動作の個別asset入口。Inspector draft／Scene視覚編集／同asset明示保存。
- 5a57d0e: 20既存技を各Data.assetへ分離し画像と近接、カタログ参照、Spatial明示保存。
- 042fbb2: Awari.prefab共有、既存Scene役割overrideを保持。
- 続段階: 開発Option partialのコンパイル条件分離、Editor検証のTests移動、9旧共有値の互換SO化、選択キャラ／履歴の最小保存API。詳細はDEVELOPMENT_BOUNDARIES.mdとPROFILES_AND_FLOW.md。

## 今回最終コードの検証
- Unity6000.4.1f1 import/compile、シーン依存と2D身体／表示参照。
- 入力基礎73、戦闘式146、KeyConfig59、製品入力Play36、プロフィール18、個別動作Play24成功。
- 通常Windows64非Developmentビルド成功、errors0、387836690bytes。ローカル検証出力のみ、配布なし。実行起動／物理入力検査ではない。
- KeyConfig自動描画1280x720／640x360を目視。自動UI呼出しと仮想入力であり実マウス／物理パッドではない。
- Assets GUID重複0。ビルドで更新された4設定は退避して元へ復元。実ユーザーKeyConfig hashは不変。
- 根拠ログはtask作業領域のfinal-stage-{edit,keyconfig,live,release}.log/txtとprofile-foundation-checks.log。

## 確認待ち（未完）
- 新版弱P10枚の承認原本／順序／phase対応が未特定。現在の採用済み単一active画像は保持。Library検索0件は原本不存在の証明ではない。
- Downloads/Shioka-air-back-dash-v1.pngは透明PNGとして画素確認済み。しかし既存に後方空中dash経路なし。採用／入力／時間／距離の仕様を推測して導入していない。
- 移動性能統合: 現在1PのみApprovedLocomotionTuningがあり、2Pにはない。統一すると2P挙動が変わるため保留。未接続試作はtask/pending-motion-unificationへ復旧可能に保存。
- D1–D3技割当、Mania新効果／消費、Title/FighterSelect画面、Online等は未実装。FlowはIDとPrefabの解決APIのみ。
- 画像／HurtBoxのSceneハンドルは生成・保存回帰済みだが、実マウスdrag／実機操作は未検証。以前のcomputer-use承認timeoutを迂回していない。

旧ごみ箱／private archive／公開コピー／remoteと実ユーザー保存は不変更。無断の性能・計算式変更、新5MP候補導入はない。
