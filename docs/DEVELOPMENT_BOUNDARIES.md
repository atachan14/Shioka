# 製品・開発・検証の境界

現役: G:/source/repos/unity/Shioka。フォルダ名だけでビルド除外にはならない。

- Runtime/Battle: 共有戦闘・身体・表示。Runtime/Training: 製品の設定、ダミー、録画再生、入力履歴。v0.1の接触診断色もここに残す。
- Development/Runtime/UI: 旧比較／Motion試験用Optionのpartial実装。UNITY_EDITOR || DEVELOPMENT_BUILDでコンパイルを限定。通常版はRuntime側のoptional partial呼出しが消え、試験UIを生成しない。
- Development/Runtime/Authoring: Sceneに付くSpatialホスト。既存UNITY_EDITOR条件を維持。Sceneコンポーネントを単純にEditorへ移していない。
- Editor/Authoring: Inspector、Sceneハンドル、明示保存。Editor/Validation: 導入／参照／ビルド検査。
- Tests/EditMode/Editor と Tests/PlayMode/Editor: 明示起動のEditor検証。Editor特別フォルダが製品から除外する。GUIDを保った移動で、NUnit Test Runnerへの全面移行ではない。

比較互換型とcanonical locomotion適用コンポーネントは必要な依存を保って残る。『全開発コードを完全除去』という主張ではない。製品のTraining自体は開発専用ではない。
