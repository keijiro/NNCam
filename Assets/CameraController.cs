using System.Collections.Generic;
using UnityEngine;
using Unity.Barracuda;

namespace NNCam {

sealed class CameraController : MonoBehaviour
{
    #region Editable attributes

    [SerializeField] UnityEngine.UI.RawImage _preview = null;
    [SerializeField] UnityEngine.UI.RawImage _overlay = null;

    #endregion

    #region Hidden asset references

    [SerializeField, HideInInspector] Unity.Barracuda.NNModel _model = null;
    [SerializeField, HideInInspector] ComputeShader _converter = null;

    #endregion

    #region Compile-time constants

    public const int Size = 416;

    #endregion

    #region Internal objects

    WebCamTexture _webcam;
    RenderTexture _cropped;
    ComputeBuffer _buffer;
    IWorker _worker;

    #endregion

    #region MonoBehaviour implementation

    void Start()
    {
        _webcam = new WebCamTexture();
        _webcam.Play();

        _preview.texture = _cropped = new RenderTexture(Size, Size, 0);
        _buffer = new ComputeBuffer(Size * Size * 3, sizeof(float));

        _worker = ModelLoader.Load(_model).CreateWorker();
    }

    void OnDisable()
    {
        _buffer?.Dispose();
        _buffer = null;

        _worker?.Dispose();
        _worker = null;
    }

    void OnDestroy()
    {
        if (_webcam != null) Destroy(_webcam);
        if (_cropped != null) Destroy(_cropped);
        if (_overlay.texture != null) Destroy(_overlay.texture);
    }

    void Update()
    {
        // Check if the last task has been completed.
        if (_worker.scheduleProgress >= 1)
        {
            // Replace the overlay texture with the output.
            if (_overlay.texture != null) Destroy(_overlay.texture);
            var output = _worker.PeekOutput("float_segments");
            using (var segs = output.Reshape(new TensorShape(1, 26, 26, 1)))
                _overlay.texture = segs.ToRenderTexture();
        }

        // Input image cropping
        var aspect = (float)_webcam.height / _webcam.width;
        var scale = new Vector2(aspect, 1);
        var offset = new Vector2(aspect / 2, 0);
        Graphics.Blit(_webcam, _cropped, scale, offset);

        // Image to tensor conversion
        _converter.SetTexture(0, "_Image", _cropped);
        _converter.SetBuffer(0, "_Tensor", _buffer);
        _converter.SetInt("_Width", Size);
        _converter.Dispatch(0, Size / 8, Size / 8, 1);

        // New task scheduling
        using (var tensor = new Tensor(1, Size, Size, 3, _buffer))
        {
            var inputs = new Dictionary<string, Tensor> {{ "sub_2", tensor }};
            _worker.Execute(inputs);
            _worker.FlushSchedule();
        }
    }

    #endregion
}

} // namespace NNCam
