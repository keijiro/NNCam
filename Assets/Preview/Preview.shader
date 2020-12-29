Shader "NNCam/Preview"
{
    Properties
    {
        _BackgroundTex("Background", 2D) = ""{}
        _Threshold("Threshold", Range(0, 1)) = 0.5
        [HideInInspector] _CameraTex("", 2D) = ""{}
        [HideInInspector] _MaskTex("", 2D) = ""{}
    }

    CGINCLUDE

    #include "UnityCG.cginc"

    sampler2D _BackgroundTex;
    float _Threshold;

    sampler2D _CameraTex;
    sampler2D _MaskTex;
    float4 _MaskTex_TexelSize;

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

        // Hack: Slide UVs to fit the mask to the camera texture.
        float2 mask_uv = uv + _MaskTex_TexelSize * float2(0.5 , -0.5);

        // Sample the mask texture and un-normalize the value.
        float mask = (tex2D(_MaskTex, mask_uv).r - 0.5) * 32;

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
