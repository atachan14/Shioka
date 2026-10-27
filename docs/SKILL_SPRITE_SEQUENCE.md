# 技画像列の編集

入口: Assets/Shioka/調整Data/Fighter/Awari/Skill/5LP/Data.asset。弱Pに限らずTrainingMoveData共通の構造。現在の5LPはv14の02..11を導入済み。従来単画像はLegacySpriteへ保持。未設定の他技は従来表示。WEAKP_V14_IMPORT.md参照。

## 操作（フォルダ検出）
各Skill/<技>/Data.assetの隣にSpriteフォルダを置く。既存SkillのSpritesはSpriteへGUIDを保って統一した。両方あれば同期を止め、二重に読まない。Sprite配下を再帰検出するため、原本／不採用画像／旧版退避はこの外へ置く。

検出はEditorだけ。Inspectorに検出数、追加／削除／rename／順序の差を表示する。Sync detected images to draftで自然数順の列を一時draftへ反映し、表示Fだけを編集する。画像欄は読取専用で、外の画像の手動追加や配列順の手動入替はしない。順序を変えるならファイル名を変更して同期する。1,2,10の順で並ぶ。

同期はauthoring assetを保存しない。Save same skill assetで採用、Cancel / reloadや選択を閉じると未保存draftを破棄。検出差分とUNSAVEDは別で、Cancel後もフォルダに未反映差分があることは表示される。空列は従来表示のまま保存できる。列がある場合、未同期／欠落／別技画像の混在を保存時に拒否する。

holdは画像GUID＋Sprite local file IDで保持する。削除した画像のholdも記憶し、同じIDを復元すれば戻る。同名でも別GUIDの新画像には流用しない。追加画像は暫定1F。新しいPNGの初回importのみSprite/Single、透過、mipmap無しを既定にする。既存または同梱.metaのPPU・pivot等は上書きしない。PPU/pivotを自動身体高正規化する機能ではなく、新素材の校正は別途必要。すでにTextureとしてimport済みのものはUnity InspectorでSpriteへ設定する。

保存後、Preview frameを選びOpen spatial editing for this skill。未保存draftがある間はこのボタンを無効化。Sceneで同じ列・時計・反転を使用し、画像位置／HitBox／HurtBox／本体移動を編集する。Spatial Saveは同じData.assetへ反映する。画像列の長短で戦闘数値は自動変更しない。

## 時間の定義
- 単位は60HzのF。技の開始をframe0とし、既存の技経過時計（ground timer、air action clock）×60と同じ値を使う。Hitstop／Block等で既存時計が止まれば表示列も止まる。入力の時計とは分け、描画時間で勝手に進めない。
- hold2、hold3なら画像区間は[0,2)、[2,5)。小数frame7.2等を技側から差し引いたり丸めて補正しない。浮動小数の誤差対策として整数の1e−4F未満近傍だけ整数境界へ寄せる（画像選択のみ）。小数の技終了も1e−4F未満近傍は終了扱いにし、積算float誤差で最終画像を残さない。
- 画像列が短ければ最後を技終了まで保持。長ければ技終了で打切り。技時間への均等割り／速度の自動伸縮／ループはしない。技範囲外は通常表示へ戻す。描画fpsが低いと短い画像区間が画面に描かれない場合はあり、画像枚数でsimulationを遅らせない。
- 現在の弱Pデータは発生7.2F／持続4.8F／硬直13.2F（計25.2F）。これは既存float Fのまま。整数画像holdとの割切れは不要。first renderは既存戦闘step後の時計位置を表示し、追加の描画用1Fずらしは行わない。
- 空列／未設定なら既存互換表示。実行時に不正列が来てもnullを返して互換表示へ戻す。

通常Playはauthoring assetを保存しない。検証は一時assetと一時runtime値で行い、採用済み5LP数値・画像と移動統一は変更していない。実マウスでの並替／Scene dragは未検証。

## 新弱P素材の状態
ユーザー提供ローカル原本から12枚を照合・保持し、再生10枚だけを5LP/Spriteへ導入済み。暫定24Fの表示列と既存25.2Fの技時間は別。660msレビューを換算していない。判定の先端と拳には約0.43mの差が残る。詳細と復旧手順はWEAKP_V14_IMPORT.md。
