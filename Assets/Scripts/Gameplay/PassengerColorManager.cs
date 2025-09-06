using UnityEngine;

[DisallowMultipleComponent]
public class PassengerColorManager : MonoBehaviour
{
    [Header("Darkening")]
    [Range(0f, 1f)] public float unreachableColorMultiplier = 0.45f;
    [Range(0f, 1f)] public float reachableColorMultiplier = 1f;

    [Header("Smoothness (optional)")]
    [Range(0f, 1f)] public float reachableSmoothness = 0.5f;
    [Range(0f, 1f)] public float unreachableSmoothness = 0f;

    // Common shader color property names to try
    private static readonly string[] ColorProps = new[] { "_BaseColor", "_Color", "_TintColor" };
    private const string EMISSION_PROP = "_EmissionColor";
    private const string GLOSS_PROP = "_Glossiness";
    private const string SMOOTH_PROP = "_Smoothness";

    Renderer _renderer;
    MaterialPropertyBlock _mpb;

    // cached original color and which property we read it from
    Color _originalColor = Color.white;
    string _colorPropertyFound = null;

    void Awake()
    {
        _renderer = GetComponentInChildren<Renderer>();
        if (_renderer == null)
        {
            Debug.LogWarning($"[PassengerColorManager] No Renderer found on '{name}'. Visuals will be skipped.");
            return;
        }

        _mpb = new MaterialPropertyBlock();

        // read & cache original color (from shared material, do NOT modify that material)
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

            // fallback: try mat.color if nothing matched
            if (_colorPropertyFound == null)
            {
                try
                {
                    _originalColor = mat.color;
                    _colorPropertyFound = "_Color";
                }
                catch
                {
                    _originalColor = Color.white;
                    _colorPropertyFound = null;
                }
            }
        }
    }

    /// <summary>
    /// Apply visual changes for reachability while preserving original hue.
    /// Call from Passenger.SetReachable(...)
    /// </summary>
    public void ApplyReachability(bool reachable)
    {
        if (_renderer == null) return;

        // compute display color by darkening or keeping original
        float mult = reachable ? reachableColorMultiplier : unreachableColorMultiplier;
        Color display = _originalColor * mult;
        display.a = _originalColor.a; // preserve alpha

        _mpb.Clear();

        // Set color to the property we found (or common fallbacks)
        if (!string.IsNullOrEmpty(_colorPropertyFound))
        {
            _mpb.SetColor(_colorPropertyFound, display);
        }
        else
        {
            // fallback: set common names if shader ignores them, no harm done
            _mpb.SetColor("_Color", display);
            _mpb.SetColor("_BaseColor", display);
        }

        // emission: subtle highlight for reachable, only set if shader likely supports it
        Color emission = reachable ? new Color(0.06f, 0.06f, 0.06f, 1f) : Color.black;
        var mat = _renderer.sharedMaterial;
        if (mat != null && mat.HasProperty(EMISSION_PROP))
        {
            _mpb.SetColor(EMISSION_PROP, emission);
        }

        // smoothness (set commonly used smoothness/gloss props)
        float smooth = reachable ? reachableSmoothness : unreachableSmoothness;
        if (mat != null && mat.HasProperty(GLOSS_PROP))
            _mpb.SetFloat(GLOSS_PROP, smooth);
        if (mat != null && mat.HasProperty(SMOOTH_PROP))
            _mpb.SetFloat(SMOOTH_PROP, smooth);

        // apply to renderer (non-destructive)
        _renderer.SetPropertyBlock(_mpb);
    }

    // Allow external code to force re-reading the renderer's material color
    public void RefreshOriginalColor()
    {
        var mat = _renderer.sharedMaterial;
        _colorPropertyFound = null;
        _originalColor = Color.white;

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

            if (_colorPropertyFound == null)
            {
                _originalColor = mat.color;
                _colorPropertyFound = "_Color";
            }
        }
    }


    // ADD THIS METHOD
    public Color GetOriginalColor()
    {
        return _originalColor;
    }
}