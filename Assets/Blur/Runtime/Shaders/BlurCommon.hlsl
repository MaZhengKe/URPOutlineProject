struct DefaultAttributes
{
    uint vertexID : VERTEXID_SEMANTIC;
};

struct DefaultVaryings
{
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD0;
};

TEXTURE2D(_BlitTexture);
SAMPLER(sampler_BlitTexture);

CBUFFER_START(BlurCommon)
float4 _BlitTexture_ST;
float4 _BlitTexture_TexelSize;
float _Iteration;
float _Offset;
half4 _BlurOffset;
CBUFFER_END

DefaultVaryings defaultVert(DefaultAttributes IN)
{
    DefaultVaryings output;
    output.positionCS = GetFullScreenTriangleVertexPosition(IN.vertexID);
    output.uv = output.positionCS.xy * 0.5 + 0.5;

    #if UNITY_UV_STARTS_AT_TOP
    output.uv.y = 1 - output.uv.y;
    #endif
    return output;
}




