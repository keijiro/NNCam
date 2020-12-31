using UnityEngine;

namespace NNCam {

static class Util
{
    public static RenderTextureFormat SingleChannelRTFormat
      => SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.R8)
           ? RenderTextureFormat.R8 : RenderTextureFormat.Default;

    public static RenderTexture NewSingleChannelRT(int width, int height)
      => new RenderTexture(width, height, 0, SingleChannelRTFormat);
}

} // namespace NNCam
