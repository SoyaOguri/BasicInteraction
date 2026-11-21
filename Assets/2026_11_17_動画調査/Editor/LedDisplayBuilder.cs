using UnityEditor;
using UnityEngine;

// IntersectionLayout の ⋮ メニューに「LED表示板を作り直す（車両接近の電光表示）」を追加する。
//  ・Info の下に新しい表示板（支柱・筐体・黒い発光面・「車両接近」の文字・LED の粒の模様）を作る
//  ・今の LED表示板（仮部品）は非表示にして残し、IntersectionLayout の Led Board 欄を新しい方に替える
//  ・置き場所は今の仮部品と同じ（向こう側の右の角、基準点の方を向く）
public static class LedDisplayBuilder
{
    const string Root = "Assets/2026_11_17_動画調査";
    const string GenDir = Root + "/Generated";

    // 大きさ [m]
    const float PanelWidth = 1.2f;
    const float PanelHeight = 0.42f;
    const float BoardCenterHeight = 2.5f;
    const float DotPitch = 0.025f;       // LED の粒の間隔
    const float FarCornerOffset = 0.8f;  // IntersectionPropBuilder と同じ

    [MenuItem("CONTEXT/IntersectionLayout/LED表示板を作り直す（車両接近の電光表示）")]
    static void Build(MenuCommand cmd)
    {
        var L = (IntersectionLayout)cmd.context;
        Undo.RecordObject(L, "LED表示板を作り直す");

        Transform info = L.transform.Find("Info");
        if (info == null)
        {
            var g = new GameObject("Info");
            Undo.RegisterCreatedObjectUndo(g, "Create Info");
            g.transform.SetParent(L.transform, false);
            info = g.transform;
        }

        // 置き場所：今の仮部品があればその位置と向き、無ければ向こう側の右の角
        Vector3 pos;
        Quaternion rot;
        GameObject old = L.ledBoard;
        if (old != null && old.GetComponent<VehicleApproachDisplay>() == null)
        {
            pos = old.transform.localPosition;
            rot = old.transform.localRotation;
            Undo.RecordObject(old, "hide old LED board");
            old.SetActive(false);
            old.name = old.name + "（旧・仮）";
        }
        else
        {
            if (old != null)
            {
                Debug.Log("[LedDisplayBuilder] すでに新しい LED表示板が割り当てられています。作り直す場合は Led Board 欄を空にしてから実行してください。", old);
                return;
            }
            pos = new Vector3(L.bikeRoadRightLineX + FarCornerOffset, 0f, L.crossRoadFarLineZ + 0.7f);
            Vector3 d = new Vector3(0f, 0f, L.RefZ) - pos;
            d.y = 0f;
            rot = Quaternion.LookRotation(-d.normalized, Vector3.up);
        }

        Material grey = Mat("支柱グレー", new Color(0.6f, 0.6f, 0.62f), "Standard");
        Material housing = Mat("LED筐体", new Color(0.12f, 0.12f, 0.13f), "Standard");
        Material panelMat = Mat("LED発光面（黒）", new Color(0.02f, 0.02f, 0.02f), "Unlit/Color");
        Material dots = DotMaskMaterial();

        var root = new GameObject("車両接近LED表示板");
        root.transform.SetParent(info, false);
        root.transform.localPosition = pos;
        root.transform.localRotation = rot;

        Prim(PrimitiveType.Cylinder, "支柱", root.transform, new Vector3(0f, (BoardCenterHeight - PanelHeight * 0.5f) * 0.5f, 0f),
            new Vector3(0.08f, (BoardCenterHeight - PanelHeight * 0.5f) * 0.5f, 0.08f), grey);
        Prim(PrimitiveType.Cube, "筐体", root.transform, new Vector3(0f, BoardCenterHeight, 0f),
            new Vector3(PanelWidth + 0.12f, PanelHeight + 0.12f, 0.14f), housing);
        Prim(PrimitiveType.Quad, "発光面", root.transform, new Vector3(0f, BoardCenterHeight, -0.071f),
            new Vector3(PanelWidth, PanelHeight, 1f), panelMat);

        var textGo = new GameObject("文字");
        textGo.transform.SetParent(root.transform, false);
        textGo.transform.localPosition = new Vector3(0f, BoardCenterHeight, -0.073f);
        var tm = textGo.AddComponent<TextMesh>();

        var dotGo = Prim(PrimitiveType.Quad, "LEDの粒", root.transform, new Vector3(0f, BoardCenterHeight, -0.075f),
            new Vector3(PanelWidth, PanelHeight, 1f), dots);
        var block = new MaterialPropertyBlock();
        block.SetVector("_MainTex_ST", new Vector4(PanelWidth / DotPitch, PanelHeight / DotPitch, 0f, 0f));
        dotGo.GetComponent<Renderer>().SetPropertyBlock(block);

        var disp = root.AddComponent<VehicleApproachDisplay>();
        disp.layout = L;
        disp.text = tm;
        disp.textShader = Shader.Find("Custom/LedTextOccluded");
        if (disp.textShader == null)
            Debug.LogWarning("[LedDisplayBuilder] Custom/LedTextOccluded シェーダーが見つかりません。文字が塀を透けて見える可能性があります。");
        disp.textArea = new Vector2(PanelWidth * 0.88f, PanelHeight * 0.75f);

        Undo.RegisterCreatedObjectUndo(root, "Create LED board");
        L.ledBoard = root;
        EditorUtility.SetDirty(L);
        L.Apply(L.preview);
        Selection.activeGameObject = root;
        Debug.Log("[LedDisplayBuilder] 新しい LED表示板を作り、Led Board 欄に割り当てました。旧い仮部品は非表示で残しています。", root);
    }

    static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale, Material mat)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }

    static void EnsureDir()
    {
        if (!AssetDatabase.IsValidFolder(GenDir)) AssetDatabase.CreateFolder(Root, "Generated");
    }

    static Material Mat(string name, Color c, string shader)
    {
        EnsureDir();
        string path = GenDir + "/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m != null) return m;
        m = new Material(Shader.Find(shader)) { color = c };
        if (shader == "Standard") m.SetFloat("_Glossiness", 0.2f);
        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    // 黒い板に丸い穴が並んだ模様（穴から後ろの文字の色が見える）
    static Material DotMaskMaterial()
    {
        EnsureDir();
        string texPath = GenDir + "/LEDの粒.png";
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        if (tex == null)
        {
            const int n = 32;
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false);
            float c = (n - 1) * 0.5f, r = n * 0.36f;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                    float a = Mathf.Clamp01(d - r + 0.5f);   // 穴の中は透明、外は黒
                    t.SetPixel(x, y, new Color(0f, 0f, 0f, a));
                }
            t.Apply();
            System.IO.File.WriteAllBytes(texPath, t.EncodeToPNG());
            Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(texPath);
            var imp = (TextureImporter)AssetImporter.GetAtPath(texPath);
            imp.alphaIsTransparency = true;
            imp.wrapMode = TextureWrapMode.Repeat;
            imp.mipmapEnabled = true;
            imp.SaveAndReimport();
            tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        }
        string matPath = GenDir + "/LEDの粒.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (m != null) return m;
        m = new Material(Shader.Find("Unlit/Transparent")) { mainTexture = tex };
        AssetDatabase.CreateAsset(m, matPath);
        return m;
    }
}
