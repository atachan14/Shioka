# FrontWalkと弱Pの現在の編集入口

すべてAssets/Shioka/調整Data/Fighter/Awari以下。Playを止めて操作する。

## FrontWalk
Move/ForwardWalk/ForwardWalk.assetを選択。Inspectorでposesのsprite、holdFrames（60Hz整数F）、visualOffset、hurtを編集。Open Scene visual editingのcyan=画像位置、green=選択HurtBox。Mirror leftで反転プレビュー。Save same assetで採用、Cancel/reloadで破棄。プレビューを閉じるだけでは保存しない。
4画像の4F×4は作業上の暫定値で未承認の周期。画像/HurtBoxを調整しても移動速度は変わらない。歩行のHitBoxは無く、ここに追加して攻撃を作る機能はない。HurtBoxの部位IDは既存Hurtboxes.assetに対応し、防御値・優先度はそちらに残る。

## 弱P (5LP)
Skill/5LP/Data.assetを選択。通常InspectorのdataでstartupFrames7.2／activeFrames4.8／recoveryFrames13.2、center／halfSize（旧互換座標forward=Z、up=Y）、attackLevel等を編集する。値は既存のfloat Fで、実行は60Hz tickに従う。Inspector変更はUnityの通常asset保存（Ctrl+S等）。
Open spatial editing for this skillから既存Scene編集へ。command Lightを選択済みで開く。フレームを選び、画像offset／本体移動／HitBox／HurtBoxをdraft編集し、Saveで同じData.assetのSpatial subassetへ明示保存。既存hit/hurtの防御優先度／部位ID規則は維持。

未対応: 弱Pの画像列と各画像hold時間をData.assetで編集する機能。画像はSkill/5LP/Sprites/WeakP.pngの旧単画像で、Data/AwariCharacter.assetのweakPunch参照をactive中だけ表示する。startup/recoveryは既存表示へ戻る。v14の10枚は導入していない。技用Spatial編集で画像の位置は動かせるが、画像枚数や再生順を定義するものではない。

Sceneハンドルは生成／保存の自動検証済み。実マウスdragは未検証。画像・時間の新仕様を追加したという意味ではない。
