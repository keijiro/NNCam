Shader "NNCam/Overlay"
{
    Properties
    {
        _MainTex("Texture", 2D) = ""{}
    }

    CGINCLUDE

    #include "UnityCG.cginc"

    sampler2D _MainTex;
    float4 _MainTex_TexelSize;

    float3 CubicHermite(float3 A, float3 B, float3 C, float3 D, float t)
    {
        float t2 = t * t;
        float t3 = t * t * t;
        float3 a = -A / 2 + (3 * B) / 2 - (3 * C) / 2 + D / 2;
        float3 b = A - (5 * B) / 2 + 2 * C - D / 2;
        float3 c = -A / 2 + C / 2;
        float3 d = B;
        return a * t3 + b * t2 + c * t + d;
    }

    float3 BicubicHermiteTextureSample(float2 P)
    {
        float2 s1 = _MainTex_TexelSize.xy;
        float2 s2 = s1 * 2;

        float2 pixel = P * _MainTex_TexelSize.zw + 0.5;
        float2 fract = frac(pixel);
        pixel = (floor(pixel) - 0.5) * s1;

        float3 C00 = tex2D(_MainTex, pixel + float2(-s1.x, -s1.y)).rgb;
        float3 C10 = tex2D(_MainTex, pixel + float2(    0, -s1.y)).rgb;
        float3 C20 = tex2D(_MainTex, pixel + float2( s1.x, -s1.y)).rgb;
        float3 C30 = tex2D(_MainTex, pixel + float2( s2.x, -s1.y)).rgb;

        float3 C01 = tex2D(_MainTex, pixel + float2(-s1.x, 0)).rgb;
        float3 C11 = tex2D(_MainTex, pixel + float2(    0, 0)).rgb;
        float3 C21 = tex2D(_MainTex, pixel + float2( s1.x, 0)).rgb;
        float3 C31 = tex2D(_MainTex, pixel + float2( s2.x, 0)).rgb;    

        float3 C02 = tex2D(_MainTex, pixel + float2(-s1.x, s1.y)).rgb;
        float3 C12 = tex2D(_MainTex, pixel + float2(    0, s1.y)).rgb;
        float3 C22 = tex2D(_MainTex, pixel + float2( s1.x, s1.y)).rgb;
        float3 C32 = tex2D(_MainTex, pixel + float2( s2.x, s1.y)).rgb;    

        float3 C03 = tex2D(_MainTex, pixel + float2(-s1.x, s2.y)).rgb;
        float3 C13 = tex2D(_MainTex, pixel + float2(    0, s2.y)).rgb;
        float3 C23 = tex2D(_MainTex, pixel + float2( s1.x, s2.y)).rgb;
        float3 C33 = tex2D(_MainTex, pixel + float2( s2.x, s2.y)).rgb;    

        float3 CP0X = CubicHermite(C00, C10, C20, C30, fract.x);
        float3 CP1X = CubicHermite(C01, C11, C21, C31, fract.x);
        float3 CP2X = CubicHermite(C02, C12, C22, C32, fract.x);
        float3 CP3X = CubicHermite(C03, C13, C23, C33, fract.x);

        return CubicHermite(CP0X, CP1X, CP2X, CP3X, fract.y);
    }

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
        //return color * tex2D(_MainTex, uv).r; 
        return color * smoothstep(0.4, 0.6, BicubicHermiteTextureSample(uv).r);
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
