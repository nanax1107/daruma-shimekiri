using UnityEngine;

// 画面表示（タイトル・HUD・ミニマップ・手牌・結果）。IMGUI で描くので、シーンに UI を置かなくてよい
public class GameHUD : MonoBehaviour
{
    // マウスが画面の表示（ボタン・手牌・地図など）の上にあるか。上にあるときはドラッグで視点を動かさない
    public static bool PointerOverUI;

    const float BaseH = 720f;
    GUIStyle label, small, center, big, button, modeTitle, tagStyle;
    static readonly Color Gold = new Color(1f, 0.83f, 0.42f);
    static readonly Color DoraCol = new Color(0.88f, 0.66f, 0f);
    static readonly Color WantCol = new Color(0.24f, 0.86f, 0.35f);
    static readonly Color RecCol = new Color(1f, 0.62f, 0.26f);

    Rect[] uiRects = new Rect[0];

    void InitStyles()
    {
        if (label != null) return;
        // 画面の文字は Unity 標準フォントを使う（日本語は OS のフォントで自動的に表示される）
        // 筆文字の日本語フォント（Resources/BrushFont.ttf = Yuji Syuku）
        var font = JPText.Font;
        label = new GUIStyle(GUI.skin.label) { font = font, fontSize = 16, richText = true, wordWrap = true };
        label.normal.textColor = Color.white;
        small = new GUIStyle(label) { fontSize = 14 };
        center = new GUIStyle(label) { alignment = TextAnchor.MiddleCenter, fontSize = 15, fontStyle = FontStyle.Normal };
        big = new GUIStyle(center) { fontSize = 40 };
        button = new GUIStyle(center) { fontSize = 14 };
        modeTitle = new GUIStyle(label) { fontSize = 17, fontStyle = FontStyle.Normal, wordWrap = false };
        tagStyle = new GUIStyle(center) { fontSize = 11 };
        tagStyle.normal.textColor = Color.black;
    }

    static void Fill(Rect r, Color c)
    {
        var old = GUI.color; GUI.color = c;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = old;
    }
    static void Border(Rect r, Color c, float w)
    {
        Fill(new Rect(r.x, r.y, r.width, w), c); Fill(new Rect(r.x, r.yMax - w, r.width, w), c);
        Fill(new Rect(r.x, r.y, w, r.height), c); Fill(new Rect(r.xMax - w, r.y, w, r.height), c);
    }
    static void TileImg(Rect r, int t)
    {
        var tex = TileUtil.Tex(t);
        if (tex != null) GUI.DrawTexture(r, tex, ScaleMode.StretchToFill);
        else Fill(r, new Color(0.96f, 0.95f, 0.9f));
    }
    GUIStyle Colored(GUIStyle s, Color c) { var n = new GUIStyle(s); n.normal.textColor = c; return n; }

    // ブラウザ版と同じ色のボタン（押されたら true）
    bool Btn(Rect r, string text, Color bg)
    {
        bool hover = r.Contains(Event.current.mousePosition);
        Fill(r, hover ? Color.Lerp(bg, Color.white, 0.12f) : bg);
        GUI.Label(r, "" + text + "", button);
        return GUI.Button(r, "", GUIStyle.none);
    }
    static Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }
    static readonly Color PanelBg = new Color(0.03f, 0.04f, 0.07f, 0.92f);

    void OnGUI()
    {
        var g = DarumaGame.I;
        if (g == null) return;
        InitStyles();
        float scale = UnityEngine.Screen.height / BaseH;
        float W = UnityEngine.Screen.width / scale, H = BaseH;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
        var rects = new System.Collections.Generic.List<Rect>();

        if (g.Phase == DarumaGame.Screen.Title) { DrawTitle(g, W, H); rects.Add(new Rect(0, 0, W, H)); }
        else
        {
            DrawHud(g, W, H, rects);
            if (g.Phase == DarumaGame.Screen.Result) { DrawResult(g, W, H); rects.Add(new Rect(0, 0, W, H)); }
        }

        if (Event.current.type == EventType.Repaint)
        {
            uiRects = rects.ToArray();
            var mp = Event.current.mousePosition;
            bool over = false;
            foreach (var r in uiRects) if (r.Contains(mp)) { over = true; break; }
            PointerOverUI = over;
        }
    }

    // ===== タイトル（モード選択） =====
    void DrawTitle(DarumaGame g, float W, float H)
    {
        Fill(new Rect(0, 0, W, H), new Color(0.02f, 0.024f, 0.047f, 0.9f));
        float pw = Mathf.Min(860, W - 80), px = (W - pw) / 2, ph = 560, y = (H - ph) / 2;
        var panel = new Rect(px - 34, y - 26, pw + 68, ph + 44);
        Fill(panel, Hex("#161a26")); Border(panel, Hex("#2f3756"), 2);
        GUI.Label(new Rect(px, y, pw, 64), "<size=48><color=#ffffff>だるまさんが締め切った</color></size>", label); y += 78;
        GUI.Label(new Rect(px, y, pw, 70),
            "<size=22>ダルマが<color=#ffd36b>振り向いた瞬間</color>に立っていた牌が手に入る。\n牌をそろえてダルマを倒そう。<color=#ff6b6b>振り向き中に動く</color>と、締め切りの壁が迫る！</size>", label);
        y += 92;

        var modes = GameModes.Selectable;
        DrawModeButton(g, modes[0], new Rect(px, y, pw, 96)); y += 110;
        float bw = (pw - 24) / 3f;
        for (int i = 1; i < modes.Length; i++)
            DrawModeButton(g, modes[i], new Rect(px + (i - 1) * (bw + 12), y, bw, 84));
        y += 104;
        GUI.Label(new Rect(px, y, pw, 32), "<size=22><color=#ffd36b>W A S D</color> 移動　／　<color=#ffd36b>ドラッグ</color> 見回す</size>", label); y += 46;
        GUI.Label(new Rect(px, y, pw, 22), "<color=#8890a8>牌の画像：FluffyStuff「riichi-mahjong-tiles」（パブリックドメイン CC0）</color>", small);
    }

    void DrawModeButton(DarumaGame g, GameMode m, Rect r)
    {
        bool hover = r.Contains(Event.current.mousePosition);
        Fill(r, m.Tutorial ? Hex("#14213a") : Hex("#1e2436"));
        Border(r, hover ? Color.white : m.Tutorial ? Hex("#3b82f6") : Hex("#2f3756"), 3);
        if (GUI.Button(r, "", GUIStyle.none)) g.StartMode(m);
        GUI.Label(new Rect(r.x + 16, r.y + 10, r.width - 28, 36), "<size=26>" + m.Name + "</size>", modeTitle);
        var lv = new Rect(r.x + 16, r.y + 48, 120, 24);
        Fill(lv, m.LevelColor);
        GUI.Label(lv, "<size=16>" + m.Level + "</size>", Colored(tagStyle, Color.white));
        if (!string.IsNullOrEmpty(m.Note)) GUI.Label(new Rect(r.x + 150, r.y + 46, r.width - 160, 28), "<size=19><color=#dfe4f2>" + m.Note + "</color></size>", label);
    }

    // ===== プレイ中の表示 =====
    void DrawHud(DarumaGame g, float W, float H, System.Collections.Generic.List<Rect> rects)
    {
        var m = g.Mode;
        bool looking = g.Phase == DarumaGame.Screen.Playing && g.State == DarumaGame.DState.Look;
        float dist = g.Cx - g.WallX - 1;

        // 画面のふち：振り向き中は赤、壁が近いと薄い赤
        if (looking || dist < 1.5f)
        {
            // 外側ほど濃い赤（ブラウザ版の内側の影に近づける）
            var baseCol = looking ? new Color(1, 0, 0, 0.5f) : new Color(1, 0, 0, 0.35f);
            for (int k = 0; k < 10; k++)
            {
                float a = baseCol.a * (1 - k / 10f) * 0.22f, o = k * 9;
                var c2 = new Color(baseCol.r, baseCol.g, baseCol.b, a);
                Fill(new Rect(0, o, W, 9), c2); Fill(new Rect(0, H - o - 9, W, 9), c2);
                Fill(new Rect(o, 0, 9, H), c2); Fill(new Rect(W - o - 9, 0, 9, H), c2);
            }
        }

        // 左上：時間
        var timeR = new Rect(12, 10, 96, 30);
        Fill(timeR, PanelBg);
        GUI.Label(new Rect(22, 14, 90, 24), "時間 <color=#ffd36b>" + ((int)g.T / 60) + ":" + ((int)g.T % 60).ToString("00") + "</color>", label);

        // 上：モード・HP・状態バー・通路の全体図
        float bw = Mathf.Min(600, W * 0.48f), bx = (W - bw) / 2;
        var topR = new Rect(bx - 14, 8, bw + 28, 112);
        Fill(topR, PanelBg);
        GUI.Label(new Rect(bx, 12, bw, 22), m.Name + "｜" + m.DarumaName + " HP " + g.Hp + " / " + m.Hp, center);
        float ratio; Color sc;
        if (looking)
        {
            sc = new Color(1f, 0.42f, 0.42f); ratio = 1 - g.StateT / m.Look;
            GUI.Label(new Rect(bx, 36, bw, 22), "振り向き中！ 動くな　あと " + Mathf.Max(0, m.Look - g.StateT).ToString("0.0") + " 秒", Colored(center, sc));
        }
        else { sc = new Color(0.5f, 0.88f, 0.55f); ratio = 1 - g.StateT / Mathf.Max(0.01f, g.StateDur); }
        Fill(new Rect(bx, 62, bw, 10), new Color(1, 1, 1, 0.13f));
        Fill(new Rect(bx, 62, bw * Mathf.Clamp01(ratio), 10), sc);
        // 通路（左端 = 壁の初期位置、右端 = ダルマ）
        var trk = new Rect(bx, 82, bw, 24);
        Fill(trk, Hex("#1c2233")); Border(trk, Hex("#3a4366"), 1);
        float lo = DarumaGame.WallStart, hi = DarumaGame.Cols + 2;
        System.Func<float, float> pct = x => Mathf.Clamp01((x - lo) / (hi - lo));
        float ww = trk.width * pct(g.WallX + 1);
        Fill(new Rect(trk.x, trk.y, ww, trk.height), Hex("#a0141c"));
        Fill(new Rect(trk.x + ww - 3, trk.y, 3, trk.height), Hex("#ff4040"));
        GUI.Label(new Rect(trk.x + 4, trk.y + 2, 60, 20), "締切", small);
        float mx = trk.x + trk.width * pct(g.Cx + 0.5f);
        Fill(new Rect(mx - 10, trk.y + 2, 20, 20), Color.white); Fill(new Rect(mx - 8, trk.y + 4, 16, 16), Hex("#3b82f6"));
        GUI.Label(new Rect(mx - 9, trk.y + 3, 18, 18), "私", Colored(tagStyle, Color.white));
        Fill(new Rect(trk.xMax - 18, trk.y + 5, 14, 14), looking ? Color.red : m.DarumaColor);
        rects.Add(topR);

        // チュートリアル
        if (g.TutActive) rects.Add(DrawTutorial(g));

        // 右上：自分の周り9マスの地図
        rects.Add(DrawMiniMap(g, W));

        // 左下：操作
        var keysR = new Rect(12, H - 210, 150, 46);
        Fill(keysR, PanelBg);
        GUI.Label(new Rect(20, H - 206, 140, 40), "<color=#ffd36b>W A S D</color> 移動\n<color=#ffd36b>ドラッグ</color> 見回す", small);

        // 中央のメッセージ
        string text = ""; Color col = Color.white;
        if (g.T < g.MsgUntil) { text = g.Msg; col = g.MsgColor; }
        else if (looking) { text = m.LookText; col = new Color(1f, 0.86f, 0.2f); }
        if (text != "")
        {
            // 見やすいように：暗い帯の上に、太い黒ふち付きの大きな文字
            int lines = text.Split('\n').Length;
            float msgH = 70 * lines + 20, ty = H * 0.38f - msgH / 2;
            for (int k = 0; k < 6; k++) Fill(new Rect(0, ty - 6 + k * 3, W, msgH + 12 - k * 6), new Color(0, 0, 0, 0.12f));
            var st = new GUIStyle(big) { fontSize = 60 };
            var outline = Colored(st, Color.black);
            for (int ox = -3; ox <= 3; ox += 3)
                for (int oy = -3; oy <= 3; oy += 3)
                    if (ox != 0 || oy != 0) GUI.Label(new Rect(ox, ty + oy, W, msgH), text, outline);
            GUI.Label(new Rect(0, ty, W, msgH), text, Colored(st, col));
        }

        // 下：ヒント文
        string hint;
        if (g.Win != null) hint = "上がり！ 攻撃中（" + g.Win.Han + "翻）";
        else if (g.Drawn >= 0) hint = "いらない牌を自動で捨てるよ";
        else hint = g.HintsOn ? "緑に光る牌の上で、振り向きを待とう" : "欲しい牌の上に立って待とう。振り向いた瞬間の足元の牌がツモになる";
        var hr = new Rect((W - 480) / 2, H - 156, 480, 26);
        Fill(hr, g.Win != null ? Hex("#b8870b") : new Color(0, 0, 0, 0.67f));
        GUI.Label(hr, hint, center);

        // 下：足元の牌・手牌・ボタン
        var bar = new Rect(0, H - 124, W, 124);
        for (int k = 0; k < 8; k++) Fill(new Rect(0, bar.y + k * 6, W, 6), new Color(0, 0, 0, 0.67f * k / 8f));
        Fill(new Rect(0, bar.y + 48, W, bar.height - 48), new Color(0, 0, 0, 0.67f));
        rects.Add(bar);
        float tw = 48, th = 64, gap = 4;
        float handW = 13 * (tw + gap) + 14 + tw;
        float x0 = Mathf.Max(100, (W - handW) / 2 - 40), y0 = H - 84;

        // 足元の牌
        GUI.Label(new Rect(x0 - 86, y0 - 22, 70, 18), "<color=#ffd36b>足元の牌</color>", Colored(tagStyle, Gold));
        var footR = new Rect(x0 - 76, y0, tw, th);
        var under = g.UnderTile();
        if (under != null) { TileImg(footR, under.Type); Border(footR, Gold, 2); }
        else { Border(footR, new Color(0.7f, 0.6f, 0.3f, 0.6f), 1); GUI.Label(footR, "なし", Colored(tagStyle, new Color(0.8f, 0.75f, 0.6f))); }

        for (int i = 0; i < 14; i++)
        {
            float x = x0 + i * (tw + gap) + (i == 13 ? 14 : 0);
            var r = new Rect(x, y0, tw, th);
            int t = i < 13 ? (i < g.Hand.Count ? g.Hand[i] : -1) : g.Drawn;
            if (t < 0) { if (i == 13) Border(r, new Color(1, 1, 1, 0.3f), 1); continue; }
            TileImg(r, t);
            if (t == g.Dora) Border(r, DoraCol, 3);
            if (i == 13) Border(r, Gold, 2);
            if (i == g.RecIdx && g.Win == null)
            {
                Border(new Rect(r.x - 2, r.y - 2, r.width + 4, r.height + 4), RecCol, 3);
                var tr = new Rect(r.x + 4, r.y - 20, r.width - 8, 16);
                Fill(tr, RecCol);
                GUI.Label(tr, "捨て", tagStyle);
            }
        }

        // ボタン
        float bx2 = Mathf.Min(W - 190, x0 + handW + 24), by = H - 112;
        if (Btn(new Rect(bx2, by, 176, 34), "もういいや（残り" + (g.MoiiyaLeft > 50 ? "∞" : g.MoiiyaLeft.ToString()) + "回）", Hex("#3a4366"))) g.Moiiya();
        if (m.Hints && Btn(new Rect(bx2, by + 42, 176, 34), "ヒント：" + (g.HintsOn ? "ON" : "OFF"), Hex("#245a2f"))) g.ToggleHints();
    }

    Rect DrawTutorial(DarumaGame g)
    {
        var st = DarumaGame.TutSteps[g.Tut];
        float w = 340, x = 12, y = 50, h = 34;
        // 高さを先に計算
        float textH = label.CalcHeight(new GUIContent(st.Text), w - 28);
        h += textH + 6;
        if (st.Rows != null) h += st.Rows.Length * 38 + 4;
        if (st.Note != null) h += 22;
        if (st.Ev == "next") h += 40;
        var box = new Rect(x, y, w, h);
        Fill(box, new Color(0.04f, 0.12f, 0.27f, 0.95f));
        Border(box, Hex("#3b82f6"), 1);
        float cy = y + 8;
        GUI.Label(new Rect(x + 14, cy, w - 28, 20), "<color=#9cc3ff>チュートリアル " + (g.Tut + 1) + " / " + DarumaGame.TutSteps.Length +
            (st.Title != null ? "　" + st.Title : "") + "</color>", small);
        cy += 24;
        GUI.Label(new Rect(x + 14, cy, w - 28, textH), st.Text, label); cy += textH + 6;
        if (st.Rows != null)
        {
            for (int i = 0; i < st.Rows.Length; i++)
            {
                float tx = x + 14;
                foreach (var t in st.Rows[i]) { TileImg(new Rect(tx, cy, 26, 34), t); tx += 28; }
                GUI.Label(new Rect(x + 104, cy + 6, w - 118, 24), st.Captions[i], small);
                cy += 38;
            }
            cy += 4;
        }
        if (st.Note != null) { GUI.Label(new Rect(x + 14, cy, w - 28, 20), "<color=#b8c8ec>" + st.Note + "</color>", small); cy += 22; }
        if (st.Ev == "next" && Btn(new Rect(x + w - 124, cy + 2, 110, 30), "次へ ▶", Hex("#1565c0"))) g.TutEvent("next");
        return box;
    }

    Rect DrawMiniMap(DarumaGame g, float W)
    {
        float cw = 52, ch = 68, pad = 6;
        float mw = cw * 3 + pad * 2, mh = ch * 3 + pad * 2;
        float x0 = W - mw - 12, y0 = 10;
        var box = new Rect(x0, y0, mw, mh + 24);
        Fill(box, PanelBg);
        for (int dc = 1; dc >= -1; dc--)
        {
            for (int dr = -1; dr <= 1; dr++)
            {
                int c = g.Cx + dc, r = g.Cy + dr;
                var cell = new Rect(x0 + pad + (dr + 1) * cw + 2, y0 + pad + (1 - dc) * ch + 2, cw - 4, ch - 4);
                var tile = g.Stage.TileAt(c, r);
                if (c + 0.5f < g.WallX + 1) Fill(cell, new Color(0.55f, 0.05f, 0.08f));
                else if (tile == null) Fill(cell, new Color(0.15f, 0.17f, 0.24f));
                else
                {
                    TileImg(cell, tile.Type);
                    if (tile.Type == g.Dora) Border(cell, DoraCol, 3);
                    if (g.HintsOn && g.Useful.Contains(tile.Type)) Border(new Rect(cell.x + 1, cell.y + 1, cell.width - 2, cell.height - 2), WantCol, 4);
                }
                if (dc == 0 && dr == 0)
                {
                    Border(new Rect(cell.x - 2, cell.y - 2, cell.width + 4, cell.height + 4), Gold, 3);
                    var lr = new Rect(cell.x + 6, cell.yMax - 16, cell.width - 12, 15);
                    Fill(lr, Gold);
                    GUI.Label(lr, "足元", tagStyle);
                }
            }
        }
        string legend = "<color=#ffd36b>■</color>足元　<color=#e0a800>■</color>ドラ" + (g.HintsOn ? "　<color=#3ddc5a>■</color>ほしい牌" : "");
        GUI.Label(new Rect(x0, y0 + mh, mw, 22), legend, Colored(tagStyle, Color.white));
        return box;
    }

    // ===== 結果 =====
    void DrawResult(DarumaGame g, float W, float H)
    {
        Fill(new Rect(0, 0, W, H), new Color(0.02f, 0.024f, 0.047f, 0.9f));
        float pw = 460, px = (W - pw) / 2, py = H / 2 - 130;
        Fill(new Rect(px, py, pw, 250), Hex("#161a26")); Border(new Rect(px, py, pw, 250), Hex("#2f3756"), 1);
        string title, body;
        if (g.Cleared && g.Mode.Tutorial) { title = "<color=#ffd36b>チュートリアル完了！</color>"; body = "これで遊び方はばっちり。\nモード選択から「学科分けモード」に挑戦しよう。"; }
        else if (g.Cleared) { title = "<color=#ffd36b>クリア！</color>"; body = g.Mode.Name + "：" + g.Mode.DarumaName + "を倒した。\nタイム " + ((int)g.T / 60) + ":" + ((int)g.T % 60).ToString("00") + "／見つかった回数 " + g.Found + " 回"; }
        else { title = "<color=#ff5a5a>締め切りアウト</color>"; body = "締め切りの壁に追いつかれた…（ダルマの残りHP " + g.Hp + "）"; }
        GUI.Label(new Rect(px, py + 20, pw, 50), "<size=34>" + title + "</size>", center);
        GUI.Label(new Rect(px + 20, py + 80, pw - 40, 70), body, new GUIStyle(label) { alignment = TextAnchor.UpperCenter });
        if (Btn(new Rect(px + 40, py + 170, 180, 44), "もう一度", Hex("#dd3333"))) g.StartMode(g.Mode);
        if (Btn(new Rect(px + 240, py + 170, 180, 44), "モード選択へ", Hex("#3a4366"))) g.BackToTitle();
    }
}
