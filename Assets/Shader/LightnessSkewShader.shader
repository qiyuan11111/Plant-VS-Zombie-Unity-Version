Shader "Custom/LightnessSkewShader" {
    Properties {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        [PerRendererData] _Brightness ("Brightness", Range(0.0, 2.0)) = 1.0
        [PerRendererData] _Color ("Color", Color) = (1,1,1,1)
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
                UNITY_DEFINE_INSTANCED_PROP(float, _Alpha)
            UNITY_INSTANCING_BUFFER_END(PerSprite)

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
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
