using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

sealed class CameraController : MonoBehaviour
{
    [SerializeField] UnityEngine.UI.RawImage _display = null;

    [SerializeField, HideInInspector] Unity.Barracuda.NNModel _model = null;
    [SerializeField, HideInInspector] ComputeShader _converter = null;
    [SerializeField, HideInInspector] Mesh _mesh = null;
    [SerializeField, HideInInspector] Shader _shader;

    const int Size = Detector.IMAGE_SIZE;

    WebCamTexture _webcam;
    RenderTexture _cropped;
    ComputeBuffer _buffer;
    Detector _detector;
    Material _material;

    void Start()
    {
        _webcam = new WebCamTexture();
        _webcam.Play();

        _display.texture = _cropped = new RenderTexture(Size, Size, 0);
        _buffer = new ComputeBuffer(Size * Size * 3, sizeof(float));

        _detector = new Detector(_model);
        _material = new Material(_shader);
    }

    void OnDisable()
    {
        _buffer?.Dispose();
        _buffer = null;

        _detector?.Dispose();
        _detector = null;
    }

    void OnDestroy()
    {
        if (_webcam != null) Destroy(_webcam);
        if (_cropped != null) Destroy(_cropped);
        if (_material != null) Destroy(_material);
    }

    void Update()
    {
        // Retrieve the last results and draw the bounding boxes.
        _detector.RetrieveResults(DrawBoundingBoxes);

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

        // Detection start
        _detector.StartDetection(_buffer);
    }

    void DrawBoundingBoxes(IList<BoundingBox> boxes)
    {
        foreach (var box in boxes)
        {
            var dim = box.Dimensions;

            var t = math.float3(dim.X, dim.Y, 0);
            var r = quaternion.identity;
            var s = math.float3(dim.Width, dim.Height, 1);

            t.xy = math.float2(1, -1) * ((t.xy + s.xy / 2) / Size - 0.5f);
            s.xy /= Size;

            Graphics.DrawMesh(_mesh, float4x4.TRS(t, r, s), _material, 0);
        }
    }
}
