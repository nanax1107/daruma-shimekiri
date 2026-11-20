using System.Collections.Generic;
using UnityEngine;

// ゲーム全体の進行（GameManager）。プレイヤー・ダルマ・壁・手牌・チュートリアルのルールをまとめて持つ
public class DarumaGame : MonoBehaviour
{
    public static DarumaGame I;

    // ===== 共通パラメータ =====
    public const int Cols = 6, Rows = 20;
    public const float TileSize = 1.5f;
    public const float MoveSpeed = 3.2f;
    public const float Grace = 0.4f;          // 振り向き直後に判定しない時間
    public const float WallStart = -6f;       // 壁の初期位置（床に沿った距離）
    public const float Eye = 2.3f;            // 目の高さ
    public float HalfW => Cols * TileSize / 2f;
    public float FloorLen => Rows * TileSize;

    // ===== 状態 =====
    public enum Screen { Title, Playing, Result }
    public enum DState { Back, Warn, Look }

    public Screen Phase = Screen.Title;
    public GameMode Mode = GameModes.Gakka;
    public Stage Stage;

    public int Hp;
    public List<int> Hand = new List<int>();
    public int Drawn = -1;                    // ツモった牌（-1 = なし）
    public int Sel;
    public WinResult Win;
    public int MoiiyaLeft, Found;
    public int Dora, DoraInd;
    public float WallS, Px, Pz, Yaw, Pitch;
    public int Cell = -1;
    public DState State;
    public float StateT, StateDur;
    public float T;
    public string Msg = "";
    public Color MsgColor = Color.white;
    public float MsgUntil;
    public bool Cleared;
    public int Tut;

    bool foundThisLook, quickNext;
    float shakeUntil, moved, turned, lastYaw;
    Vector3 darumaBase;

    void Awake()
    {
        I = this;
        // シーンに置かれたステージを使う（なければその場で組み立てる）
        Stage = FindFirstObjectByType<Stage>();
        if (Stage == null) Stage = Stage.Build(transform, Cols, Rows, TileSize);
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
        Phase = Screen.Playing;
        LockCursor(true);
    }

    public void BackToTitle() { Phase = Screen.Title; LockCursor(false); }

    void ResetGame()
    {
        Hp = Mode.Hp;
        Hand = TileUtil.Deal13(); Drawn = -1; Sel = 0; Win = null;
        MoiiyaLeft = Mode.Moiiya; Found = 0;
        WallS = WallStart;
        Px = 0; Pz = -1.8f; Yaw = 0; Pitch = 0.12f;
        Cell = -1;
        State = DState.Back; StateT = 0; StateDur = 4; foundThisLook = false; quickNext = false;
        T = 0; Msg = ""; MsgUntil = 0; shakeUntil = 0;
        Tut = 0; moved = 0; turned = 0; lastYaw = 0;
        RollDora();
        foreach (var t in Stage.Tiles) { t.SetType(TileUtil.RandomTile()); t.FlashUntil = 0; }
        Stage.Daruma.rotation = Quaternion.Euler(0, 180, 0);
    }

    void End(bool cleared)
    {
        Cleared = cleared;
        Phase = Screen.Result;
        LockCursor(false);
    }

    static void LockCursor(bool on)
    {
        Cursor.lockState = on ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !on;
    }
    public static bool Locked => Cursor.lockState == CursorLockMode.Locked;

    public void Flash(string text, Color color, float dur)
    {
        Msg = text; MsgColor = color; MsgUntil = T + dur;
    }

    // ===== 手牌 =====
    public List<int> FullHand()
    {
        var l = new List<int>(Hand);
        if (Drawn >= 0) l.Add(Drawn);
        return l;
    }
    public int MaxSel => Drawn >= 0 ? 13 : 12;

    void RollDora()
    {
        DoraInd = TileUtil.RandomTile();
        Dora = TileUtil.DoraFromIndicator(DoraInd);
    }

    void CheckWin() { Win = Drawn >= 0 ? HandEvaluator.Evaluate(FullHand(), Dora) : null; }

    void TsumoTile(int t) { Drawn = t; Sel = 13; CheckWin(); }

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
        Sel = Mathf.Min(Sel, 12);
        Win = null;
        TutEvent("discard");
    }

    public void ClickTile(int i)
    {
        if (Sel == i) Discard(i);
        else Sel = i;
    }

    void DeclareWin()
    {
        if (Win == null) return;
        var r = Win;
        Hp = Mathf.Max(0, Hp - r.Han);
        shakeUntil = T + 0.6f;
        float before = WallS;
        WallS = Mathf.Max(WallStart, WallS - Mode.WallPushback);
        float back = before - WallS;
        RollDora();
        Flash(r.YakuNames + "\n" + r.Han + "翻 ダメージ！" +
              (back > 0.05f ? "\n締め切りが" + back.ToString("0.0") + "m後退！" : "") + "\nドラが変わった",
              new Color(1f, 0.83f, 0.42f), 2.8f);
        Hand = TileUtil.Deal13(); Drawn = -1; Sel = 0; Win = null;
        TutEvent("win");
        if (Hp <= 0) End(true);
    }

    void Moiiya()
    {
        if (Drawn >= 0) { Flash("先に1枚捨ててから", Color.gray, 1.2f); return; }
        if (MoiiyaLeft <= 0) { Flash("もう使えない…", Color.gray, 1.2f); return; }
        MoiiyaLeft--;
        Hand = TileUtil.Deal13(); Sel = 0;
        WallS += Mode.WallPenalty;
        Flash(Mode.WallPenalty > 0 ? "もういいや…\n締め切りが" + Mode.WallPenalty + "m迫る" : "もういいや…\n手牌を配り直した",
              new Color(1f, 0.54f, 0.54f), 1.8f);
        TutEvent("moiiya");
    }

    // ダルマが振り向いた瞬間に足元の牌をツモる
    void LookTsumo()
    {
        if (Cell < 0) { Flash("牌の上にいない…\nツモなし", Color.gray, 1.4f); return; }
        string cut = "";
        if (Drawn >= 0) { cut = "（捨てなかった牌はツモ切り）"; Drawn = -1; }
        var tile = Stage.Tiles[Cell];
        int t = tile.Type;
        TsumoTile(t);
        tile.SetType(TileUtil.RandomTile());
        tile.FlashUntil = T + 0.6f;
        Flash(TileUtil.Name(t) + " をツモ！" + (cut != "" ? "\n" + cut : ""), Color.white, 1.3f);
        TutEvent("tsumo");
    }

    void SwapFloor()
    {
        foreach (var t in Stage.Tiles)
            if (Random.value < Mode.SwapRate) { t.SetType(TileUtil.RandomTile()); t.FlashUntil = T + 0.5f; }
    }

    // ===== チュートリアル =====
    public static readonly (string ev, string text, bool setup)[] TutSteps =
    {
        ("move", "WASD で歩いてみよう。", false),
        ("turn", "マウス（または ← → キー）で、まわりを見渡してみよう。", false),
        ("tsumo", "上の状態バーが緑の間は動ける。好きな牌の上に立って、ダルマが振り向くのを待とう。振り向いた瞬間、足元の牌がツモになる。", false),
        ("discard", "ツモできた！ 振り向き中は動かずに、ホイールか Z・X で捨てる牌を選び、左クリックか F で1枚捨てよう。", false),
        ("safeLook", "振り向き中に動くと見つかって、ふだんは後ろの「締切」の壁が迫ってくる。次の振り向きの間、最後まで止まっていよう。", false),
        ("moiiya", "手が悪いときは Q で「もういいや」。手牌を全部配り直せる（本番は回数制限あり・壁が迫る）。押してみよう。", false),
        ("win", "最後は上がりの練習。あと1枚で上がれる手を用意した。緑色に光っている 4萬 か 7萬 の上に立って振り向きを待ち、ツモったら Space で攻撃！", true),
    };
    public static readonly int[] TutWin = { 3, 6 };
    public bool TutSetupActive => Mode.Tutorial && Tut < TutSteps.Length && TutSteps[Tut].setup;

    void TutSeed()
    {
        int k = 0, pr = Mathf.Max(0, Mathf.FloorToInt(Pz / TileSize));
        for (int i = 0; i < Stage.Tiles.Length; i++)
        {
            int r = i / Cols;
            if (Mathf.Abs(r - pr) <= 1 && i != Cell) Stage.Tiles[i].SetType(TutWin[(k++) % 2]);
        }
    }

    void TutEvent(string ev)
    {
        if (!Mode.Tutorial || Tut >= TutSteps.Length || TutSteps[Tut].ev != ev) return;
        Tut++;
        if (Tut >= TutSteps.Length) return;
        if (TutSteps[Tut].setup)
        {
            Drawn = -1; Sel = 0;
            Hand = new List<int> { 1, 2, 3, 4, 5, 10, 11, 12, 13, 13, 19, 20, 21 };   // 4萬・7萬待ち
            TutSeed();
        }
        Flash("OK！", new Color(0.61f, 0.76f, 1f), 0.9f);
    }

    // ===== 毎フレーム =====
    void Update()
    {
        float dt = Mathf.Min(Time.deltaTime, 0.05f);
        if (Phase == Screen.Playing)
        {
            T += dt;
            HandleActions();
            float speed = StepPlayer(dt);
            moved += speed * dt;
            turned += Mathf.Abs(Yaw - lastYaw); lastYaw = Yaw;
            if (moved > 2) TutEvent("move");
            if (turned > 0.8f) TutEvent("turn");
            StepFloor();
            StepDaruma(dt, speed);
            WallS += Mode.WallSpeed * dt;
            if (Pz - WallS <= 0.45f) End(false);
        }
        else
        {
            Yaw = Mathf.Sin(Time.time / 4f) * 0.15f;
        }
        UpdateVisuals(dt);
    }

    void HandleActions()
    {
        if (GameInput.Down(GKey.Space)) DeclareWin();
        if (GameInput.Down(GKey.Q)) Moiiya();
        if (GameInput.Down(GKey.F) || GameInput.Down(GKey.Enter)) Discard(Sel);
        if (GameInput.Down(GKey.Z)) Sel = Sel <= 0 ? MaxSel : Sel - 1;
        if (GameInput.Down(GKey.X)) Sel = Sel >= MaxSel ? 0 : Sel + 1;
        float sc = GameInput.Scroll();
        if (sc != 0) Sel = Mathf.Clamp(Sel + (sc < 0 ? 1 : -1), 0, MaxSel);
        if (GameInput.Down(GKey.Escape)) LockCursor(false);
        if (GameInput.LeftDown())
        {
            if (Locked) Discard(Sel);
            else if (!GameHUD.PointerOverHand) LockCursor(true);
        }
        if (Locked)
        {
            var d = GameInput.MouseDelta();
            Yaw += d.x * 0.0025f;
            Pitch = Mathf.Clamp(Pitch - d.y * 0.0025f, -0.6f, 1.0f);
        }
    }

    float StepPlayer(float dt)
    {
        float f = 0, s = 0;
        if (GameInput.Held(GKey.W)) f += 1;
        if (GameInput.Held(GKey.S)) f -= 1;
        if (GameInput.Held(GKey.D)) s += 1;
        if (GameInput.Held(GKey.A)) s -= 1;
        if (GameInput.Held(GKey.Left)) Yaw -= 2.2f * dt;
        if (GameInput.Held(GKey.Right)) Yaw += 2.2f * dt;
        float len = Mathf.Sqrt(f * f + s * s);
        if (len < 0.001f) return 0;
        float fx = Mathf.Sin(Yaw), fz = Mathf.Cos(Yaw);
        float rx = Mathf.Cos(Yaw), rz = -Mathf.Sin(Yaw);
        float ox = Px, oz = Pz;
        Px += (fx * f + rx * s) / len * MoveSpeed * dt;
        Pz += (fz * f + rz * s) / len * MoveSpeed * dt;
        Px = Mathf.Clamp(Px, -HalfW + 0.25f, HalfW - 0.25f);
        Pz = Mathf.Clamp(Pz, WallS + 0.2f, FloorLen - 0.3f);
        return Mathf.Sqrt((Px - ox) * (Px - ox) + (Pz - oz) * (Pz - oz)) / dt;
    }

    void StepFloor()
    {
        int c = Mathf.FloorToInt((Px + HalfW) / TileSize), r = Mathf.FloorToInt(Pz / TileSize);
        Cell = (c >= 0 && c < Cols && r >= 0 && r < Rows) ? r * Cols + c : -1;
    }

    float NextBackDur() => Random.Range(Mode.BackMin, Mode.BackMax);

    void StepDaruma(float dt, float speed)
    {
        StateT += dt;
        if (State == DState.Back && StateT >= StateDur && quickNext)
        {
            // 振り向き直し：合図なしでいきなり振り向く
            quickNext = false;
            State = DState.Look; StateT = 0; foundThisLook = false;
            Flash("……と思ったら、もう一回！", new Color(1f, 0.54f, 0.54f), 1.3f);
            LookTsumo(); SwapFloor();
        }
        else if (State == DState.Back && StateT >= StateDur)
        {
            State = DState.Warn; StateT = 0;
            if (TutSetupActive) TutSeed();
        }
        else if (State == DState.Warn && StateT >= Mode.Warn && Mode.Feint > 0 && Random.value < Mode.Feint)
        {
            // フェイント：振り向くふりをしてやめる
            State = DState.Back; StateT = 0; StateDur = NextBackDur();
            shakeUntil = T + 0.4f;
            Flash("……やっぱりいいや", new Color(0.79f, 0.71f, 1f), 1.3f);
        }
        else if (State == DState.Warn && StateT >= Mode.Warn)
        {
            State = DState.Look; StateT = 0; foundThisLook = false;
            LookTsumo(); SwapFloor();
        }
        else if (State == DState.Look)
        {
            if (!foundThisLook && StateT > Grace && speed > Mode.DetectSpeed)
            {
                foundThisLook = true; Found++;
                WallS += Mode.WallPenalty;
                Flash(Mode.WallPenalty > 0 ? "見つかった！\n締め切りが" + Mode.WallPenalty + "m迫る" : "見つかった！\n（練習なので壁は動かない）",
                      new Color(1f, 0.35f, 0.35f), 1.6f);
            }
            if (StateT >= Mode.Look)
            {
                State = DState.Back; StateT = 0; StateDur = NextBackDur();
                if (Mode.Quick > 0 && Random.value < Mode.Quick) { quickNext = true; StateDur = 1.0f; }
                if (!foundThisLook) TutEvent("safeLook");
            }
        }
    }

    // ===== 見た目の更新 =====
    void UpdateVisuals(float dt)
    {
        bool playing = Phase == Screen.Playing;
        bool looking = playing && State == DState.Look, warning = playing && State == DState.Warn;

        // ダルマの向き・揺れ・光る目
        var d = Stage.Daruma;
        float targetY = looking ? 0f : 180f;
        float curY = d.eulerAngles.y;
        d.rotation = Quaternion.Euler(0, Mathf.LerpAngle(curY, targetY, Mathf.Min(1, dt * 5)), 0);
        float shake = T < shakeUntil ? Mathf.Sin(T * 60) * 0.25f : 0;
        d.position = darumaBase + new Vector3(shake, 0, 0);
        foreach (var e in Stage.GlowEyes) e.SetActive(looking);
        float glareTarget = looking ? 25f + Mathf.Sin(Time.time * 11) * 4f : 0f;
        Stage.Glare.intensity = Mathf.Lerp(Stage.Glare.intensity, glareTarget, 0.15f);

        // 振り向き中は空気が赤く、合図中は黄色っぽく
        var fogTarget = looking ? Stage.FogLook : warning ? Stage.FogWarn : Stage.FogNormal;
        RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, fogTarget, 0.08f);
        Stage.Cam.backgroundColor = RenderSettings.fogColor;

        // 床の牌：遠いほどこちらに起き上がる＋色の強調
        for (int i = 0; i < Stage.Tiles.Length; i++)
        {
            var t = Stage.Tiles[i];
            float dist = t.S - Pz;
            float tilt = Mathf.Clamp01((dist - 5f) / 16f) * 0.6f;
            t.transform.localRotation = Quaternion.Euler(-tilt * Mathf.Rad2Deg, 0, 0);
            t.transform.localPosition = new Vector3(t.X, 0.06f + Mathf.Sin(tilt) * TileSize * 0.47f, t.S);
            Color tint = new Color(0.96f, 0.95f, 0.9f);
            if (t.FlashUntil > T) tint = new Color(1f, 0.78f, 0.35f);
            else if (playing && i == Cell) tint = new Color(0.7f, 0.82f, 1f);
            else if (TutSetupActive && System.Array.IndexOf(TutWin, t.Type) >= 0) tint = new Color(0.55f, 1f, 0.55f);
            else if (t.Type == Dora) tint = new Color(1f, 0.9f, 0.55f);
            t.SetTint(tint);
        }

        // 締め切りの壁
        Stage.Wall.position = new Vector3(0, Stage.WorldY(WallS) + 3, Stage.WorldZ(WallS));
        Stage.Wall.rotation = Quaternion.Euler(WallS > 0 ? -Stage.Th * Mathf.Rad2Deg : 0, 0, 0);

        // カメラ
        var cam = Stage.Cam;
        cam.transform.position = new Vector3(Px, Stage.WorldY(Pz) + Eye, Stage.WorldZ(Pz));
        cam.transform.rotation = Quaternion.Euler(Pitch * Mathf.Rad2Deg, Yaw * Mathf.Rad2Deg, 0);
        bool zoom = playing && (GameInput.Held(GKey.E) || GameInput.RightHeld());
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, zoom ? 34f : 70f, 0.18f);
    }
}
