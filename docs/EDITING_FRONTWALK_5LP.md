# FrontWalkと弱Pの編集入口

すべてAssets/Shioka/調整Data/Fighter/Awari以下。Playを止めて操作する。

## FrontWalk
Move/ForwardWalk/ForwardWalk.assetを選択。Inspectorのposesでsprite、holdFrames（60Hz整数F）、visualOffset、hurtを編集。Open Scene visual editingのcyan=画像位置、green=選択HurtBox。Mirror leftで反転プレビュー。Save same assetで採用、Cancel/reloadで破棄。選択を閉じるだけでは保存しない。
4画像の4F×4は作業上の暫定値で未承認の周期。画像/HurtBoxを調整しても歩行性能は変わらない。HitBoxは無く、歩行を攻撃にする機能はない。部位IDはHurtboxes.assetに対応し、防御値・優先度はそちらに残る。

## 弱P (5LP)
Skill/5LP/Data.assetを選択。先頭のSprite orderで＋して画像を入れ、並替と各hold Fを編集できる。その他のdataでstartupFrames7.2／activeFrames4.8／recoveryFrames13.2、HitBox center／halfSize、攻撃値等を編集。現在は技全体が一時draftで、Save same skill assetを押して反映、Cancel/reloadで破棄。Ctrl+Sだけでは未保存draftを採用しない。
保存後にPreview frameを選びOpen spatial editing for this skill。未保存draftがある間は無効。既存Sceneでcommand Lightを選択済みで開き、同じ画像列をプレビューしながら、画像offset／本体移動／HitBox／HurtBoxをframe別draft編集する。SpatialのSaveは同じData.assetのSpatial subassetへ明示保存。旧互換座標はforward=Z、up=Yで、SceneハンドルはXYに変換して扱う。

画像列は技開始から再生し、短ければ最後を保持、長ければ技終了で打切り。合計Fで発生・持続・硬直を自動変更しない。小数の技Fと整数holdは割切れる必要がない。詳細はSKILL_SPRITE_SEQUENCE.md。

現在の採用5LPは空列なので、Skill/5LP/Sprites/WeakP.pngの旧単画像をData/AwariCharacter.asset経由でactive中だけ表示する。v14の10枚は未導入。列を明示保存した技だけが新しい表示を使う。通常Playからauthoring保存はしない。実マウスの並替／Scene dragは未検証。
