using UnityEngine;
using UnityEngine.InputSystem;

namespace Track11.EuropeanWindowPackVol01
{
/// <summary>
/// Small adapter that adds center-screen Physics raycast interaction to an existing controller version 0.1.
/// It does not replace the movement controller and only forwards clicks to WindowHandlePopup and Popup3DButton.
/// </summary>
public sealed class WindowInteractionBridge : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera targetCamera;

    [Header("Interaction")]
    [SerializeField] private float interactDistance = 3.0f;
    [SerializeField] private LayerMask interactMask = ~0;
    [SerializeField] private bool useMouseClick = true;
    [SerializeField] private bool useKeyboardInteract = true;
    [SerializeField] private Key interactKey = Key.E;
    [SerializeField] private bool closePopupOnEscape = true;

    [Header("Cursor")]
    [SerializeField] private bool keepCursorLocked = true;
    [SerializeField] private bool keepCursorHidden = true;

    private Popup3DButton _hoveredButton;

    private void Reset()
    {
        targetCamera = Camera.main;
    }

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    private void OnDisable()
    {
        ClearHoveredButton();
    }

    private void Update()
    {
        if (targetCamera == null) return;

        if (closePopupOnEscape && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            WindowHandlePopup.CloseAll();

        UpdateInteraction();
    }

    private void LateUpdate()
    {
        if (!keepCursorLocked) return;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = !keepCursorHidden;
    }

    private void UpdateInteraction()
    {
        Ray ray = targetCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        bool hasHit = Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactMask, QueryTriggerInteraction.Ignore);

        Popup3DButton button = hasHit ? hit.collider.GetComponent<Popup3DButton>() : null;
        SetHoveredButton(button);

        if (!WasPrimaryInteractPressed() || !hasHit)
            return;

        if (button != null)
        {
            button.Trigger();
            return;
        }

        WindowHandlePopup popup = hit.collider.GetComponentInParent<WindowHandlePopup>();
        if (popup != null)
            popup.Toggle(hit);
    }

    private bool WasPrimaryInteractPressed()
    {
        bool mousePressed = useMouseClick && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        bool keyPressed = useKeyboardInteract && Keyboard.current != null && Keyboard.current[interactKey].wasPressedThisFrame;
        return mousePressed || keyPressed;
    }

    private void SetHoveredButton(Popup3DButton button)
    {
        if (_hoveredButton == button) return;

        ClearHoveredButton();

        _hoveredButton = button;
        if (_hoveredButton != null && _hoveredButton.Owner != null)
            _hoveredButton.Owner.SetHoveredButton(_hoveredButton);
    }

    private void ClearHoveredButton()
    {
        if (_hoveredButton == null) return;

        if (_hoveredButton.Owner != null)
            _hoveredButton.Owner.ClearHoveredButton(_hoveredButton);

        _hoveredButton = null;
    }
}
}
