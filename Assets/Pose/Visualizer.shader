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

    Buffer<uint2> _HeatmapPositions;
    uint _ShapeX, _ShapeY;

    texture2D _KeyPointOffsets;

    float2 SampleKeyPointOffsets(uint2 position, uint id)
    {
        uint2 ix = position * uint2(34, 1) + uint2(id + 17, 0);
        uint2 iy = position * uint2(34, 1) + uint2(id +  0, 0);
        return float2(_KeyPointOffsets[ix].x, _KeyPointOffsets[iy].x);
    }

    void VertexKeyPoints(uint vid : SV_VertexID,
                         uint iid : SV_InstanceID,
                         out float4 position : SV_Position)
    {
        uint2 hp = _HeatmapPositions[iid];

        float x = ((hp.x + 0.5) / _ShapeX) * 2 - 1;
        float y = ((hp.y + 0.5) / _ShapeY) * 2 - 1;

        float2 offs = SampleKeyPointOffsets(hp, iid);
        x += offs.x * 8 / _ScreenParams.x;
        y -= offs.y * 8 / _ScreenParams.y;

        float aspect = _ScreenParams.y * (_ScreenParams.z - 1);
        float dx = lerp(-1, 1, vid >> 1              ) * 0.01 * aspect;
        float dy = lerp(-1, 1, (vid & 1) ^ (vid >> 1)) * 0.01;

        position = float4(x + dx, y + dy, 1, 1);
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
