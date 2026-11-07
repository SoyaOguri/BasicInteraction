using UnityEditor;
using UnityEngine;

// IntersectionLayout の ⋮ メニューに「インフラの仮部品を自動生成」を追加する。
// 空いている欄（stopLine など）だけ、仮の形を作って Norm / Attention / Info の下に置き、欄に割り当てる。
// すでに割り当て済みの欄には触らない（本物のモデルに差し替えたあとで実行しても上書きしない）。
//
// 置き場所はすべて IntersectionLayout のローカル座標（原点＝自転車の進路×交差道路の中心線、+Z＝進行方向、+X＝右）。
public static class IntersectionPropBuilder
{
    const string GenDir = "Assets/2026_11_06_複数インフラ/Generated";

    // ---- 配置の初期値（必要なら生成後に Scene ビューで動かしてよい） ----
    const float StopLineBeforeEdge = 1.0f;   // 停止線：交差道路の建物線の何 m 手前か
    const float StopLineWidth = 0.45f;       // 停止線の太さ
    const float MarkBeforeStopLine = 3.5f;   // 「止まれ」路面表示の中心：停止線の何 m 手前か
    const float SideFromWall = 0.4f;         // 標識・看板の支柱：左の塀から何 m 道路側か
    const float CautionBeforeEdge = 30f;     // 注意看板：交差道路の建物線の何 m 手前か
    const float FarCornerOffset = 0.8f;      // ミラー・LED：向こう側の右の角（白線の外側）

    [MenuItem("CONTEXT/IntersectionLayout/インフラの仮部品を自動生成（空の欄だけ）")]
    static void Build(MenuCommand cmd)
    {
        var L = (IntersectionLayout)cmd.context;
        Undo.RecordObject(L, "インフラの仮部品を自動生成");

        Transform norm = Group(L, "Norm");
        Transform att = Group(L, "Attention");
        Transform info = Group(L, "Info");

        float edgeZ = L.NearEdgeZ;
        float stopZ = edgeZ - StopLineBeforeEdge;
        float lineL = L.bikeRoadLeftLineX;
        float lineR = L.bikeRoadRightLineX;
        float roadCx = (lineL + lineR) * 0.5f;
        float roadW = lineR - lineL;
        float sideX = -L.leftWallOffset + SideFromWall;
        Vector3 farCorner = new Vector3(lineR + FarCornerOffset, 0f, L.crossRoadFarLineZ + 0.7f);
        Vector3 refPoint = new Vector3(0f, 0f, L.RefZ);

        Material white = Mat("白", new Color(0.95f, 0.95f, 0.95f));
        Material red = Mat("赤", new Color(0.80f, 0.05f, 0.05f));
        Material grey = Mat("支柱グレー", new Color(0.6f, 0.6f, 0.62f));
        Material black = Mat("黒", new Color(0.05f, 0.05f, 0.05f));
        Material bengara = Mat("ベンガラ", new Color(0.55f, 0.22f, 0.15f));
        Material yellow = Mat("黄", new Color(0.98f, 0.80f, 0.05f));
        Material orange = Mat("ミラー支柱オレンジ", new Color(0.95f, 0.45f, 0.05f));
        Material mirrorGrey = Mat("鏡（仮）", new Color(0.75f, 0.8f, 0.85f));
        Material redOn = Mat("赤点滅_点灯", new Color(1f, 0.1f, 0.05f), true);
        Material redOff = Mat("赤点滅_消灯", new Color(0.25f, 0.03f, 0.03f));
        Material ledOn = Mat("LED_点灯", new Color(1f, 0.55f, 0.05f), true);
        Material ledOff = Mat("LED_消灯", new Color(0.08f, 0.08f, 0.08f));

        int made = 0;

        // ---- 規範 ----
        // 交差点の路面を「止まれ」入りのマテリアルに替える方式なら、停止線と路面表示は路面のテクスチャに含まれるので作らない
        bool surfaceHasMarks = L.intersectionSurface != null && L.surfaceStop != null;
        if (L.stopLine == null && !surfaceHasMarks)
        {
            var go = Quad("停止線", norm, new Vector3(roadCx, 0.012f, stopZ), new Vector2(roadW, StopLineWidth), white);
            L.stopLine = Register(go); made++;
        }
        if (L.stopSign == null)
        {
            var root = Empty("一時停止標識", norm, new Vector3(sideX, 0f, stopZ - 0.3f));
            Prim(PrimitiveType.Cylinder, "支柱", root, new Vector3(0f, 1.15f, 0f), new Vector3(0.07f, 1.15f, 0.07f), Vector3.zero, grey);
            Tri("縁（白）", root, new Vector3(0f, 2.3f, -0.05f), 1.18f, white);
            Tri("板（赤）", root, new Vector3(0f, 2.3f, -0.06f), 1.0f, red);
            L.stopSign = Register(root.gameObject); made++;
        }
        if (L.stopMark == null && !surfaceHasMarks)
        {
            // 「止まれ」の3文字を、進行方向に細長い白い長方形3つで仮に表す（あとで文字のテクスチャに差し替える）
            var root = Empty("止まれ路面表示", norm, new Vector3(roadCx, 0.012f, stopZ - MarkBeforeStopLine));
            for (int i = 0; i < 3; i++)
                Quad("文字" + (i + 1), root, new Vector3((i - 1) * 1.1f, 0f, 0f), new Vector2(0.8f, 2.5f), white);
            L.stopMark = Register(root.gameObject); made++;
        }
        if (L.redFlash == null)
        {
            var root = Empty("赤点滅信号", norm, new Vector3(sideX, 0f, edgeZ - 0.5f));
            Prim(PrimitiveType.Cylinder, "支柱", root, new Vector3(0f, 1.5f, 0f), new Vector3(0.1f, 1.5f, 0.1f), Vector3.zero, grey);
            Prim(PrimitiveType.Cube, "灯器", root, new Vector3(0f, 3.2f, -0.05f), new Vector3(0.4f, 0.4f, 0.25f), Vector3.zero, black);
            var lamp = Prim(PrimitiveType.Cylinder, "灯（赤）", root, new Vector3(0f, 3.2f, -0.19f), new Vector3(0.28f, 0.02f, 0.28f), new Vector3(90f, 0f, 0f), redOn);
            var blink = root.gameObject.AddComponent<BlinkingLamp>();
            blink.lamp = lamp.GetComponent<Renderer>();
            blink.onMaterial = redOn;
            blink.offMaterial = redOff;
            L.redFlash = Register(root.gameObject); made++;
        }

        // ---- 注意喚起 ----
        bool surfaceHasColor = L.intersectionSurface != null && L.surfaceDefaultColor != null && L.surfaceStopColor != null;
        if (L.colorPavement == null && !surfaceHasColor)
        {
            float nz = L.crossRoadNearLineZ, fz = L.crossRoadFarLineZ;
            var go = Quad("交差点カラー舗装", att, new Vector3(roadCx, 0.008f, (nz + fz) * 0.5f), new Vector2(roadW, fz - nz), bengara);
            L.colorPavement = Register(go); made++;
        }
        if (L.cautionSign == null)
        {
            // 黄色の板に黒い十字（交差点あり）。文字はあとでテクスチャに差し替える
            var root = Empty("注意看板", att, new Vector3(sideX, 0f, edgeZ - CautionBeforeEdge));
            Prim(PrimitiveType.Cylinder, "支柱", root, new Vector3(0f, 1.0f, 0f), new Vector3(0.07f, 1.0f, 0.07f), Vector3.zero, grey);
            Prim(PrimitiveType.Cube, "板（黄）", root, new Vector3(0f, 2.3f, -0.05f), new Vector3(0.6f, 0.9f, 0.03f), Vector3.zero, yellow);
            QuadUpright("十字（縦）", root, new Vector3(0f, 2.3f, -0.07f), new Vector2(0.1f, 0.6f), black);
            QuadUpright("十字（横）", root, new Vector3(0f, 2.3f, -0.07f), new Vector2(0.45f, 0.1f), black);
            L.cautionSign = Register(root.gameObject); made++;
        }

        // ---- 情報 ----
        if (L.mirror == null)
        {
            var root = Empty("カーブミラー（仮）", info, farCorner);
            FaceTowards(root, refPoint);
            Prim(PrimitiveType.Cylinder, "支柱", root, new Vector3(0f, 1.25f, 0f), new Vector3(0.08f, 1.25f, 0.08f), Vector3.zero, orange);
            Prim(PrimitiveType.Cylinder, "鏡（左）", root, new Vector3(-0.35f, 2.7f, -0.08f), new Vector3(0.6f, 0.02f, 0.6f), new Vector3(90f, -20f, 0f), mirrorGrey);
            Prim(PrimitiveType.Cylinder, "鏡（右）", root, new Vector3(0.35f, 2.7f, -0.08f), new Vector3(0.6f, 0.02f, 0.6f), new Vector3(90f, 20f, 0f), mirrorGrey);
            L.mirror = Register(root.gameObject); made++;
        }
        if (L.ledBoard == null)
        {
            var root = Empty("車両接近LED表示板", info, farCorner);
            FaceTowards(root, refPoint);
            Prim(PrimitiveType.Cylinder, "支柱", root, new Vector3(0f, 1.1f, 0f), new Vector3(0.08f, 1.1f, 0.08f), Vector3.zero, grey);
            Prim(PrimitiveType.Cube, "筐体", root, new Vector3(0f, 2.5f, 0f), new Vector3(1.0f, 0.45f, 0.12f), Vector3.zero, black);
            var panel = QuadUpright("発光面", root, new Vector3(0f, 2.5f, -0.065f), new Vector2(0.85f, 0.32f), ledOff);
            var board = root.gameObject.AddComponent<ApproachWarningBoard>();
            board.layout = L;
            board.panel = panel.GetComponent<Renderer>();
            board.onMaterial = ledOn;
            board.offMaterial = ledOff;
            L.ledBoard = Register(root.gameObject); made++;
        }

        EditorUtility.SetDirty(L);
        L.Apply(L.preview);
        Debug.Log(string.Format("[IntersectionPropBuilder] 仮部品を {0} 個作りました（割り当て済みの欄はそのまま）。", made), L);
    }

    [MenuItem("CONTEXT/IntersectionLayout/塀と空き地に見た目を付ける（ブロック塀・砂利）")]
    static void DressWalls(MenuCommand cmd)
    {
        var L = (IntersectionLayout)cmd.context;
        const string dir = "Assets/2026_11_06_複数インフラ/Materials/";
        var wallMat = AssetDatabase.LoadAssetAtPath<Material>(dir + "Mat_BlockWall.mat");
        var lotMat = AssetDatabase.LoadAssetAtPath<Material>(dir + "Mat_Gravel.mat");
        if (wallMat == null || lotMat == null)
        {
            Debug.LogError("[IntersectionPropBuilder] Mat_BlockWall / Mat_Gravel が Materials フォルダに見つかりません。");
            return;
        }
        Undo.RecordObject(L, "塀と空き地に見た目を付ける");

        foreach (var w in new[] { L.leftWallAlong, L.leftWallAcross, L.rightWallAlong, L.rightWallAcross })
        {
            if (w == null) continue;
            var r = w.GetComponent<Renderer>();
            if (r == null) continue;
            Undo.RecordObject(r, "wall material");
            r.sharedMaterial = wallMat;
        }

        Transform sight = L.sightRoot != null ? L.sightRoot : L.transform.Find("Sight");
        if (sight == null) sight = L.transform;
        if (L.leftLot == null) L.leftLot = MakeLot("Lot_Left", sight, lotMat);
        if (L.rightLot == null) L.rightLot = MakeLot("Lot_Right", sight, lotMat);

        EditorUtility.SetDirty(L);
        L.Apply(L.preview);
        Debug.Log("[IntersectionPropBuilder] 塀にブロック塀、空き地に砂利を付けました。", L);
    }

    static Transform MakeLot(string name, Transform parent, Material mat)
    {
        Transform existing = parent.Find(name);
        if (existing != null) return existing;
        var go = Prim(PrimitiveType.Quad, name, parent, Vector3.zero, Vector3.one, new Vector3(90f, 0f, 0f), mat);
        go.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        return go.transform;
    }

    // ---------------- 部品づくりの道具 ----------------

    static Transform Group(IntersectionLayout L, string name)
    {
        Transform t = L.transform.Find(name);
        if (t != null) return t;
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Create group");
        go.transform.SetParent(L.transform, false);
        return go.transform;
    }

    static Transform Empty(string name, Transform parent, Vector3 localPos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        return go.transform;
    }

    static GameObject Register(GameObject go)
    {
        Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
        return go;
    }

    static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale, Vector3 euler, Material mat)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localEulerAngles = euler;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }

    // 路面に寝かせた四角（上から見える）。size.x = 左右の幅、size.y = 進行方向の長さ
    static GameObject Quad(string name, Transform parent, Vector3 pos, Vector2 size, Material mat)
    {
        var go = Prim(PrimitiveType.Quad, name, parent, pos, new Vector3(size.x, size.y, 1f), new Vector3(90f, 0f, 0f), mat);
        var r = go.GetComponent<Renderer>();
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return go;
    }

    // 立てた四角（自転車の来る −Z 側から見える）
    static GameObject QuadUpright(string name, Transform parent, Vector3 pos, Vector2 size, Material mat)
    {
        return Prim(PrimitiveType.Quad, name, parent, pos, new Vector3(size.x, size.y, 1f), Vector3.zero, mat);
    }

    // 逆三角形（一辺 0.8 m × scale）。−Z 側から見える
    static GameObject Tri(string name, Transform parent, Vector3 pos, float scale, Material mat)
    {
        var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = Vector3.one * scale;
        go.GetComponent<MeshFilter>().sharedMesh = TriangleMesh();
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return go;
    }

    // 部品の正面（−Z）を target に向ける（水平方向だけ）
    static void FaceTowards(Transform t, Vector3 targetLocal)
    {
        Vector3 d = targetLocal - t.localPosition;
        d.y = 0f;
        if (d.sqrMagnitude < 1e-4f) return;
        t.localRotation = Quaternion.LookRotation(-d.normalized, Vector3.up);
    }

    static void EnsureDir()
    {
        if (!AssetDatabase.IsValidFolder(GenDir))
            AssetDatabase.CreateFolder("Assets/2026_11_06_複数インフラ", "Generated");
    }

    static Material Mat(string name, Color c, bool emissive = false)
    {
        EnsureDir();
        string path = GenDir + "/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m != null) return m;
        m = new Material(Shader.Find("Standard"));
        m.color = c;
        m.SetFloat("_Glossiness", 0.1f);
        if (emissive)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", c * 2f);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    static Mesh TriangleMesh()
    {
        EnsureDir();
        string path = GenDir + "/逆三角形.asset";
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (mesh != null) return mesh;
        // 一辺 0.8 m の正三角形（頂点が下）。重心が原点
        float h = 0.8f * Mathf.Sqrt(3f) * 0.5f;
        mesh = new Mesh { name = "逆三角形" };
        mesh.vertices = new[]
        {
            new Vector3(-0.4f, h / 3f, 0f),
            new Vector3(0.4f, h / 3f, 0f),
            new Vector3(0f, -2f * h / 3f, 0f),
        };
        mesh.uv = new[] { new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 0f) };
        mesh.triangles = new[] { 0, 1, 2 };   // −Z 側から見て時計回り＝表
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }
}
