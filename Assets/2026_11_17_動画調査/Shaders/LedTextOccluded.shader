// 3D テキスト（TextMesh）用。標準の GUI/Text Shader と違い、手前の塀や建物に隠れる（ZTest LEqual）
Shader "Custom/LedTextOccluded"
{
    Properties
    {
        _MainTex ("Font Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Lighting Off
        Cull Off
        ZWrite Off
        ZTest LEqual
        // 文字は黒い発光面の 2 mm 手前にあるだけなので、遠くから見ると奥行きの精度が足りず、
        // 発光面と奪い合って文字の一部が楕円形に欠ける（Z ファイティング）。奥行きを少しだけ手前に寄せて必ず勝たせる
        Offset -2, -2
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;

            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 c = i.color;
                c.a *= tex2D(_MainTex, i.uv).a;
                return c;
            }
            ENDCG
        }
    }
}
