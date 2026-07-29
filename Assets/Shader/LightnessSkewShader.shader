Shader "Custom/LightnessSkewShader" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _Brightness ("Brightness", Range(0.0, 2.0)) = 1.0
        _Color ("Color", Color) = (1,1,1,1)
        _SkewX ("Skew X", Range(-90, 90)) = 0
        _SkewY ("Skew Y", Range(-90, 90)) = 0
        _ScaleX ("Scale X", float) = 1
        _ScaleY ("Scale Y", float) = 1
        _Alpha ("Alpha", Range(0.0, 1.0)) = 1
    }
    SubShader {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "DisableBatching"="True" }
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

            float _SkewX, _SkewY;
            float _ScaleX, _ScaleY;
            float _Alpha;
            

            float4x4 Skew(float _SkewX, float _SkewY)
			{
                float cosX = cos(-_SkewX / 180.0 * UNITY_PI);
                float sinX = sin(-_SkewX / 180.0 * UNITY_PI);

                float cosY = cos(-_SkewY / 180.0 * UNITY_PI);
                float sinY = sin(-_SkewY / 180.0 * UNITY_PI);

                float4x4 skew = float4x4(cosY, -sinX, 0.0, 0.0,
                    sinY, cosX, 0.0, 0.0,
                    0.0, 0.0, 1.0, 0.0,
                    0.0, 0.0, 0.0, 1.0);

                /*float4x4 rorate = float4x4(sinX, -cosY, 0.0, 0.0,
                    cosX, sinY, 0.0, 0.0,
                    0.0, 0.0, 1.0, 0.0,
                    0.0, 0.0, 0.0, 1.0);*/
                
				return skew;
			}

            float4x4 Scale(float _ScaleX, float _ScaleY)
            {
                float4x4 scale = float4x4(_ScaleX / 100.0, 0.0, 0.0, 0.0,
                    0.0, _ScaleY / 100.0, 0.0, 0.0,
                    0.0, 0.0, 1.0, 0.0,
                    0.0, 0.0, 0.0, 1.0);

                return scale;
            }

            v2f vert (appdata v) {
                v2f o;
                float4x4 affine = mul(Skew(_SkewX, _SkewY), Scale(_ScaleX, _ScaleY));
                v.vertex = mul(affine, v.vertex);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.texcoord1 = v.texcoord1;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;
                col.rgb *= _Brightness;
                clip(col.a - 0.01);
                col.a *= _Alpha;
                
                return col;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
