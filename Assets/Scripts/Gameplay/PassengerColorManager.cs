using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public class PassengerColorManager : MonoBehaviour
{
    [Header("Darkening")]
    [Tooltip("Multiplier applied to original color when unreachable (0..1).")]
    [Range(0f, 1f)] public float unreachableColorMultiplier = 0.45f;
    [Tooltip("Multiplier applied to original color when reachable (usually 1).")]
    [Range(0f, 1f)] public float reachableColorMultiplier = 1f;

    [Header("Smoothness (optional)")]
    [Range(0f, 1f)] public float reachableSmoothness = 0.5f;
    [Range(0f, 1f)] public float unreachableSmoothness = 0f;

    [Header("Outline fallback")]
    public Color outlineColor = Color.white;
    [Range(1.00f, 1.12f)] public float outlineScale = 1.03f;

    // Common shader color property names (we try these in order)
    private static readonly string[] ColorProps = new[] { "_BaseColor", "_Color", "_TintColor" };
    private const string EMISSION_PROP = "_EmissionColor";
    private const string GLOSS_PROP = "_Glossiness";
    private const string SMOOTH_PROP = "_Smoothness";
    private const string OUTLINE_WIDTH_PROP = "_OutlineWidth";
    private const string OUTLINE_COLOR_PROP = "_OutlineColor";

    Renderer _renderer;
    MaterialPropertyBlock _mpb;

    // original color and which property we read it from
    Color _originalColor = Color.white;
    string _colorPropertyFound = null;

    // outline fallback
    GameObject _outlineGO;
    Renderer _outlineRenderer;
    static Material _sharedOutlineMaterial;

    void Awake()
    {
        _renderer = GetComponentInChildren<Renderer>();
        if (_renderer == null)
        {
            Debug.LogWarning($"[PassengerColorManager] No Renderer found on '{name}'. Visuals will be skipped.");
            return;
        }

        _mpb = new MaterialPropertyBlock();

        // Try to read the original color from the shared material (no instancing)
        var mat = _renderer.sharedMaterial;
        if (mat != null)
        {
            foreach (var prop in ColorProps)
            {
                if (mat.HasProperty(prop))
                {
                    _originalColor = mat.GetColor(prop);
                    _colorPropertyFound = prop;
                    break;
                }
            }

            // fallback: if no color prop found, try material.color (may still be present)
            if (_colorPropertyFound == null)
            {
                try
                {
                    _originalColor = mat.color;
                    _colorPropertyFound = "_Color"; // use _Color as fallback name
                }
                catch
                {
                    _originalColor = Color.white;
                }
            }
        }

        // ensure shared outline material exists (for mesh-copy fallback)
        EnsureSharedOutlineMaterial();
    }

    /// <summary>
    /// Apply visual changes for reachability. Preserves original hue by darkening using multiplier.
    /// Call from Passenger.SetReachable(...)
    /// </summary>
    public void ApplyReachability(bool reachable)
    {
        if (_renderer == null) return;

        // compute final color: originalColor * multiplier
        float mult = reachable ? reachableColorMultiplier : unreachableColorMultiplier;
        Color display = _originalColor * mult;

        // clear and set property block
        _mpb.Clear();

        // If we found a property name earlier, use it; otherwise use common names (one may work)
        if (!string.IsNullOrEmpty(_colorPropertyFound))
        {
            _mpb.SetColor(_colorPropertyFound, display);
        }
        else
        {
            // try safe defaults
            _mpb.SetColor("_Color", display);
            _mpb.SetColor("_BaseColor", display);
        }

        // emission fallback to make reachable pop a bit (only if shader supports)
        Color emission = reachable ? new Color(0.08f, 0.08f, 0.08f) : Color.black;
        _mpb.SetColor(EMISSION_PROP, emission);

        // smoothness (optional): set both common properties to maximize compatibility
        float smooth = reachable ? reachableSmoothness : unreachableSmoothness;
        _mpb.SetFloat(GLOSS_PROP, smooth);
        _mpb.SetFloat(SMOOTH_PROP, smooth);

        // apply property block to renderer
        _renderer.SetPropertyBlock(_mpb);

        // Outline: try shader outline props first via property block (works only if shader uses them)
        bool outlineSet = false;
        var mat = _renderer.sharedMaterial;
        if (mat != null)
        {
            if (mat.HasProperty(OUTLINE_WIDTH_PROP) || mat.HasProperty(OUTLINE_COLOR_PROP))
            {
                // use a separate mpb so we don't lose color settings (we can reuse _mpb but this is fine)
                var p = new MaterialPropertyBlock();
                _renderer.GetPropertyBlock(p);
                p.SetFloat(OUTLINE_WIDTH_PROP, reachable ? 1f : 0f);
                p.SetColor(OUTLINE_COLOR_PROP, outlineColor);
                _renderer.SetPropertyBlock(p);
                outlineSet = true;
            }
        }

        // Fallback: mesh-copy outline child (created once)
        if (!outlineSet)
        {
            EnsureOutlineChildExists();
            if (_outlineGO != null)
            {
                _outlineGO.SetActive(reachable);
                if (_outlineRenderer != null && _sharedOutlineMaterial != null)
                {
                    // update shared outline material color (cheap)
                    _sharedOutlineMaterial.color = outlineColor;
                }
            }
        }
    }

    void EnsureSharedOutlineMaterial()
    {
        if (_sharedOutlineMaterial != null) return;

        Shader s = Shader.Find("Unlit/Color") ?? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Standard");
        if (s == null) return;
        _sharedOutlineMaterial = new Material(s) { name = "PassengerOutlineShared" };
        _sharedOutlineMaterial.renderQueue = 3100;
        _sharedOutlineMaterial.SetInt("_ZWrite", 0);
    }

    void EnsureOutlineChildExists()
    {
        if (_outlineGO != null) return;
        if (_renderer == null) return;

        Mesh mesh = null;
        var mf = _renderer.GetComponent<MeshFilter>();
        if (mf != null) mesh = mf.sharedMesh;
        else
        {
            var smr = _renderer.GetComponent<SkinnedMeshRenderer>();
            if (smr != null) mesh = smr.sharedMesh;
        }
        if (mesh == null) return;

        _outlineGO = new GameObject("Outline");
        _outlineGO.transform.SetParent(_renderer.transform, false);
        _outlineGO.transform.localPosition = Vector3.zero;
        _outlineGO.transform.localRotation = Quaternion.identity;
        _outlineGO.transform.localScale = Vector3.one * outlineScale;

        var outFilter = _outlineGO.AddComponent<MeshFilter>();
        outFilter.sharedMesh = mesh;
        _outlineRenderer = _outlineGO.AddComponent<MeshRenderer>();
        _outlineRenderer.sharedMaterial = _sharedOutlineMaterial;
        _outlineRenderer.shadowCastingMode = ShadowCastingMode.Off;
        _outlineRenderer.receiveShadows = false;
        _outlineGO.SetActive(false);
    }

    void OnDestroy()
    {
#if UNITY_EDITOR
        if (_sharedOutlineMaterial != null && Application.isEditor && !Application.isPlaying)
        {
            // do not destroy shared material in editor play mode cleanup
        }
#endif
    }
}
