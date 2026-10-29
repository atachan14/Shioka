# 現行の暫定仕様

2026-10-29 の整理。バランス最終値ではありません。旧 `COMBAT_TEST_V01.md` / `TRAINING_SESSION.md`、現コードと共有 SO、および後続の採用会話を照合しています。出典区分は [DOCS_HISTORY](DOCS_HISTORY.md)。

## 接触と TG

接触解決の前に両者の TG を snapshot。同 tick の接触は共通の解決前値を使います。

```
M(T) = 1 + T/200                 (T >= 0)
       1 / (1 + abs(T)/100)      (T < 0)
H = A * M(attackerTG) / M(defenderTG) / (1 + D/100)
B = D
```

A は選択 HitBox の位置・持続補正後の強度、D は選択 HurtBox の位置補正後防御（非負）。A/D に TG を足しません。最高 HurtBox 優先度を先に選び、各候補で重なる攻撃の最弱 A を評価、同優先度ではその候補自身の D と A による H が最大のものを採用します。

方向・姿勢が合うガードは H にかかわらず Guard、ガードブレイクなし。それ以外は H <= B が Block（元の動作を停止後再開）、H > B は Hit（中断）。分類には丸め前の H/B を使います。

- 基本硬直 H/10 F。Guard は直ガ減算。最後に正の端数だけ切上げ、0 は 0、負は 0 に clamp。負の余剰ボーナスは未実装。
- Hitstop は `ceil(A * M攻 / M防 / 40)` F。H ではなく A を使い、双方を停止。行動・硬直・押し返しは停止し、入力と直ガの時計は進む。凍結中の同じ接触を毎 F 再ヒットさせない。
- ダメージは H×技倍率。通常既定 1、Guard/Block の chip 既定 0.1（技別調整）。FD は chip 0。小数 HP/ダメージを保持し、フレーム切上げを流用しない。chip でも KO 可。基準 HP 2300。
- 等 TG、D100、A260/360/460 の代表接触では H130/180/230、基本硬直13/18/23 F、Hitstop7/9/12 F。これは位置・持続補正後の代表接触でありピーク値保証ではない。

## 直ガ・FD・押し返し

新しいガード方向入力で age0。同じ方向保持では更新せず、非ガード方向で解除。向きの反転だけでは更新しません。通常 age0..29 の減算は `10-floor(age/3)`、連続ガード硬直中は age0..19 で `10-floor(age/2)`、範囲外0。Hitstop 中も age が進むため、次の攻撃直前の入力ほど強い直ガです。

K = H×技別 push scale（既定0.001m）。Guard は K、Block は K/3、Hit は1.2K。Hitstop 後10戦闘Fで速く始まり減速し、操作速度とは別に進みます。硬直が先に終わっても残り押し返しは続きます。

j=直ガ減算/10。通常 Guard の自分移動は K(1−0.4j)。FD の自分移動は K のまま、攻撃側へ K×0.2(1+1.5j) を与える。FD でも直ガ硬直減算は有効。壁ダウン免疫は将来要件で、壁ダウンそのものは未決です。

共通係数の正本は `Assets/Shioka/調整Data/共通System/{Guard,TG,Combat}.asset`。技別値は各 Skill の Data.asset。変更箇所は [SHARED_SYSTEM_TUNING](SHARED_SYSTEM_TUNING.md)。

## 入力の現在形

汎用攻撃9枠 ABCJKLXYZ とキャラ操作名を分離。ぁゎりの表示は ABC=D1/D2/D3、JKL=LP/MP/HP、XYZ=LK/MK/HK。D1–D3 の技割当は未決です。Delirium は専用ボタンでなく暗転を含む TG 消費技カテゴリー。Mania は独立 hold 修飾で、追加強化性能は未決。

旧30ms、試行2Fの同時押し確定待ちは撤廃。通常の先行入力バッファと startup は維持。T1 とボタン同 tick は chord のみ。通常ボタン先押し後の T1 でも成立エッジで chord を1回新規発行し、先の通常攻撃は取消・巻戻しせず既存のキャンセル/先行入力判定へ渡す。保持で連発せず、T1 release だけで通常攻撃を再発火しない。旧 Special1/2 専用入力、弱P+弱K FD、中P+中K 投げ、U FD は現行入力ではありません。

Hold は投げの改名。FD は Mania＋相手基準後ろ1/4/7、既存の空中・姿勢・行動制限に従う。Menu/Training と Battle の入力を分離し、閉じたボタンを戦闘へ漏らさない。物理既定値・設定 UI・保存は [入力](INPUT_REWORK_CHECKPOINT.md) と [KEYCONFIG](KEYCONFIG.md) を参照。

## Training の要点

HP設定・約3秒後自動回復、符号付き TG と固定、Dummy Guard/FD/常時FD・直ガ減算指定、論理入力の録画/一回再生、表示切替を備えます。固定 TG は増減処理そのものを止め、設定/Reset は指定値を復元。TG 獲得と FD 消費は未実装、既存 TRC 消費とは区別します。

録画は3秒カウント後、Player 操作を Dummy へ切替、最大10秒、1スロットのメモリ内保存。メニューキー自体を戦闘記録しません。回復は最後の実ダメージから数え、継続中の硬直等を待つのでコンボ途中に回復させません。

カメラ幅6..10m、16:9 viewport。画面側境界で身体を保持、OS ウィンドウ寸法を強制固定する保証ではありません。Player/CPU は確定時だけ別保存し、Training Dummy と将来 VS CPU は CPU 選択を共有します。詳細は [TRAINING_FLOW](TRAINING_FLOW.md) と [PROFILES_AND_FLOW](PROFILES_AND_FLOW.md)。
