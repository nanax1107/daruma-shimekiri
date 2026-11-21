# だるまさんが締め切った

「だるまさんが転んだ」×「麻雀」×「締め切りに追われる」大学生の3Dゲーム。
欲しい牌の上に立って待ち、ダルマが振り向いた瞬間に足元の牌をツモる。役を作ってダルマを倒そう。振り向き中に動くと見つかって、後ろから「締切」の壁が迫ってくる。

## フォルダ構成

| フォルダ | 中身 |
| --- | --- |
| `unity/` | Unity プロジェクト（Unity 6000.3.24f1 / URP） |
| `web-prototype/index.html` | ブラウザで遊べる3D試作版（開くだけで動く） |
| `web-prototype/2d/index.html` | 上から見下ろす2D試作版（本物の牌の画像・1マスずつ移動） |
| `web-prototype/3d-top/index.html` | 3D試作版の改良版（一人称視点＋右上ミニマップ・本物の牌の画像・1マス移動・壁の位置表示。視点ボタンで上からにも切り替え可） |

## Unity で開く

1. Unity Hub の **Add → Add project from disk** で `unity/` フォルダを選ぶ
2. エディタのバージョンは **6000.3.24f1**
3. `Assets/Scenes/DarumaGame.unity` を開いて ▶ を押す
4. 床やダルマがシーンに無いときは、メニュー **だるま → ステージをシーンに作り直す**

### スクリプト（`unity/Assets/Daruma/`）

| ファイル | 内容 |
| --- | --- |
| `Scripts/Core/HandEvaluator.cs` | 上がり判定と役・ドラの計算 |
| `Scripts/Core/GameModes.cs` | 4つのモードの数値（難易度調整はここ） |
| `Scripts/Core/TileUtil.cs` | 牌の名前・牌の画像の読み込み |
| `Scripts/Core/Hints.cs` | ヒント（ほしい牌・捨てるといい牌）の計算 |
| `Resources/Tiles/` | 牌の画像（t00〜t26.png） |
| `Scripts/Game/DarumaGame.cs` | ゲームの進行（振り向き・壁・ツモ・チュートリアル） |
| `Scripts/Game/Stage.cs` | 床・坂・机・ダルマ・壁の組み立て |
| `Scripts/Game/FloorTile.cs` | 床の牌1枚 |
| `Scripts/Game/GameHUD.cs` | タイトル・状態バー・ミニマップ・手牌・結果の画面表示 |
| `Scripts/Game/GameInput.cs` | キーボードとマウスの入力 |
| `Editor/DarumaSceneSetup.cs` | シーンとステージを自動で用意するメニュー |

## 操作（Unity 版・ブラウザ版 共通）

| 入力 | 動作 |
| --- | --- |
| W A S D | 1マスずつ移動（押しっぱなしで連続移動）。振り向き中は止まる！ |
| 画面をドラッグ | まわりを見回す（ダブルクリックで正面に戻る） |
| 牌の上で待つ | ダルマが振り向いた瞬間に足元の牌をツモ |
| （自動） | いらない牌は自動で捨てる／そろったら自動で攻撃（Space でも攻撃できる） |
| 「もういいや」ボタン | 手牌を配り直す（回数制限あり・壁が迫る） |
| 「ヒント」ボタン | ほしい牌を緑に光らせる（チュートリアルと学科分けモードだけ） |

## モード

チュートリアル／学科分け（やさしい）／ゼミ分け（ふつう）／研究室分け（むずかしい）

## チームで作業するときの注意

- 同じシーン（`DarumaGame.unity`）を同時に編集しない。結合が難しいため、編集する人を決めてから触る
- `Library/` などの自動生成フォルダは共有しない（`.gitignore` で除外済み）
- WebGL で公開するときは、日本語フォントを `unity/Assets/Daruma/Resources/JPFont.ttf` として入れる

## 素材

- 牌の画像：[FluffyStuff/riichi-mahjong-tiles](https://github.com/FluffyStuff/riichi-mahjong-tiles)（パブリックドメイン CC0）
