using UnityEngine;

// モードごとの設定。数値を変えれば難易度を調整できる（距離の単位は「マス」、時間は「秒」）
public class GameMode
{
    public string Key, Name, Level, Note;
    public string DarumaName, FaceLabel, LookText;
    public Color DarumaColor, LevelColor;
    public bool Tutorial;
    public bool Hints;    // ほしい牌を緑に光らせる（チュートリアルと一番やさしいモードだけ）

    public int Hp, Moiiya;
    public float BackMin, BackMax, Look;
    public float WallSpeed, WallPenalty, WallPushback, SwapRate;
    public float Quick;   // 振り向き直し（すぐもう一度振り向く）の確率
}

public static class GameModes
{
    public static readonly GameMode Tutorial = new GameMode
    {
        Key = "tut", Name = "チュートリアル", Level = "はじめての人", LevelColor = new Color(0.08f, 0.40f, 0.75f),
        Note = "麻雀を知らなくてもOK",
        DarumaName = "練習ダルマ", FaceLabel = "練習", LookText = "振り向いたよ！ 止まって！",
        DarumaColor = new Color(0.88f, 0.48f, 0.0f), Tutorial = true, Hints = true,
        Hp = 1, Moiiya = 99, BackMin = 7, BackMax = 10, Look = 8,
        WallSpeed = 0, WallPenalty = 0, WallPushback = 0, SwapRate = 0.3f,
    };

    public static readonly GameMode Gakka = new GameMode
    {
        Key = "gakka", Name = "学科分けモード", Level = "やさしい", LevelColor = new Color(0.18f, 0.49f, 0.20f),
        DarumaName = "GPAダルマ", FaceLabel = "GPA", LookText = "GPA足りてる？",
        DarumaColor = new Color(0.78f, 0.13f, 0.16f), Hints = true,
        Hp = 3, Moiiya = 3, BackMin = 6, BackMax = 10, Look = 8,
        WallSpeed = 0.035f, WallPenalty = 1.5f, WallPushback = 3, SwapRate = 0.3f,
    };

    public static readonly GameMode Zemi = new GameMode
    {
        Key = "zemi", Name = "ゼミ分けモード", Level = "ふつう", LevelColor = new Color(0.78f, 0.47f, 0.0f),
        DarumaName = "教授ダルマ", FaceLabel = "教授", LookText = "志望理由を言ってみて！",
        DarumaColor = new Color(0.42f, 0.18f, 0.70f),
        Hp = 5, Moiiya = 2, BackMin = 5, BackMax = 8, Look = 6,
        WallSpeed = 0.055f, WallPenalty = 2, WallPushback = 2.5f, SwapRate = 0.4f,
    };

    public static readonly GameMode Lab = new GameMode
    {
        Key = "lab", Name = "研究室分けモード", Level = "むずかしい", LevelColor = new Color(0.72f, 0.11f, 0.11f),
        DarumaName = "ボスダルマ", FaceLabel = "ボス", LookText = "進捗どうですか！",
        DarumaColor = new Color(0.12f, 0.12f, 0.14f),
        Hp = 6, Moiiya = 1, BackMin = 4, BackMax = 7, Look = 5,
        WallSpeed = 0.075f, WallPenalty = 2, WallPushback = 2, SwapRate = 0.5f, Quick = 0.25f,
    };

    public static readonly GameMode[] Selectable = { Tutorial, Gakka, Zemi, Lab };
}
