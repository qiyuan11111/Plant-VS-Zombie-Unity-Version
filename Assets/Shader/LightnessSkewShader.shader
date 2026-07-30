Shader "Custom/LightnessSkewShader" {
    Properties {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        [PerRendererData] _Brightness ("Brightness", Range(0.0, 2.0)) = 1.0
        [PerRendererData] _Color ("Color", Color) = (1,1,1,1)
        [PerRendererData] _SkewX ("Skew X", Range(-90, 90)) = 0
        [PerRendererData] _SkewY ("Skew Y", Range(-90, 90)) = 0
        [PerRendererData] _ScaleX ("Scale X", float) = 1
        [PerRendererData] _ScaleY ("Scale Y", float) = 1
        [PerRendererData] _Alpha ("Alpha", Range(0.0, 1.0)) = 1
    }
    SubShader {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100

        Pass {
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 texcoord1 : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 texcoord1 : TEXCOORD3;
                fixed4 color : COLOR0;
                float2 materialParams : TEXCOORD2;
            };

            sampler2D _MainTex;

            UNITY_INSTANCING_BUFFER_START(PerSprite)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
                UNITY_DEFINE_INSTANCED_PROP(float, _Brightness)
                UNITY_DEFINE_INSTANCED_PROP(float, _SkewX)
                UNITY_DEFINE_INSTANCED_PROP(float, _SkewY)
                UNITY_DEFINE_INSTANCED_PROP(float, _ScaleX)
                UNITY_DEFINE_INSTANCED_PROP(float, _ScaleY)
                UNITY_DEFINE_INSTANCED_PROP(float, _Alpha)
            UNITY_INSTANCING_BUFFER_END(PerSprite)

            float4x4 Skew(float skewX, float skewY)
            {
                float cosX = cos(-skewX / 180.0 * UNITY_PI);
                float sinX = sin(-skewX / 180.0 * UNITY_PI);

                float cosY = cos(-skewY / 180.0 * UNITY_PI);
                float sinY = sin(-skewY / 180.0 * UNITY_PI);

                return float4x4(cosY, -sinX, 0.0, 0.0,
                    sinY, cosX, 0.0, 0.0,
                    0.0, 0.0, 1.0, 0.0,
                    0.0, 0.0, 0.0, 1.0);
            }

            float4x4 Scale(float scaleX, float scaleY)
            {
                return float4x4(scaleX / 100.0, 0.0, 0.0, 0.0,
                    0.0, scaleY / 100.0, 0.0, 0.0,
                    0.0, 0.0, 1.0, 0.0,
                    0.0, 0.0, 0.0, 1.0);
            }

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);

                float skewX = UNITY_ACCESS_INSTANCED_PROP(PerSprite, _SkewX);
                float skewY = UNITY_ACCESS_INSTANCED_PROP(PerSprite, _SkewY);
                float scaleX = UNITY_ACCESS_INSTANCED_PROP(PerSprite, _ScaleX);
                float scaleY = UNITY_ACCESS_INSTANCED_PROP(PerSprite, _ScaleY);
                float4x4 affine = mul(Skew(skewX, skewY), Scale(scaleX, scaleY));
                v.vertex = mul(affine, v.vertex);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.texcoord1 = v.texcoord1;
                o.color = UNITY_ACCESS_INSTANCED_PROP(PerSprite, _Color);
                o.materialParams = float2(
                    UNITY_ACCESS_INSTANCED_PROP(PerSprite, _Brightness),
                    UNITY_ACCESS_INSTANCED_PROP(PerSprite, _Alpha));
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * i.color;
                col.rgb *= i.materialParams.x;
                clip(col.a - 0.01);
                col.a *= i.materialParams.y;
                return col;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
