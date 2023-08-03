struct Attributes
{
    uint vertexID : VERTEXID_SEMANTIC;
    float4 positionOS : POSITION;
    float2 uv : TEXCOORD0;
};

struct v2f_DownSample
{
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD0;
    float4 uv01: TEXCOORD1;
    float4 uv23: TEXCOORD2;
};


struct v2f_UpSample
{
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD0;
    float4 uv01: TEXCOORD1;
    float4 uv23: TEXCOORD2;
    float4 uv45: TEXCOORD3;
    float4 uv67: TEXCOORD4;
};

TEXTURE2D(_BlitTexture);
SAMPLER(sampler_BlitTexture);


CBUFFER_START(UnityPerMaterial)
float4 _BlitTexture_ST;
float4 _BlitTexture_TexelSize;
uniform half _Offset;

CBUFFER_END


v2f_DownSample vert_DownSample(Attributes IN)
{
    v2f_DownSample output;
    output.positionCS = GetFullScreenTriangleVertexPosition(IN.vertexID);
    output.uv = output.positionCS.xy * 0.5 + 0.5;

    #if UNITY_UV_STARTS_AT_TOP
    output.uv.y = 1 - output.uv.y;
    #endif

    float2 uv = TRANSFORM_TEX(output.uv, _BlitTexture);

    _BlitTexture_TexelSize *= 0.5;
    output.uv = uv;
    output.uv01.xy = uv - _BlitTexture_TexelSize * float2(1 + _Offset, 1 + _Offset); //top right
    output.uv01.zw = uv + _BlitTexture_TexelSize * float2(1 + _Offset, 1 + _Offset); //bottom left
    output.uv23.xy = uv - float2(_BlitTexture_TexelSize.x, -_BlitTexture_TexelSize.y) * float2(1 + _Offset, 1 + _Offset); //top left
    output.uv23.zw = uv + float2(_BlitTexture_TexelSize.x, -_BlitTexture_TexelSize.y) * float2(1 + _Offset, 1 + _Offset); //bottom right

    return output;
}

half4 frag_DownSample(v2f_DownSample i): SV_Target
{
    half4 sum = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, i.uv) * 4;
    sum += SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, i.uv01.xy);
    sum += SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, i.uv01.zw);
    sum += SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, i.uv23.xy);
    sum += SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, i.uv23.zw);

    return sum * 0.125;
}


v2f_UpSample vert_UpSample(Attributes IN)
{
    v2f_UpSample output;
    output.positionCS = GetFullScreenTriangleVertexPosition(IN.vertexID);
    output.uv = output.positionCS.xy * 0.5 + 0.5;

    #if UNITY_UV_STARTS_AT_TOP
    output.uv.y = 1 - output.uv.y;
    #endif

    float2 uv = TRANSFORM_TEX(output.uv, _BlitTexture);

    
    _BlitTexture_TexelSize *= 0.5;
    _Offset = float2(1 + _Offset, 1 + _Offset);

    output.uv01.xy = uv + float2(-_BlitTexture_TexelSize.x * 2, 0) * _Offset;
    output.uv01.zw = uv + float2(-_BlitTexture_TexelSize.x, _BlitTexture_TexelSize.y) * _Offset;
    output.uv23.xy = uv + float2(0, _BlitTexture_TexelSize.y * 2) * _Offset;
    output.uv23.zw = uv + _BlitTexture_TexelSize * _Offset;
    output.uv45.xy = uv + float2(_BlitTexture_TexelSize.x * 2, 0) * _Offset;
    output.uv45.zw = uv + float2(_BlitTexture_TexelSize.x, -_BlitTexture_TexelSize.y) * _Offset;
    output.uv67.xy = uv + float2(0, -_BlitTexture_TexelSize.y * 2) * _Offset;
    output.uv67.zw = uv - _BlitTexture_TexelSize * _Offset;

    return output;
}

half4 frag_UpSample(v2f_UpSample i): SV_Target
{
    half4 sum = 0;
    sum += SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, i.uv01.xy);
    sum += SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, i.uv01.zw) * 2;
    sum += SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, i.uv23.xy);
    sum += SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, i.uv23.zw) * 2;
    sum += SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, i.uv45.xy);
    sum += SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, i.uv45.zw) * 2;
    sum += SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, i.uv67.xy);
    sum += SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, i.uv67.zw) * 2;

    return sum * 0.0833;
}
