using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// カーブミラーの下の白い看板に、赤い縦書きの「注意」を貼る（IntersectionLayout の ⋮ メニュー）
//   文字は画像（Textures/MirrorPlate_注意.png）を板に貼る。看板（立方体）の表と裏の両面に貼るので、どちらから見ても読める
public static class MirrorPlateBuilder
{
    const string TexturePath = "Assets/2026_11_17_動画調査/Textures/MirrorPlate_注意.png";
    const string MaterialPath = "Assets/2026_11_17_動画調査/Materials/Mat_MirrorPlate_注意.mat";
    const string PlateName = "看板";
    const string TextName = "注意（文字）";

    [MenuItem("CONTEXT/IntersectionLayout/カーブミラーの看板に「注意」を付ける")]
    static void Build(MenuCommand cmd)
    {
        var L = (IntersectionLayout)cmd.context;
        if (L.mirror == null)
        {
            EditorUtility.DisplayDialog("カーブミラー", "IntersectionLayout の Mirror が空です。", "OK");
            return;
        }
        Transform plate = FindDeep(L.mirror.transform, PlateName);
        if (plate == null)
        {
            EditorUtility.DisplayDialog("カーブミラー", "カーブミラーの中に「" + PlateName + "」が見つかりません。", "OK");
            return;
        }
        var mat = GetMaterial();
        if (mat == null)
        {
            EditorUtility.DisplayDialog("カーブミラー", TexturePath + " が見つかりません。", "OK");
            return;
        }

        // 前に貼ったものは消して貼り直す
        for (int i = plate.childCount - 1; i >= 0; i--)
        {
            var c = plate.GetChild(i);
            if (c.name.StartsWith(TextName)) Undo.DestroyObjectImmediate(c.gameObject);
        }

        // 看板の面の位置は形で違う（板＝Quad なら z = 0、立方体なら z = ±0.5）。メッシュの大きさから面の位置を求め、
        // そこから 5 mm だけ浮かせて、看板の 85% の大きさで貼る
        float face = 0f;
        var mf = plate.GetComponent<MeshFilter>();
        if (mf != null && mf.sharedMesh != null) face = mf.sharedMesh.bounds.extents.z;
        float lift = 0.005f / Mathf.Max(1e-4f, Mathf.Abs(plate.lossyScale.z));
        float z = face + lift;
        MakeQuad(plate, mat, TextName + "（表）", new Vector3(0f, 0f, -z), Quaternion.identity);
        MakeQuad(plate, mat, TextName + "（裏）", new Vector3(0f, 0f, z), Quaternion.Euler(0f, 180f, 0f));

        EditorSceneManager.MarkSceneDirty(L.gameObject.scene);
        Selection.activeGameObject = plate.gameObject;
        Debug.Log("[MirrorPlateBuilder] カーブミラーの看板に「注意」を貼りました：" + plate.name);
    }

    static void MakeQuad(Transform plate, Material mat, string name, Vector3 pos, Quaternion rot)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = name;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(plate, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = rot;
        go.transform.localScale = new Vector3(0.85f, 0.85f, 1f);
        var r = go.GetComponent<Renderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        Undo.RegisterCreatedObjectUndo(go, "Mirror plate text");
    }

    static Material GetMaterial()
    {
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
        if (tex == null) return null;
        var imp = (TextureImporter)AssetImporter.GetAtPath(TexturePath);
        if (imp != null && !imp.alphaIsTransparency)
        {
            imp.alphaIsTransparency = true;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.SaveAndReimport();
            tex = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
        }
        var mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Unlit/Transparent")) { mainTexture = tex };
            AssetDatabase.CreateAsset(mat, MaterialPath);
        }
        else if (mat.mainTexture != tex)
        {
            mat.mainTexture = tex;
            EditorUtility.SetDirty(mat);
        }
        return mat;
    }

    static Transform FindDeep(Transform t, string name)
    {
        if (t.name == name) return t;
        for (int i = 0; i < t.childCount; i++)
        {
            var f = FindDeep(t.GetChild(i), name);
            if (f != null) return f;
        }
        return null;
    }
}
