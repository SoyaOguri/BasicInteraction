using UnityEngine;
using UnityEngine.Rendering;

// 接近動画のパノラマ出力。ApproachVideoRig が録画のときだけ作る。
// 毎フレーム、目の位置から全方向をキューブマップに撮り、画面には横 180° の円筒に展開して描く
[RequireComponent(typeof(Camera))]
public class ApproachPanoramaOutput : MonoBehaviour
{
    public Transform eye;               // 目の位置と向き（ApproachVideoRig のカメラの根元）
    public float horizontalFov = 180f;  // 横の視野角 [deg]
    public float verticalFov = 60f;     // 縦の視野角 [deg]（シミュレータの正面スクリーンと同じ）
    public int cubeSize = 2048;

    Camera cubeCam;
    RenderTexture cube;
    Material mat;

    void Awake()
    {
        var shader = Shader.Find("Hidden/ApproachPanorama");
        if (shader == null)
        {
            Debug.LogError("[ApproachVideo] Hidden/ApproachPanorama のシェーダーが見つかりません。");
            enabled = false;
            return;
        }
        mat = new Material(shader);
        cube = new RenderTexture(cubeSize, cubeSize, 24) { dimension = TextureDimension.Cube, antiAliasing = 1 };
        cube.Create();

        var go = new GameObject("PanoramaCubeCamera");
        go.transform.SetParent(transform, false);
        cubeCam = go.AddComponent<Camera>();
        cubeCam.enabled = false;   // 画面には描かず、キューブマップにだけ撮る
        cubeCam.nearClipPlane = 0.1f;
        cubeCam.farClipPlane = 1000f;
        cubeCam.clearFlags = CameraClearFlags.Skybox;
    }

    void LateUpdate()
    {
        if (eye == null || cubeCam == null) return;
        cubeCam.transform.position = eye.position;
        cubeCam.RenderToCubemap(cube, 63);
    }

    void OnRenderImage(RenderTexture src, RenderTexture dst)
    {
        if (mat == null) { Graphics.Blit(src, dst); return; }
        mat.SetTexture("_Cube", cube);
        mat.SetFloat("_HFov", horizontalFov * Mathf.Deg2Rad);
        mat.SetFloat("_TanHalfV", Mathf.Tan(verticalFov * 0.5f * Mathf.Deg2Rad));
        mat.SetMatrix("_Rot", eye != null ? Matrix4x4.Rotate(eye.rotation) : Matrix4x4.identity);
        Graphics.Blit(src, dst, mat);
    }

    void OnDestroy()
    {
        if (cube != null) cube.Release();
    }
}
