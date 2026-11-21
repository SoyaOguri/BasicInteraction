// LED表示板の文字（画像版）。白い文字の画像（透明の背景）を _Color の色で描く。
// 手前の塀や建物には隠れる（ZTest LEqual）。黒い発光面のすぐ手前にあるので、奥行きを少し手前に寄せて必ず発光面に勝たせる
Shader "Custom/LedPanelText"
{
    Properties
    {
        _MainTex ("Label (white on transparent)", 2D) = "white" {}
        _Color ("Color", Color) = (1, 0.55, 0.08, 1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Lighting Off
        Cull Off
        ZWrite Off
        ZTest LEqual
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
            fixed4 _Color;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 c = _Color;
                c.a *= tex2D(_MainTex, i.uv).a;
                return c;
            }
            ENDCG
        }
    }
}
