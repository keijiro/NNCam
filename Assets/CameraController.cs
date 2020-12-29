using System.Collections.Generic;
using UnityEngine;
using Unity.Barracuda;

namespace NNCam {

sealed class CameraController : MonoBehaviour
{
    #region Hidden asset references

    [SerializeField, HideInInspector] Unity.Barracuda.NNModel _model = null;
    [SerializeField, HideInInspector] ComputeShader _converter = null;

    #endregion

    #region Compile-time constants

    public const int Width = 640;
    public const int Height = 360;

    #endregion

    #region Internal objects

    WebCamTexture _webcam;
    ComputeBuffer _buffer;
    RenderTexture _mask;
    IWorker _worker;
    Material _material;

    #endregion

    #region MonoBehaviour implementation

    void Start()
    {
        _webcam = new WebCamTexture();
        _buffer = new ComputeBuffer(Width * Height * 3, sizeof(float));
        _worker = ModelLoader.Load(_model).CreateWorker();
        _material = GetComponent<Renderer>().material;

        _webcam.Play();
        _material.SetTexture("_SourceTex", _webcam);
    }

    void OnDisable()
    {
        _buffer?.Dispose();
        _worker?.Dispose();
        _buffer = null;
        _worker = null;
    }

    void OnDestroy()
    {
        if (_webcam != null) Destroy(_webcam);
        if (_mask != null) Destroy(_mask);
    }

    void Update()
    {
        // Check if the last task has been completed.
        if (_worker.scheduleProgress >= 1)
        {
            // Replace the overlay texture with the output.
            if (_mask != null) Destroy(_mask);
            var output = _worker.PeekOutput("float_segments");
            using (var segs = output.Reshape(new TensorShape(1, 23, 40, 1)))
            {
                _mask = segs.ToRenderTexture();
                _material.SetTexture("_MaskTex", _mask);
            }
        }

        // Image to tensor conversion
        _converter.SetTexture(0, "_Texture", _webcam);
        _converter.SetBuffer(0, "_Tensor", _buffer);
        _converter.SetInt("_Width", Width);
        _converter.SetInt("_Height", Height);
        _converter.Dispatch(0, Width / 8, Height / 8, 1);

        // New task scheduling
        using (var tensor = new Tensor(1, Height, Width, 3, _buffer))
        {
            var inputs = new Dictionary<string, Tensor> {{ "sub_2", tensor }};
            _worker.Execute(inputs);
            _worker.FlushSchedule();
        }
    }

    #endregion
}

} // namespace NNCam
