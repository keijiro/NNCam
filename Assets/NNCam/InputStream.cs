using UnityEngine;

namespace NNCam {

sealed class InputStream : MonoBehaviour
{
    #region Editable attributes

    [SerializeField] ResourceSet _resources = null;

    #endregion

    #region Internal objects

    WebCamTexture _webcam;
    RenderTexture _buffer;
    SegmentationFilter _filter;

    #endregion

    #region Public properties

    public Texture CameraTexture => _buffer;
    public Texture MaskTexture => _filter.MaskTexture;

    #endregion

    #region MonoBehaviour implementation

    void Start()
    {
        _webcam = new WebCamTexture();
        _buffer = new RenderTexture(1920, 1080, 0);
        _filter = new SegmentationFilter(_resources);

        _webcam.Play();
    }

    void OnDestroy()
    {
        Destroy(_webcam);
        Destroy(_buffer);
        _filter.Dispose();
    }

    void Update()
    {
        if (!_webcam.didUpdateThisFrame) return;

        var vflip = _webcam.videoVerticallyMirrored;
        var scale = new Vector2(1, vflip ? -1 : 1);
        var offset = new Vector2(0, vflip ? 1 : 0);
        Graphics.Blit(_webcam, _buffer, scale, offset);

        _filter.ProcessImage(_buffer);
    }

    #endregion
}

} // namespace NNCam
