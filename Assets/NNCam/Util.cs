using UnityEngine;

namespace NNCam {

static class Util
{
    public static RenderTextureFormat SingleChannelRTFormat
      => SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.R8)
           ? RenderTextureFormat.R8 : RenderTextureFormat.Default;

    public static RenderTextureFormat SingleChannelHalfRTFormat
      => SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RHalf)
           ? RenderTextureFormat.RHalf : RenderTextureFormat.ARGBHalf;

    public static RenderTexture NewSingleChannelRT(int width, int height)
      => new RenderTexture(width, height, 0, SingleChannelRTFormat);

    public static RenderTexture NewSingleChannelHalfRT(int width, int height)
      => new RenderTexture(width, height, 0, SingleChannelHalfRTFormat);
}

static class ComputeShaderExtensions
{
    static int[] i2 = new int[2];

    public static void SetInts
      (this ComputeShader cs, string name, int x, int y)
    {
        i2[0] = x; i2[1] = y;
        cs.SetInts(name, i2);
    }
}

} // namespace NNCam
