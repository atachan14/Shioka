# Profile / selection / flow

- `調整Data/Fighter/Catalog.asset`: Fighter Prefab 参照。ID/名前はキャラの authoring profile に由来します。
- `Runtime/Profiles/PlayerPreferences`: version 2。Player は互換用フィールド `selectedFighterId`、相手選択は `cpuFighterId`。Dummy 別保存は作りません。
- `Runtime/Persistence/PlayerPreferencesStore`: `Profiles/PlayerPreferences-v1.json`。旧 Player-only 形式のメモリ内移行、明示保存のみ、取消時不変。詳細は TRAINING_FLOW.md。
- `Runtime/Flow/TrainingFlowRequest`: Catalog の安定 ID 解決。未定義 ID は表示用 fallback を返し、保存済み ID を勝手に置換しません。
- `TrainingFrontMenu`: 共通 Player / CPU 選択画面のモード切替。Player 確定後 Title、CPU 確定後 Training。
- `TrainingFlowSession / TrainingSceneEntryGate`: 確定した両者 ID の引渡しと neutral gate。Title Option の一時ホストも管理。既存の 1P/2P 役割や調整数値を上書きしません。
- `Runtime/History`: 明示的なローカル対戦結果、最大 1000 件、重複 matchId は拒否。Training は対象外。実対戦結果の自動収集・オンライン認証や履歴は未実装。
- `LocalProfileFile`: 4 MiB 上限、壊れた内容を残す fallback、明示書込時の atomic replace と .bak。個人設定は authoring SO に書き込みません。

Title のアートは余白のみ、将来モードは非活性表示。複数キャラの実戦生成、オンライン、実機全種検証はこの段階の完成対象ではありません。
