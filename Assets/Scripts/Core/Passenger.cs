using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PassengerColorManager))]
public class Passenger : MonoBehaviour
{
    //[Header("Passenger Info")]
    //public Material material;

    public bool isReachable = true;
    public Vector2Int gridCoord = new Vector2Int(-1,-1);
    //public System.Action<Passenger> onPassengerClicked;

    // last computed path (grid coordinates start..goal). Null if unreachable or not computed.
    public List<Vector2Int> currentPath = null;

    [Header("Events")]
    // called by GridSpawner when this passenger is clicked
    public Action<Passenger> onClickedByPlayer;

    // reference to color manager (cached)
    private PassengerColorManager colorManager;

    // Input Actions (created in code for simplicity)
    private InputAction pointerPosAction;
    private InputAction clickAction;

    private void Awake()
    {
        if (this.GetComponent<Collider>() == null)
        {
            var box = gameObject.AddComponent<CapsuleCollider>();
        }

        colorManager = GetComponent<PassengerColorManager>();
        if (colorManager == null)
            colorManager = gameObject.AddComponent<PassengerColorManager>();

        // ensure initial visuals match initial isReachable
        colorManager.ApplyReachability(isReachable);

        // Create pointer position action (Vector2 screen pos)
        pointerPosAction = new InputAction(
            name: "PointerPosition",
            type: InputActionType.Value,
            binding: "<Pointer>/position"
        );

        // Create click/press action (button)
        clickAction = new InputAction(
            name: "PointerPress",
            type: InputActionType.Button,
            binding: "<Pointer>/press"
        );

        // When a press happens, we'll check if it hit this passenger
        clickAction.performed += OnClickPerformed;
    }

    private void OnEnable()
    {
        pointerPosAction.Enable();
        clickAction.Enable();
    }

    private void OnDisable()
    {
        clickAction.performed -= OnClickPerformed;
        clickAction.Disable();
        pointerPosAction.Disable();
    }

    private void OnDestroy()
    {
        // Clean up
        pointerPosAction.Dispose();
        clickAction.Dispose();
    }

    // Called when any pointer press occurs. We read the pointer position
    // and raycast — if the raycast hit belongs to this GameObject, we handle it.
    private void OnClickPerformed(InputAction.CallbackContext ctx)
    {
        // Read screen position from pointer action
        Vector2 screenPos = pointerPosAction.ReadValue<Vector2>();

        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("[Passenger] Camera.main not found. Tag your camera as MainCamera.");
            return;
        }

        Ray ray = cam.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
        {
            // If the hit object is this passenger (or a child), treat as click
            if (hit.collider != null && (hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform)))
            {
                HandleClick();
            }
        }
    }

    private void HandleClick()
    {
        onClickedByPlayer?.Invoke(this);

        if (isReachable)
        {
            Debug.Log($"[Passenger] Clicked reachable passenger at ({gridCoord.x},{gridCoord.y}) — {gameObject.name}");
            // Add any further behavior here (events, notify manager, play animation, etc.)
        }
        else
        {
            Debug.Log($"[Passenger] Clicked passenger at ({gridCoord.x},{gridCoord.y}) but NOT reachable.");
        }
    }

    public void InitializeGridCoord(int x, int y)
    {
        gridCoord.x = x;
        gridCoord.y = y;
    }

    // Called by GridSpawner to store the path (may be null - future check)
    public void SetPath(List<Vector2Int> path)
    {
        currentPath = path;
    }

    public void SetReachable(bool isReachable)
    {
        if (this.isReachable == isReachable) return;
        this.isReachable = isReachable;

        // delegate visuals to the manager
        if (colorManager != null)
            colorManager.ApplyReachability(isReachable);
    }

    // Optional: immediate visual set without state-change debounce
    public void SetReachableImmediate(bool isReachable)
    {
        this.isReachable = isReachable;
        if (colorManager != null) colorManager.ApplyReachability(isReachable);
    }

}
