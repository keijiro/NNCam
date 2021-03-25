using UnityEngine;
using Unity.Barracuda;

namespace NNCam {

public sealed class SegmentationFilter : System.IDisposable
{
    #region Public constructor

    public SegmentationFilter(ResourceSet resources)
    {
        _resources = resources;
        _preprocessed = new ComputeBuffer(Width * Height * 3, sizeof(float));
        _postprocessed = Util.NewSingleChannelRT(1920, 1080);
        _postprocessor = new Material(_resources.postprocess);
        _worker = ModelLoader.Load(_resources.model).CreateWorker();
    }

    #endregion

    #region IDisposable implementation

    public void Dispose()
    {
        if (_postprocessed != null) Object.Destroy(_postprocessed);
        if (_postprocessor != null) Object.Destroy(_postprocessor);

        _preprocessed?.Dispose();
        _preprocessed = null;

        _worker?.Dispose();
        _worker = null;
    }

    #endregion

    #region Public accessors

    public Texture MaskTexture => _postprocessed;

    #endregion

    #region Compile-time constants

    // We use a bit strange aspect ratio (20:11) because we have to use 16n+1
    // for these dimension values. It may distort input images a bit, but it
    // might not be a problem for the segmentation models.
    const int Width = 640 + 1;
    const int Height = 352 + 1;

    #endregion

    #region Internal objects

    ResourceSet _resources;
    ComputeBuffer _preprocessed;
    RenderTexture _postprocessed;
    Material _postprocessor;
    IWorker _worker;

    #endregion

    #region Main image processing function

    public void ProcessImage(Texture sourceTexture)
    {
        // Preprocessing for BodyPix
        var pre = _resources.preprocess;
        var kernel = (int)_resources.architecture;
        pre.SetTexture(kernel, "_Texture", sourceTexture);
        pre.SetBuffer(kernel, "_Tensor", _preprocessed);
        pre.SetInt("_Width", Width);
        pre.SetInt("_Height", Height);
        pre.Dispatch(kernel, Width / 8 + 1, Height / 8 + 1, 1);

        // BodyPix invocation
        using (var tensor = new Tensor(1, Height, Width, 3, _preprocessed))
            _worker.Execute(tensor);

        // BodyPix output retrieval
        var output = _worker.PeekOutput("float_segments");

        // Bake into a render texture with normalizing into [0, 1].
        var segsRT = output.ToRenderTexture(0, 0, 1.0f / 32, 0.5f);

        // Postprocessing shader invocation
        Graphics.Blit(segsRT, _postprocessed, _postprocessor);
        Object.Destroy(segsRT);
    }

    #endregion
}

} // namespace NNCam
