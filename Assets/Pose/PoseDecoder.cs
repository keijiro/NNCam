using UnityEngine;
using Unity.Barracuda;

namespace NNCam {

sealed class PoseDecoder : MonoBehaviour
{
    #region Enum definitions

    enum Architecture { MobileNetV1, ResNet50 }

    #endregion

    #region Editable attributes

    [SerializeField] Architecture _architecture = Architecture.MobileNetV1;
    [SerializeField] Unity.Barracuda.NNModel _model = null;
    [SerializeField, HideInInspector] ComputeShader _preprocessor = null;
    [SerializeField, HideInInspector] ComputeShader _decoder = null;
    [SerializeField, HideInInspector] Shader _visualizerShader = null;

    #endregion

    #region Compile-time constants

    // We use a bit strange aspect ratio (20:11) because we have to use 16n+1
    // for these dimension values. It may distort input images a bit, but it
    // might not be a problem for the segmentation models.
    const int Width = 640 + 1;
    const int Height = 352 + 1;

    const int KeyPointCount = 17;

    #endregion

    #region Internal objects

    WebCamTexture _webcamRaw;
    RenderTexture _webcamBuffer;
    ComputeBuffer _preprocessed;
    ComputeBuffer _argmaxes;
    RenderTexture _offsets;
    Material _visualizer;
    IWorker _worker;

    #endregion

    #region MonoBehaviour implementation

    void Start()
    {
        _webcamRaw = new WebCamTexture();
        _webcamBuffer = new RenderTexture(1920, 1080, 0);
        _preprocessed = new ComputeBuffer(Width * Height * 3, sizeof(float));
        _visualizer = new Material(_visualizerShader);
        _worker = ModelLoader.Load(_model).CreateWorker();

        _webcamRaw.Play();
    }

    void OnDisable()
    {
        _preprocessed?.Dispose();
        _preprocessed = null;

        _argmaxes?.Dispose();
        _argmaxes = null;

        _worker?.Dispose();
        _worker = null;
    }

    void OnDestroy()
    {
        if (_webcamRaw != null) Destroy(_webcamRaw);
        if (_webcamBuffer != null) Destroy(_webcamBuffer);
        if (_offsets != null) Destroy(_offsets);
        if (_visualizer != null) Destroy(_visualizer);
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

        // Postprocessing for pose estimation
        {
            var tensor = _worker.PeekOutput("float_heatmaps");
            var shape = tensor.shape;
            var flat = new TensorShape(1, shape.height, shape.width * shape.channels, 1);
            if (_argmaxes == null)
                _argmaxes = new ComputeBuffer(KeyPointCount, sizeof(uint) * 2);
            using (var flatten = tensor.Reshape(flat))
            {
                var rt = RenderTexture.GetTemporary(flat.width, flat.height, 0, RenderTextureFormat.RHalf);
                flatten.ToRenderTexture(rt);
                _decoder.SetTexture(0, "_Heatmaps", rt);
                _decoder.SetBuffer(0, "_HeatmapPositions", _argmaxes);
                _decoder.SetInts("_Dimensions", shape.width, shape.height);
                _visualizer.SetInt("_ShapeX", shape.width);
                _visualizer.SetInt("_ShapeY", shape.height);
                _decoder.Dispatch(0, 1, 1, 1);
                RenderTexture.ReleaseTemporary(rt);
            }
        }

        {
            var tensor = _worker.PeekOutput("float_short_offsets");
            var shape = tensor.shape;
            var flat = new TensorShape(1, shape.height, shape.width * shape.channels, 1);
            using (var flatten = tensor.Reshape(flat))
            {
                if (_offsets == null)
                    _offsets = new RenderTexture(flat.width, flat.height, 0, RenderTextureFormat.RHalf);
                flatten.ToRenderTexture(_offsets);
            }
        }
    }

    void OnPostRender()
    {
        _visualizer.SetPass(0);
        _visualizer.SetTexture("_CameraFeed", _webcamBuffer);
        Graphics.DrawProceduralNow(MeshTopology.Quads, 4, 1);

        _visualizer.SetPass(1);
        _visualizer.SetBuffer("_HeatmapPositions", _argmaxes);
        _visualizer.SetTexture("_KeyPointOffsets", _offsets);
        Graphics.DrawProceduralNow(MeshTopology.Quads, 4, KeyPointCount);
    }

    #endregion
}

} // namespace NNCam
