using UnityEngine;

namespace NNCam {

public sealed class Effector : MonoBehaviour
{
    #region Editor only attributes

    [SerializeField] WebcamInput _input = null;
    [SerializeField] ResourceSet _resources = null;
    [SerializeField] Shader _shader = null;

    #endregion

    #region Editable properties

    [SerializeField] float _feedbackLength = 3;
    [SerializeField] float _feedbackDecay = 1;
    [SerializeField] float _noiseFrequency = 1;
    [SerializeField] float _noiseSpeed = 1;
    [SerializeField] float _noiseAmount = 1;

    public float FeedbackLength
      { get => _feedbackLength; set => _feedbackLength = value; }

    public float FeedbackDecay
      { get => _feedbackDecay; set => _feedbackDecay = value; }

    public float NoiseFrequency
      { get => _noiseFrequency; set => _noiseFrequency = value; }

    public float NoiseSpeed
      { get => _noiseSpeed; set => _noiseSpeed = value; }

    public float NoiseAmount
      { get => _noiseAmount; set => _noiseAmount = value; }

    #endregion

    #region Private members

    RenderTexture NewBuffer()
      => new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGBHalf);

    Vector2 FeedbackParamsVector
      => new Vector3(_feedbackLength, _feedbackDecay / 100);

    Vector3 NoiseParamsVector
      => new Vector3(_noiseFrequency, _noiseSpeed, _noiseAmount / 1000);

    SegmentationFilter _filter;
    (RenderTexture rt1, RenderTexture rt2) _buffer;
    Material _material;

    #endregion

    #region MonoBehaviour implementation

    void Start()
    {
        _filter = new SegmentationFilter(_resources);
        _material = new Material(_shader);
        _buffer.rt1 = NewBuffer();
        _buffer.rt2 = NewBuffer();
    }

    void OnDestroy()
    {
        _filter.Dispose();
        Destroy(_material);
        Destroy(_buffer.rt1);
        Destroy(_buffer.rt2);
    }

    void Update()
    {
        // Segmentation filter
        _filter.ProcessImage(_input.Texture);

        // Effector shader
        _material.SetTexture("_FeedbackTex", _buffer.rt1);
        _material.SetTexture("_MaskTex", _filter.MaskTexture);
        _material.SetVector("_Feedback", FeedbackParamsVector);
        _material.SetVector("_Noise", NoiseParamsVector);
        Graphics.Blit(_input.Texture, _buffer.rt2, _material, 0);

        // Double buffer swapping
        _buffer = (_buffer.rt2, _buffer.rt1);
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
      => Graphics.Blit(_buffer.rt1, destination);

    #endregion
}

} // namespace NNCam
