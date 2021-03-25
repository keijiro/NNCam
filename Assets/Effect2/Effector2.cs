using UnityEngine;

namespace NNCam {

//
// BodyPix effect example 2: Pixel sorting with person segmentation
//
public sealed class Effector2 : MonoBehaviour
{
    #region Editor only attributes

    [SerializeField] WebcamInput _input = null;
    [SerializeField, Range(0, 1)] float _maskLower = 0.3f;
    [SerializeField, Range(0, 1)] float _maskUpper = 0.5f;
    [SerializeField, Range(0, 1)] float _lumaLower = 0.3f;
    [SerializeField, Range(0, 1)] float _lumaUpper = 0.5f;
    [SerializeField] ResourceSet _resources = null;
    [SerializeField] ComputeShader _compute = null;

    #endregion

    #region Private members

    SegmentationFilter _filter;
    RenderTexture _tempRT;

    Vector4 ThresholdVector
      => new Vector4(_maskLower, _lumaLower, _maskUpper, _lumaUpper);

    #endregion

    #region MonoBehaviour implementation

    void OnValidate()
    {
        _maskUpper = Mathf.Max(_maskLower, _maskUpper);
        _lumaUpper = Mathf.Max(_lumaLower, _lumaUpper);
    }

    void Start()
    {
        _filter = new SegmentationFilter(_resources);

        _tempRT = new RenderTexture(1920, 1080, 0);
        _tempRT.enableRandomWrite = true;
        _tempRT.Create();
    }

    void OnDestroy()
    {
        _filter.Dispose();
        Destroy(_tempRT);
    }

    void Update()
      => _filter.ProcessImage(_input.Texture);

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        _compute.SetTexture(0, "_MainTex", _input.Texture);
        _compute.SetTexture(0, "_MaskTex", _filter.MaskTexture);
        _compute.SetTexture(0, "_OutTex", _tempRT);
        _compute.SetVector("_Threshold", ThresholdVector);
        _compute.SetInt("_Width", 1920);
        _compute.Dispatch(0, 1080 / 24, 1, 1);
        Graphics.Blit(_tempRT, destination);
    }

    #endregion
}

} // namespace NNCam
