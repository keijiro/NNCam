Shader "NNCam/Overlay"
{
    Properties
    {
        _MainTex("Texture", 2D) = ""{}
    }

    CGINCLUDE

    #include "UnityCG.cginc"

    sampler2D _MainTex;

    void Vertex(float4 position : POSITION,
                float4 color : COLOR,
                float2 uv : TEXCOORD0,
                out float4 outPosition : SV_Position,
                out float4 outColor : COLOR,
                out float2 outUV : TEXCOORD0)
    {
        outPosition = UnityObjectToClipPos(position);
        outColor = color;
        outUV = uv;
    }

    float4 Fragment(float4 position : SV_Position,
                    float4 color : COLOR,
                    float2 uv : TEXCOORD0) : SV_Target
    {
        return color * tex2D(_MainTex, uv).r; 
    }

    ENDCG

    SubShader
    {
        Tags { "Queue"="Overlay" }
        Pass
        {
            Blend One One ZWrite Off ZTest Always
            CGPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            ENDCG
        }
    }
}
