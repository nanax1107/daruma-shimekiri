using System.Collections.Generic;
using UnityEngine;

// 牌の番号：0〜8 = 萬子1〜9、9〜17 = 筒子1〜9、18〜26 = 索子1〜9（字牌は今回なし）
public static class TileUtil
{
    public static readonly string[] Suits = { "萬", "筒", "索" };
    static readonly string[] Kanji = { "一", "二", "三", "四", "五", "六", "七", "八", "九" };

    public static int Num(int t) => t % 9 + 1;
    public static int Suit(int t) => t / 9;
    public static string Name(int t) => Kanji[t % 9] + Suits[Suit(t)];
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

    // 牌の画像（Assets/Daruma/Resources/Tiles/t00〜t26.png）
    // 素材：FluffyStuff「riichi-mahjong-tiles」（パブリックドメイン CC0）
    static Texture2D[] tex;
    public static Texture2D Tex(int t)
    {
        if (tex == null || tex[0] == null)
        {
            tex = new Texture2D[27];
            for (int i = 0; i < 27; i++) tex[i] = Resources.Load<Texture2D>("Tiles/t" + i.ToString("00"));
        }
        return t >= 0 && t < 27 ? tex[t] : null;
    }
}
