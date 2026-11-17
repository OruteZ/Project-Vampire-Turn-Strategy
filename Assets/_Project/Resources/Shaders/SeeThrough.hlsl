#ifndef VAMPIRE_SEE_THROUGH_INCLUDED
#define VAMPIRE_SEE_THROUGH_INCLUDED

// Set by SeeThrough.cs each frame: xy viewport point, z eye depth, w hole radius as a fraction of screen height (0 = off).
float4 _SeeThroughA;
float4 _SeeThroughB;

// docs/art/see-through.md 2: only what stands in front of the focus opens, so a wall behind a unit stays whole.
half SeeThroughHole(float4 f, float2 uv, float eye)
{
    if (f.w <= 0 || eye > f.z - 0.5) return 0;
    float2 d = uv - f.xy;
    d.x *= _ScreenParams.x / _ScreenParams.y;
    return 1 - smoothstep(f.w * 0.55, f.w, length(d));
}

void SeeThroughClip(float4 positionCS)
{
    float2 uv = GetNormalizedScreenSpaceUV(positionCS);
    float eye = LinearEyeDepth(positionCS.z, _ZBufferParams);
    half hole = max(SeeThroughHole(_SeeThroughA, uv, eye), SeeThroughHole(_SeeThroughB, uv, eye));
    const float bayer[16] = { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 };
    uint2 p = uint2(positionCS.xy) % 4;
    clip((bayer[p.y * 4 + p.x] + 0.5) / 16 - hole);
}

#if defined(UNIVERSAL_FORWARD_LIT_PASS_INCLUDED)
void SeeThroughForward(Varyings input, out half4 outColor : SV_Target0
#ifdef _WRITE_RENDERING_LAYERS
    , out uint outRenderingLayers : SV_Target1
#endif
)
{
    SeeThroughClip(input.positionCS);
    LitPassFragment(input, outColor
#ifdef _WRITE_RENDERING_LAYERS
        , outRenderingLayers
#endif
    );
}
#elif defined(UNIVERSAL_LIT_GBUFFER_PASS_INCLUDED)
GBufferFragOutput SeeThroughGBuffer(Varyings input)
{
    SeeThroughClip(input.positionCS);
    return LitGBufferPassFragment(input);
}
#elif defined(UNIVERSAL_FORWARD_LIT_DEPTH_NORMALS_PASS_INCLUDED)
void SeeThroughDepthNormals(Varyings input, out half4 outNormalWS : SV_Target0
#ifdef _WRITE_RENDERING_LAYERS
    , out uint outRenderingLayers : SV_Target1
#endif
)
{
    SeeThroughClip(input.positionCS);
    DepthNormalsFragment(input, outNormalWS
#ifdef _WRITE_RENDERING_LAYERS
        , outRenderingLayers
#endif
    );
}
#elif defined(UNIVERSAL_DEPTH_ONLY_PASS_INCLUDED)
half SeeThroughDepthOnly(Varyings input) : SV_TARGET
{
    SeeThroughClip(input.positionCS);
    return DepthOnlyFragment(input);
}
#endif

#endif
