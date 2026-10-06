using UnityEngine;

namespace Track11.EuropeanWindowPackVol01
{
/// <summary>
/// Small helper for 3D popup buttons driven by Physics raycasts instead of Unity UI version 0.1.
/// </summary>
public sealed class Popup3DButton : MonoBehaviour
{
    public enum ActionType
    {
        HideMenu,
        OpenTurn,
        OpenTilt,
        OpenSlide,
        CloseWindow
    }

    public enum VisualState
    {
        Normal,
        Hovered,
        Pressed
    }

    private WindowHandlePopup _owner;
    private Renderer _renderer;
    private ActionType _action;
    private Vector3 _baseScale;
    private VisualState _visualState;

    public WindowHandlePopup Owner => _owner;

    public void Setup(WindowHandlePopup owner, ActionType action, Renderer targetRenderer)
    {
        _owner = owner;
        _action = action;
        _renderer = targetRenderer;
        _baseScale = transform.localScale;
        ApplyVisualState(VisualState.Normal);
    }

    public void SetHovered(bool hovered)
    {
        ApplyVisualState(hovered ? VisualState.Hovered : VisualState.Normal);
    }

    public void Trigger()
    {
        ApplyVisualState(VisualState.Pressed);
        if (_owner != null)
            _owner.ExecuteButtonAction(_action);
    }

    private void ApplyVisualState(VisualState state)
    {
        _visualState = state;

        if (_renderer != null)
        {
            Material material = _renderer.material;
            material.color = _owner != null ? _owner.GetButtonColor(_visualState) : Color.white;
        }

        float scale = 1f;
        if (_owner != null && _visualState == VisualState.Hovered)
            scale = _owner.GetHoverScaleMultiplier();

        transform.localScale = _baseScale * scale;
    }
}
}
