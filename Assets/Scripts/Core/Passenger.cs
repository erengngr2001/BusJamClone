using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PassengerColorManager))]
public class Passenger : MonoBehaviour
{
    // inside Passenger class (add these fields near the top)
    public Pipe originPipe = null; // optional origin pipe reference

    private bool _interactable = true;
    private Collider _collider;

    public bool IsColorHidden { get; set; } = false;
    public Material OriginalMaterial { get; private set; }
    private Material hiddenMaterial;
    private Renderer bodyRenderer;

    public bool IsMoving { get; set; } = false;
    public bool isReachable = true;
    public Vector2Int gridCoord = new Vector2Int(-1, -1);
    //public System.Action<Passenger> onPassengerClicked;

    // last computed path (grid coordinates start..goal). Null if unreachable or not computed.
    public List<Vector2Int> currentPath = null;

    [Header("Events")]
    // called by GridSpawner when this passenger is clicked
    public Action<Passenger> onClickedByPlayer;

    // reference to color manager (cached)
    private PassengerColorManager colorManager;
    // ADD THIS PROPERTY
    public Color PassengerColor => colorManager != null ? colorManager.GetOriginalColor() : Color.white;

    // Input Actions (created in code for simplicity)
    private InputAction pointerPosAction;
    private InputAction clickAction;

    private void Awake()
    {
        _collider = GetComponent<Collider>();
        if (_collider == null)
        {
            _collider = gameObject.AddComponent<CapsuleCollider>();
        }

        colorManager = GetComponent<PassengerColorManager>();
        if (colorManager == null)
            colorManager = gameObject.AddComponent<PassengerColorManager>();

        Transform bodyTransform = transform.Find("Body");
        if (bodyTransform != null)
        {
            bodyRenderer = bodyTransform.GetComponent<Renderer>();
        }
        else
        {
            // Fallback to any renderer in children if "Body" isn't found.
            bodyRenderer = GetComponentInChildren<Renderer>();
        }
        if (bodyRenderer == null)
        {
            Debug.LogError("[Passenger] No renderer found for material swapping!");
        }

        // ensure initial visuals match initial isReachable
        colorManager.ApplyReachability(isReachable, IsColorHidden);

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

    }

    private void OnEnable()
    {
        // When a press happens, we'll check if it hit this passenger
        clickAction.performed += OnClickPerformed;
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
    // and raycast if the raycast hit belongs to this GameObject, we handle it.
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
        if (!_interactable || IsMoving) return;
        onClickedByPlayer?.Invoke(this);
        Collider c = GetComponent<Collider>();
        c.enabled = false; // disable further clicks
    }

    public void InitializeAsHidden(Material originalMat, Material hiddenMat)
    {
        IsColorHidden = true;
        OriginalMaterial = originalMat;
        hiddenMaterial = hiddenMat;

        // Apply the initial hidden material since it will be unreachable at spawn.
        if (bodyRenderer != null && hiddenMaterial != null)
        {
            bodyRenderer.material = hiddenMaterial;
        }
    }

    public void SetInteractable(bool v)
    {
        _interactable = v;
        _collider.enabled = v;
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

        // If this passenger had a hidden material and is now becoming reachable
        if (IsColorHidden && isReachable)
        {
            if (bodyRenderer != null && OriginalMaterial != null)
            {
                // assign original material back to the renderer
                bodyRenderer.material = OriginalMaterial;
            }

            IsColorHidden = false;

            // refresh color manager to read the material and apply visuals
            if (colorManager != null)
            {
                colorManager.RefreshOriginalColor();
                colorManager.ApplyReachability(isReachable, false);
            }
        }
        else
        {
            // normal path: let color manager handle appearance (still hidden -> manager will early-return)
            if (colorManager != null)
                colorManager.ApplyReachability(isReachable, this.IsColorHidden);
        }
    }

    public void SetReachableImmediate(bool isReachable)
    {
        this.isReachable = isReachable;

        // mirror runtime behavior from SetReachable
        if (IsColorHidden && isReachable)
        {
            if (bodyRenderer != null && OriginalMaterial != null)
            {
                bodyRenderer.material = OriginalMaterial;
            }
            IsColorHidden = false;
            if (colorManager != null)
            {
                colorManager.RefreshOriginalColor();
                colorManager.ApplyReachability(isReachable, false);
            }
        }
        else
        {
            if (colorManager != null)
                colorManager.ApplyReachability(isReachable, this.IsColorHidden);
        }
    }


}