#ifndef VAMPIRE_SEE_THROUGH_INCLUDED
#define VAMPIRE_SEE_THROUGH_INCLUDED

// Set by SeeThrough.cs each frame. Points: xy viewport, z eye depth, w hole radius as a fraction of screen height.
#define SEE_THROUGH_MAX 64
float4 _SeeThroughPoints[SEE_THROUGH_MAX];
float _SeeThroughCount;
// Tile mode: xy = board min xz, zw = board max xz (world), _SeeThroughFloor = board floor height; zero size = off.
float4 _SeeThroughBoard;
float _SeeThroughFloor;

// docs/art/see-through.md 2: only what stands in front of the focus opens, so a wall behind a unit stays whole.
half SeeThroughHole(float4 f, float2 uv, float eye)
{
    if (eye > f.z - 0.5) return 0;
    float2 d = uv - f.xy;
    d.x *= _ScreenParams.x / _ScreenParams.y;
    return 1 - smoothstep(f.w * 0.55, f.w, length(d));
}

// docs/art/see-through.md 2-3: open whatever stands between the camera and a board tile.
half SeeThroughBoardHole(float2 uv, float deviceDepth)
{
    if (_SeeThroughBoard.z <= _SeeThroughBoard.x) return 0;
    float3 p = ComputeWorldSpacePosition(uv, deviceDepth, UNITY_MATRIX_I_VP);
    float3 cam = _WorldSpaceCameraPos;
    if (p.y < _SeeThroughFloor + 0.3 || cam.y <= p.y) return 0;
    float2 g = cam.xz + (p.xz - cam.xz) * ((cam.y - _SeeThroughFloor) / (cam.y - p.y));
    float2 inside = min(g - _SeeThroughBoard.xy, _SeeThroughBoard.zw - g);
    return smoothstep(0, 0.8, min(inside.x, inside.y));
}

void SeeThroughClip(float4 positionCS)
{
    float2 uv = GetNormalizedScreenSpaceUV(positionCS);
    float eye = LinearEyeDepth(positionCS.z, _ZBufferParams);
    half hole = SeeThroughBoardHole(uv, positionCS.z);
    for (int i = 0; i < (int)_SeeThroughCount; i++) hole = max(hole, SeeThroughHole(_SeeThroughPoints[i], uv, eye));
    const float bayer[16] = { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 };
    uint2 q = uint2(positionCS.xy) % 4;
    clip((bayer[q.y * 4 + q.x] + 0.5) / 16 - hole);
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
