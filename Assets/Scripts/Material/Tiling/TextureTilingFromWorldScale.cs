// ──────────────────────────────────────────────────────────────
//  TextureTilingFromWorldScale.cs
//  Corrigé & optimisé – avril 2025
// ──────────────────────────────────────────────────────────────
using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(Renderer))]
public class TextureTilingFromWorldScale : MonoBehaviour
{
    public enum ProjectionAxis { XY, XZ, ZY }

    [Header("Visuel souhaité")]
    [Min(0.0001f)] public Vector2 desiredVisualTiling = new(5, 1);
    [Tooltip("Facteur de correction si la texture paraît trop zoomée ou trop étirée.")]
    [Min(0.0001f)] public float referenceScale = 1f;

    [Header("Projection")]
    [Tooltip("Essaie de déduire automatiquement l’axe le plus pertinent.")]
    public bool autoDetectProjectionAxis = true;
    [Tooltip("Axe utilisé si la détection auto est désactivée.")]
    public ProjectionAxis manualAxis = ProjectionAxis.XY;

    // ──────────────────────────────────────────────────────────
    private Renderer _renderer;
    private readonly MaterialPropertyBlock _mpb = new();

#if UNITY_EDITOR
    private Material _previewMaterial;        // instance uniquement en mode éditeur
#endif
    private Vector3 _cachedScale;             // pour ne recalculer qu’en cas de changement
    private Vector2 _cachedDesiredVisualTiling;

    // ──────────────────────────────────────────────────────────
    #region Unity messages
    void OnEnable()
    {
        _renderer = GetComponent<Renderer>();
        SetupPreviewMaterialInEditor();
        ForceUpdateTiling();
    }

#if UNITY_EDITOR
    void OnValidate() => ForceUpdateTiling();
#endif

    void Update()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            if (transform.hasChanged || desiredVisualTiling != _cachedDesiredVisualTiling)
                ForceUpdateTiling();
            return;
        }
#endif
        if (transform.lossyScale != _cachedScale)
            ForceUpdateTiling();
    }

#if UNITY_EDITOR
    void OnDisable()  => CleanupPreviewMaterial();
    void OnDestroy()  => CleanupPreviewMaterial();
#endif
    #endregion
    // ──────────────────────────────────────────────────────────
    #region Public helpers
    /// <summary>
    /// Force l’actualisation (appelé automatiquement si besoin).
    /// </summary>
    public void ForceUpdateTiling()
    {
        if (!_renderer) return;

        _cachedScale                 = transform.lossyScale;
        _cachedDesiredVisualTiling   = desiredVisualTiling;

        Vector2 corrected = ComputeCorrectedTiling();

#if UNITY_EDITOR
        if (!Application.isPlaying)                    // mode scène
        {
            if (_renderer.sharedMaterial)
                _renderer.sharedMaterial.mainTextureScale = corrected;
            return;
        }
#endif
        // Mode Play : override via MaterialPropertyBlock (pas de duplication de matériaux)
        _renderer.GetPropertyBlock(_mpb);
        _mpb.SetVector("_MainTex_ST", new Vector4(corrected.x, corrected.y, 0, 0));
        _renderer.SetPropertyBlock(_mpb);
    }

    /// <summary>
    /// “Photographie” le tiling courant pour le stocker comme visuel de référence.
    /// </summary>
    public void CaptureCurrentTiling()
    {
        if (!_renderer) return;

        Vector2 current;
#if UNITY_EDITOR
        current = _renderer.sharedMaterial
            ? _renderer.sharedMaterial.mainTextureScale
            : Vector2.one;
#else
        _renderer.GetPropertyBlock(_mpb);
        Vector4 st = _mpb.GetVector("_MainTex_ST");     // (x,y) = tiling
        current = new(st.x == 0 ? 1 : st.x, st.y == 0 ? 1 : st.y);
#endif
        Vector3 s   = transform.lossyScale;
        var axis    = autoDetectProjectionAxis ? DetectAxis() : manualAxis;

        desiredVisualTiling = axis switch
        {
            ProjectionAxis.XY => new Vector2(current.x * s.x * referenceScale,
                                             current.y * s.y * referenceScale),
            ProjectionAxis.XZ => new Vector2(current.x * s.x * referenceScale,
                                             current.y * s.z * referenceScale),
            _/*ZY*/          => new Vector2(current.x * s.z * referenceScale,
                                             current.y * s.y * referenceScale)
        };
    }
    #endregion
    // ──────────────────────────────────────────────────────────
    #region Internals
    Vector2 ComputeCorrectedTiling()
    {
        Vector3 s   = transform.lossyScale;
        var axis    = autoDetectProjectionAxis ? DetectAxis() : manualAxis;
        const float EPS = 0.0001f;

        return axis switch
        {
            ProjectionAxis.XY => new Vector2(
                desiredVisualTiling.x / Mathf.Max(s.x * referenceScale, EPS),
                desiredVisualTiling.y / Mathf.Max(s.y * referenceScale, EPS)),

            ProjectionAxis.XZ => new Vector2(
                desiredVisualTiling.x / Mathf.Max(s.x * referenceScale, EPS),
                desiredVisualTiling.y / Mathf.Max(s.z * referenceScale, EPS)),

            _/*ZY*/ => new Vector2(
                desiredVisualTiling.x / Mathf.Max(s.z * referenceScale, EPS),
                desiredVisualTiling.y / Mathf.Max(s.y * referenceScale, EPS))
        };
    }

    public ProjectionAxis DetectAxis()
    {
        // On choisit la surface projetée la plus grande
        Vector3 s = transform.lossyScale;
        float xz  = s.x * s.z;
        float xy  = s.x * s.y;
        float zy  = s.z * s.y;

        if (xz >= xy && xz >= zy) return ProjectionAxis.XZ;
        return xy >= zy ? ProjectionAxis.XY : ProjectionAxis.ZY;
    }
    #endregion
    // ──────────────────────────────────────────────────────────
    #region Editor-only utilities
#if UNITY_EDITOR
    void SetupPreviewMaterialInEditor()
    {
        if (Application.isPlaying || !_renderer || !_renderer.sharedMaterial || _previewMaterial) return;

        _previewMaterial = new Material(_renderer.sharedMaterial)
        {
            name = _renderer.sharedMaterial.name + " (Preview)"
        };
        _renderer.sharedMaterial = _previewMaterial;
    }

    void CleanupPreviewMaterial()
    {
        if (!_previewMaterial) return;

        if (!Application.isPlaying)
            DestroyImmediate(_previewMaterial);
        else
            Destroy(_previewMaterial);

        _previewMaterial = null;
    }
#endif
    #endregion
}
