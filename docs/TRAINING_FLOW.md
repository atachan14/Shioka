# Title / Player / CPU selection

入口は `Assets/Shioka/Scenes/Title.unity` を開いて Play。Editor メニュー `Shioka > Play > Open title scene` でも開けます。`Play-Shioka.cmd` は正式プロジェクトを開きます（既存の未保存 Scene を自動で切り替えません）。ビルドの先頭 Scene は Title。`CombatFormulaComparison.unity` の直接 Play も残しています。

- Title 左列: 確定済み Player 名、FighterSelect、Training、CommandList（未実装）、Option。
- Online / Offline は見出し。Rank / Exhibition / Arcade / VS CPU はグレーの「（未実装）」表示で決定不可。
- FighterSelect は共通選択画面の Player モード。確定すると Player だけ保存して Title に戻ります。
- Training は同じ画面の Dummy（CPU）モード。左上は確定 Player、右上は選択中 CPU。確定すると CPU だけ保存し、両方の確定 ID を Training へ渡します。
- 下部は左右各 2 行 × 3 列の 12 枠＋中央 Random。現在はぁゎりだけ実装。残りは決定不可。Random は実装済みかつ現在の Training に接続済みの Fighter のみ対象。
- カーソル移動・プレビュー・取消では保存しません。Player / CPU ごとに前回確定した Fighter から開始します。保存不能なら確定・遷移を止めます。
- WASD / Dpad / stick と現在の Menu 決定・取消 binding を使用。Esc は選択中は Title へ戻り、Title では Option を開きます。Mouse のクリック・hover も同じ選択状態を使います。
- Option は既存の製品 KeyConfig UI を再利用します。実装上は Training Scene を一度だけ読み、最初の戦闘 tick より前に停止して Option を開きます。閉じると Title へ戻ります。別の戦闘インスタンスを重ねたり、選択を保存したりしません。独立した Option Scene の新設はしていません。
- 通常 Training の Option にある「Title へ戻る」も維持。Scene を破棄し、次回入場時に新しい 2 Fighter を使用します。決定・戻るの保持入力は neutral gate で戦闘へ漏らしません。

## 保存

`Application.persistentDataPath/Profiles/PlayerPreferences-v1.json` を引き続き使用します。中身は version 2、`selectedFighterId`（Player、旧フィールド名を互換維持）と `cpuFighterId`。Dummy 独立データはありません。将来 VS CPU も `cpuFighterId` を使う想定です。

旧 version 1 は Player を保持し、CPU は Catalog の既定キャラでメモリ内補完。Load だけでは書き換えず、明示確定時に version 2 として保存します。壊れた JSON / 未対応 version / 未定義 ID は安全な表示用 fallback を使い、Load や取消で元ファイルを失いません。片方の確定は他方の保存 ID を保持します。KeyConfig-v1.json と技・移動 SO は別で変更しません。

## Training のキャラと描画

現在の Catalog と実行可能キャラはぁゎりのみ。既存 Scene の 1P/2P は同じ Awari Prefab を使い、役割・入力主体・採用済み数値は維持します。選択 ID が接続済みか検証し、未実装キャラの架空の対戦生成は行いません。複数キャラ追加時の Prefab 置換は別の実装段階です。

CPU には旧追加画像への `image` Scene override が残り、Prefab 自体の承認画像も有効でした。追加画像とその override だけを除去し、Prefab の単一の表示 Renderer を使用します。コライダーやキャラ画像原本は削除しません。

## 検証入口

`TrainingFrontChecks.Run` は隔離した保存先と仮想 Keyboard で、保存分離・旧形式・取消・Random・未実装枠・画面遷移・Option 往復・再入場・直接 Training・描画/判定所有を検証します。`-frontSavedPreferencesFile` 指定で別 Unity プロセスの復元も検証可能。実マウス・物理パッドでの操作確認とは区別します。

## 2026-10-28 検証記録

Unity 6000.4.1f1 / D3D11 の Editor batch Play で検証。正式 G の Unity を開いたまま保護するため、Assets・Packages・ProjectSettings を隔離した検証コピーで実行しました。正式側との 754 ファイル比較で意味上の差分なし（ShaderGraphSettings.asset の改行だけ Unity が正規化）。正式 Editor 自身の再起動や実マウス・物理パッド操作は未検証です。

- 新画面・保存・仮想 Keyboard / Gamepad・往復・直接起動・描画/判定所有: 90 件。
- 別 Unity プロセスで保存復元: 3 件。Profile / history API: 18 件。
- 入力基礎: 73 件。戦闘式: 146 件。既存 KeyConfig UI: 59 件。製品入力経路: 36 件。
- 修正前 Scene の隔離再現: 4 件。Fighter は 2 体のまま、CPU にだけ有効画像 2 枚（うち駆動対象 1 枚）。修正後は Title 経由・直接・再入場・Title Option ホストとも本体 2 体、キャラ画像 2 枚。床 Sprite は別に数えています。
- 実描画 PNG: title-ui-front-results-{title,player,cpu,training,options}.png。修正前比較: title-ui-baseline-results-baseline-duplicate.png。1280×720 の Camera.Render を目視確認しました。物理画面のスクリーンショットではありません。
- テスト内で移動 SO・5LP v14 データ・個人 KeyConfig の前後バイト不変を確認。テスト保存先は隔離 Temp。

初回の空 Library import では同梱 ShaderGraph の GUID API 更新後に再起動が必要でした。テスト実行中も Editor SearchDatabase の起動時例外がログに残りますが、製品テストの例外・コンパイルエラーとは分離し、上記各プロセスは exit 0 で完了しています。この作業では無関係なパッケージや検索機能を改修していません。
