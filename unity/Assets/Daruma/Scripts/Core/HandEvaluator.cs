using System.Collections.Generic;
using System.Linq;

// 上がり判定と役・翻数の計算（Unity に依存しない普通の C#）
public class WinResult
{
    public int Han;
    public List<(string name, int han)> Yaku = new List<(string, int)>();
    public string YakuNames => string.Join("・", Yaku.Select(y => y.name));
}

public static class HandEvaluator
{
    static bool CanMentsu(int[] c, int i, bool allowSeq)
    {
        while (i < 34 && c[i] == 0) i++;
        if (i == 34) return true;
        if (c[i] >= 3)
        {
            c[i] -= 3; bool ok = CanMentsu(c, i, allowSeq); c[i] += 3;
            if (ok) return true;
        }
        if (allowSeq && i < 27 && i % 9 <= 6 && c[i + 1] > 0 && c[i + 2] > 0)
        {
            c[i]--; c[i + 1]--; c[i + 2]--;
            bool ok = CanMentsu(c, i, allowSeq);
            c[i]++; c[i + 1]++; c[i + 2]++;
            if (ok) return true;
        }
        return false;
    }

    static bool IsStandard(int[] c, bool allowSeq)
    {
        for (int i = 0; i < 34; i++)
        {
            if (c[i] < 2) continue;
            c[i] -= 2; bool ok = CanMentsu(c, 0, allowSeq); c[i] += 2;
            if (ok) return true;
        }
        return false;
    }

    static bool IsChiitoi(int[] c)
    {
        int pairs = 0;
        for (int i = 0; i < 34; i++)
        {
            if (c[i] == 2) pairs++;
            else if (c[i] != 0) return false;
        }
        return pairs == 7;
    }

    // 14枚を渡す。上がっていなければ null
    public static WinResult Evaluate(IList<int> tiles, int dora)
    {
        var c = new int[34];
        foreach (var t in tiles) c[t]++;
        bool std = IsStandard(c, true), chi = IsChiitoi(c);
        if (!std && !chi) return null;

        bool tanyao = tiles.All(t => t % 9 >= 1 && t % 9 <= 7);
        int s0 = tiles[0] / 9;
        bool chinitsu = tiles.All(t => t / 9 == s0);

        var basic = new List<(string, int)> { ("ツモ", 1) };
        if (tanyao) basic.Add(("タンヤオ", 1));
        if (chinitsu) basic.Add(("清一色", 6));

        WinResult best = null;
        void Consider(List<(string, int)> list)
        {
            int han = list.Sum(y => y.Item2);
            if (best == null || han > best.Han) best = new WinResult { Han = han, Yaku = list };
        }
        if (std)
        {
            var l = new List<(string, int)>(basic);
            if (IsStandard(c, false)) l.Add(("対々和", 2));
            Consider(l);
        }
        if (chi)
        {
            var l = new List<(string, int)>(basic) { ("七対子", 2) };
            Consider(l);
        }

        int doraCount = tiles.Count(t => t == dora);
        if (doraCount > 0) { best.Yaku.Add(("ドラ" + doraCount, doraCount)); best.Han += doraCount; }
        return best;
    }
}
