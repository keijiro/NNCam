Shader "Hidden/NNCam/Postprocess"
{
    Properties
    {
        _MainTex("", 2D) = ""{}
    }

    CGINCLUDE

    #include "UnityCG.cginc"

    sampler2D _MainTex;

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
        // Denormalization
        float s = (tex2D(_MainTex, uv).r - 0.5) * 32;

        // Sigmoid activator
        return 1 / (1 + exp(-s));
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
