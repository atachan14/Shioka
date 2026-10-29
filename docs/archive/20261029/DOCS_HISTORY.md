# docs の移行と旧資料を読む方法

## 確認した経緯

新公開履歴の初回15966deには docs がありません。`PUBLICATION_SCOPE.md` は旧私有履歴を分離し、内部対応記録等を公開snapshotから除外したと記録しています。現行 docs は300d06d以降の構造整理・実装手順が中心です。新履歴で旧conceptを削除したcommitがある、という形ではありません。78241ccのroot README削除はユーザー依頼であり、今回もroot READMEを復活させていません。

旧資料は消失していません。ローカル退役保管 `Shioka-retirement-20261026` に `old-history.bundle` と `old-source-and-settings.zip`、RECOVERY.txt、SHA256.txtがあります。旧repoは私有 `Shioka-archive`、旧退役HEADは `1281e2f0f62ddd04abe0f1bcafd7294c742950c0`。bundleはこのHEADと複数旧branchを保持し、ZIP内には56件のdocsがあります。現行作業treeへ上書き復元する必要はありません。

Claude Codeも、権限のあるarchiveまたはZIP内の対象文書を直接読めます。旧repo/旧pathの資料は歴史資料として読み、新公開repoへ旧Git履歴を混ぜないでください。私有archiveの公開、認証取得、旧フォルダ復元は今回の作業に含みません。

## 最小の参照順

|目的|旧ZIP内の参照先|扱い|
|---|---|---|
|初期コンセプト|docs/concept.md|意図の根拠。古い数式は現仕様ではない|
|移行前の原案|docs/imported/shioka2/concept.md、combat_model.md、v0.1.md|元資料を保存した履歴。SDC/旧操作等を含む|
|後続の戦闘合意|docs/COMBAT_TEST_V01.md|式の重要根拠。ただし旧入力・旧pathは後続変更あり|
|Training詳細|docs/TRAINING_SESSION.md|符号付きTG・録画・Dummy等。キー名は現行入力と照合|
|2D移行の記録|docs/PHYSICS2D_MIGRATION.md、OBSOLETE_3D_CLEANUP.md|当時の実装/検証記録|
|古い実験/決定簿|docs/CURRENT_DECISIONS.md、各TRIAL.md|日付付きの旧試作記録。現在の正本ではない|

`docs/imported/shioka2/README.md` は原文コピーとsource-hashes.csvを説明しています。名前が似た別の現地docsは空ファイルの場合もあるため、上記ZIP/履歴の実体を確認してください。

## 今回補ったものと出典の違い

初期の「駆け引き・アクション・キャラ表現」「即興性」と、A/D・TG・残り硬直に応じたTRCの意図は旧資料に存在します。Dotだけの記憶ではありません。現行にはまとまった入口がなかったため [CONCEPT](CONCEPT.md) へ薄く復元しました。

現行v0.1式・負TG・直ガ時計・押し返しも旧資料とコードにあります。一方、最新の9枠、Delirium/Mania/Holdの名称、後押しT1成立、Player/CPU共通保存の意図、Play中調整が未承認という状態は、後続文書・コード・会話の照合が必要です。たとえば旧COMBAT_TEST_V01のU/弱PK FDは現在と異なります。会話のみに見える情報も、他の保存先にないとは断定していません。

採用会話の照合キー（Slack message ID）:

- 1792997893.299559 / 1792998081.038709: ABCJKLXYZ、Deliriumカテゴリー、確定待ち撤廃。
- 1793002129.287689 / 1793002267.594759 / 1793002348.111659: Hold改名と後押しT1の新規chord許可。
- 1793156727.305629 / 1793156770.263249: Player/CPU保存、Dummyと将来VS CPUでCPU選択共有。
- 1793162999.337349: Play中調整の相談（承認済み実装ではない）。1793163504.495679: 2D整理承認。

これらは今回親タスクから受領した信頼済み会話に基づく索引です。全文ログをrepoへ複製せず、矛盾や追加仕様は本人へ確認します。現コードと旧提案が違うだけで「旧意図は撤回」と断定しないでください。
