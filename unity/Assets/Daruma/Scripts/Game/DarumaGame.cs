using System.Collections.Generic;
using UnityEngine;

// ゲーム全体の進行（GameManager）。プレイヤー・ダルマ・壁・手牌・ヒント・チュートリアルのルールをまとめて持つ。
// ブラウザ版（web-prototype/3d-top/index.html）と同じルール。
public class DarumaGame : MonoBehaviour
{
    public static DarumaGame I;

    // ===== 盤面 =====
    public const int Cols = 20, Rows = 6;     // Cols = 前後のマス数、Rows = 左右のマス数
    public const float CX = 1.2f, CZ = 1.6f;  // 1マスの横幅・奥行き
    public const float Grace = 0.4f;          // 振り向き直後に判定しない時間
    public const float StepRepeat = 0.13f;    // キー押しっぱなしの連続移動の間隔
    public const float WallStart = -3.5f;     // 壁の初期位置（マス）
    public const float Eye = 1.9f;            // 目の高さ
    public static float CellX(float r) => (r - (Rows - 1) / 2f) * CX;
    public static float CellZ(float c) => (c + 0.5f) * CZ;

    // ===== 状態 =====
    public enum Screen { Title, Playing, Result }
    public enum DState { Back, Look }

    public Screen Phase = Screen.Title;
    public GameMode Mode = GameModes.Gakka;
    public Stage Stage;

    public int Hp;
    public List<int> Hand = new List<int>();
    public int Drawn = -1;                    // ツモった牌（-1 = なし）
    public WinResult Win;
    public int MoiiyaLeft, Found;
    public int Dora, DoraInd;
    public float WallX;                       // 壁の位置（マス）
    public int Cx = -1, Cy = 2;               // 自分のマス（Cx = 前後、Cy = 左右）
    public DState State;
    public float StateT, StateDur;
    public float T;
    public string Msg = "";
    public Color MsgColor = Color.white;
    public float MsgUntil;
    public bool Cleared;
    public int Tut;

    // ヒント
    public bool HintsOn;
    public int RecIdx = -1;                   // 捨てるといい牌の位置
    public HashSet<int> Useful = new HashSet<int>();

    // 視点（マウスのドラッグで見回す）
    public float LookYaw, LookPitch;

    bool foundThisLook, quickNext, dragging;
    float shakeUntil, stepCool, autoWinAt = -1, autoDiscardAt = -1, lastClick = -1;
    int moved;
    float dx = -1, dy = 2;                    // 描画用のなめらかな位置
    Vector3 camPos;
    Vector3 darumaBase;

    void Awake()
    {
        I = this;
        // シーンに置かれたステージを使う（なければその場で組み立てる）
        Stage = FindFirstObjectByType<Stage>();
        if (Stage == null) Stage = Stage.Build(transform);
        Stage.InitRuntime();
        darumaBase = Stage.Daruma.position;
        if (GetComponent<GameHUD>() == null) gameObject.AddComponent<GameHUD>();
        ResetGame();
    }

    // ===== 開始・終了 =====
    public void StartMode(GameMode m)
    {
        Mode = m;
        Stage.ApplyMode(m);
        ResetGame();
        HintsOn = m.Hints;
        UpdateHints();
        Phase = Screen.Playing;
    }

    public void BackToTitle() { Phase = Screen.Title; }

    void ResetGame()
    {
        Hp = Mode.Hp;
        Hand = TileUtil.Deal13(); Drawn = -1; Win = null;
        MoiiyaLeft = Mode.Moiiya; Found = 0;
        WallX = WallStart;
        Cx = -1; Cy = 2; dx = Cx; dy = Cy;
        State = DState.Back; StateT = 0; StateDur = 4; foundThisLook = false; quickNext = false;
        T = 0; Msg = ""; MsgUntil = 0; shakeUntil = 0;
        Tut = 0; moved = 0; stepCool = 0; autoWinAt = -1; autoDiscardAt = -1;
        LookYaw = 0; LookPitch = 0;
        RollDora();
        foreach (var t in Stage.Tiles) { t.SetType(TileUtil.RandomTile()); t.FlashUntil = 0; }
        Stage.Daruma.rotation = Quaternion.Euler(0, 180, 0);
        camPos = new Vector3(CellX(Cy), Eye, CellZ(Cx) - 0.35f);
        UpdateHints();
    }

    void End(bool cleared)
    {
        Cleared = cleared;
        Phase = Screen.Result;
    }

    public void Flash(string text, Color color, float dur)
    {
        Msg = text; MsgColor = color; MsgUntil = T + dur;
    }

    public FloorTile UnderTile() => Stage.TileAt(Cx, Cy);

    // ===== 手牌 =====
    public List<int> FullHand()
    {
        var l = new List<int>(Hand);
        if (Drawn >= 0) l.Add(Drawn);
        return l;
    }

    void RollDora()
    {
        DoraInd = TileUtil.RandomTile();
        Dora = TileUtil.DoraFromIndicator(DoraInd);
    }

    void CheckWin() { Win = Drawn >= 0 ? HandEvaluator.Evaluate(FullHand(), Dora) : null; }

    public void UpdateHints()
    {
        RecIdx = -1; Useful = new HashSet<int>();
        if (Drawn >= 0)
        {
            // 捨てる牌の計算は自動で捨てるために常に行う
            if (Win == null) RecIdx = Hints.BestDiscard(FullHand());
        }
        else if (HintsOn && Hand.Count == 13)
        {
            var u = Hints.Useful(Hand, out int s);
            Useful = Hints.Narrow(Hand, s, u);
        }
    }

    public void ToggleHints() { HintsOn = !HintsOn; UpdateHints(); }

    public void Discard(int i)
    {
        if (Phase != Screen.Playing || Drawn < 0) return;
        if (i == 13) Drawn = -1;
        else
        {
            Hand.RemoveAt(i);
            Hand.Add(Drawn);
            Hand.Sort();
            Drawn = -1;
        }
        Win = null;
        UpdateHints();
        TutEvent("discard");
    }

    void DeclareWin()
    {
        if (Phase != Screen.Playing || Win == null) return;
        var r = Win;
        Hp = Mathf.Max(0, Hp - r.Han);
        shakeUntil = T + 0.6f;
        float before = WallX;
        WallX = Mathf.Max(WallStart, WallX - Mode.WallPushback);
        float back = before - WallX;
        RollDora();
        Flash(r.YakuNames + "\n" + r.Han + "翻 ダメージ！" +
              (back > 0.05f ? "\n締め切りが" + back.ToString("0.0") + "マス後退！" : "") + "\nドラが変わった",
              new Color(1f, 0.83f, 0.42f), 2.8f);
        Hand = TileUtil.Deal13(); Drawn = -1; Win = null; autoWinAt = -1;
        UpdateHints();
        TutEvent("win");
        if (Hp <= 0) End(true);
    }

    public void Moiiya()
    {
        if (Phase != Screen.Playing) return;
        if (Drawn >= 0) { Flash("先に1枚捨ててから", Color.gray, 1.2f); return; }
        if (MoiiyaLeft <= 0) { Flash("もう使えない…", Color.gray, 1.2f); return; }
        MoiiyaLeft--;
        Hand = TileUtil.Deal13();
        WallX += Mode.WallPenalty;
        Flash(Mode.WallPenalty > 0 ? "もういいや…\n締め切りが" + Mode.WallPenalty + "マス迫る" : "もういいや…\n手牌を配り直した",
              new Color(1f, 0.54f, 0.54f), 1.8f);
        UpdateHints();
        TutEvent("moiiya");
    }

    // ダルマが振り向いた瞬間に足元の牌をツモる
    void LookTsumo()
    {
        var tile = UnderTile();
        if (tile == null) { Flash("牌の上にいない…\nツモなし", Color.gray, 1.4f); return; }
        string cut = "";
        if (Drawn >= 0)
        {
            Discard(RecIdx >= 0 ? RecIdx : 13);
            cut = "（「捨て」の牌を自動で捨てた）";
        }
        int t = tile.Type;
        Drawn = t; CheckWin();
        tile.SetType(TileUtil.RandomTile());
        tile.FlashUntil = T + 0.7f;
        Flash(TileUtil.Name(t) + " をツモ！" + (cut != "" ? "\n" + cut : ""), Color.white, 1.3f);
        UpdateHints();
        TutEvent("tsumo");
        // そろったら自動で攻撃。そろわなければ「捨て」の牌を少し見せてから自動で捨てる
        if (Win != null) { Flash("そろった！ 上がり！", new Color(1f, 0.83f, 0.42f), 0.8f); autoWinAt = Time.time + 0.7f; }
        else autoDiscardAt = Time.time + 0.9f;
    }

    void SwapFloor()
    {
        foreach (var t in Stage.Tiles)
            if (Random.value < Mode.SwapRate) { t.SetType(TileUtil.RandomTile()); t.FlashUntil = T + 0.6f; }
    }

    // ===== チュートリアル（麻雀を知らない人向け） =====
    public class TutStep
    {
        public string Ev, Title, Text, Note;
        public int[][] Rows;          // 牌の画像の行
        public string[] Captions;     // 行の説明
        public bool Setup;
    }

    public static readonly TutStep[] TutSteps =
    {
        new TutStep { Ev = "next", Title = "麻雀の牌", Text = "牌は 3種類 × 1〜9 の数字。",
            Rows = new[] { new[] { 0, 4, 8 }, new[] { 9, 13, 17 }, new[] { 18, 22, 26 } },
            Captions = new[] { "萬子（マンズ）：漢字の数字", "筒子（ピンズ）：丸の数", "索子（ソーズ）：棒の数" },
            Note = "索子の「1」だけは鳥の絵。" },
        new TutStep { Ev = "next", Title = "そろえる形", Text = "手牌は14枚。3枚1組を4つ ＋ 同じ牌2枚を1つ そろえたら「上がり」。",
            Rows = new[] { new[] { 1, 2, 3 }, new[] { 13, 13, 13 }, new[] { 24, 24 } },
            Captions = new[] { "同じ種類の連番（2・3・4萬）", "同じ牌3枚（5筒×3）", "同じ牌2枚（7索×2）" },
            Note = "種類が違う牌は連番にならない（1萬・2筒・3索はダメ）。" },
        new TutStep { Ev = "next", Title = "ヒント機能", Text =
            "覚えなくても大丈夫。\n・床で緑に光る牌＝今ほしい牌。その上で待とう\n・いらない牌は自動で捨てる（「捨て」マークが付いた牌）\n・そろったら自動でダルマに攻撃",
            Note = "緑に光るヒントは、チュートリアルと学科分けモードだけ。" },
        new TutStep { Ev = "move", Text = "W A S D キーで、1マスずつ歩いてみよう（W=前・S=後ろ・A=左・D=右）。右上の地図の真ん中が「足元の牌」。" },
        new TutStep { Ev = "tsumo", Text = "上のバーが緑の間は動ける。緑に光る牌の上に立って、ダルマが振り向くのを待とう。振り向いた瞬間、足元の牌が手牌に入る（ツモ）。" },
        new TutStep { Ev = "safeLook", Text = "振り向き中に動くと見つかって、ふだんは後ろの「締切」の壁が迫ってくる。次の振り向きの間、最後まで止まっていよう。" },
        new TutStep { Ev = "moiiya", Text = "手が悪いときは右下の「もういいや」ボタン。手牌を全部配り直せる。押してみよう。" },
        new TutStep { Ev = "win", Setup = true, Text = "最後は上がりの練習。あと1枚で上がれる手を用意した。緑に光る牌の上に立って振り向きを待とう。ツモると自動で攻撃！",
            Note = "役（点数の種類）は覚えなくてOK。上がれば必ずダメージが入る。" },
    };
    public static readonly int[] TutWin = { 0, 3, 6 };
    public bool TutActive => Mode.Tutorial && Tut < TutSteps.Length;
    public bool TutSetupActive => TutActive && TutSteps[Tut].Setup;
    // 説明を読んでいる間（「次へ」のステップ）はダルマも壁も止めておく
    public bool TutPaused => Phase == Screen.Playing && TutActive && TutSteps[Tut].Ev == "next";

    void TutSeed()
    {
        for (int c = Mathf.Max(0, Cx - 1); c <= Mathf.Min(Cols - 1, Cx + 2); c++)
            for (int r = 0; r < Rows; r++)
                if (!(c == Cx && r == Cy)) Stage.TileAt(c, r).SetType(TutWin[(c + r) % 3]);
    }

    public void TutEvent(string ev)
    {
        if (!TutActive || TutSteps[Tut].Ev != ev) return;
        Tut++;
        if (Tut < TutSteps.Length && TutSteps[Tut].Setup)
        {
            Drawn = -1; Win = null; autoDiscardAt = -1;
            Hand = new List<int> { 1, 2, 3, 4, 5, 10, 11, 12, 13, 13, 19, 20, 21 };   // 1萬・4萬・7萬待ち
            UpdateHints();
            TutSeed();
        }
        if (ev != "next") Flash("OK！", new Color(0.61f, 0.76f, 1f), 0.9f);
    }

    // ===== 毎フレーム =====
    void Update()
    {
        float dt = Mathf.Min(Time.deltaTime, 0.05f);
        if (Phase == Screen.Playing)
        {
            bool paused = TutPaused;
            if (!paused) T += dt;
            HandleInput(dt);
            if (!paused)
            {
                StepDaruma(dt);
                WallX += Mode.WallSpeed * dt;
            }
            if (autoWinAt > 0 && Time.time >= autoWinAt) { autoWinAt = -1; if (Win != null) DeclareWin(); }
            if (autoDiscardAt > 0 && Time.time >= autoDiscardAt)
            {
                autoDiscardAt = -1;
                if (Drawn >= 0 && Win == null)
                {
                    int i = RecIdx >= 0 ? RecIdx : 13;
                    string name = TileUtil.Name(FullHand()[i]);
                    Discard(i);
                    Flash(name + " を捨てた", new Color(1f, 0.7f, 0.42f), 1.0f);
                }
            }
            if (Cx - WallX - 1 <= -0.3f) End(false);
        }
        UpdateVisuals(dt);
    }

    static readonly (GKey key, int dc, int dr)[] Dirs =
    {
        (GKey.W, 1, 0), (GKey.S, -1, 0), (GKey.A, 0, -1), (GKey.D, 0, 1),
    };

    void HandleInput(float dt)
    {
        // 移動：1回押すと1マス。押しっぱなしで連続移動
        stepCool -= dt;
        bool stepped = false;
        foreach (var d in Dirs)
            if (GameInput.Down(d.key)) { TryStep(d.dc, d.dr); stepCool = StepRepeat * 1.8f; stepped = true; break; }
        if (!stepped && stepCool <= 0)
            foreach (var d in Dirs)
                if (GameInput.Held(d.key)) { TryStep(d.dc, d.dr); stepCool = StepRepeat; break; }

        if (GameInput.Down(GKey.Space)) DeclareWin();

        // 視点：画面をドラッグで見回す（ダブルクリックで正面に戻る）
        if (GameInput.LeftDown() && !GameHUD.PointerOverUI)
        {
            dragging = true;
            if (Time.unscaledTime - lastClick < 0.3f) { LookYaw = 0; LookPitch = 0; }
            lastClick = Time.unscaledTime;
        }
        if (!GameInput.LeftHeld()) dragging = false;
        if (dragging)
        {
            var m = GameInput.MouseDelta();
            LookYaw = Mathf.Clamp(LookYaw + m.x * 0.003f, -1.7f, 1.7f);
            LookPitch = Mathf.Clamp(LookPitch + m.y * 0.003f, -0.9f, 0.6f);
        }
    }

    // dc = 前後（＋でダルマ側）、dr = 左右（＋で右）
    void TryStep(int dc, int dr)
    {
        int nx = Cx + dc, ny = Cy + dr;
        if (ny < 0 || ny >= Rows || nx > Cols - 1) return;
        if (nx - WallX - 1 < 0) return;                   // 壁の中には入れない
        Cx = nx; Cy = ny; moved++;
        if (moved >= 3) TutEvent("move");
        if (State == DState.Look && StateT > Grace && !foundThisLook)
        {
            foundThisLook = true; Found++; WallX += Mode.WallPenalty;
            Flash(Mode.WallPenalty > 0 ? "見つかった！\n締め切りが" + Mode.WallPenalty + "マス迫る" : "見つかった！\n（練習なので壁は動かない）",
                  new Color(1f, 0.35f, 0.35f), 1.6f);
        }
    }

    float NextBackDur() => Random.Range(Mode.BackMin, Mode.BackMax);

    void StepDaruma(float dt)
    {
        StateT += dt;
        if (State == DState.Back && StateT >= StateDur)
        {
            // 「もうすぐ振り向く」の合図はなし。いきなり振り向く
            if (quickNext) { quickNext = false; Flash("……と思ったら、もう一回！", new Color(1f, 0.54f, 0.54f), 1.3f); }
            else if (TutSetupActive) TutSeed();
            State = DState.Look; StateT = 0; foundThisLook = false;
            LookTsumo(); SwapFloor();
        }
        else if (State == DState.Look && StateT >= Mode.Look)
        {
            State = DState.Back; StateT = 0; StateDur = NextBackDur();
            if (Mode.Quick > 0 && Random.value < Mode.Quick) { quickNext = true; StateDur = 1.0f; }
            if (!foundThisLook) TutEvent("safeLook");
        }
    }

    // ===== 見た目の更新 =====
    void UpdateVisuals(float dt)
    {
        bool playing = Phase == Screen.Playing;
        bool looking = playing && State == DState.Look;

        // ダルマの向き・揺れ・光る目
        var d = Stage.Daruma;
        float targetY = looking ? 0f : 180f;
        d.rotation = Quaternion.Euler(0, Mathf.LerpAngle(d.eulerAngles.y, targetY, Mathf.Min(1, dt * 5)), 0);
        float shake = T < shakeUntil ? Mathf.Sin(T * 60) * 0.25f : 0;
        d.position = darumaBase + new Vector3(shake, 0, 0);
        foreach (var e in Stage.GlowEyes) e.SetActive(looking);
        float glareTarget = looking ? 25f + Mathf.Sin(Time.time * 11) * 4f : 0f;
        Stage.Glare.intensity = Mathf.Lerp(Stage.Glare.intensity, glareTarget, 0.15f);

        // 振り向き中は空気が赤く
        var fogTarget = looking ? Stage.FogLook : Stage.FogNormal;
        RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, fogTarget, 0.08f);
        Stage.Cam.backgroundColor = RenderSettings.fogColor;

        // 床の牌の色：ツモった跡・ほしい牌（緑）・ドラ
        foreach (var t in Stage.Tiles)
        {
            Color tint = Color.white;
            if (t.FlashUntil > T) tint = new Color(1f, 0.78f, 0.45f);
            else if (playing && ((TutSetupActive && System.Array.IndexOf(TutWin, t.Type) >= 0) || (HintsOn && Useful.Contains(t.Type))))
                tint = new Color(0.55f, 1f, 0.55f);
            else if (playing && t.Type == Dora) tint = new Color(1f, 0.92f, 0.6f);
            t.SetTint(tint);
        }

        // 自分の位置（なめらかに動かす）と足元の枠
        dx = Mathf.Lerp(dx, Cx, Mathf.Min(1, dt * 14));
        dy = Mathf.Lerp(dy, Cy, Mathf.Min(1, dt * 14));
        Stage.FootFrame.position = new Vector3(CellX(Cy), 0, CellZ(Cx));
        Stage.FootFrame.gameObject.SetActive(playing && UnderTile() != null);

        // 締め切りの壁（前面 = 壁の位置 + 1マス）
        float frontZ = (WallX + 1) * CZ;
        Stage.Wall.position = new Vector3(0, 1.2f, frontZ - Stage.WallDepth / 2);
        float dist = Cx - WallX - 1;
        Stage.SetEdgeColor(dist < 2 && Mathf.Sin(Time.time * 14) > 0 ? Color.white : new Color(1f, 0.25f, 0.25f));

        // カメラ：一人称。少し下を向いて足元の近くも見える。ドラッグで見回せる
        var cam = Stage.Cam;
        var target = new Vector3(CellX(dy), Eye, CellZ(dx) - 0.35f);
        camPos = Vector3.Lerp(camPos, target, 0.25f);
        if (!playing) camPos = target;
        cam.transform.position = camPos;
        float yaw = playing ? LookYaw : Mathf.Sin(Time.time / 4f) * 0.15f;
        cam.transform.rotation = Quaternion.Euler((0.21f - LookPitch) * Mathf.Rad2Deg, yaw * Mathf.Rad2Deg, 0);
    }
}
