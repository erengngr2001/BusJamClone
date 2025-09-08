// Assets/Scripts/Runtime/LightingInitializer.cs
using UnityEngine;

[DisallowMultipleComponent]
public class LightingInitializer : MonoBehaviour
{
    [Header("Desired Gameplay Lighting (fallback)")]
    public float directionalIntensity = 1.35f;
    public Color ambientLight = Color.white;
    public Material skyboxMaterial; // assign the expected gameplay skybox here (optional)

    // optional: name of the directional light to find (if null we'll find any directional light)
    public string directionalLightName = "";

    void Start()
    {
        // Set ambient
        RenderSettings.ambientLight = ambientLight;

        // Set skybox if provided
        if (skyboxMaterial != null)
            RenderSettings.skybox = skyboxMaterial;

        // Find directional light in scene — try by name first, else any Directional Light
        Light dir = null;
        if (!string.IsNullOrEmpty(directionalLightName))
        {
            var go = GameObject.Find(directionalLightName);
            if (go != null) dir = go.GetComponent<Light>();
        }

        if (dir == null)
        {
            var all = FindObjectsOfType<Light>();
            foreach (var l in all)
            {
                if (l != null && l.type == LightType.Directional)
                {
                    dir = l;
                    break;
                }
            }
        }

        if (dir != null)
        {
            dir.intensity = directionalIntensity;
            dir.enabled = true;
        }
        else
        {
            Debug.LogWarning("[LightingInitializer] No directional light found in scene to set intensity on.");
        }
    }
}
