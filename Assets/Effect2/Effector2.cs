using UnityEngine;

namespace NNCam {

//
// BodyPix effect example 2: Pixel sorting with person segmentation
//
public sealed class Effector2 : MonoBehaviour
{
    #region Editor only attributes

    [SerializeField] InputStream _inputStream = null;
    [SerializeField, Range(0, 1)] float _maskThreshold = 0.3f;
    [SerializeField, Range(0, 1)] float _lowerThreshold = 0.3f;
    [SerializeField, Range(0, 1)] float _upperThreshold = 0.5f;
    [SerializeField, HideInInspector] ComputeShader _compute = null;

    #endregion

    #region Private members

    RenderTexture _tempRT;

    Vector3 ThresholdVector
      => new Vector3(_maskThreshold, _lowerThreshold, _upperThreshold);

    #endregion

    #region MonoBehaviour implementation

    void Start()
    {
        _tempRT = new RenderTexture(1920, 1080, 0);
        _tempRT.enableRandomWrite = true;
        _tempRT.Create();
    }

    void OnDestroy()
    {
        if (_tempRT != null) Destroy(_tempRT);
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        _compute.SetTexture(0, "_MainTex", _inputStream.CameraTexture);
        _compute.SetTexture(0, "_MaskTex", _inputStream.MaskTexture);
        _compute.SetTexture(0, "_OutTex", _tempRT);
        _compute.SetVector("_Threshold", ThresholdVector);
        _compute.SetInt("_Width", 1920);
        _compute.Dispatch(0, 1080 / 24, 1, 1);
        Graphics.Blit(_tempRT, destination);
    }

    #endregion
}

} // namespace NNCam
