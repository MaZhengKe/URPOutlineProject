Shader "KuanMi/DepthPyramid"
{

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType"="Transparent"
            "Queue"="Transparent"
        }

        Pass
        {

            Name "DepthPyramid"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            float _DepthMipLevel;


            TEXTURE2D_X_FLOAT(_TMPCameraDepthTexture);
            SAMPLER(sampler_TMPCameraDepthTexture);
            float4 _TMPCameraDepthTexture_TexelSize;


            struct Attributes
            {
                uint vertexID : VERTEXID_SEMANTIC;
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 texCoord0 : TEXCOORD0;
            };

            Varyings vert(Attributes IN)
            {
                Varyings output;
                output.positionCS = GetFullScreenTriangleVertexPosition(IN.vertexID);
                output.texCoord0 = output.positionCS.xy * 0.5 + 0.5;

                #if UNITY_UV_STARTS_AT_TOP
                output.texCoord0.y = 1 - output.texCoord0.y;
                #endif
                return output;
            }
            
            #if UNITY_REVERSED_Z
            # define MIN_DEPTH(l, r) max(l, r)
            #else
            # define MIN_DEPTH(l, r) min(l, r)
            #endif
            
            half frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.texCoord0;

                int2 screen = int2(_ScreenParams.xy)>> ((int)_DepthMipLevel);
                int2 UPScreen = screen << 1;
                
                int2 CurrentPOS = int2(uv * screen);

                float2 o = (float2)CurrentPOS / screen;

                int2 upUV = o * UPScreen;



                float p00 = LOAD_TEXTURE2D_X_LOD(_TMPCameraDepthTexture, upUV + int2(0,0), _DepthMipLevel-1).r;
                float p01 = LOAD_TEXTURE2D_X_LOD(_TMPCameraDepthTexture, upUV + int2(0,1), _DepthMipLevel-1).r;
                float p10 = LOAD_TEXTURE2D_X_LOD(_TMPCameraDepthTexture, upUV + int2(1,0), _DepthMipLevel-1).r;
                float p11 = LOAD_TEXTURE2D_X_LOD(_TMPCameraDepthTexture, upUV + int2(1,1), _DepthMipLevel-1).r;
                float4 depths = float4(p00, p10, p01, p11);

                float minDepth = MIN_DEPTH(MIN_DEPTH(depths.x, depths.y), MIN_DEPTH(depths.z, depths.w));


                // float depth = LOAD_TEXTURE2D_X_LOD(_TMPCameraDepthTexture, upUV, _DepthMipLevel-1).r;

                return minDepth;
            }
            ENDHLSL
        }
    }
}