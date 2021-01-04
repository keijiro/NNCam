using UnityEngine;
using Unity.Barracuda;

namespace NNCam {

sealed class InputStream : MonoBehaviour
{
    #region Enum definitions

    enum Architecture { MobileNetV1, ResNet50 }

    #endregion

    #region Editable attributes

    [SerializeField] Architecture _architecture = Architecture.MobileNetV1;
    [SerializeField] Unity.Barracuda.NNModel _model = null;
    [SerializeField, HideInInspector] ComputeShader _preprocessor = null;
    [SerializeField, HideInInspector] Shader _postprocessShader = null;

    #endregion

    #region Compile-time constants

    // We use a bit strange aspect ratio (20:11) because we have to use 16n+1
    // for these dimension values. It may distort input images a bit, but it
    // might not be a problem for the segmentation models.
    const int Width = 640 + 1;
    const int Height = 352 + 1;

    #endregion

    #region Internal objects

    WebCamTexture _webcamRaw;
    RenderTexture _webcamBuffer;
    ComputeBuffer _preprocessed;
    RenderTexture _postprocessed;
    Material _postprocessor;
    IWorker _worker;

    #endregion

    #region Public properties

    public Texture CameraTexture => _webcamBuffer;
    public Texture MaskTexture => _postprocessed;

    #endregion

    #region MonoBehaviour implementation

    void Start()
    {
        _webcamRaw = new WebCamTexture();
        _webcamBuffer = new RenderTexture(1920, 1080, 0);
        _preprocessed = new ComputeBuffer(Width * Height * 3, sizeof(float));
        _postprocessed = Util.NewSingleChannelRT(1920, 1000);
        _postprocessor = new Material(_postprocessShader);
        _worker = ModelLoader.Load(_model).CreateWorker();

        _webcamRaw.Play();
    }

    void OnDisable()
    {
        _preprocessed?.Dispose();
        _preprocessed = null;

        _worker?.Dispose();
        _worker = null;
    }

    void OnDestroy()
    {
        if (_webcamRaw != null) Destroy(_webcamRaw);
        if (_webcamBuffer != null) Destroy(_webcamBuffer);
        if (_postprocessed != null) Destroy(_postprocessed);
        if (_postprocessor != null) Destroy(_postprocessor);
    }

    void Update()
    {
        // Do nothing if there is no update on the webcam.
        if (!_webcamRaw.didUpdateThisFrame) return;

        // Input buffer update
        var vflip = _webcamRaw.videoVerticallyMirrored;
        var scale = new Vector2(1, vflip ? -1 : 1);
        var offset = new Vector2(0, vflip ? 1 : 0);
        Graphics.Blit(_webcamRaw, _webcamBuffer, scale, offset);

        // Preprocessing for BodyPix
        var kernel = (int)_architecture;
        _preprocessor.SetTexture(kernel, "_Texture", _webcamBuffer);
        _preprocessor.SetBuffer(kernel, "_Tensor", _preprocessed);
        _preprocessor.SetInt("_Width", Width);
        _preprocessor.SetInt("_Height", Height);
        _preprocessor.Dispatch(kernel, Width / 8 + 1, Height / 8 + 1, 1);

        // BodyPix invocation
        using (var tensor = new Tensor(1, Height, Width, 3, _preprocessed))
            _worker.Execute(tensor);

        // BodyPix output retrieval
        var output = _worker.PeekOutput("float_segments");
        var (w, h) = (output.shape.sequenceLength, output.shape.height);
        using (var segs = output.Reshape(new TensorShape(1, h, w, 1)))
        {
            // Bake into a render texture with normalizing into [0, 1].
            var segsRT = segs.ToRenderTexture(0, 0, 1.0f / 32, 0.5f);
            // Postprocessing shader invocation
            Graphics.Blit(segsRT, _postprocessed, _postprocessor);
            Destroy(segsRT);
        }
    }

    #endregion
}

} // namespace NNCam
