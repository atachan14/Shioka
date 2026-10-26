# Title → Fighter select → Training

入口: Assets/Shioka/Scenes/Title.unityを開いてPlay。Editorメニュー Shioka > Play > Open title sceneでも開ける。Play-Shioka.cmdは正式projectを開く（最後に開いたSceneを勝手に切り替えない）。通常ビルドはTitleが先頭。従来CombatFormulaComparison.unityの直接Playも維持。

- TitleのTraining → キャラ選択 → Training開始／保存。現在ぁゎりのみ。他モードは非表示。
- W/S・Dpad／stickで選択、Menuの決定／取消（既定J/○、K/×）、Escで戻る。マウスも同じButton経路。案内は実KeyConfigから解決。
- キャラクリックは一時選択。Training開始でだけ安定IDをPlayerPreferences-v1.jsonへ明示保存。戻る／読込だけでは保存しない。保存できなければScene移動せず通知。
- 未定義IDや破損ファイルは既定キャラを一時表示し、開始前には元ファイルを保持。明示開始で選択を保存し直す。
- TrainingのOption左下『Titleへ戻る』でTitleへ戻る。途中の戦闘／録画状態はScene終了で破棄。KeyConfig未保存draftは既存の閉じる処理に従う。
- Trainingへは既存Sceneをロードするだけ。CatalogでPrefabを解決するが生成／置換しない。既存1Pと2P、調整差、対戦式を保持。現Sceneに接続済みID以外は選択不可。
- Scene移動時の決定キーはneutral gateで抑止。押し直しで通常入力へ戻る。

選択保存: Application.persistentDataPath/Profiles/PlayerPreferences-v1.json（KeyConfigと別）。履歴の自動記録や新しい対戦モードは実装しない。履歴APIはPROFILES_AND_FLOW.md参照。

確認範囲: 実Scene遷移・仮想Keyboard・Button.onClickによる自動検証。物理Keyboard／pad、実マウスは未検証。画像は自動Camera.Renderによる確認。通常版ローカルビルドの結果は作業記録に追記。
