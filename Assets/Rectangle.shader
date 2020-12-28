Shader "Hidden/Rectangle"
{
    CGINCLUDE

    #include "UnityCG.cginc"

    float4 _Color;

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
        return _Color;
    }

    ENDCG

    SubShader
    {
        Tags { "Queue" = "Overlay" }
        Pass
        {
            Blend One One
            ZWrite Off
            ZTest Always
            CGPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            ENDCG
        }
    }
}
