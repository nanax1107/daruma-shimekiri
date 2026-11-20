using UnityEngine;

// モードごとの設定。数値を変えれば難易度を調整できる
public class GameMode
{
    public string Key, Name, Level, Description;
    public string DarumaName, FaceLabel, WarnText, LookText;
    public Color DarumaColor, LevelColor;
    public bool Tutorial;

    public int Hp, Moiiya;
    public float BackMin, BackMax, Warn, Look, DetectSpeed;
    public float WallSpeed, WallPenalty, WallPushback, SwapRate;
    public float Feint;   // 振り向くふりをしてやめる確率
    public float Quick;   // 振り向き直し（合図なしでもう一度振り向く）の確率
}

public static class GameModes
{
    public static readonly GameMode Tutorial = new GameMode
    {
        Key = "tut", Name = "チュートリアル", Level = "はじめての人", LevelColor = new Color(0.08f, 0.40f, 0.75f),
        Description = "指示に沿って操作を1つずつ練習。壁は動かないので負けない。",
        DarumaName = "練習ダルマ", FaceLabel = "練習", WarnText = "だるまさんが…", LookText = "振り向いたよ！ 止まって！",
        DarumaColor = new Color(0.88f, 0.48f, 0.0f), Tutorial = true,
        Hp = 1, Moiiya = 99, BackMin = 4, BackMax = 6, Warn = 2.5f, Look = 8, DetectSpeed = 0.3f,
        WallSpeed = 0, WallPenalty = 0, WallPushback = 0, SwapRate = 0.3f,
    };

    public static readonly GameMode Gakka = new GameMode
    {
        Key = "gakka", Name = "学科分けモード", Level = "やさしい", LevelColor = new Color(0.18f, 0.49f, 0.20f),
        Description = "GPAダルマ。振り向き8秒・壁はゆっくり・HP3。",
        DarumaName = "GPAダルマ", FaceLabel = "GPA", WarnText = "成績…どう…", LookText = "GPA足りてる？",
        DarumaColor = new Color(0.78f, 0.13f, 0.16f),
        Hp = 3, Moiiya = 3, BackMin = 3, BackMax = 6, Warn = 2, Look = 8, DetectSpeed = 0.3f,
        WallSpeed = 0.05f, WallPenalty = 2, WallPushback = 4, SwapRate = 0.3f,
    };

    public static readonly GameMode Zemi = new GameMode
    {
        Key = "zemi", Name = "ゼミ分けモード", Level = "ふつう", LevelColor = new Color(0.78f, 0.47f, 0.0f),
        Description = "教授ダルマ。振り向き6秒・壁が速い・HP5・フェイントあり。",
        DarumaName = "教授ダルマ", FaceLabel = "教授", WarnText = "志望理由…は…", LookText = "志望理由を言ってみて！",
        DarumaColor = new Color(0.42f, 0.18f, 0.70f),
        Hp = 5, Moiiya = 2, BackMin = 2.5f, BackMax = 5, Warn = 1.5f, Look = 6, DetectSpeed = 0.25f,
        WallSpeed = 0.08f, WallPenalty = 3, WallPushback = 3, SwapRate = 0.4f, Feint = 0.25f,
    };

    public static readonly GameMode Lab = new GameMode
    {
        Key = "lab", Name = "研究室分けモード", Level = "むずかしい", LevelColor = new Color(0.72f, 0.11f, 0.11f),
        Description = "ボスダルマ。振り向き5秒・壁がとても速い・HP6・振り向き直しあり。",
        DarumaName = "ボスダルマ", FaceLabel = "ボス", WarnText = "進捗…どう…", LookText = "進捗どうですか！",
        DarumaColor = new Color(0.12f, 0.12f, 0.14f),
        Hp = 6, Moiiya = 1, BackMin = 2, BackMax = 4, Warn = 1.2f, Look = 5, DetectSpeed = 0.2f,
        WallSpeed = 0.11f, WallPenalty = 3, WallPushback = 3, SwapRate = 0.5f, Feint = 0.3f, Quick = 0.25f,
    };

    public static readonly GameMode[] Selectable = { Tutorial, Gakka, Zemi, Lab };
}
