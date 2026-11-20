using System.Collections.Generic;
using UnityEngine;

// 牌の番号：0〜8 = 萬子1〜9、9〜17 = 筒子1〜9、18〜26 = 索子1〜9（字牌は今回なし）
public static class TileUtil
{
    public static readonly string[] Suits = { "萬", "筒", "索" };
    public static readonly Color[] SuitColors =
    {
        new Color(0.78f, 0.16f, 0.16f),
        new Color(0.10f, 0.34f, 0.77f),
        new Color(0.11f, 0.50f, 0.23f),
    };

    public static int Num(int t) => t % 9 + 1;
    public static int Suit(int t) => t / 9;
    public static string Name(int t) => Num(t) + Suits[Suit(t)];
    public static int RandomTile() => Random.Range(0, 27);

    public static List<int> Deal13()
    {
        var hand = new List<int>();
        for (int i = 0; i < 13; i++) hand.Add(RandomTile());
        hand.Sort();
        return hand;
    }

    // ドラ表示牌の次の数字（9の次は1）
    public static int DoraFromIndicator(int ind) => Suit(ind) * 9 + (ind % 9 + 1) % 9;

    public static string ColorTag(int t, string text) =>
        "<color=#" + ColorUtility.ToHtmlStringRGB(SuitColors[Suit(t)]) + ">" + text + "</color>";
}
