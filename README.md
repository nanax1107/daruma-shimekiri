# だるまさんが締め切った

「だるまさんが転んだ」×「麻雀」×「締め切りに追われる」大学生の3Dゲーム。
欲しい牌の上に立って待ち、ダルマが振り向いた瞬間に足元の牌をツモる。役を作ってダルマを倒そう。振り向き中に動くと見つかって、後ろから「締切」の壁が迫ってくる。

## フォルダ構成

| フォルダ | 中身 |
| --- | --- |
| `unity/` | Unity プロジェクト（Unity 6000.3.24f1 / URP） |
| `web-prototype/index.html` | ブラウザで遊べる3D試作版（開くだけで動く） |
| `web-prototype/2d/index.html` | 上から見下ろす2D試作版（本物の牌の画像・1マスずつ移動） |

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
| `Scripts/Core/TileUtil.cs` | 牌の名前や色 |
| `Scripts/Game/DarumaGame.cs` | ゲームの進行（振り向き・壁・ツモ・チュートリアル） |
| `Scripts/Game/Stage.cs` | 床・坂・机・ダルマ・壁の組み立て |
| `Scripts/Game/FloorTile.cs` | 床の牌1枚 |
| `Scripts/Game/GameHUD.cs` | タイトル・HP・手牌・結果の画面表示 |
| `Scripts/Game/GameInput.cs` | キーボードとマウスの入力 |
| `Editor/DarumaSceneSetup.cs` | シーンとステージを自動で用意するメニュー |

## 操作

| 入力 | 動作 |
| --- | --- |
| WASD | 移動（振り向き中は止まる！） |
| マウス / ← → | 視点移動 |
| E / 右クリック長押し | ズーム |
| 牌の上で待つ | ダルマが振り向いた時に足元の牌をツモ |
| ホイール / Z・X | 捨てる牌を選ぶ |
| 左クリック / F | 捨てる |
| Space | 上がり → ダルマに攻撃（翻数＝ダメージ） |
| Q | もういいや（手牌を配り直す） |

## モード

チュートリアル／学科分け（やさしい）／ゼミ分け（ふつう）／研究室分け（むずかしい）

## チームで作業するときの注意

- 同じシーン（`DarumaGame.unity`）を同時に編集しない。結合が難しいため、編集する人を決めてから触る
- `Library/` などの自動生成フォルダは共有しない（`.gitignore` で除外済み）
- WebGL で公開するときは、日本語フォントを `unity/Assets/Daruma/Resources/JPFont.ttf` として入れる

## 素材

- 牌の画像：[FluffyStuff/riichi-mahjong-tiles](https://github.com/FluffyStuff/riichi-mahjong-tiles)（パブリックドメイン CC0）
