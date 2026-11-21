using UnityEditor;
using UnityEngine;

// IntersectionLayout の ⋮ メニュー「「止まれ」路面表示を板で置く」
//
//  交差点の路面（Quad3）は 22 m 四方しかなく、その外側は Road4 が覆っているので、
//  「止まれ」を交差点から離すとテクスチャの端で切れてしまう。
//  そこで「止まれ」の文字を路面テクスチャから外し（TEX-25_*_back.png は停止線だけ）、
//  文字だけの透明な板（デカール）を路面の少し上に置く。
//
//  ・板は Norm の下に「止まれ路面表示（板）」としてまとめ、IntersectionLayout の Stop Mark 欄に入れる
//    → 停止線セットの条件（実験の本番も含む）でだけ表示される
//  ・位置は「元のテクスチャで文字があった画素」を、路面の板の位置・向き・テクスチャのずらし量から
//    ワールド座標に直し、停止線と同じだけ（shiftMeters）交差点から離す
//  ・もう一度実行すると、前に作った板を消して作り直す（shiftMeters を変えて置き直すときなど）
public static class StopMarkDecalBuilder
{
    const string TexDir = "Assets/2026_11_06_複数インフラ/Textures/";
    const string MatDir = "Assets/2026_11_06_複数インフラ/Materials/";

    // 元のテクスチャ（TEX-25_nocross.png, 1147×1138）で文字があった範囲（画素, 左上原点）
    const int TexW = 1147, TexH = 1138;
    static readonly RectInt NearText = new RectInt(119, 488, 266, 93);   // 自転車の来る側（左）
    static readonly RectInt FarText = new RectInt(800, 579, 289, 101);   // 向こう側（右）

    // 停止線をずらした量（shift_marks.py の SHIFT_L / SHIFT_R = 78 画素 ≒ 1.5 m）と同じにする
    const float ShiftPixels = 78f;
    // 路面から浮かせる高さ [m]（Road4 と重なってちらつかないように）
    const float Lift = 0.02f;

    [MenuItem("CONTEXT/IntersectionLayout/「止まれ」路面表示を板で置く（交差点から 1.5 m 手前）")]
    static void Build(MenuCommand cmd)
    {
        var L = (IntersectionLayout)cmd.context;
        Renderer surface = L.intersectionSurface;
        Material stopMat = L.surfaceStop;
        if (surface == null || stopMat == null)
        {
            Debug.LogError("[StopMarkDecal] IntersectionLayout の Intersection Surface（Quad3）と Surface Stop を設定してください。");
            return;
        }

        Texture2D near = LoadDecalTexture("TOMARE_near.png");
        Texture2D far = LoadDecalTexture("TOMARE_far.png");
        if (near == null || far == null) return;
        Material nearMat = DecalMaterial("Mat_TOMARE_near", near);
        Material farMat = DecalMaterial("Mat_TOMARE_far", far);

        Undo.RecordObject(L, "「止まれ」路面表示を板で置く");
        Transform norm = L.transform.Find("Norm");
        if (norm == null)
        {
            norm = new GameObject("Norm").transform;
            norm.SetParent(L.transform, false);
            Undo.RegisterCreatedObjectUndo(norm.gameObject, "Create Norm");
        }

        // 前に作った板があれば消す
        Transform old = norm.Find("止まれ路面表示（板）");
        if (old != null)
        {
            if (L.stopMark == old.gameObject) L.stopMark = null;
            Undo.DestroyObjectImmediate(old.gameObject);
        }

        var root = new GameObject("止まれ路面表示（板）");
        Undo.RegisterCreatedObjectUndo(root, "Create stop mark decal");
        root.transform.SetParent(norm, false);

        // 自転車の来る側は画素の x を小さく（左へ）、向こう側は大きく（右へ）ずらす
        MakeDecal(root.transform, "手前（自転車側）", surface, stopMat, NearText, -ShiftPixels, nearMat);
        MakeDecal(root.transform, "向こう側", surface, stopMat, FarText, +ShiftPixels, farMat);

        L.stopMark = root;
        EditorUtility.SetDirty(L);
        L.Apply(L.preview);
        Selection.activeGameObject = root;
        Debug.Log("[StopMarkDecal] 「止まれ」の板を置き、Stop Mark 欄に入れました。停止線セットの条件でだけ表示されます。", root);
    }

    static void MakeDecal(Transform parent, string name, Renderer surface, Material surfaceMat, RectInt box, float shiftPx, Material mat)
    {
        Vector2 scale = surfaceMat.mainTextureScale;
        Vector2 offset = surfaceMat.mainTextureOffset;
        float cx = box.x + box.width * 0.5f + shiftPx;
        float cy = box.y + box.height * 0.5f;

        // 画素 → テクスチャ座標（u, v）→ 板の UV → 板のローカル座標（Unity の Quad は −0.5〜0.5）
        float u = cx / TexW;
        float v = 1f - cy / TexH;
        float mu = (u - offset.x) / scale.x;
        float mv = (v - offset.y) / scale.y;
        Transform s = surface.transform;
        Vector3 center = s.TransformPoint(new Vector3(mu - 0.5f, mv - 0.5f, 0f));
        center += Vector3.up * Lift;

        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = name;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent, true);
        go.transform.position = center;
        go.transform.rotation = s.rotation;   // 路面の板と同じ向き（文字の向きもテクスチャのとおり）
        // 大きさ：路面の板の大きさ × 文字の範囲が占める割合
        Vector3 ls = s.lossyScale;
        Vector3 world = new Vector3(ls.x * box.width / TexW / scale.x, ls.y * box.height / TexH / scale.y, 1f);
        Vector3 ps = parent.lossyScale;
        go.transform.localScale = new Vector3(world.x / ps.x, world.y / ps.y, 1f);
        var r = go.GetComponent<Renderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
    }

    static Texture2D LoadDecalTexture(string file)
    {
        string path = TexDir + file;
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp == null)
        {
            AssetDatabase.ImportAsset(path);
            imp = AssetImporter.GetAtPath(path) as TextureImporter;
        }
        if (imp == null)
        {
            Debug.LogError("[StopMarkDecal] " + path + " が見つかりません。");
            return null;
        }
        if (!imp.alphaIsTransparency || imp.wrapMode != TextureWrapMode.Clamp)
        {
            imp.alphaIsTransparency = true;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    // 光の当たり方を路面と合わせるため、影を受ける切り抜き（Cutout）のシェーダーにする
    static Material DecalMaterial(string name, Texture2D tex)
    {
        string path = MatDir + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(Shader.Find("Legacy Shaders/Transparent/Cutout/Diffuse"));
            AssetDatabase.CreateAsset(m, path);
        }
        m.mainTexture = tex;
        m.color = new Color(0.95f, 0.95f, 0.95f, 1f);
        m.SetFloat("_Cutoff", 0.4f);
        EditorUtility.SetDirty(m);
        return m;
    }
}
