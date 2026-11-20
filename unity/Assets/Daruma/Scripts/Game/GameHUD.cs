using UnityEngine;

// 画面表示（タイトル・HUD・手牌・結果）。IMGUI で描くので、シーンに UI を置かなくてよい
public class GameHUD : MonoBehaviour
{
    public static bool PointerOverHand;

    const float BaseH = 720f;
    GUIStyle label, center, big, small, tileNum, tileSuit, button, modeTitle, modeDesc;
    Rect handRect;

    void InitStyles()
    {
        if (label != null) return;
        // 画面の文字は Unity 標準フォントを使う（日本語は OS のフォントで自動的に表示される）
        label = new GUIStyle(GUI.skin.label) { fontSize = 15, richText = true, wordWrap = true };
        label.normal.textColor = Color.white;
        small = new GUIStyle(label) { fontSize = 13 };
        center = new GUIStyle(label) { alignment = TextAnchor.MiddleCenter, fontSize = 16, fontStyle = FontStyle.Bold };
        big = new GUIStyle(center) { fontSize = 40 };
        tileNum = new GUIStyle(center) { fontSize = 22 };
        tileSuit = new GUIStyle(center) { fontSize = 13 };
        button = new GUIStyle(GUI.skin.button) { fontSize = 17, fontStyle = FontStyle.Bold, richText = true };
        modeTitle = new GUIStyle(label) { fontSize = 18, fontStyle = FontStyle.Bold, wordWrap = false };
        modeDesc = new GUIStyle(label) { fontSize = 13 };
        modeDesc.normal.textColor = new Color(0.75f, 0.78f, 0.86f);
    }

    static void Fill(Rect r, Color c)
    {
        var old = GUI.color; GUI.color = c;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = old;
    }

    void OnGUI()
    {
        var g = DarumaGame.I;
        if (g == null) return;
        InitStyles();
        float scale = UnityEngine.Screen.height / BaseH;
        float W = UnityEngine.Screen.width / scale, H = BaseH;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));

        if (g.Phase == DarumaGame.Screen.Title) { DrawTitle(g, W, H); PointerOverHand = true; return; }
        DrawHud(g, W, H);
        if (g.Phase == DarumaGame.Screen.Result) DrawResult(g, W, H);

        var mp = Event.current.mousePosition;
        PointerOverHand = g.Phase != DarumaGame.Screen.Playing || handRect.Contains(mp);
    }

    // ===== タイトル（モード選択） =====
    void DrawTitle(DarumaGame g, float W, float H)
    {
        Fill(new Rect(0, 0, W, H), new Color(0.02f, 0.025f, 0.05f, 0.82f));
        float pw = 640, px = (W - pw) / 2, y = 40;
        Fill(new Rect(px - 20, y - 16, pw + 40, H - 50), new Color(0.086f, 0.1f, 0.15f, 0.98f));
        GUI.Label(new Rect(px, y, pw, 44), "<size=30><b>だるまさんが締め切った</b></size>", label); y += 50;
        GUI.Label(new Rect(px, y, pw, 70),
            "締め切り前夜の大学。欲しい牌の上に立って待とう。ダルマが振り向いた瞬間、足元の牌がツモになる。麻雀の役を作って倒そう。" +
            "振り向いている間に動くと見つかって、後ろから<b>締め切りの壁</b>が迫ってくる。", label);
        y += 80;

        // チュートリアル（横長）＋3モード
        var modes = GameModes.Selectable;
        DrawModeButton(g, modes[0], new Rect(px, y, pw, 70)); y += 80;
        float bw = (pw - 20) / 3f;
        for (int i = 1; i < modes.Length; i++)
            DrawModeButton(g, modes[i], new Rect(px + (i - 1) * (bw + 10), y, bw, 120));
        y += 135;

        GUI.Label(new Rect(px, y, pw, 24), "<b>操作</b>", label); y += 26;
        string ops =
            "<color=#ffd36b>WASD</color>　移動（振り向き中は止まる！）\n" +
            "<color=#ffd36b>マウス / ← →</color>　視点移動　　<color=#ffd36b>E / 右クリック長押し</color>　ズーム\n" +
            "<color=#ffd36b>牌の上で待つ</color>　ダルマが振り向いた時に足元の牌をツモ（捨て忘れはツモ切り）\n" +
            "<color=#ffd36b>ホイール / Z・X</color>　捨てる牌を選ぶ　　<color=#ffd36b>左クリック / F</color>　捨てる\n" +
            "<color=#ffd36b>Space</color>　上がり → ダルマに攻撃（翻数＝ダメージ）　　<color=#ffd36b>Q</color>　もういいや（配り直し）\n" +
            "<color=#ffd36b>Esc</color>　マウスのロック解除（クリックで戻る）";
        GUI.Label(new Rect(px, y, pw, 140), ops, small); y += 140;
        GUI.Label(new Rect(px, y, pw, 60),
            "役：ツモ1／タンヤオ1／七対子2／対々和2／清一色6。ドラ1枚につき＋1翻。上がりの形は4面子1雀頭か七対子。字牌はなし。上がると締め切りの壁が後退する。", modeDesc);
    }

    void DrawModeButton(DarumaGame g, GameMode m, Rect r)
    {
        if (GUI.Button(r, "", button)) g.StartMode(m);
        Fill(new Rect(r.x + 2, r.y + 2, r.width - 4, r.height - 4), new Color(0.12f, 0.14f, 0.21f));
        GUI.Label(new Rect(r.x + 12, r.y + 8, r.width - 24, 26), m.Name, modeTitle);
        var lv = new Rect(r.x + 12, r.y + 34, 90, 18);
        Fill(lv, m.LevelColor);
        GUI.Label(lv, "<b>" + m.Level + "</b>", new GUIStyle(small) { alignment = TextAnchor.MiddleCenter, fontSize = 11 });
        GUI.Label(new Rect(r.x + 12, r.y + 54, r.width - 24, r.height - 58), m.Description, modeDesc);
    }

    // ===== プレイ中の表示 =====
    void DrawHud(DarumaGame g, float W, float H)
    {
        var m = g.Mode;
        // 振り向き中の赤い縁
        if (g.Phase == DarumaGame.Screen.Playing && g.State != DarumaGame.DState.Back)
        {
            var edge = g.State == DarumaGame.DState.Look ? new Color(1, 0, 0, 0.35f) : new Color(1, 0.75f, 0, 0.25f);
            Fill(new Rect(0, 0, W, 18), edge); Fill(new Rect(0, H - 18, W, 18), edge);
            Fill(new Rect(0, 0, 18, H), edge); Fill(new Rect(W - 18, 0, 18, H), edge);
        }

        // HP バー
        float bw = Mathf.Min(520, W * 0.6f), bx = (W - bw) / 2;
        GUI.Label(new Rect(bx, 8, bw, 22), m.Name + "｜" + m.DarumaName + " HP " + g.Hp + " / " + m.Hp, center);
        Fill(new Rect(bx, 32, bw, 14), new Color(0.16f, 0.07f, 0.08f));
        Fill(new Rect(bx, 32, bw * g.Hp / Mathf.Max(1, m.Hp), 14), new Color(0.93f, 0.25f, 0.2f));

        // 状態バー
        string st; Color sc; float ratio;
        if (g.State == DarumaGame.DState.Back) { st = m.DarumaName + "は背を向けている（動ける）"; sc = new Color(0.5f, 0.88f, 0.55f); ratio = 1 - g.StateT / Mathf.Max(0.01f, g.StateDur); }
        else if (g.State == DarumaGame.DState.Warn) { st = "合図中…もうすぐ振り向く！"; sc = new Color(1f, 0.83f, 0.42f); ratio = 1 - g.StateT / m.Warn; }
        else { st = "振り向き中！ 動くな　あと " + Mathf.Max(0, m.Look - g.StateT).ToString("0.0") + " 秒"; sc = new Color(1f, 0.42f, 0.42f); ratio = 1 - g.StateT / m.Look; }
        var stStyle = new GUIStyle(center); stStyle.normal.textColor = sc;
        GUI.Label(new Rect(bx, 52, bw, 22), st, stStyle);
        Fill(new Rect(bx, 76, bw, 8), new Color(1, 1, 1, 0.15f));
        Fill(new Rect(bx, 76, bw * Mathf.Clamp01(ratio), 8), sc);

        // 左上の情報
        float dist = Mathf.Max(0, g.Pz - g.WallS);
        string foot = g.Cell >= 0 ? TileUtil.ColorTag(g.Stage.Tiles[g.Cell].Type, TileUtil.Name(g.Stage.Tiles[g.Cell].Type)) : "なし";
        string info =
            "時間 <color=#ffd36b>" + ((int)g.T / 60) + ":" + ((int)g.T % 60).ToString("00") + "</color>\n" +
            "締め切りまで <color=" + (dist < 3 ? "#ff6b6b" : "#ffd36b") + ">" + dist.ToString("0.0") + " m</color>\n" +
            "もういいや 残り <color=#ffd36b>" + g.MoiiyaLeft + "</color> 回\n" +
            "見つかった回数 <color=#ffd36b>" + g.Found + "</color>\n" +
            "ドラ " + TileUtil.ColorTag(g.Dora, TileUtil.Name(g.Dora)) + "（表示牌 " + TileUtil.Name(g.DoraInd) + "）\n" +
            "足元の牌 " + foot;
        Fill(new Rect(10, 10, 210, 136), new Color(0, 0, 0, 0.55f));
        GUI.Label(new Rect(20, 16, 200, 130), info, small);

        // チュートリアル
        if (m.Tutorial && g.Tut < DarumaGame.TutSteps.Length)
        {
            Fill(new Rect(10, 156, 300, 150), new Color(0.04f, 0.12f, 0.28f, 0.85f));
            GUI.Label(new Rect(20, 162, 280, 20), "<color=#9cc3ff><b>チュートリアル " + (g.Tut + 1) + " / " + DarumaGame.TutSteps.Length + "</b></color>", small);
            GUI.Label(new Rect(20, 182, 280, 120), DarumaGame.TutSteps[g.Tut].text, label);
        }

        // 中央のメッセージ
        string text = ""; Color col = Color.white;
        if (g.T < g.MsgUntil) { text = g.Msg; col = g.MsgColor; }
        else if (g.Phase == DarumaGame.Screen.Playing && g.State == DarumaGame.DState.Warn) { text = m.WarnText; col = new Color(1f, 0.83f, 0.42f); }
        else if (g.Phase == DarumaGame.Screen.Playing && g.State == DarumaGame.DState.Look) { text = m.LookText; col = new Color(1f, 0.35f, 0.35f); }
        if (text != "")
        {
            var s2 = new GUIStyle(big); s2.normal.textColor = Color.black;
            GUI.Label(new Rect(3, H * 0.55f + 3, W, 160), text, s2);
            s2.normal.textColor = col;
            GUI.Label(new Rect(0, H * 0.55f, W, 160), text, s2);
        }

        // ヒント
        string hint; Color hc = new Color(0, 0, 0, 0.6f);
        if (g.Win != null) { hint = "上がれる！ Space で攻撃（" + g.Win.Han + "翻）"; hc = new Color(0.72f, 0.53f, 0.04f, 0.95f); }
        else if (g.Drawn >= 0) hint = "1枚捨てる：ホイール/Z・Xで選んで 左クリック/F";
        else hint = "欲しい牌の上に立とう。ダルマが振り向いた時の足元の牌をツモ";
        var hr = new Rect((W - 520) / 2, H - 112, 520, 26);
        Fill(hr, hc);
        GUI.Label(hr, hint, center);

        // 手牌
        float tw = 40, th = 56, gap = 4;
        float total = 14 * tw + 13 * gap + 16;
        float x0 = (W - total) / 2, y0 = H - 70;
        handRect = new Rect(x0, y0 - 12, total, th + 14);
        for (int i = 0; i < 14; i++)
        {
            float x = x0 + i * (tw + gap) + (i == 13 ? 16 : 0);
            bool sel = i == g.Sel;
            var r = new Rect(x, y0 - (sel ? 10 : 0), tw, th);
            int t = i < 13 ? (i < g.Hand.Count ? g.Hand[i] : -1) : g.Drawn;
            if (t < 0)
            {
                if (i == 13) { Fill(new Rect(r.x, r.y, r.width, 2), new Color(1, 1, 1, 0.4f)); Fill(new Rect(r.x, r.yMax - 2, r.width, 2), new Color(1, 1, 1, 0.4f)); }
                continue;
            }
            if (sel) Fill(new Rect(r.x - 3, r.y - 3, r.width + 6, r.height + 6), new Color(1f, 0.8f, 0.2f));
            Fill(r, t == g.Dora ? new Color(1f, 0.95f, 0.75f) : new Color(0.96f, 0.95f, 0.9f));
            var ns = new GUIStyle(tileNum); ns.normal.textColor = TileUtil.SuitColors[TileUtil.Suit(t)];
            var ss = new GUIStyle(tileSuit); ss.normal.textColor = TileUtil.SuitColors[TileUtil.Suit(t)];
            GUI.Label(new Rect(r.x, r.y + 4, r.width, 28), TileUtil.Num(t).ToString(), ns);
            GUI.Label(new Rect(r.x, r.y + 30, r.width, 20), TileUtil.Suits[TileUtil.Suit(t)], ss);
            if (!DarumaGame.Locked && g.Phase == DarumaGame.Screen.Playing && GUI.Button(r, "", GUIStyle.none)) g.ClickTile(i);
        }
    }

    // ===== 結果 =====
    void DrawResult(DarumaGame g, float W, float H)
    {
        Fill(new Rect(0, 0, W, H), new Color(0.02f, 0.025f, 0.05f, 0.82f));
        float pw = 460, px = (W - pw) / 2, py = H / 2 - 130;
        Fill(new Rect(px, py, pw, 250), new Color(0.086f, 0.1f, 0.15f, 0.98f));
        string title, body;
        if (g.Cleared && g.Mode.Tutorial) { title = "<color=#ffd36b>チュートリアル完了！</color>"; body = "基本の操作はこれでばっちり。\nモード選択から「学科分けモード」に挑戦しよう。"; }
        else if (g.Cleared) { title = "<color=#ffd36b>クリア！</color>"; body = g.Mode.Name + "：" + g.Mode.DarumaName + "を倒した。\nタイム " + ((int)g.T / 60) + ":" + ((int)g.T % 60).ToString("00") + "／見つかった回数 " + g.Found + " 回"; }
        else { title = "<color=#ff5a5a>締め切りアウト</color>"; body = "締め切りの壁に追いつかれた…（ダルマの残りHP " + g.Hp + "）"; }
        GUI.Label(new Rect(px, py + 20, pw, 50), "<size=34><b>" + title + "</b></size>", center);
        GUI.Label(new Rect(px + 20, py + 80, pw - 40, 70), body, new GUIStyle(label) { alignment = TextAnchor.UpperCenter });
        if (GUI.Button(new Rect(px + 40, py + 170, 180, 44), "もう一度", button)) g.StartMode(g.Mode);
        if (GUI.Button(new Rect(px + 240, py + 170, 180, 44), "モード選択へ", button)) g.BackToTitle();
    }
}
