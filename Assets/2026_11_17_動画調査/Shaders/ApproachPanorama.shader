// 接近動画のパノラマ：キューブマップ（目の位置から全方向を撮ったもの）を、
// 横 _HFov ラジアン（既定 180°）の円筒に展開して 1 枚の横長の画像にする。
// 縦の線はまっすぐのまま、左右の画面のつなぎ目が出ない
Shader "Hidden/ApproachPanorama"
{
    Properties
    {
        _MainTex ("Unused", 2D) = "black" {}
        _Cube ("Cube", Cube) = "" {}
    }
    SubShader
    {
        ZTest Always Cull Off ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            samplerCUBE _Cube;
            float _HFov;      // 横の視野角 [rad]
            float _TanHalfV;  // tan(縦の視野角 / 2)
            float4x4 _Rot;    // カメラの向き（ローカル → ワールド）

            fixed4 frag (v2f_img i) : SV_Target
            {
                float th = (i.uv.x - 0.5) * _HFov;             // 横の角度（右が +）
                float y = (i.uv.y - 0.5) * 2.0 * _TanHalfV;     // 円筒の上の高さ（縦はふつうのカメラと同じ）
                float3 d = normalize(float3(sin(th), y, cos(th)));
                d = mul((float3x3)_Rot, d);
                return texCUBE(_Cube, d);
            }
            ENDCG
        }
    }
}
