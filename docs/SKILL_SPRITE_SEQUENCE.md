# 技画像列の編集

入口: Assets/Shioka/調整Data/Fighter/Awari/Skill/5LP/Data.asset。弱Pに限らずTrainingMoveData共通の構造。現在の採用Dataは空列のままで、旧WeakP.pngとactive時の単画像表示を維持。v14は導入していない。

## 操作
Playを止め、Inspector先頭のSprite orderで＋して画像を指定、ドラッグで並替、右のhold Fを編集する。最低1F、最大256画像／合計36000F。各行はspriteと整数holdのみ。画像位置／HitBox／HurtBox／本体移動は既存Spatial側の責務で、二重offsetを増やさない。

Inspectorは技全体の一時draft。Save same skill assetで同じData.assetへ明示保存、Cancel / reloadや選択を閉じると未保存draftを破棄。無効な画像／holdは保存を拒否。画像列の編集は発生・持続・硬直を自動変更しない（他の技フィールドを自分で編集した場合だけその値を保存）。

保存後、Preview frameを選びOpen spatial editing for this skill。未保存draftがある間はこのボタンを無効化し、保存／取消してから移る。既存Sceneの画像プレビューも同じ技画像列・同じframe位置を使用し、左右反転とSpatialの画像offsetを適用。判定／本体移動のSaveは従来どおり同じData.assetのSpatial subassetへ保存。プレビューは戦闘を進めたりhitを再実行しない。

## 時間の定義
- 単位は60HzのF。技の開始をframe0とし、既存の技経過時計（ground timer、air action clock）×60と同じ値を使う。Hitstop／Block等で既存時計が止まれば表示列も止まる。入力の時計とは分け、描画時間で勝手に進めない。
- hold2、hold3なら画像区間は[0,2)、[2,5)。小数frame7.2等を技側から差し引いたり丸めて補正しない。浮動小数の誤差対策として整数の1e−4F未満近傍だけ整数境界へ寄せる（画像選択のみ）。小数の技終了も1e−4F未満近傍は終了扱いにし、積算float誤差で最終画像を残さない。
- 画像列が短ければ最後を技終了まで保持。長ければ技終了で打切り。技時間への均等割り／速度の自動伸縮／ループはしない。技範囲外は通常表示へ戻す。描画fpsが低いと短い画像区間が画面に描かれない場合はあり、画像枚数でsimulationを遅らせない。
- 現在の弱Pデータは発生7.2F／持続4.8F／硬直13.2F（計25.2F）。これは既存float Fのまま。整数画像holdとの割切れは不要。first renderは既存戦闘step後の時計位置を表示し、追加の描画用1Fずらしは行わない。
- 空列／未設定なら既存互換表示。実行時に不正列が来てもnullを返して互換表示へ戻す。

通常Playはauthoring assetを保存しない。検証は一時assetと一時runtime値で行い、採用済み5LP数値・画像と移動統一は変更していない。実マウスでの並替／Scene dragは未検証。
