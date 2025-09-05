using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Vehicle : MonoBehaviour
{
    public int capacity = 3;
    public Color color;
    public bool isFull => _passengersOnBoard >= capacity;

    private int _passengersOnBoard = 0;
    private List<GameObject> _passengerSeats = new List<GameObject>();
    private Renderer _vehicleRenderer;

    public Action<Vehicle> onVehicleFull;

    private void Awake()
    {
        _vehicleRenderer = GetComponentInChildren<Renderer>();

        for (int i = 0; i < capacity; i++)
        {
            Transform seat = transform.Find($"Passenger_{i}");
            if (seat != null)
            {
                _passengerSeats.Add(seat.gameObject);
                seat.gameObject.SetActive(false);
            }
        }
    }

    public void Initialize(Color vehicleColor, int vehicleCapacity)
    {
        color = vehicleColor;
        capacity = vehicleCapacity;
        ApplyColorToRenderer();
    }

    private void ApplyColorToRenderer()
    {
        if (_vehicleRenderer != null)
        {
            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            mpb.SetColor("_BaseColor", color);
            mpb.SetColor("_Color", color);
            _vehicleRenderer.SetPropertyBlock(mpb);
        }
    }

    public bool TryAddPassenger(GameObject passenger)
    {
        if (isFull) return false;

        if (_passengersOnBoard < _passengerSeats.Count)
        {
            _passengerSeats[_passengersOnBoard].SetActive(true);
        }

        _passengersOnBoard++;

        if (isFull)
        {
            onVehicleFull?.Invoke(this);
            Debug.Log($"{name} is now full!");
        }

        return true;
    }

    public void ResetForReuse()
    {
        _passengersOnBoard = 0;
        foreach (var seat in _passengerSeats)
        {
            seat.SetActive(false);
        }
    }

    public void Depart()
    {
        StartCoroutine(DepartRoutine());
    }

    private IEnumerator DepartRoutine()
    {
        // Prevent interaction while departing
        if (GetComponent<Collider>() != null)
        {
            GetComponent<Collider>().enabled = false;
        }

        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + transform.forward * 40f; // Move further off-screen
        float duration = 1.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(startPos, endPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // The vehicle is now responsible for its own destruction after notifying the manager.
        VehicleManager.Instance.NotifyBusDeparted(this);
        Destroy(gameObject);
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

