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

    // The total count of the key points defined in the BodyPix model
    const int KeyPointCount = 17;

    #endregion

    #region Internal objects

    WebCamTexture _webcamRaw;
    RenderTexture _webcamBuffer;
    ComputeBuffer _preprocessed;
    ComputeBuffer _keyPoints;
    Material _visualizer;
    IWorker _worker;

    #endregion

    #region MonoBehaviour implementation

    void Start()
    {
        _webcamRaw = new WebCamTexture();
        _webcamBuffer = new RenderTexture(1920, 1080, 0);
        _preprocessed = new ComputeBuffer(Width * Height * 3, sizeof(float));
        _keyPoints = new ComputeBuffer(KeyPointCount, sizeof(float) * 2);
        _visualizer = new Material(_visualizerShader);
        _worker = ModelLoader.Load(_model).CreateWorker();

        _webcamRaw.Play();
    }

    void OnDisable()
    {
        _preprocessed?.Dispose();
        _preprocessed = null;

        _keyPoints?.Dispose();
        _keyPoints = null;

        _worker?.Dispose();
        _worker = null;
    }

    void OnDestroy()
    {
        if (_webcamRaw != null) Destroy(_webcamRaw);
        if (_webcamBuffer != null) Destroy(_webcamBuffer);
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

        // Keypoint retrieval
        var heatmaps3d = _worker.PeekOutput("float_heatmaps");
        var offsets3d = _worker.PeekOutput("float_short_offsets");

        var (mw, mh) = (heatmaps3d.shape.width, heatmaps3d.shape.height);

        var heatmapsShape = new TensorShape(1, mh, mw * KeyPointCount, 1);
        var offsetsShape = new TensorShape(1, mh, mw * KeyPointCount * 2, 1);
        using (var heatmaps = heatmaps3d.Reshape(heatmapsShape))
        using (var offsets = offsets3d.Reshape(offsetsShape))
        {
            var heatmapsRT = RenderTexture.GetTemporary(mw * KeyPointCount, mh, 0, RenderTextureFormat.RHalf);
            var offsetsRT = RenderTexture.GetTemporary(mw * KeyPointCount * 2, mh, 0, RenderTextureFormat.RHalf);

            heatmaps.ToRenderTexture(heatmapsRT);
            offsets.ToRenderTexture(offsetsRT);

            _decoder.SetTexture(0, "_Heatmaps", heatmapsRT);
            _decoder.SetTexture(0, "_Offsets", offsetsRT);
            _decoder.SetInts("_Dimensions", mw, mh);
            _decoder.SetInt("_Stride", Width / mw + 1);
            _decoder.SetBuffer(0, "_KeyPoints", _keyPoints);
            _decoder.Dispatch(0, 1, 1, 1);

            RenderTexture.ReleaseTemporary(heatmapsRT);
            RenderTexture.ReleaseTemporary(offsetsRT);
        }

        var stride = Width / mw + 1.0f;
        _visualizer.SetVector("_Scale", new Vector2((Width + stride) / Width, (Height + stride) / Height));
    }

    void OnPostRender()
    {
        _visualizer.SetPass(0);
        _visualizer.SetTexture("_CameraFeed", _webcamBuffer);
        Graphics.DrawProceduralNow(MeshTopology.Quads, 4, 1);

        _visualizer.SetPass(1);
        _visualizer.SetBuffer("_KeyPoints", _keyPoints);
        Graphics.DrawProceduralNow(MeshTopology.Quads, 4, KeyPointCount);
    }

    #endregion
}

} // namespace NNCam
