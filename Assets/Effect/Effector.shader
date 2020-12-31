Shader "Hidden/NNCam/Effector"
{
    Properties
    {
        _Feedback("", 2D) = ""{}
        _CameraFeed("", 2D) = ""{}
        _Mask("", 2D) = ""{}
    }

    CGINCLUDE

    #include "UnityCG.cginc"
    #include "Packages/jp.keijiro.noiseshader/Shader/SimplexNoise3D.hlsl"

    sampler2D _Feedback;
    sampler2D _CameraFeed;
    sampler2D _Mask;
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
        float2 uv2 = (uv - 0.5) * float2(1, 1) + 0.5;

        float2 n1 = snoise_grad(float3(uv2 * 1.8174 + 3.1784, _Time.y * 0.3)).xy;
        float2 n2 = cross(float3(n1, 0), float3(0, 0, 1)).xy;

        uv2 += n2 * 0.004;

        float4 bg = tex2D(_Feedback, uv2);// * 0.99;
        bg.a *= 0.99;
        float4 fg = tex2D(_CameraFeed, uv);
        float mask = tex2D(_Mask, uv).r;
        float th1 = max(0, _Threshold - 0.1);
        float th2 = min(1, _Threshold + 0.1);
        mask = smoothstep(th1, th2, mask);

        bg = float4(lerp(fg.rgb, bg.rgb, saturate(bg.a)), bg.a);

        return lerp(bg, float4(fg.rgb, 3), mask);
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
