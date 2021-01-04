Shader "Hidden/NNCam/Pose/Visualizer"
{
    Properties
    {
        _CameraFeed("", 2D) = ""{}
    }

    CGINCLUDE

    #include "UnityCG.cginc"

    // Camera feed blit

    sampler2D _CameraFeed;

    void VertexBlit(uint vid : SV_VertexID,
                    out float4 position : SV_Position,
                    out float2 uv : TEXCOORD0)
    {
        float x = vid >> 1;
        float y = (vid & 1) ^ (vid >> 1);

        position = float4(float2(x, y) * 2 - 1, 1, 1);
        uv = float2(x, y);
    }

    float4 FragmentBlit(float4 position : SV_Position,
                        float2 uv : TEXCOORD0) : SV_Target
    {
        return tex2D(_CameraFeed, uv);
    }

    // Key point draw

    Buffer<float2> _KeyPoints;
    float2 _Scale;

    void VertexKeyPoints(uint vid : SV_VertexID,
                         uint iid : SV_InstanceID,
                         out float4 position : SV_Position)
    {
        float2 kp = (_KeyPoints[iid] * 2 - 1) * _Scale;

        float aspect = _ScreenParams.y * (_ScreenParams.z - 1);
        kp.x += lerp(-1, 1, vid >> 1              ) * 0.01 * aspect;
        kp.y += lerp(-1, 1, (vid & 1) ^ (vid >> 1)) * 0.01;

        position = float4(kp, 1, 1);
    }

    float4 FragmentKeyPoints(float4 position : SV_Position) : SV_Target
    {
        return 1;
    }

    ENDCG

    SubShader
    {
        Pass
        {
            ZTest Always ZWrite Off Cull Off
            CGPROGRAM
            #pragma vertex VertexBlit
            #pragma fragment FragmentBlit
            ENDCG
        }
        Pass
        {
            ZTest Always ZWrite Off Cull Off
            CGPROGRAM
            #pragma vertex VertexKeyPoints
            #pragma fragment FragmentKeyPoints
            ENDCG
        }
    }
}
