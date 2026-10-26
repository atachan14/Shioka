# KeyConfig

入口: CombatFormulaComparison を Play → Esc または右上 Option → KeyConfig。
Battle の「基本キー設定」は全キャラ共通の物理入力、「ぁゎり」はキャラ操作→slot の配置です。キャラ側は物理 binding を変更しません。

行を選択して Keyboard/Pad を入力します。同値は解除、単一の競合は確認後に交換します。T1/T2 単独では chord の登録を確定しません。Esc 自体も登録可能なので、捕捉取消はマウスボタンを使えます。方向入力はこの最小版では固定です。

変更は draft。Save で明示反映・保存、Cancel/Back/閉じるでは未保存変更を破棄します。Defaults も Save するまでは draft です。authoring SO は個人設定で書き換えません。
保存先: Application.persistentDataPath/Profiles/KeyConfig-v1.json。version=1、安定 action/character/operation ID を使用。既存保存は .bak を残して置換。欠損・破損・未知versionは既定値に戻し、元ファイルは保持します。

T1/T2 と通常操作の不正な物理重複は拒否します。Menu/Battle の妥当な共有は許可。複数行と一度に競合する登録は安全のため拒否し、個別に解除してから登録してください。設定変更後は保持入力を解除するまで戦闘へ流しません。

二人目のデバッグ用キーボードと機器ペアリングUIは対象外。D1/D2/D3 の技追加、Mania の新効果、対戦履歴等は未実装です。

検証: KeyConfig 49、入力基礎73、戦闘回帰146、仮想入力72、製品Play入力36項目すべて成功。仮想Keyboard/GamepadとUI Buttonイベントによる検証です。保存は専用一時profileで行い、ユーザー設定は変更していません。実物パッド、ユーザー物理キー、通常画面の目視・実マウスは未検証（computer-use app承認タイムアウト）。

開発用 Shioka/Development/Preview KeyConfig は未保存sceneを破棄せず通常UIを開く補助です。戦闘側の同時押し確定待ちは追加していません。
