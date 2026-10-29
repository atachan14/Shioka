# 2D Editor / Scene整理（2026-10-28）

正式Sceneへ適用済みです。Unityが閉じていること、Sceneが準備時から変わっていないこと、5LPのユーザー調整が保護した内容と一致することを確認してから適用しました。

## 変更

- ProjectSettings/EditorSettings.assetの既定をMode2Dへ変更。既存Sceneビューの2Dスイッチ状態とは別です。既存ウィンドウでまだ立体視点なら、上部の2Dボタンで平面表示にできます。現在ウィンドウの表示を強制変更する処理は追加していません。
- CombatFormulaComparison.unityから旧Ground contact lineのCubeと、無効Plane／MeshColliderを除去。SceneのGUID、ゲームCameraの正投影、床Y0は維持しています。
- 旧Cubeと同じXY位置・大きさの平面SpriteマーカーをTrainingStageへ追加。中心(0,-0.02)、大きさ(0.06,0.04)、描画用Z=-5。旧Sceneを読む場合は既存Cubeを残し、マーカーを二重生成しません。
- 非稼働の旧Humanoid足接地partialとPhysics.RaycastNonAlloc／RaycastHitバッファを除去。現在のSprite描画・移動・戦闘値は変更していません。
- Directional Lightを除くと背景Skyboxの色が変わるため、このLightとGlobal Volumeは維持。一般のAnimationClip対応、Vector3互換データ、XY変換、描画順Z、平面の判定表示用Meshも維持しています。

## 保護したデータ

ユーザー調整の5LPは発生6F／持続3F／硬直10F、画像hold合計19F。作業前にリポジトリ外へ退避し、適用後のSHA256一致を確認しました。このユーザー差分は今回の整理コミットへ混ぜません。個人KeyConfigや他の保存データも変更していません。Play中リアルタイム調整は別件で、実装していません。

## 検証

Unity 6000.4.1f1／D3D11、正式Gプロジェクトでコンパイル・参照整合とPlayを確認。2D接触等22、Title往復・CPU重複なし90、入力基礎73、戦闘式146、製品入力36、移動一致66件を検証しました。Profile API18件も同時に実行。各プロセスexit 0。

隔離環境の同条件比較では、床マーカー・Camera・両Fighterを含む1280×720描画が旧Cube／Planeありの状態と完全一致（RGB差0）。正式環境でも旧Sceneと再比較し、補助線等の画素差（各RGB平均差0.46/255未満）はありますが、背景・床位置・キャラ配置を目視確認しました。完全一致の主張は隔離環境内に限定します。Lightを除くと背景色が変わるため、必要な依存として残しています。

検証はバッチPlay、仮想入力／論理入力、Camera.Renderによるものです。実マウス・物理パッド、通常GUIのSceneビュー表示は未検証です。現在の入力・画像・移動性能を変更する追加機能はありません。