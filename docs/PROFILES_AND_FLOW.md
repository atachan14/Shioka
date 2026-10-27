# 選択キャラ・履歴の最小土台

これはデータ/API基盤。TitleMenu／FighterSelect画面、キャラ生成・対戦結果の自動収集、オンライン認証は未実装。

- 調整Data/Fighter/Catalog.asset: Awari.prefab参照だけ。キャラ安定IDはcharacter profileから解決。
- Runtime/Profiles/PlayerPreferences: version1＋selectedFighterId。
- Runtime/Flow/TrainingFlowRequest: 選択IDからPrefabを解決するだけ。存在しないIDは既定キャラへ一時fallbackし保存値は書き換えない。
- Runtime/History: 明示的なローカル対戦結果。Trainingは記録対象外。最大1000件、重複matchIdを拒否。サーバーで保証された履歴ではない。
- Runtime/Persistence: 明示Save/Append時だけJSON保存。通常LoadやPlay開始では書かない。4MiB上限、version/値検証、壊れたファイルを残してfallback。置換時.bakを残す。

既定保存先はApplication.persistentDataPath以下の Profiles/PlayerPreferences-v1.json と History/Matches-v1.json。既存KeyConfig-v1.jsonとは別。今回の検証は一時パスだけを使用し、実ユーザー保存には触れていない。

本番の画面遷移／一般化した生成へ接続する前に、現在1Pだけに適用されるlocomotion調整を2Pへ統一するか決める。無断で両者の挙動を変えない。

## 後続段階
Title／FighterSelectから既存Trainingへの最小画面導線は追加済み（TRAINING_FLOW.md）。ここまでの「画面未実装」は保存API導入時の記録。一般化したキャラ生成は行わず既存Scene役割を保持するため、1P/2Pの移動統一判断を待たず利用できる。履歴の自動記録／Onlineは引き続き未実装。

移動統一の後続記録: 現在は同じAwari Prefabから両者に採用locomotion SOを適用するため、1P/2P差の確認待ちは解消。一般化したキャラ生成を今回追加したわけではない。
