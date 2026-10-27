# 同キャラの移動性能統一

## 原因として確認できたこと
公開履歴先頭15966deのCombatFormulaComparison時点で、SharedCharacterは共通でもApprovedLocomotionTuningは1Pだけに付いていた。300d06d配置整理でも保持し、042fbb2のPrefab化では既存挙動保持のため1P専用Scene追加componentのまま残した。最初に1P限定で追加した時の意図は現在の公開履歴からは分からず、意図したキャラ性能差とする根拠はない。

今回、その既存componentをAwari.prefabへApplyし、両個体が同じApprovedLocomotionProfile.assetを一度だけ適用する形にした。元の1P採用値／SOの数値は変更しない。役割入力・左右向きは別のまま。Play時のdefinitionは個体別複製なのでHP等の可変状態を共有しない。

## 変更前後の実効設定
| 項目 | 旧1P（採用済み） | 旧2P | 現在の両者 |
|---|---:|---:|---:|
| walkSpeed m/s |3.5|3.5|3.5|
| dashSpeed m/s |6|6|6|
| acceleration m/s² |30|30|30|
| dashDeceleration m/s² |10|10|10|
| backstepSpeed m/s |3.6|6|3.6|
| backstepSeconds |25/60−0.000001|0.18|25/60−0.000001|
| jumpVelocity m/s |約9.053498|9.5|約9.053498|
| gravity m/s² |約−19.753086|−25|約−19.753086|
| takeoffSeconds |0.1|0.1|0.1|
| landingSeconds |0.14|0.14|0.14|

採用SOはbackstep25F／1.5m、jumpAir54F／height2m、pose境界10/15F。ジャンプ初速の半step補正等も既存計算のまま。旧2Pの秒数を実際の量子化済み滞空Fや実距離と誤解しないこと。

## 調整元
- Move/ApprovedLocomotionProfile.asset: jump/backstepの採用値（Awari.prefabの共通componentが参照）。
- Data/SharedCharacter.asset: walk/dash/acceleration、ジャンプ移行／着地硬直等。古いjump/backstep基礎値はcanonical適用で上書きされる互換値であり、現役調整入口には使わない。
- Move/AirDashProfile.asset: 現在の空中dash設定。今回数値不変更。

これは小さい参照統一であり、全movementデータを一つの新SOへ全面再設計したものではない。画像/HurtBox用の個別motion assetとは責務を分ける。

## 検証
MotionParityChecksの66件成功。左右を入れ替え、非接触・ステージ端に届かない条件で速度、加速、dash減速、移動量、backstep、jumpのphase／高さを比較。backstep実測約1.499999m、jump頂点約2.000001m。最初の試験は互いの押し合い／ステージ端が混じったため開始位置を分離した。ゲームの衝突処理やステージ範囲は変更していない。論理入力fixtureによるPlay検証であり実キーボード／pad検証ではない。
最終回帰: 入力基礎73／戦闘146・参照検査／Title導線28／製品Play36成功。根拠: task/motion-parity-acceptance.txt、parity-{input,combat,flow,live}.txt。通常画面の物理操作は未検証。
