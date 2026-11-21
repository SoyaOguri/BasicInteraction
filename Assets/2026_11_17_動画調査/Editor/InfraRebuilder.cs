using UnityEditor;
using UnityEngine;

// IntersectionLayout の ⋮ メニューに、インフラの部品を作り直す項目を追加する。
//  ・「注意看板を「交差点注意」の立て看板に作り直す」
//  ・「赤点滅信号を日本の車両用信号機に作り直す」
// どちらも、今の部品は「（旧・仮）」と名前を付けて非表示で残し、IntersectionLayout の欄を新しい方に替える。
// あわせて、名前に「（旧・仮）」が付いた部品（旧い LED表示板など）をすべて非表示にする
// （IntersectionLayout の管理から外れた部品は、表示のままだと全条件に出てしまうため）。
public static class InfraRebuilder
{
    const string Root = "Assets/2026_11_17_動画調査";
    const string GenDir = Root + "/Generated";
    const string OldGen = "Assets/2026_11_06_複数インフラ/Generated/";
    const string OldTag = "（旧・仮）";

    // ---------------- 「交差点注意」の立て看板 ----------------

    const float BoardW = 0.55f, BoardH = 1.8f, LegH = 0.9f;

    [MenuItem("CONTEXT/IntersectionLayout/注意看板を「交差点注意」の立て看板に作り直す")]
    static void BuildCautionBoard(MenuCommand cmd)
    {
        var L = (IntersectionLayout)cmd.context;
        Undo.RecordObject(L, "注意看板を作り直す");
        Transform parent = Group(L, "Attention");

        Vector3 pos;
        Quaternion rot;
        if (!RetireOld(L.cautionSign, "注意看板", out pos, out rot))
        {
            pos = new Vector3(-L.leftWallOffset + 0.4f, 0f, L.NearEdgeZ - 30f);
            rot = Quaternion.identity;
        }

        var face = TexturedMat("交差点注意_表面", Root + "/Textures/CautionBoard_交差点注意.png");
        var grey = Mat("看板_裏・脚", new Color(0.55f, 0.56f, 0.58f));

        var root = new GameObject("注意看板（交差点注意）");
        root.transform.SetParent(parent, false);
        root.transform.localPosition = pos;
        root.transform.localRotation = rot;

        foreach (float x in new[] { -0.2f, 0.2f })
            Prim(PrimitiveType.Cylinder, "脚", root.transform, new Vector3(x, LegH * 0.5f + 0.1f, 0.02f),
                new Vector3(0.035f, LegH * 0.5f + 0.1f, 0.035f), Vector3.zero, grey);
        Prim(PrimitiveType.Cube, "板", root.transform, new Vector3(0f, LegH + BoardH * 0.5f, 0f),
            new Vector3(BoardW + 0.02f, BoardH + 0.02f, 0.025f), Vector3.zero, grey);
        Prim(PrimitiveType.Quad, "表面", root.transform, new Vector3(0f, LegH + BoardH * 0.5f, -0.014f),
            new Vector3(BoardW, BoardH, 1f), Vector3.zero, face);

        Undo.RegisterCreatedObjectUndo(root, "Create caution board");
        L.cautionSign = root;
        Finish(L, root, "注意看板を「交差点注意」の立て看板にしました。");
    }

    // ---------------- 日本の車両用信号機（赤点滅） ----------------

    const float SignalHeight = 5.0f;     // 灯器の中心の高さ [m]
    const float PoleFromRoadEdge = 0.3f; // 支柱：道路の左端（建物線）から道路側へ [m]
    const float PastFarEdge = 0.5f;      // 支柱：交差道路の向こう側の建物線から、さらに先へ [m]

    [MenuItem("CONTEXT/IntersectionLayout/赤点滅信号を日本の車両用信号機に作り直す")]
    static void BuildSignal(MenuCommand cmd)
    {
        var L = (IntersectionLayout)cmd.context;
        Undo.RecordObject(L, "赤点滅信号を作り直す");
        Transform parent = Group(L, "Norm");
        Vector3 dummyPos;
        Quaternion dummyRot;
        RetireOld(L.redFlash, "赤点滅信号", out dummyPos, out dummyRot);

        // 交差点の向こう側・左の角に支柱。アームを道路の上に張り出し、自転車の車線の真上に灯器
        float poleX = -L.leftWallOffset + PoleFromRoadEdge;
        float laneCenter = (L.bikeRoadLeftLineX + (L.bikeRoadLeftLineX + L.bikeRoadRightLineX) * 0.5f) * 0.5f;
        float armLen = laneCenter - poleX + 0.3f;
        float housingX = laneCenter - poleX;

        var pole = Mat("信号_支柱", new Color(0.62f, 0.63f, 0.64f));
        var body = Mat("信号_灯器", new Color(0.30f, 0.31f, 0.32f));
        var black = Mat("信号_黒", new Color(0.04f, 0.04f, 0.04f));
        var greenOff = Mat("信号_青_消灯", new Color(0.03f, 0.12f, 0.10f));
        var yellowOff = Mat("信号_黄_消灯", new Color(0.18f, 0.13f, 0.02f));
        var redOn = AssetDatabase.LoadAssetAtPath<Material>(OldGen + "赤点滅_点灯.mat") ?? Mat("信号_赤_点灯", new Color(1f, 0.08f, 0.05f), true);
        var redOff = AssetDatabase.LoadAssetAtPath<Material>(OldGen + "赤点滅_消灯.mat") ?? Mat("信号_赤_消灯", new Color(0.22f, 0.03f, 0.03f));

        var root = new GameObject("赤点滅信号（車両用）");
        root.transform.SetParent(parent, false);
        root.transform.localPosition = new Vector3(poleX, 0f, L.crossRoadFarEdge + PastFarEdge);
        root.transform.localRotation = Quaternion.identity;   // 灯器の正面（−Z）が自転車の来る方を向く

        float top = SignalHeight + 0.6f;
        Prim(PrimitiveType.Cylinder, "支柱", root.transform, new Vector3(0f, top * 0.5f, 0f), new Vector3(0.2f, top * 0.5f, 0.2f), Vector3.zero, pole);
        Prim(PrimitiveType.Cube, "アーム", root.transform, new Vector3(armLen * 0.5f, SignalHeight + 0.4f, 0f), new Vector3(armLen, 0.1f, 0.1f), Vector3.zero, pole);
        Prim(PrimitiveType.Cube, "吊り金具", root.transform, new Vector3(housingX, SignalHeight + 0.3f, 0f), new Vector3(0.06f, 0.2f, 0.06f), Vector3.zero, pole);

        // 灯器：左から 青・黄・赤（自転車から見て）。正面に黒い板、各灯に庇（ひさし）
        var head = new GameObject("灯器").transform;
        head.SetParent(root.transform, false);
        head.localPosition = new Vector3(housingX, SignalHeight, 0f);
        Prim(PrimitiveType.Cube, "筐体", head, Vector3.zero, new Vector3(1.25f, 0.42f, 0.2f), Vector3.zero, body);
        Prim(PrimitiveType.Cube, "前面", head, new Vector3(0f, 0f, -0.101f), new Vector3(1.2f, 0.38f, 0.002f), Vector3.zero, black);
        GameObject red = null;
        float[] xs = { -0.4f, 0f, 0.4f };
        Material[] mats = { greenOff, yellowOff, redOff };
        string[] names = { "青", "黄", "赤" };
        for (int i = 0; i < 3; i++)
        {
            var lamp = Prim(PrimitiveType.Cylinder, "灯（" + names[i] + "）", head, new Vector3(xs[i], 0f, -0.104f),
                new Vector3(0.3f, 0.004f, 0.3f), new Vector3(90f, 0f, 0f), mats[i]);
            Prim(PrimitiveType.Cube, "庇（" + names[i] + "）", head, new Vector3(xs[i], 0.165f, -0.2f),
                new Vector3(0.34f, 0.012f, 0.2f), new Vector3(-8f, 0f, 0f), body);
            if (i == 2) red = lamp;
        }

        var blink = root.AddComponent<BlinkingLamp>();
        blink.lamp = red.GetComponent<Renderer>();
        blink.onMaterial = redOn;
        blink.offMaterial = redOff;
        blink.period = 1.0f;
        blink.duty = 0.5f;

        Undo.RegisterCreatedObjectUndo(root, "Create signal");
        L.redFlash = root;
        Finish(L, root, "赤点滅信号を日本の車両用信号機（赤だけ点滅）にしました。");
    }

    // ---------------- 共通 ----------------

    // 今の部品を「（旧・仮）」にして非表示にする。位置と向きを返す（無ければ false）
    static bool RetireOld(GameObject old, string label, out Vector3 pos, out Quaternion rot)
    {
        pos = Vector3.zero;
        rot = Quaternion.identity;
        if (old == null) return false;
        pos = old.transform.localPosition;
        rot = old.transform.localRotation;
        Undo.RecordObject(old, "retire " + label);
        if (!old.name.Contains(OldTag)) old.name += OldTag;
        old.SetActive(false);
        return true;
    }

    static void HideAllOld(IntersectionLayout L)
    {
        int n = 0;
        foreach (var t in L.GetComponentsInChildren<Transform>(true))
        {
            if (!t.name.Contains(OldTag) || !t.gameObject.activeSelf) continue;
            Undo.RecordObject(t.gameObject, "hide old");
            t.gameObject.SetActive(false);
            n++;
        }
        if (n > 0) Debug.Log("[InfraRebuilder] 「" + OldTag + "」の付いた部品を " + n + " 個、非表示にしました。");
    }

    static void Finish(IntersectionLayout L, GameObject root, string message)
    {
        HideAllOld(L);
        EditorUtility.SetDirty(L);
        L.Apply(L.preview);
        Selection.activeGameObject = root;
        Debug.Log("[InfraRebuilder] " + message + " シーンを保存してください（Ctrl+S）。", root);
    }

    static Transform Group(IntersectionLayout L, string name)
    {
        Transform t = L.transform.Find(name);
        if (t != null) return t;
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        go.transform.SetParent(L.transform, false);
        return go.transform;
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

    static void EnsureDir()
    {
        if (!AssetDatabase.IsValidFolder(GenDir)) AssetDatabase.CreateFolder(Root, "Generated");
    }

    static Material Mat(string name, Color c, bool emissive = false)
    {
        EnsureDir();
        string path = GenDir + "/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m != null) return m;
        m = new Material(Shader.Find("Standard")) { color = c };
        m.SetFloat("_Glossiness", 0.25f);
        if (emissive)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", c * 2f);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    static Material TexturedMat(string name, string texPath)
    {
        EnsureDir();
        string path = GenDir + "/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(Shader.Find("Standard"));
            m.SetFloat("_Glossiness", 0.15f);
            AssetDatabase.CreateAsset(m, path);
        }
        AssetDatabase.ImportAsset(texPath);
        var imp = AssetImporter.GetAtPath(texPath) as TextureImporter;
        if (imp != null && imp.wrapMode != TextureWrapMode.Clamp)
        {
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.SaveAndReimport();
        }
        m.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        if (m.mainTexture == null) Debug.LogError("[InfraRebuilder] " + texPath + " が見つかりません。");
        m.color = Color.white;
        EditorUtility.SetDirty(m);
        return m;
    }
}
