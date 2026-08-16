Shader "Custom/Particle" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _Brightness ("Brightness", Range(0.0, 2.0)) = 1.0
        _Color ("Color", Color) = (1,1,1,1)
        _Alpha ("Alpha", Range(0.0, 1.0)) = 1
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
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 texcoord1 : TEXCOORD1;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 texcoord1 : TEXCOORD3;
            };

            sampler2D _MainTex;
            float _Brightness;
            float4 _Color;
            float _Alpha;

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.texcoord1 = v.texcoord1;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;
                _Brightness = i.texcoord1.x;
                col.rgb *= _Brightness;
                clip(col.a - 0.01);
                _Alpha = i.texcoord1.y;
                col.a *= _Alpha;
                
                return col;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
