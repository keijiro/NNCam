Shader "NNCam/Composition"
{
    Properties
    {
        _BackgroundTex("Background", 2D) = ""{}
        [HideInInspector] _CameraTex("Camera", 2D) = ""{}
        [HideInInspector] _MaskTex("Mask", 2D) = ""{}
        [HideInInspector] _Threshold("Threshold", Range(0, 1)) = 0.5
    }

    CGINCLUDE

    #include "UnityCG.cginc"

    sampler2D _BackgroundTex;
    sampler2D _CameraTex;
    sampler2D _MaskTex;
    float4 _MaskTex_TexelSize;
    float _Threshold;

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
        float3 fg = tex2D(_CameraTex, uv).rgb;
        float3 bg = tex2D(_BackgroundTex, uv).rgb;

        // Sample the mask texture and un-normalize the value.
        float mask = (tex2D(_MaskTex, uv).r - 0.5) * 32;

        // Apply a sigmoid activator to the mask value.
        mask = 1 / (1 + exp(-mask));

        return float4(lerp(bg, fg, smoothstep(_Threshold, 1, mask)), 1);
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
