using UnityEngine;

namespace NNCam {

sealed class Effector : MonoBehaviour
{
    [SerializeField] InputStream _inputStream = null;
    [SerializeField, Range(0.01f, 0.99f)] float _threshold = .5f;
    [SerializeField, HideInInspector] Shader _shader = null;

    (RenderTexture rt1, RenderTexture rt2) _buffer;

    Material _material;

    void Start()
    {
        _material = new Material(_shader);
        _buffer.rt1 = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGBHalf);
        _buffer.rt2 = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGBHalf);
    }

    void OnDestroy()
    {
        Destroy(_material);
        Destroy(_buffer.rt1);
        Destroy(_buffer.rt2);
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        _material.SetTexture("_Feedback", _buffer.rt1);
        _material.SetTexture("_CameraFeed", _inputStream.CameraTexture);
        _material.SetTexture("_Mask", _inputStream.MaskTexture);
        _material.SetFloat("_Threshold", _threshold);
        Graphics.Blit(null, _buffer.rt2, _material, 0);
        Graphics.Blit(_buffer.rt2, destination);
        _buffer = (_buffer.rt2, _buffer.rt1);
    }
}

} // namespace NNCam
