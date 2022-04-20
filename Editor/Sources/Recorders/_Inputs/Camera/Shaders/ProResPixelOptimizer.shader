Shader "Hidden/Recorder/ProResPixelOptimizer" {
    Properties
    {
        _MainTex ("Texture", any) = "" {}
    }
    SubShader {
        Pass {
            ZTest Always Cull Off ZWrite Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #define BT709 // Rec.709
            #include "UnityCG.cginc"
            #include "CUPacking.cginc"
            #include "CUColorConversions.cginc"

            UNITY_DECLARE_SCREENSPACE_TEXTURE(_MainTex);
            uniform float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            #pragma multi_compile ___ RGB24_TO_2VUY8BITS // pack RGB24 into 2VUY format
            #pragma multi_compile ___ RGBA64_TO_AYCBCR
            #pragma multi_compile ___ INPUT_IS_SRGB
            struct appdata_t {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert (appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord.xy, _MainTex);
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float2 t = i.texcoord;
                half4 result;
                #if RGBA64_TO_AYCBCR
                    half4 c = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, t);
                    result.x = c.a; // We need to go from RGBA64 to ARGB64
                    result.yzw = RGB2YUV_8BITS(c.rgb);
                #elif RGB24_TO_2VUY8BITS
                    const float3 ts = float3(_MainTex_TexelSize.xy, 0);
                    float2 uv = i.texcoord;
                    uv.x -= ts.x * 0.5; // pick inside the pixel
                    half3 c = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, uv);
                    half3 cNext = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, uv + ts.xz);
                    half3 packedCurrent = RGB2YUV_8BITS(c.rgb);
                    half3 packedNext = RGB2YUV_8BITS(cNext.rgb);
                    result = half4(0.5 * (packedCurrent.y + packedNext.y), packedCurrent.x, 0.5 * (packedCurrent.z + packedNext.z), packedNext.x); // Cb, Y0, Cr, Y1
                #else
                    result = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, t);
                #endif
                return result;
            }
            ENDCG
        }
    }
    Fallback Off
}
