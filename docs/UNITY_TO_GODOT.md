# Unity 版から Godot 版への移行

2026-11-16 に、Unity 版(コード `973a086` 時点)を Godot 4.7(GDScript)に移した。これ以降は Godot 版(`godot/`)が正。Unity のプロジェクトは参照用に残している(消さない、更新しない)。

方針は、**操作の手ざわり(フレーム数・タイミング)を Unity 版と同じにすること**を最優先にした。Unity の Training シーン(`CombatFormulaComparison.unity`)で実際に動いていた経路だけを移し、使われていなかった古い経路は移していない。

## 対応表

| Unity(`Assets/Shioka/` の下) | Godot(`godot/` の下) |
|---|---|
| `Runtime/Battle/Fighters/TrainingFighter*.cs`(本体と部分クラス) | `scripts/core/fighter.gd` |
| `TrainingMotor2D` | `scripts/core/motor.gd` |
| `CombatV01Formula`、`TrainingHitFormula`、`TrainingContactOrder` | `scripts/core/combat_formula.gd` |
| `CombatTestV01`、`CombatV01ContactBatch`、各スクリプトの実行順 | `scripts/core/match.gd` |
| `TrainingCommandBuffer` | `scripts/core/command_buffer.gd` |
| `TrainingTension` | `scripts/core/tension.gd` |
| `TrainingAirActionClock` | `scripts/core/air_clock.gd` |
| `TrainingCameraBounds` | `scripts/core/camera_bounds.gd` |
| `ArmoredStaticPoseView`、`TrainingProvisionalPoses`、`TrainingMotionAction`、`TrainingSpriteSequence`、`TrainingBodyCalibration` | `scripts/core/fighter_view.gd`、`scripts/core/game_data.gd` |
| 調整データ(ScriptableObject、`調整Data/`) | `data/awari.json`、`data/system.json`、`data/sprites.json` |
| 画像(`調整Data/Fighter/Awari/**/*.png`) | `assets/awari/`(使っているものだけ) |
| `TrainingSession`、`TrainingRecoveryClock` | `scripts/training/training_session.gd` |
| `TrainingRecorder` | `scripts/training/training_recorder.gd` |
| `TrainingDummy`、`TrainingDummyCommand` | `scripts/training/training_dummy.gd` |
| `TrainingFrameBar`、`TrainingFrameHistory` | `scripts/training/frame_bar.gd` |
| `TrainingCommandHistory` | `scripts/training/command_history.gd` |
| `MatchHud.ContactState` | `scripts/training/contact_state.gd` |
| `MatchHud`、`TrainingOverlay.*`(HUD・Option・KeyConfig 画面) | `scripts/ui/training_ui.gd` |
| `TrainingOverlay`(当たり判定の線)、`TrainingStage`、`TrainingCancelEffect` | `scripts/ui/training_scene.gd` |
| Unity Input System のイベント | `scripts/input/input_hub.gd`(自動読み込み `InputHub`) |
| `ProfileDeviceInput`、`ModifierPressOwnership` | `scripts/input/profile_device_input.gd` |
| `TrainingDeviceInput`(2P のテンキー) | `scripts/input/second_device_input.gd` |
| `TrainingControls`、`TrainingSecondPlayerControls` | `scripts/input/player_controls.gd` |
| `TrainingMenuInput` | `scripts/input/menu_input.gd` |
| `KeyBindingCapture` | `scripts/input/key_capture.gd` |
| `KeyConfigProfile`、`KeyConfigService`、`KeyConfigStore`、`CharacterCommandResolver` | `scripts/input/key_config.gd` |
| `LocalProfileFile` | `scripts/local_file.gd` |
| `TrainingFrontMenu` | `scripts/ui/title_menu.gd` |
| `TrainingFlowSession`、`TrainingFlow`、`PlayerPreferencesStore`、`TrainingSceneEntryGate` | `scripts/flow.gd`(自動読み込み `Flow`)、`scripts/ui/training_scene.gd` |
| `Tests/`(EditMode / PlayMode) | `tests/run_tests.gd`(一部。内容は [guides/godot.md](guides/godot.md)) |

## 移さなかったもの

- **Editor の道具**(`Editor/`): Inspector での技・動作の編集、シーン上のハンドル、導入・検査のスクリプト。Godot では JSON を直接編集する([guides/godot.md](guides/godot.md))。
- **開発用の機能**(`Development/`): 3P の式の比較(`TrainingCombatComparison`)と Motion tuning の画面。どちらも Training シーンでは無効になっていた(`CombatTestV01` があると比較は止まり、Motion tuning も切られる)。
- **使われていなかった経路**: 3D・Humanoid の表示、v0.1 より前の戦闘の計算(Training シーンでは常に v0.1 が有効だった)、6ボタン時代の入力(入力の版 1・2)、`ArmoredStaticPoseInput`(入力の版 3 では何もしていなかった)、技の空間プロファイル(どの技も未設定)、攻撃の強さの分布(どの技も無効)、ダウン追い打ち(共通ルールで無効)。
- **対戦履歴の保存**(`LocalMatchHistoryStore`): 使う画面がなかった。
- **ワールド上の防御の強さの表示**: Unity でも既定で非表示(`showWorldDefenseLabels` が false)。

投げのダウン・起き上がりなど、v0.1 の経路から今も呼ばれている古い計算は移している。

## Unity 版と違う点

- **数値の精度**: Unity の float は32ビット、GDScript は64ビット。フレームの境目が同じになるように、時間を足したり比べたりするところは32ビットに丸めている。このため、Unity 版と同じく **5HK の全体は 57F**(データ上は 20+6+30=56F だが、32ビットの誤差で1F延びる。ほかの技は表どおり)。位置や速度の一部は64ビットのままなので、ミリ単位の差は出うる。
- **床との当たり**: Unity は Physics2D のカプセルの判定で床に立っていた。Godot では、Training の床(高さ0の平面)に対して同じ規則を計算で再現した。接触のすき間(0.006m)は Unity の既定値から。
- **攻撃判定と食らい判定の重なり**: 箱どうしの重なりで判定する。Unity(Box2D)は形のわずかな厚みを持つので、ちょうど端が触れるくらいの距離では、当たる・当たらないが変わる可能性がある。
- **1P と 2P の処理の順番**: Godot では毎フレーム必ず 1P → 2P。Unity では、同じ実行順のスクリプトどうしの順番は決まっていなかった。押し合いなどでわずかな差が出うる。
- **入力の読み取り**: Godot の入力イベントから、Unity の Input System と同じ形(機器ごとの押下・T1 の状態)を作っている。スティックのデッドゾーン(0.125〜0.925)、トリガーの押した判定(0.5、離した判定 0.375)は Unity の既定値に合わせた。ゲームパッド以外のジョイスティックは未対応。
- **メニューの操作**: Godot の標準のメニュー操作(方向キー・パッドの × で決定など)は切り、Unity と同じく KeyConfig の決定・取消と WASD / 十字キー / スティックで動かす。Option の中で、方向で次の項目を選ぶ規則と、K(戻る)の動き(左の項目に戻り、そこでもう一度押すと閉じる)は、Unity の `TrainingOptionNavigation` を細部まで写していない。値の編集(HP・TG)は「J で編集、W/S ±10、A/D ±1、J で決定、K で取消」。
- **画面**: 1600×900 の論理座標で、16:9 を保って拡大縮小する(Unity の 16:9 のビューポートと同じ配置)。文字は OS のフォント(Yu Gothic UI など)。2人が重なったときは 1P が手前。
- **追加したもの**: Training settings に「2P: dummy / keyboard」の切り替えを足した。Unity 版にも 2P をテンキーで動かす仕組みはあったが、切り替えるボタンが画面に出ていなかった。
- **保存先**: `user://Profiles/`(Windows では `%APPDATA%\Godot\app_userdata\Shioka\Profiles\`)。ファイル名と中身の形は Unity 版と同じ。Unity 版の保存ファイルは引き継いでいない。

## 確かめられなかった点(ユーザーに確認してほしいこと)

- **実機での操作感**: キーボード・パッドで実際に遊んだときの手ざわり。特に、パッドの T1 同時押し、前ダッシュの2回押し(0.25秒)、TRC の後に「一度すべて離す」まで入力が効かない動き(Unity 版と同じ作り)。
- **パッドのボタンの対応**: Godot(SDL)の配置で、□ = West、△ = North、○ = East、× = South、L1/R1 = 肩、L2/R2 = トリガーとして扱っている。PlayStation のパッドで名前どおりに動くかは未確認。
- **ジャンプの滞空**: Godot 版では、空中に上がってから着地までが 53F(設定は 54F)。Unity 版での実測値と比べていない。
- **見た目の位置**: 画像の基準点と大きさは Unity の取り込み設定から写したが、Unity の画面と並べての比較はしていない(スクリーンショットで配置を確かめただけ)。
- **ビルド**: 書き出し(exe など)の設定はまだ作っていない。

## 移行のときに気づいたこと

- 空中の通常技は、ボタンの割り当てどおりに動かすと jLK・jMK・jHP・jHK・j2HK の5つ。空中で LP・MP を押しても何も出ない(Unity 版も同じ)。jLK・jMK には技のデータがなく、攻撃の強さはキャラの既定値 60 が使われる。
- 3HK(しゃがみ前 + HK)は足払い(`Sweep`、下段)になる。v0.1 の計算ではダウンしない(ダウンを起こすのは投げだけ)。
