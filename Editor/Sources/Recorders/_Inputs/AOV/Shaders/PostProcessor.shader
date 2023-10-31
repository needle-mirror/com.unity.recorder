Shader "Hidden/Recorder/PostProcessor" {
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
            #include "UnityCG.cginc"

            UNITY_DECLARE_SCREENSPACE_TEXTURE(_MainTex);
            uniform float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            #pragma multi_compile ___ CONVERT_TO_SRGB // pack RGB24 into 2VUY format
            #pragma multi_compile ___ DROP_ALPHA
            #pragma multi_compile ___ FLIP_Y

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

            half4 frag (v2f_img img) : SV_Target
            {
                const float3 ts = float3(_MainTex_TexelSize.xy, 0);

                float2 uv = img.uv;
                uv.x -= ts.x * 0.5; // pick inside the pixel

                #if FLIP_Y
                    uv.y = 1.0 - uv.y; // Flip the image in Y
                #endif

                half4 outColor = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, uv);
                 #if CONVERT_TO_SRGB
                    outColor.rgb = LinearToGammaSpace(outColor.rgb);
                #endif

                #if DROP_ALPHA
                    half4 nextTex = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, uv + ts.xz);

                    #if CONVERT_TO_SRGB
                        nextTex.rgb = LinearToGammaSpace(nextTex.rgb);
                    #endif

                    const int pattern = int ( img.pos.x ) % 3;
                    if (pattern == 0)
                    {
                        outColor.rgb = outColor;
                        outColor.a = nextTex.r;
                    }
                    else if (pattern == 1)
                    {
                        outColor.rg = outColor.gb;
                        outColor.ba = nextTex.rg;
                    }
                    else //if (pattern == 2)
                    {
                        outColor.r = outColor.b;
                        outColor.gba = nextTex.rgb;
                    }
                #endif
                return outColor;
            }
            ENDCG
        }
    }
    Fallback Off
}
