using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// ===== マテリアルとフォントの共通処理 =====
public static class Mat
{
    static Shader lit;
    public static Shader Lit
    {
        get
        {
            if (lit == null) lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null) lit = Shader.Find("Standard");
            return lit;
        }
    }
    public static void SetColor(Material m, Color c)
    {
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
    }
    public static Color GetColor(Material m)
    {
        if (m.HasProperty("_BaseColor")) return m.GetColor("_BaseColor");
        if (m.HasProperty("_Color")) return m.GetColor("_Color");
        return Color.white;
    }
    public static void SetEmission(Material m, Color c)
    {
        m.EnableKeyword("_EMISSION");
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", c);
    }
}

public static class JPText
{
    // 日本語フォント：Resources/BrushFont.ttf（Yuji Syuku・筆文字）。なければ Unity 標準フォント
    // フォントを変えるときは BrushFont.ttf を差し替える
    public static Font Font
    {
        get
        {
            var f = Resources.Load<Font>("BrushFont");
            if (f == null) f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return f;
        }
    }

    public static TextMesh Make(Transform parent, string name, string text, Color color, float height,
                                Vector3 localPos, Quaternion localRot, Material mat)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = localRot;
        var tm = go.AddComponent<TextMesh>();
        tm.font = Font;
        tm.fontSize = 96;
        tm.characterSize = height / 9.6f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.fontStyle = FontStyle.Normal;
        tm.richText = false;
        tm.text = text;
        tm.color = color;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return tm;
    }
}

// ===== ステージ =====
// シーンに置かれた床・机・ダルマ・締め切りの壁・カメラへの参照をまとめて持つ。
// メニュー「だるま/ステージをシーンに作り直す」で組み立てられ、シーンに保存される。
// 机や壁の位置・色、ダルマの見た目などは Unity の画面で自由に編集してよい。
// （床の牌の画像・ダルマの向き・壁とカメラの位置はゲーム中にコードが動かす）
// 座標：前（ダルマ側）が +z。マス (col, row) の中心は x = (row - 2.5) * 1.2、z = (col + 0.5) * 1.6
[ExecuteAlways]
public class Stage : MonoBehaviour
{
    public const int CurrentVersion = 3;     // 組み立て方を変えたら上げる（古いステージは自動で作り直す）
    public int BuildVersion;

    [Header("シーン内の参照（作り直すと自動で入る）")]
    public Camera Cam;
    public FloorTile[] Tiles;                 // [col * Rows + row]
    public Transform Daruma;
    public Renderer DarumaBody;
    public TextMesh FaceLabel;
    public GameObject[] GlowEyes;
    public Light Glare;
    public Transform Wall;
    public Renderer WallEdge;
    public Transform FootFrame;
    public Material TextMaterial;

    [Header("霧の色（ふだん／振り向き中）")]
    public Color FogNormal = new Color(0.043f, 0.051f, 0.078f);
    public Color FogLook = new Color(0.17f, 0.024f, 0.035f);

    public const float WallDepth = 14f;
    Material darumaMat, edgeMat;

    // 文字のマテリアルにフォントの文字画像をつなぐ（編集画面でも文字が見えるように）
    static bool hooked;
    static Stage current;
    void OnEnable() { current = this; HookFontTexture(); }
    void HookFontTexture()
    {
        if (TextMaterial == null) return;
        var f = JPText.Font;
        TextMaterial.mainTexture = f.material.mainTexture;
        if (!hooked)
        {
            hooked = true;
            Font.textureRebuilt += font =>
            {
                if (current != null && current.TextMaterial != null && font == JPText.Font)
                    current.TextMaterial.mainTexture = font.material.mainTexture;
            };
        }
    }

    public FloorTile TileAt(int c, int r)
    {
        int i = c * DarumaGame.Rows + r;
        return (c >= 0 && c < DarumaGame.Cols && r >= 0 && r < DarumaGame.Rows && i < Tiles.Length) ? Tiles[i] : null;
    }

    // ゲーム開始時に1回呼ぶ
    public void InitRuntime()
    {
        HookFontTexture();
        darumaMat = DarumaBody.material;   // ゲーム中だけの複製（元のマテリアルは書き換えない）
        if (WallEdge != null) edgeMat = WallEdge.material;
    }

    public void ApplyMode(GameMode m)
    {
        if (darumaMat != null) Mat.SetColor(darumaMat, m.DarumaColor);
        FaceLabel.text = m.FaceLabel;
    }

    public void SetEdgeColor(Color c) { if (edgeMat != null) { Mat.SetColor(edgeMat, c); Mat.SetEmission(edgeMat, c); } }

    // ===== 組み立て =====
#if UNITY_EDITOR
    static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Daruma")) AssetDatabase.CreateFolder("Assets", "Daruma");
        if (!AssetDatabase.IsValidFolder("Assets/Daruma/Materials")) AssetDatabase.CreateFolder("Assets/Daruma", "Materials");
    }
#endif
    static Material MakeMat(string name, Color c, Color? emission = null)
    {
        Material m = null;
#if UNITY_EDITOR
        string path = "Assets/Daruma/Materials/" + name + ".mat";
        if (!Application.isPlaying)
        {
            EnsureFolder();
            m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(Mat.Lit); AssetDatabase.CreateAsset(m, path); }
        }
#endif
        if (m == null) m = new Material(Mat.Lit);
        m.name = name;
        Mat.SetColor(m, c);
        if (emission.HasValue) Mat.SetEmission(m, emission.Value);
#if UNITY_EDITOR
        if (!Application.isPlaying) EditorUtility.SetDirty(m);
#endif
        return m;
    }

    static Material MakeTextMat()
    {
        var sh = Shader.Find("Daruma/Text3D");
        Material m = null;
#if UNITY_EDITOR
        string path = "Assets/Daruma/Materials/Text3D.mat";
        if (!Application.isPlaying)
        {
            EnsureFolder();
            m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(sh != null ? sh : JPText.Font.material.shader); AssetDatabase.CreateAsset(m, path); }
        }
#endif
        if (m == null) m = new Material(sh != null ? sh : JPText.Font.material.shader);
        m.name = "Text3D";
        return m;
    }

    // だるまの顔の絵：光の当たり方に関係なくはっきり見えるよう Unlit、透明部分は切り抜き
    static Material MakeFaceMat()
    {
        var sh = Shader.Find("Universal Render Pipeline/Unlit");
        if (sh == null) sh = Shader.Find("Unlit/Transparent Cutout");
        Material m = null;
#if UNITY_EDITOR
        string path = "Assets/Daruma/Materials/DarumaFacePicture.mat";
        if (!Application.isPlaying)
        {
            EnsureFolder();
            m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(sh); AssetDatabase.CreateAsset(m, path); }
        }
#endif
        if (m == null) m = new Material(sh);
        m.shader = sh;
        m.name = "DarumaFacePicture";
        var tex = Resources.Load<Texture2D>("DarumaFace");
        if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
        if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white);
        if (m.HasProperty("_AlphaClip")) m.SetFloat("_AlphaClip", 1);
        if (m.HasProperty("_Cutoff")) m.SetFloat("_Cutoff", 0.5f);
        m.EnableKeyword("_ALPHATEST_ON");
        m.SetOverrideTag("RenderType", "TransparentCutout");
        m.renderQueue = 2450;
#if UNITY_EDITOR
        if (!Application.isPlaying) EditorUtility.SetDirty(m);
#endif
        return m;
    }

    static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 localPos, Vector3 scale, Material mat)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        var col = go.GetComponent<Collider>();
        if (Application.isPlaying) Destroy(col); else DestroyImmediate(col);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }
    static GameObject Group(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go;
    }

    public static Stage Build(Transform parent)
    {
        int cols = DarumaGame.Cols, rows = DarumaGame.Rows;
        float CX = DarumaGame.CX, CZ = DarumaGame.CZ;
        float halfW = rows * CX / 2f, floorLen = cols * CZ;
        var root = Group("Stage", parent);
        var st = root.AddComponent<Stage>();
        st.BuildVersion = CurrentVersion;
        var tr = root.transform;
        st.TextMaterial = MakeTextMat();
        st.TextMaterial.mainTexture = JPText.Font.material.mainTexture;

        // カメラ・ライト
        var camGo = Group("Main Camera", tr);
        camGo.tag = "MainCamera";
        st.Cam = camGo.AddComponent<Camera>();
        st.Cam.clearFlags = CameraClearFlags.SolidColor;
        st.Cam.backgroundColor = st.FogNormal;
        st.Cam.fieldOfView = 50;
        st.Cam.nearClipPlane = 0.1f;
        st.Cam.farClipPlane = 200f;
        camGo.AddComponent<AudioListener>();
        camGo.transform.position = new Vector3(0, DarumaGame.Eye, DarumaGame.CellZ(-1) - 0.35f);
        camGo.transform.rotation = Quaternion.Euler(12, 0, 0);

        var lightGo = Group("Directional Light", tr);
        lightGo.transform.rotation = Quaternion.Euler(50, -20, 0);
        var sun = lightGo.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = 0.8f;
        sun.shadows = LightShadows.None;

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.62f, 0.66f, 0.78f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = st.FogNormal;
        RenderSettings.fogStartDistance = 30;
        RenderSettings.fogEndDistance = 70;

        // 床と横の低い壁
        var baseMat = MakeMat("Floor", new Color(0.14f, 0.16f, 0.22f));
        var sideMat = MakeMat("SideWall", new Color(0.17f, 0.19f, 0.31f));
        var room = Group("Room", tr).transform;
        float midZ = floorLen / 2f - 4f;
        Prim(PrimitiveType.Cube, "Floor", room, new Vector3(0, -0.05f, midZ), new Vector3(halfW * 2 + 6, 0.1f, floorLen + 40), baseMat);
        foreach (int sx in new[] { -1, 1 })
            Prim(PrimitiveType.Cube, "Side Wall", room, new Vector3(sx * (halfW + 1.6f), 0.6f, midZ), new Vector3(0.4f, 1.2f, floorLen + 40), sideMat);

        // 机とモニター
        var deskMat = MakeMat("Desk", new Color(0.42f, 0.33f, 0.22f));
        var monMat = MakeMat("Monitor", new Color(0.07f, 0.07f, 0.07f), new Color(0.1f, 0.22f, 0.4f));
        var desks = Group("Desks", tr).transform;
        for (int i = 0; i < 7; i++)
        {
            foreach (int sx in new[] { -1, 1 })
            {
                float z = 2 + i * 5 + (sx > 0 ? 2.5f : 0);
                var desk = Group("Desk " + (i * 2 + (sx > 0 ? 1 : 0) + 1), desks).transform;
                desk.localPosition = new Vector3(sx * (halfW + 0.85f), 0, z);
                Prim(PrimitiveType.Cube, "Top", desk, new Vector3(0, 0.375f, 0), new Vector3(0.8f, 0.75f, 1.6f), deskMat);
                Prim(PrimitiveType.Cube, "Monitor", desk, new Vector3(sx * 0.25f, 1.0f, 0), new Vector3(0.08f, 0.45f, 0.7f), monMat);
            }
        }
        // 5マスごとの目印
        for (int c = 0; c <= cols; c += 5)
        {
            string label = c == 0 ? "スタート" : c == cols ? "ゴール" : c + "マス";
            JPText.Make(room, "Label " + label, label, new Color(0.54f, 0.58f, 0.72f), 0.45f,
                new Vector3(-halfW - 0.9f, 0.02f, c * CZ), Quaternion.Euler(90, 0, 90), st.TextMaterial);
        }

        // 床の牌：側面＋上面（牌の画像）
        var sideTileMat = MakeMat("TileSide", new Color(0.84f, 0.8f, 0.71f));
        var topMat = MakeMat("TileTop", Color.white);
        var tilesRoot = Group("Tiles", tr).transform;
        var tiles = new FloorTile[cols * rows];
        for (int c = 0; c < cols; c++)
        {
            for (int r = 0; r < rows; r++)
            {
                var go = Group("Tile " + c + "-" + r, tilesRoot);
                go.transform.localPosition = new Vector3(DarumaGame.CellX(r), 0, DarumaGame.CellZ(c));
                var t = go.AddComponent<FloorTile>();
                t.Col = c; t.Row = r;
                Prim(PrimitiveType.Cube, "Body", go.transform, new Vector3(0, 0.07f, 0), new Vector3(CX * 0.9f, 0.14f, CZ * 0.9f), sideTileMat);
                var top = Prim(PrimitiveType.Quad, "Top (牌の画像)", go.transform, new Vector3(0, 0.141f, 0), new Vector3(CX * 0.9f, CZ * 0.9f, 1), topMat);
                top.transform.localRotation = Quaternion.Euler(90, 0, 0);
                t.Top = top.GetComponent<Renderer>();
                t.SetType((c * 7 + r * 5) % 27);
                tiles[c * rows + r] = t;
            }
        }
        st.Tiles = tiles;

        // 足元の黄色い枠
        var frameMat = MakeMat("FootFrame", new Color(1f, 0.83f, 0.42f), new Color(1f, 0.83f, 0.42f) * 0.8f);
        st.FootFrame = Group("Foot Frame (足元の枠)", tr).transform;
        Prim(PrimitiveType.Cube, "F", st.FootFrame, new Vector3(0, 0.18f, CZ * 0.47f), new Vector3(CX * 0.98f, 0.08f, 0.1f), frameMat);
        Prim(PrimitiveType.Cube, "B", st.FootFrame, new Vector3(0, 0.18f, -CZ * 0.47f), new Vector3(CX * 0.98f, 0.08f, 0.1f), frameMat);
        Prim(PrimitiveType.Cube, "L", st.FootFrame, new Vector3(-CX * 0.47f, 0.18f, 0), new Vector3(0.1f, 0.08f, CZ * 0.98f), frameMat);
        Prim(PrimitiveType.Cube, "R", st.FootFrame, new Vector3(CX * 0.47f, 0.18f, 0), new Vector3(0.1f, 0.08f, CZ * 0.98f), frameMat);
        st.FootFrame.localPosition = new Vector3(DarumaGame.CellX(2), 0, DarumaGame.CellZ(-1));

        // ダルマ
        st.Daruma = Group("Daruma", tr).transform;
        st.Daruma.localPosition = new Vector3(0, 0, (cols + 3) * CZ);
        var bodyMat = MakeMat("DarumaBody", new Color(0.78f, 0.13f, 0.16f));
        var glowMat = MakeMat("EyeGlow", new Color(1f, 0.1f, 0.1f), new Color(3f, 0.2f, 0.2f));
        st.DarumaBody = Prim(PrimitiveType.Sphere, "Body", st.Daruma, new Vector3(0, 2.4f, 0), new Vector3(4.8f, 5.2f, 4.8f), bodyMat).GetComponent<Renderer>();
        var face = Group("Face (こちら側が正面)", st.Daruma).transform;
        // 顔：伝統的なだるまの顔の絵（鶴の眉・亀のひげ・金の縁取り）。Resources/DarumaFace.png
        Prim(PrimitiveType.Quad, "Face Picture", face, new Vector3(0, 2.6f, -2.45f), new Vector3(3.6f, 3.6f, 1), MakeFaceMat());
        st.GlowEyes = new[]
        {
            Prim(PrimitiveType.Sphere, "Glow L (振り向き中だけ表示)", face, new Vector3(-0.58f, 3.05f, -2.5f), Vector3.one * 0.3f, glowMat),
            Prim(PrimitiveType.Sphere, "Glow R (振り向き中だけ表示)", face, new Vector3(0.58f, 3.05f, -2.5f), Vector3.one * 0.3f, glowMat),
        };
        foreach (var e in st.GlowEyes) e.SetActive(false);
        // おなかの金文字（モードごとに GPA／教授／ボス）
        st.FaceLabel = JPText.Make(face, "Label", "GPA", new Color(1f, 0.8f, 0.25f), 0.9f, new Vector3(0, 0.75f, -1.95f), Quaternion.Euler(-20, 0, 0), st.TextMaterial);
        var glareGo = Group("Glare (振り向き中の赤いライト)", st.Daruma);
        glareGo.transform.localPosition = new Vector3(0, 3, -4);
        st.Glare = glareGo.AddComponent<Light>();
        st.Glare.type = LightType.Point;
        st.Glare.color = new Color(1f, 0.16f, 0.16f);
        st.Glare.range = 50;
        st.Glare.intensity = 0;
        st.Daruma.localRotation = Quaternion.Euler(0, 180, 0);

        // 締め切りの壁（後ろから迫ってくる）
        st.Wall = Group("Deadline Wall (締め切りの壁)", tr).transform;
        var wallMat = MakeMat("DeadlineWall", new Color(0.48f, 0.04f, 0.06f), new Color(0.16f, 0.01f, 0.02f));
        Prim(PrimitiveType.Cube, "Body", st.Wall, Vector3.zero, new Vector3(halfW * 2 + 3.4f, 2.4f, WallDepth), wallMat);
        JPText.Make(st.Wall, "Label Front", "締切", Color.white, 1.6f, new Vector3(0, 0, WallDepth / 2 + 0.02f), Quaternion.Euler(0, 180, 0), st.TextMaterial);
        JPText.Make(st.Wall, "Label Top", "締切", Color.white, 3f, new Vector3(0, 1.22f, 0), Quaternion.Euler(90, 0, 0), st.TextMaterial);
        var edgeMat = MakeMat("WallEdge", new Color(1f, 0.25f, 0.25f), new Color(1f, 0.25f, 0.25f));
        st.WallEdge = Prim(PrimitiveType.Cube, "Edge", st.Wall, new Vector3(0, 1.25f, WallDepth / 2), new Vector3(halfW * 2 + 3.4f, 0.12f, 0.25f), edgeMat).GetComponent<Renderer>();
        st.Wall.localPosition = new Vector3(0, 1.2f, (DarumaGame.WallStart + 1) * CZ - WallDepth / 2);

        return st;
    }
}
