using UnityEngine;

// 床の牌1枚。上の面（Top）に本物の牌の画像を貼る
[ExecuteAlways]
public class FloorTile : MonoBehaviour
{
    public Renderer Top;
    public int Col, Row;      // Col = 前後（ダルマ側が大きい）、Row = 左右
    public int Type;

    [System.NonSerialized] public float FlashUntil;

    MaterialPropertyBlock mpb;
    Color lastTint = Color.clear;
    int lastType = -1;

    void OnEnable() { lastType = -1; Apply(Color.white); }

    public void SetType(int t) { Type = t; Apply(lastTint == Color.clear ? Color.white : lastTint); }

    public void SetTint(Color c) { Apply(c); }

    void Apply(Color tint)
    {
        if (Top == null) return;
        if (Type == lastType && tint == lastTint) return;
        if (mpb == null) mpb = new MaterialPropertyBlock();
        Top.GetPropertyBlock(mpb);
        var tex = TileUtil.Tex(Type);
        if (tex != null) { mpb.SetTexture("_BaseMap", tex); mpb.SetTexture("_MainTex", tex); }
        mpb.SetColor("_BaseColor", tint); mpb.SetColor("_Color", tint);
        Top.SetPropertyBlock(mpb);
        lastType = Type; lastTint = tint;
    }
}
