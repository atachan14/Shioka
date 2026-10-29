# 弱P v14導入

> 2026-10-29 現在: 以下は導入時の値・検証記録です。現在ユーザー調整中の5LPは6/3/10F、hold計19F。ここにある25.2F/24Fを現在値に戻さないでください。 現在状態は [STATUS](STATUS.md)、全体入口は [README](README.md)。

ユーザーがDownloadsへ展開したv14のmanifestを照合。12原本すべてSHA256一致、1024x1536 RGBA、alpha0..254。00/01を除く02..11の10枚を再生。灰色背景のGIFは使用しない。画像原本にリサイズ／描き直し／背景合成なし。

## 保存場所
- SourceArt/Awari/5LP/v14/frames: 12原本。manifest／元READMEも保存。Assets外なので自動検出・ビルドには含めない。
- Assets/Shioka/調整Data/Fighter/Awari/Skill/5LP/Sprite: 再生10枚のみ。
- 同じ技のData.asset: 共通フォルダ検出・同期で参照列を保存。
- LegacySprite/WeakP.png: 従来単画像とmetaを保持、旧fallback参照もGUID不変。
- SourceArt/Awari/5LP/pre-v14/Data.asset.txt: 導入前データの復旧用コピー。本物の別SOではない。

## 暫定表示F
02=2、03=2、04=3、05=5、06=2、07=2、08=2、09=2、10=2、11=2。計24F（400ms）、以後は技終了まで最終画像を保持。05はframe7..12に置き、既存active7.2..12に伸ばした腕を暫定対応させた。これは最終承認済みのphase設計ではなくEditorで調整する出発点。レビューGIF660msから換算していない。

元の発生7.2F／持続4.8F／硬直13.2F（計25.2F=420ms）と攻撃値・HitBox・HurtBox・本体移動は変更していない。画像列以外のserialized技データは導入前コピーと一致。

## 位置・縮尺
700PPUと既存1.55m body calibrationを共有。基準neutral同士の白い靴領域は横に103pxの平行差があったため、共通pivotX=(0.55*1024−103)/1024=0.4494140625で補正。各コマのalpha128以上の靴底row1500,1502,1506,1501,1502,1501,1502,1503,1500,1500をroot接地へ合わせる。ポーズの透明外接高さを1.55mへ正規化していない。生成された細部・脚の微小変動は原本どおり。

## 判定との既知のずれ
05の可視先端（alpha128の右端）から見積もる拳先はroot前方約0.619m。既存HitBox先端は1.05mで約0.431m長い。表示と判定は未一致。勝手に射程や攻撃値、画像拡大、本体前進で合わせていない。今後ユーザーがSpatial編集で判断する項目。

## 編集と戻し方
Data.asset → Sync detected images to draft → hold編集 → Save same skill asset。画像欄は検出結果のみ。保存後にSpatialを開くと同じ列を表示する。旧表示へ一時復旧するなら導入前Data.asset.txtを元Data.assetへ戻す（metaは維持）。空列なら保持したLegacySpriteを使う。将来Syncを保存すると再び10枚を採用するので、その点に注意。完全復旧はGitの導入commitを戻す方法もある。

検証は実Sceneの論理入力／frameサンプルとCamera.Render。両向き10枚、接地／scale、自然時計の複数画像進行と技終了を確認。物理入力・実マウス操作は未検証。
