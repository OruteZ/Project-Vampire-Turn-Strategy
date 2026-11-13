Shader "VampireSpawn/LightBeam"
{
    Properties { _Color ("Color", Color) = (1, 0.95, 0.8, 0.35) }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Blend SrcAlpha One
        ZWrite Off
        Cull Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 _Color;
            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; float4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float4 color : COLOR; float3 n : TEXCOORD0; float3 v : TEXCOORD1; };
            v2f vert(appdata i)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(i.vertex);
                o.color = i.color * _Color;
                o.n = UnityObjectToWorldNormal(i.normal);
                o.v = WorldSpaceViewDir(i.vertex);
                return o;
            }
            float4 frag(v2f i) : SV_Target
            {
                // Edge-on sides fade out so the cone has no hard silhouette.
                float facing = abs(dot(normalize(i.n), normalize(i.v)));
                return float4(i.color.rgb, i.color.a * facing * facing);
            }
            ENDHLSL
        }
    }
}
