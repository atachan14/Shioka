# 調整のしかた(Unity Editor)

技・動作・共通パラメータは、Unity の Inspector とシーン上のハンドルで編集する。どれも **Play を止めて** 操作する(Play 中の保存は拒否される)。

## 共通の考え方

- 編集は**下書き(draft)**に対して行う。`Save` で元のアセットに保存、`Cancel / reload` で破棄。選択をほかに移しただけでも、未保存の下書きは破棄される。`Ctrl+S` だけでは下書きは保存されない。
- コードを変えた後は、コンパイルが終わってから対象のアセットを選び直す(ほかのアセットを一度選んで戻れば十分)。

## 技(例: 弱P)

`Assets/Shioka/調整Data/Fighter/Awari/Skill/5LP/Data.asset` を選ぶ。

1. **時間と強さ**: `Startup Frames` / `Active Frames` / `Recovery Frames`(発生・持続・硬直)、攻撃の強さ、判定の中心と大きさなどを編集する。
2. **画像列**: 同じフォルダの `Sprite/` にある画像が自動で検出される。画像の名前は読み取り専用で、右端の **hold F**(その画像を何フレーム表示するか。正の整数)を編集する。画像を増減したら `Sync detected images to draft` を押す。
   - 並び順はファイル名の自然な順(1, 2, 10)。順番を変えたいときはファイル名を変える。
   - 採用しない画像や原本は `Sprite/` の外(`SourceArt/` など)に置く。
   - 画像列は技の開始から再生する。短ければ最後の画像を技の終わりまで表示し、長ければ技の終わりで打ち切る。画像の合計時間で技の時間は変わらない。
3. `Save same skill asset` で保存する。
4. **位置と判定**: 保存した後、`Preview frame` を選んで `Open spatial editing for this skill` を押す(未保存の下書きがあると押せない)。編集用シーンで、同じ画像列を見ながら、画像の位置・本体の移動・攻撃判定・食らい判定をフレームごとに動かす。シーン側の保存は `Save authoritative SO`(同じ Data.asset に保存される)。

## 動作(例: 前歩き)

`調整Data/Fighter/Awari/Move/ForwardWalk/ForwardWalk.asset` を選ぶ。

- `poses` で、画像・hold F(60Hz の整数)・表示の位置・食らい判定を編集する。
- `Open Scene visual editing` でシーン上で編集する。水色のハンドルが画像の位置、緑の箱が選んだ食らい判定。`Mirror left` で左向きを確認できる。
- `Save same asset` で保存。
- 歩きの速さなどの移動性能は、ここでは変わらない(`Data/SharedCharacter.asset`、`Move/ApprovedLocomotionProfile.asset` で変える)。
- 食らい判定の部位 ID は `Hurtboxes.asset` に対応する。防御の強さと優先度はそちらにある。動作アセットの中で部位 ID の名前を変えない。

## 共通パラメータ

`調整Data/共通System/` の `Guard.asset`、`TG.asset`、`Combat.asset` を選んで Inspector で編集する。値の意味は [../spec/combat.md](../spec/combat.md)。不正な値(0で割る値、有限でない値)は読み込み時に拒否される。

## 画像を新しく取り込むとき

- 新しい PNG を `Sprite/` に初めて入れると、Sprite(Single)・透過あり・ミップマップなしで取り込まれる。既にある画像の設定(PPU、pivot など)は上書きしない。
- 体の高さに合わせて自動で縮尺をそろえる機能はない。新しい素材は、PPU と pivot を手で合わせる(今のぁゎりは PPU 700、身長1.55m)。
