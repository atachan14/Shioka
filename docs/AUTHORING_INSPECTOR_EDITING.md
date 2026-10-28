# Inspector の技・動作データ編集

Play を停止し、Unity のコンパイル完了後に対象 asset を選び直してください（別の asset を一度選択して戻れば十分です）。Project や Scene の再作成は不要です。

## 5LP

`Assets/Shioka/調整Data/Fighter/Awari/Skill/5LP/Data.asset`

- `Startup Frames` / `Active Frames` / `Recovery Frames`: 発生・持続・硬直。既存値は 7.2 / 4.8 / 13.2 F。今回変更していません。
- `Detected sprites (read only) | integer hold F`: 画像名は読み取り専用、右端の hold F が編集対象。画像は同じ技の Sprite フォルダから検出。ファイルを増減した場合は `Sync detected images to draft`。
- `Save same skill asset`: 明示的に保存。`Cancel / reload`: 未保存分を破棄。選択を変更した場合も未保存 draft は破棄されます。
- 画像 hold は正の整数。画像列の合計時間を変えても技の発生・持続・硬直は変わりません。
- `Open spatial editing for this skill`: draft が未保存／画像列が不正なら無効。保存または取消後に使用。Scene 側は画像位置・本体移動・HitBox・HurtBoxとフレーム位置のプレビュー用で、技時間や画像 hold の入力欄はありません。Scene 保存は `Save authoritative SO`。

## 移動動作

例: `調整Data/Fighter/Awari/Move/ForwardWalk/ForwardWalk.asset`。同じ編集禁止フラグの問題を修正しました。変更は `Save same asset` で保存、`Cancel / reload` または選択変更で未保存分を破棄します。速度などの移動性能はこの asset の編集対象ではありません。

## 2026-10-28 修正・検証

編集用 draft の HideAndDontSave には NotEditable が含まれていたため、SerializedProperty が編集不可になっていました。Skill / Motion Editor の draft から NotEditable だけを除外。非保存フラグと Save / Cancel、Play 中制限を維持し、プレビュー専用オブジェクトのフラグには触れていません。

Unity 6000.4.1f1 の隔離検証コピーで、修正前の編集不可を 4 件再現。修正後の編集・Save・Cancel・再選択・元 asset 不変を 24 件、画像列回帰 26 件、フォルダ同期回帰 24 件、実 Play 中の Skill / Motion 保存拒否と元 asset 不変を 3 件確認。コンパイル・各プロセス exit 0。正式 asset の値・画像は変更していません。

これは Inspector が使用する SerializedProperty と同じ経路での自動検証です。正式 Editor の未保存状態は維持し、実マウスでの Inspector 操作や画面撮影はしていません。