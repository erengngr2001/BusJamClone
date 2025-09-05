using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Vehicle : MonoBehaviour
{
    //public bool isFull = false;
    public int capacity = 3;
    public Color color;
    public bool isFull => passengers.Count >= capacity;

    private List<GameObject> passengers = new List<GameObject>();
    private Renderer vehicleRenderer;

    public Action<Vehicle> onVehicleFull;

    private void Awake()
    {
        vehicleRenderer = GetComponentInChildren<Renderer>();
        //Debug.Log(vehicleRenderer.name);
    }

    public void Initialize(Color vehicleColor, int vehicleCapacity)
    {
        color = vehicleColor;
        capacity = vehicleCapacity;
        ApplyColorToRenderer();
    }

    private void ApplyColorToRenderer()
    {
        if (vehicleRenderer != null)
        {
            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            //if (vehicleRenderer.sharedMaterial != null && vehicleRenderer.sharedMaterial.HasProperty("_BaseColor"))
            //    mpb.SetColor("_BaseColor", color);
            mpb.SetColor("_BaseColor", color); // URP/HDRP
            mpb.SetColor("_Color", color);     // Built-in fallback
            vehicleRenderer.SetPropertyBlock(mpb);
        }
    }

    public bool TryAddPassenger(GameObject passenger)
    {
        if (isFull) return false;

        passengers.Add(passenger);

        if (isFull)
        {
            onVehicleFull?.Invoke(this);
            Debug.Log($"{name} is now full!");
        }

        return true;
    }

    public void ResetForReuse()
    {
        passengers.Clear();
        // Reset any visual state you may have (animations, lights, etc.)
    }

    public void SetTransform(Vector3 position, Quaternion rotation)
    {
        transform.position = position;
        transform.rotation = rotation;
    }

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }

}
