using System.Collections.Generic;
using UnityEngine;

// ヒント：ほしい牌・捨てるといい牌を計算する（Unity にほぼ依存しない普通の C#）
// 向聴数（シャンテン数）＝あと何枚入れ替えれば「あと1枚で上がり」になるか。-1 = 上がり
public static class Hints
{
    static int best;

    static void Rec(int[] c, int i, int m, int t, int p)
    {
        while (i < 27 && c[i] == 0) i++;
        if (i >= 27)
        {
            int s = 8 - 2 * m - Mathf.Min(t, 4 - m) - p;
            if (s < best) best = s;
            return;
        }
        if (c[i] >= 3) { c[i] -= 3; Rec(c, i, m + 1, t, p); c[i] += 3; }
        if (i % 9 <= 6 && c[i + 1] > 0 && c[i + 2] > 0)
        {
            c[i]--; c[i + 1]--; c[i + 2]--; Rec(c, i, m + 1, t, p); c[i]++; c[i + 1]++; c[i + 2]++;
        }
        if (p == 0 && c[i] >= 2) { c[i] -= 2; Rec(c, i, m, t, 1); c[i] += 2; }
        if (m + t < 4)
        {
            if (c[i] >= 2) { c[i] -= 2; Rec(c, i, m, t + 1, p); c[i] += 2; }
            if (i % 9 <= 7 && c[i + 1] > 0) { c[i]--; c[i + 1]--; Rec(c, i, m, t + 1, p); c[i]++; c[i + 1]++; }
            if (i % 9 <= 6 && c[i + 2] > 0) { c[i]--; c[i + 2]--; Rec(c, i, m, t + 1, p); c[i]++; c[i + 2]++; }
        }
        c[i]--; Rec(c, i, m, t, p); c[i]++;
    }

    public static int Shanten(List<int> tiles)
    {
        var c = new int[27];
        foreach (var t in tiles) c[t]++;
        best = 8;
        Rec(c, 0, 0, 0, 0);
        int pairs = 0, kinds = 0;
        for (int i = 0; i < 27; i++) { if (c[i] >= 2) pairs++; if (c[i] > 0) kinds++; }
        int chi = 6 - pairs + Mathf.Max(0, 7 - kinds);
        return Mathf.Min(best, chi);
    }

    // 13枚の手で、引くと向聴数が下がる牌の種類
    public static HashSet<int> Useful(List<int> hand13, out int s)
    {
        s = Shanten(hand13);
        var set = new HashSet<int>();
        var tmp = new List<int>(hand13) { 0 };
        for (int t = 0; t < 27; t++)
        {
            int n = 0; foreach (var x in hand13) if (x == t) n++;
            if (n >= 4) continue;
            tmp[13] = t;
            if (Shanten(tmp) < s) set.Add(t);
        }
        return set;
    }

    // ほしい牌が多すぎると光りすぎるので、引いた後にさらに伸びる牌を上位6種類に絞る
    static readonly Dictionary<string, HashSet<int>> cache = new Dictionary<string, HashSet<int>>();
    public static HashSet<int> Narrow(List<int> hand13, int s, HashSet<int> set)
    {
        if (set.Count <= 6 || s <= 0) return set;
        var sorted = new List<int>(hand13); sorted.Sort();
        string key = string.Join(",", sorted);
        if (cache.TryGetValue(key, out var hit)) return hit;
        var sc = new List<(int t, int score)>();
        foreach (var t in set)
        {
            var h14 = new List<int>(hand13) { t };
            int bestCount = 0;
            for (int i = 0; i < 14; i++)
            {
                if (h14.IndexOf(h14[i]) != i) continue;
                var rest = new List<int>(h14); rest.RemoveAt(i);
                var u = Useful(rest, out int s2);
                if (s2 < s && u.Count > bestCount) bestCount = u.Count;
            }
            sc.Add((t, bestCount));
        }
        sc.Sort((a, b) => b.score.CompareTo(a.score));
        var res = new HashSet<int>();
        for (int i = 0; i < Mathf.Min(6, sc.Count); i++) res.Add(sc[i].t);
        if (cache.Count > 50) cache.Clear();
        cache[key] = res;
        return res;
    }

    // 14枚（手牌13枚＋ツモった牌が最後）のうち、捨てるといい牌の位置
    public static int BestDiscard(List<int> all14)
    {
        int bestI = all14.Count - 1, bestScore = int.MaxValue;
        for (int i = 0; i < all14.Count; i++)
        {
            if (i > 0 && i < 13 && all14[i] == all14[i - 1]) continue;   // 同じ牌はまとめて判定
            var rest = new List<int>(all14); rest.RemoveAt(i);
            var u = Useful(rest, out int s);
            int score = s * 100 - u.Count;
            if (score < bestScore) { bestScore = score; bestI = i; }
        }
        return bestI;
    }
}
