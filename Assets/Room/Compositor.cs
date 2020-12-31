using UnityEngine;

namespace NNCam {

sealed class Compositor : MonoBehaviour
{
    [SerializeField] InputStream _inputStream = null;
    [SerializeField] Texture2D _background = null;
    [SerializeField, Range(0.01f, 0.99f)] float _threshold = .5f;
    [SerializeField, HideInInspector] Shader _shader = null;

    Material _material;

    void OnDestroy()
    {
        if (_material != null) Destroy(_material);
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (_material == null) _material = new Material(_shader);
        _material.SetTexture("_Background", _background);
        _material.SetTexture("_CameraFeed", _inputStream.CameraTexture);
        _material.SetTexture("_Mask", _inputStream.MaskTexture);
        _material.SetFloat("_Threshold", _threshold);
        Graphics.Blit(null, destination, _material, 0);
    }
}

} // namespace NNCam
