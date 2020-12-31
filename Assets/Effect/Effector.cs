using UnityEngine;

namespace NNCam {

sealed class Effector : MonoBehaviour
{
    [SerializeField] InputStream _inputStream = null;
    [SerializeField, HideInInspector] Shader _shader = null;
    [SerializeField] float _feedbackLength = 3;
    [SerializeField] float _feedbackDecay = 1;
    [SerializeField] float _noiseFrequency = 1;
    [SerializeField] float _noiseSpeed = 1;
    [SerializeField] float _noiseAmount = 1;

    (RenderTexture rt1, RenderTexture rt2) _buffer;

    Material _material;

    RenderTexture NewBuffer()
      => new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGBHalf);

    Vector2 FeedbackParamsVector
      => new Vector3(_feedbackLength, _feedbackDecay / 100);

    Vector3 NoiseParamsVector
      => new Vector3(_noiseFrequency, _noiseSpeed, _noiseAmount / 1000);

    void Start()
    {
        _material = new Material(_shader);
        _buffer.rt1 = NewBuffer();
        _buffer.rt2 = NewBuffer();
    }

    void OnDestroy()
    {
        Destroy(_material);
        Destroy(_buffer.rt1);
        Destroy(_buffer.rt2);
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        // Effector shader
        _material.SetTexture("_FeedbackTex", _buffer.rt1);
        _material.SetTexture("_MaskTex", _inputStream.MaskTexture);
        _material.SetVector("_Feedback", FeedbackParamsVector);
        _material.SetVector("_Noise", NoiseParamsVector);
        Graphics.Blit(_inputStream.CameraTexture, _buffer.rt2, _material, 0);

        // Final blit
        Graphics.Blit(_buffer.rt2, destination);

        // Double buffer swapping
        _buffer = (_buffer.rt2, _buffer.rt1);
    }
}

} // namespace NNCam
