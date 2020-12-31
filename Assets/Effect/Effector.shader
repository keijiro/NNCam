Shader "Hidden/NNCam/Effector"
{
    Properties
    {
        _MainTex("", 2D) = ""{}
        _FeedbackTex("", 2D) = ""{}
        _MaskTex("", 2D) = ""{}
    }

    CGINCLUDE

    #include "UnityCG.cginc"
    #include "Packages/jp.keijiro.noiseshader/Shader/SimplexNoise3D.hlsl"

    sampler2D _MainTex;
    sampler2D _FeedbackTex;
    sampler2D _MaskTex;
    float2 _Feedback; // length, decay
    float3 _Noise;    // frequency, speed, amount

    float2 DFNoise(float2 uv, float3 freq)
    {
        float3 np = float3(uv, _Time.y) * freq;
        float2 n1 = snoise_grad(np).xy;
        return cross(float3(n1, 0), float3(0, 0, 1)).xy;
    }

    float2 Displacement(float2 uv)
    {
        float aspect = _ScreenParams.x / _ScreenParams.y;
        float2 p = uv * float2(aspect, 1);
        float2 n = DFNoise(p, _Noise.xxy * -1) * _Noise.z +
                   DFNoise(p, _Noise.xxy * +2) * _Noise.z * 0.5;
        return n * float2(1, aspect);
    }

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
        float3 camera = tex2D(_MainTex, uv).rgb;
        float4 feedback = tex2D(_FeedbackTex, uv + Displacement(uv));

        float mask = smoothstep(0.9, 1, tex2D(_MaskTex, uv).r);

        float alpha = lerp(feedback.a * (1 - _Feedback.y), _Feedback.x, mask);
        float3 rgb = lerp(camera, feedback.rgb, saturate(alpha) * (1 - mask));

        return float4(rgb, alpha);
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
