Shader "Hidden/Rectangle"
{
    CGINCLUDE

    #include "UnityCG.cginc"

    void Vertex(float4 position : POSITION,
                float2 uv : TEXCOORD0,
                out float4 outPosition : SV_Position,
                out float2 outUV : TEXCOORD0)
    {
        outPosition = UnityObjectToClipPos(position);
        outUV = uv;
    }

    float4 Fragment(float4 position : SV_Position,
                    float2 uv : TEXCOORD0) : SV_Target
    {
        const float width = 8;
        uv = min(uv, 1 - uv);
        float2 bd = uv / fwidth(uv);
        if (min(bd.x, bd.y) > width) discard;
        return float4(1, 0, 0, 1);
    }

    ENDCG

    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            ENDCG
        }
    }
}
