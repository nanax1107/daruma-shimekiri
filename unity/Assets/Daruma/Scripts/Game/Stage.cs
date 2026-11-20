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
    // 日本語フォント：Resources/JPFont があればそれ、なければ Unity 標準フォント（Mac/Windows では OS の日本語フォントで表示される）
    // WebGL で公開するときは Assets/Daruma/Resources/JPFont.ttf を入れること
    public static Font Font
    {
        get
        {
            var f = Resources.Load<Font>("JPFont");
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
        tm.fontStyle = FontStyle.Bold;
        tm.richText = false;
        tm.text = text;
        tm.color = color;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return tm;
    }
}

// ===== ステージ =====
// シーンに置かれた床・壁・机・ダルマ・締め切りの壁・カメラへの参照をまとめて持つ。
// メニュー「だるま/ステージをシーンに作り直す」で組み立てられ、シーンに保存される。
// 机や壁の位置・色、ダルマの見た目などは Unity の画面で自由に編集してよい。
// （床の牌の位置・ダルマの位置・壁とカメラの位置はゲーム中にコードが動かす）
[ExecuteAlways]
public class Stage : MonoBehaviour
{
    // 床の傾斜：s（床に沿った距離）> 0 の範囲が奥に向かって上り坂
    public const float Slope = 0.28f;
    public static readonly float Th = Mathf.Atan(Slope);
    public static readonly float Cos = Mathf.Cos(Th), Sin = Mathf.Sin(Th);
    public static float WorldY(float s) => s > 0 ? s * Sin : 0f;
    public static float WorldZ(float s) => s > 0 ? s * Cos : s;

    [Header("シーン内の参照（作り直すと自動で入る）")]
    public Camera Cam;
    public Transform Track;
    public FloorTile[] Tiles;
    public Transform Daruma;
    public Renderer DarumaBody;
    public TextMesh FaceLabel;
    public GameObject[] GlowEyes;
    public Light Glare;
    public Transform Wall;
    public Material TextMaterial;

    [Header("霧の色（ふだん／振り向き中／合図中）")]
    public Color FogNormal = new Color(0.043f, 0.051f, 0.078f);
    public Color FogLook = new Color(0.17f, 0.024f, 0.035f);
    public Color FogWarn = new Color(0.12f, 0.094f, 0.024f);

    Material darumaMat;

    // ゲーム開始時に1回呼ぶ
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

    public void InitRuntime()
    {
        HookFontTexture();
        darumaMat = DarumaBody.material;   // ゲーム中だけの複製（元のマテリアルは書き換えない）
        foreach (var t in Tiles) t.InitRuntime();
    }

    public void ApplyMode(GameMode m)
    {
        if (darumaMat != null) Mat.SetColor(darumaMat, m.DarumaColor);
        FaceLabel.text = m.FaceLabel;
    }

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

    public static Stage Build(Transform parent, int cols, int rows, float tileSize)
    {
        float halfW = cols * tileSize / 2f, floorLen = rows * tileSize;
        var root = Group("Stage", parent);
        var st = root.AddComponent<Stage>();
        var tr = root.transform;
        st.TextMaterial = MakeTextMat();
        st.TextMaterial.mainTexture = JPText.Font.material.mainTexture;

        // カメラ・ライト
        var camGo = Group("Main Camera", tr);
        camGo.tag = "MainCamera";
        st.Cam = camGo.AddComponent<Camera>();
        st.Cam.clearFlags = CameraClearFlags.SolidColor;
        st.Cam.backgroundColor = st.FogNormal;
        st.Cam.fieldOfView = 70;
        st.Cam.nearClipPlane = 0.1f;
        st.Cam.farClipPlane = 120f;
        camGo.AddComponent<AudioListener>();
        camGo.transform.position = new Vector3(0, DarumaGame.Eye, -1.8f);
        camGo.transform.rotation = Quaternion.Euler(7, 0, 0);

        var lightGo = Group("Directional Light", tr);
        lightGo.transform.rotation = Quaternion.Euler(55, -30, 0);
        var sun = lightGo.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = 0.9f;
        sun.shadows = LightShadows.None;

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.55f, 0.6f, 0.75f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = st.FogNormal;
        RenderSettings.fogStartDistance = 14;
        RenderSettings.fogEndDistance = 55;

        // 床と横の壁（平らな部分＋傾いた部分）
        var baseMat = MakeMat("Floor", new Color(0.14f, 0.16f, 0.22f));
        var sideMat = MakeMat("SideWall", new Color(0.17f, 0.19f, 0.31f));
        var room = Group("Room", tr).transform;
        Prim(PrimitiveType.Cube, "Floor Flat", room, new Vector3(0, -0.05f, -6), new Vector3(14, 0.1f, 12), baseMat);
        st.Track = Group("Track (傾いた床)", tr).transform;
        st.Track.localRotation = Quaternion.Euler(-Th * Mathf.Rad2Deg, 0, 0);
        Prim(PrimitiveType.Cube, "Floor Slope", st.Track, new Vector3(0, -0.05f, 23), new Vector3(14, 0.1f, 46), baseMat);
        foreach (int sx in new[] { -1, 1 })
        {
            Prim(PrimitiveType.Cube, "Side Wall Flat", room, new Vector3(sx * (halfW + 1.3f), 2, -6), new Vector3(0.4f, 4, 12), sideMat);
            Prim(PrimitiveType.Cube, "Side Wall Slope", st.Track, new Vector3(sx * (halfW + 1.3f), 2, 23), new Vector3(0.4f, 4, 46), sideMat);
        }

        // 机とモニター
        var deskMat = MakeMat("Desk", new Color(0.42f, 0.33f, 0.22f));
        var monMat = MakeMat("Monitor", new Color(0.07f, 0.07f, 0.07f), new Color(0.1f, 0.22f, 0.4f));
        var desks = Group("Desks", tr).transform;
        for (int i = 0; i < 6; i++)
        {
            foreach (int sx in new[] { -1, 1 })
            {
                float s = 2 + i * 5 + (sx > 0 ? 2.5f : 0);
                var desk = Group("Desk " + (i * 2 + (sx > 0 ? 1 : 0) + 1), desks).transform;
                desk.position = new Vector3(sx * (halfW + 0.65f), WorldY(s), WorldZ(s));
                Prim(PrimitiveType.Cube, "Top", desk, new Vector3(0, 0.375f, 0), new Vector3(0.8f, 0.75f, 1.6f), deskMat);
                Prim(PrimitiveType.Cube, "Monitor", desk, new Vector3(sx * 0.2f, 1.0f, 0), new Vector3(0.08f, 0.45f, 0.7f), monMat);
            }
        }

        // 床の牌
        var tileMat = MakeMat("Tile", new Color(0.96f, 0.95f, 0.9f));
        var tilesRoot = Group("Tiles", st.Track).transform;
        var tiles = new List<FloorTile>();
        var up = Quaternion.Euler(90, 0, 0);
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                var go = Group("Tile " + r + "-" + c, tilesRoot);
                go.transform.localPosition = new Vector3(-halfW + tileSize * (c + 0.5f), 0.06f, tileSize * (r + 0.5f));
                var t = go.AddComponent<FloorTile>();
                t.Body = Prim(PrimitiveType.Cube, "Body", go.transform, Vector3.zero,
                              new Vector3(tileSize * 0.93f, 0.12f, tileSize * 0.93f), tileMat).GetComponent<Renderer>();
                t.NumText = JPText.Make(go.transform, "Number", "1", Color.black, 0.62f, new Vector3(0, 0.065f, 0.2f), up, st.TextMaterial);
                t.SuitText = JPText.Make(go.transform, "Suit", "萬", Color.black, 0.4f, new Vector3(0, 0.065f, -0.32f), up, st.TextMaterial);
                t.SetType((r * 7 + c * 5) % 27);
                tiles.Add(t);
            }
        }
        st.Tiles = tiles.ToArray();

        // ダルマ
        float ds = floorLen + 5;
        st.Daruma = Group("Daruma", tr).transform;
        st.Daruma.position = new Vector3(0, WorldY(ds), WorldZ(ds));
        var bodyMat = MakeMat("DarumaBody", new Color(0.78f, 0.13f, 0.16f));
        var faceMat = MakeMat("DarumaFace", new Color(0.97f, 0.92f, 0.85f));
        var blackMat = MakeMat("Black", new Color(0.05f, 0.05f, 0.05f));
        var glowMat = MakeMat("EyeGlow", new Color(1f, 0.1f, 0.1f), new Color(3f, 0.2f, 0.2f));
        st.DarumaBody = Prim(PrimitiveType.Sphere, "Body", st.Daruma, new Vector3(0, 2.6f, 0), new Vector3(4.8f, 5.2f, 4.8f), bodyMat).GetComponent<Renderer>();
        var face = Group("Face (こちら側が正面)", st.Daruma).transform;
        var disc = Prim(PrimitiveType.Cylinder, "Face Disc", face, new Vector3(0, 3.0f, -2.45f), new Vector3(3.0f, 0.05f, 2.5f), faceMat);
        disc.transform.localRotation = Quaternion.Euler(90, 0, 0);
        Prim(PrimitiveType.Sphere, "Eye L", face, new Vector3(-0.6f, 3.15f, -2.5f), Vector3.one * 0.38f, blackMat);
        Prim(PrimitiveType.Sphere, "Eye R", face, new Vector3(0.6f, 3.15f, -2.5f), Vector3.one * 0.38f, blackMat);
        var b1 = Prim(PrimitiveType.Cube, "Brow L", face, new Vector3(-0.65f, 3.7f, -2.5f), new Vector3(0.8f, 0.12f, 0.05f), blackMat);
        b1.transform.localRotation = Quaternion.Euler(0, 0, -12);
        var b2 = Prim(PrimitiveType.Cube, "Brow R", face, new Vector3(0.65f, 3.7f, -2.5f), new Vector3(0.8f, 0.12f, 0.05f), blackMat);
        b2.transform.localRotation = Quaternion.Euler(0, 0, 12);
        st.GlowEyes = new[]
        {
            Prim(PrimitiveType.Sphere, "Glow L (振り向き中だけ表示)", face, new Vector3(-0.6f, 3.15f, -2.62f), Vector3.one * 0.3f, glowMat),
            Prim(PrimitiveType.Sphere, "Glow R (振り向き中だけ表示)", face, new Vector3(0.6f, 3.15f, -2.62f), Vector3.one * 0.3f, glowMat),
        };
        foreach (var e in st.GlowEyes) e.SetActive(false);
        st.FaceLabel = JPText.Make(face, "Label", "GPA", new Color(1f, 0.83f, 0.42f), 0.6f, new Vector3(0, 2.15f, -2.5f), Quaternion.identity, st.TextMaterial);
        var glareGo = Group("Glare (振り向き中の赤いライト)", st.Daruma);
        glareGo.transform.localPosition = new Vector3(0, 3, -4);
        st.Glare = glareGo.AddComponent<Light>();
        st.Glare.type = LightType.Point;
        st.Glare.color = new Color(1f, 0.16f, 0.16f);
        st.Glare.range = 45;
        st.Glare.intensity = 0;
        st.Daruma.rotation = Quaternion.Euler(0, 180, 0);

        // 締め切りの壁
        st.Wall = Group("Deadline Wall (締め切りの壁)", tr).transform;
        st.Wall.position = new Vector3(0, 3, DarumaGame.WallStart);
        var wallMat = MakeMat("DeadlineWall", new Color(0.6f, 0.05f, 0.08f), new Color(0.35f, 0.02f, 0.04f));
        Prim(PrimitiveType.Cube, "Body", st.Wall, Vector3.zero, new Vector3(halfW * 2 + 2.6f, 6, 0.3f), wallMat);
        JPText.Make(st.Wall, "Label", "締切", Color.white, 2.2f, new Vector3(0, 0, 0.17f), Quaternion.Euler(0, 180, 0), st.TextMaterial);

        return st;
    }
}
