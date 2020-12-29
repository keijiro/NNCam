Shader "NNCam/Preview"
{
    Properties
    {
        _SourceTex("Source", 2D) = ""{}
        _MaskTex("Mask", 2D) = ""{}
    }

    CGINCLUDE

    #include "UnityCG.cginc"
    #include "CubicSampler.cginc"

    sampler2D _SourceTex;
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
        float4 fg = tex2D(_SourceTex, uv);
        float4 bg = Luminance(fg) * float4(0, 0, 0.75, 0);
        float mask = BicubicTextureSample(_MaskTex, uv, _MaskTex_TexelSize).r;
        return lerp(bg, fg, smoothstep(0.4, 0.6, mask));
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
