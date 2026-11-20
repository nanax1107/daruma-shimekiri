using UnityEngine;

// 床の牌1枚。位置（行・列）はシーンに置かれた場所から読み取る
public class FloorTile : MonoBehaviour
{
    public Renderer Body;
    public TextMesh NumText, SuitText;

    [System.NonSerialized] public int Type;
    [System.NonSerialized] public float FlashUntil;
    [System.NonSerialized] public float S, X;   // 床に沿った距離・横位置

    Material mat;
    Color lastTint = Color.clear;

    public void InitRuntime()
    {
        S = transform.localPosition.z;
        X = transform.localPosition.x;
        mat = Body.material;   // ゲーム中だけの複製
    }

    public void SetType(int t)
    {
        Type = t;
        var c = TileUtil.SuitColors[TileUtil.Suit(t)];
        NumText.text = TileUtil.Num(t).ToString();
        SuitText.text = TileUtil.Suits[TileUtil.Suit(t)];
        NumText.color = c;
        SuitText.color = c;
    }

    public void SetTint(Color c)
    {
        if (mat == null || c == lastTint) return;
        lastTint = c;
        Mat.SetColor(mat, c);
    }
}
