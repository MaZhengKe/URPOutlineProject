Shader "KuanMi/DepthTest"
{
    // The _BaseColor variable is visible in the Material's Inspector, as a field
    // called Base Color. You can use it to select a custom color. This variable
    // has the default value (1, 1, 1, 1).
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"
        }

        ZTest Always

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
             #pragma enable_d3d11_debug_symbols

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;

                float3 positionWS : TEXCOORD0;
                float3 positionVS : TEXCOORD1;
                float4 positionCS : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionVS = TransformWorldToView(OUT.positionWS.xyz);
                OUT.positionCS = TransformWorldToHClip(OUT.positionWS.xyz);
                return OUT;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Returning the _BaseColor value.

                // return 0.5;

                float4 NDC = input.positionCS / input.positionCS.w;
#if UNITY_UV_STARTS_AT_TOP
                NDC.y = -NDC.y;
#endif
                
                // return float4(input.positionHCS.z, 0, 0, 1);
                //
                // return float4(NDC.z,0, 0, 1);
                //
                // return float4(input.positionCS.z,0,0,1);
                // return float4(input.positionCS.xy,0,1);
                return float4(NDC.xyz,1);

                float2 uv = input.positionCS.xy / input.positionCS.w;
                uv = input.positionHCS.xy / _ScreenParams.xy;

                return float4(uv, 0, 1);


                float c = input.positionCS.w;
                c = input.positionCS.z / input.positionCS.w;

                float rawDepth = input.positionHCS.z;
                float ZVS = LinearEyeDepth(rawDepth, _ZBufferParams);

                
                float4 posHCS = float4(uv * 2.0 - 1.0, c, 1);
                #if UNITY_UV_STARTS_AT_TOP
                posHCS.y = -posHCS.y;
                #endif


                float4 posCS = ZVS * posHCS;

                
                float3 positionVS = mul(unity_MatrixInvP, posCS);
                
                
                // uv = input.positionHCS.xy / _ScreenParams.xy;
                c = input.positionHCS.w;

                // return float4(input.positionVS.xyz, 1);
                return float4(positionVS.xyz, 1);
                // return float4(posHCS.xy, 0, 1);
                return float4(input.positionCS.xy / input.positionCS.w, 0, 1);

                
                // float c =  -input.positionVS.z;
                return float4(c, 0, 0, 1);
                return _BaseColor;
            }
            ENDHLSL
        }
    }
}