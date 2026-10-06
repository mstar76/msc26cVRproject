using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Track11.EuropeanWindowPackVol01
{
/// <summary>
/// Simple exploded-view controller for showroom models.
/// Attach it to the imported FBX root and assign the mesh parts plus a trigger button mesh.
/// </summary>
public sealed class ExplodedViewController : MonoBehaviour
{
    [System.Serializable]
    private sealed class ExplodedPart
    {
        public string label = "Part";
        public Transform target;
        public Vector3 explodeOffsetCm = new Vector3(0f, 10f, 0f);
        public Vector3 explodeRotationEuler = Vector3.zero;
        public Vector3 explodeScaleMultiplier = Vector3.one;
        public bool inspectable = true;
        public bool allowRotateX = true;
        public bool allowRotateY = true;
        public bool allowRotateZ = false;
        public float inspectRotationSensitivity = 0.2f;
    }

    [Header("Exploded Parts")]
    [SerializeField] private List<ExplodedPart> parts = new List<ExplodedPart>();

    [Header("Explosion Animation")]
    [SerializeField] private float animationDuration = 0.45f;
    [SerializeField] private AnimationCurve animationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private float explodeDistanceMultiplier = 1f;

    [Header("Trigger Button")]
    [SerializeField] private Transform triggerButton;
    [SerializeField] private Vector3 buttonPressAxis = new Vector3(0f, -1f, 0f);
    [SerializeField] private float buttonPressDistanceCm = 4f;
    [SerializeField] private float buttonPressDuration = 0.08f;
    [SerializeField] private bool allowCloseByClickingExplodedParts = true;

    [Header("Direct Interaction Fallback")]
    [SerializeField] private bool enableDirectInteraction = true;
    [SerializeField] private Camera interactionCamera;
    [SerializeField] private float interactDistance = 4f;
    [SerializeField] private LayerMask interactMask = ~0;
    [SerializeField] private bool useMouseClick = true;
    [SerializeField] private bool useKeyboardInteract = false;
    [SerializeField] private Key interactKey = Key.E;
    [SerializeField] private bool debugLogging = false;

    [Header("Inspection")]
    [SerializeField] private bool enablePartInspection = true;
    [SerializeField] private bool invertInspectY = false;
    [SerializeField] private float defaultInspectRotationSensitivity = 0.2f;

    private readonly List<Vector3> _partStartLocalPositions = new List<Vector3>();
    private readonly List<Quaternion> _partStartLocalRotations = new List<Quaternion>();
    private readonly List<Vector3> _partStartLocalScales = new List<Vector3>();
    private Vector3 _buttonStartLocalPosition;
    private bool _isExploded;
    private bool _isAnimating;
    private int _lastInteractionFrame = -1;
    private int _activeInspectionPartIndex = -1;

    public bool IsExploded => _isExploded;

    private void Awake()
    {
        CacheStartPositions();

        if (interactionCamera == null)
            interactionCamera = Camera.main;
    }

    private void OnValidate()
    {
        animationDuration = Mathf.Max(0.01f, animationDuration);
        buttonPressDuration = Mathf.Max(0.01f, buttonPressDuration);
        interactDistance = Mathf.Max(0.01f, interactDistance);
        explodeDistanceMultiplier = Mathf.Max(0f, explodeDistanceMultiplier);
        defaultInspectRotationSensitivity = Mathf.Max(0.01f, defaultInspectRotationSensitivity);
    }

    private void Update()
    {
        if (!enableDirectInteraction) return;
        if (_isAnimating) return;

        if (interactionCamera == null)
            interactionCamera = Camera.main;

        if (interactionCamera == null) return;
        UpdateInspectionRotation();

        if (_activeInspectionPartIndex >= 0)
            return;

        if (!WasPrimaryInteractPressed()) return;

        Ray ray = interactionCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (!TryGetInteractionHit(ray, out RaycastHit hit))
            return;

        if (debugLogging)
            Debug.Log($"[ExplodedViewController] Direct hit: {hit.collider.name}", this);

        TryInteract(hit);
    }

    [ContextMenu("Explode")]
    public void Explode()
    {
        SetExploded(true);
    }

    [ContextMenu("Reset View")]
    public void ResetView()
    {
        SetExploded(false);
    }

    [ContextMenu("Toggle Exploded View")]
    public void ToggleExploded()
    {
        SetExploded(!_isExploded);
    }

    public void SetExploded(bool exploded)
    {
        if (!isActiveAndEnabled) return;
        if (_isAnimating) return;
        if (_isExploded == exploded) return;

        StartCoroutine(AnimateExplodedState(exploded, false));
    }

    public bool TryInteract(RaycastHit hit)
    {
        if (IsActualTriggerButtonHit(hit))
        {
            if (_isAnimating)
                return true;

            if (_lastInteractionFrame == Time.frameCount)
                return true;

            _lastInteractionFrame = Time.frameCount;

            if (debugLogging)
                Debug.Log($"[ExplodedViewController] Triggered by: {hit.collider.name}", this);

            StartCoroutine(AnimateExplodedState(!_isExploded, true));
            return true;
        }

        if (_isExploded && TryStartInspection(hit))
            return true;

        if (!(_isExploded && allowCloseByClickingExplodedParts && IsHitOnExplodedPart(hit)))
        {
            if (debugLogging && hit.collider != null)
                Debug.Log($"[ExplodedViewController] Hit ignored: {hit.collider.name}", this);
            return false;
        }

        if (_isAnimating)
            return true;

        if (_lastInteractionFrame == Time.frameCount)
            return true;

        _lastInteractionFrame = Time.frameCount;

        if (debugLogging)
            Debug.Log($"[ExplodedViewController] Close triggered by exploded part: {hit.collider.name}", this);

        StartCoroutine(AnimateExplodedState(false, false));
        return true;
    }

    private bool TryGetInteractionHit(Ray ray, out RaycastHit interactionHit)
    {
        RaycastHit[] hits = Physics.RaycastAll(ray, interactDistance, interactMask, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0)
        {
            interactionHit = default;
            return false;
        }

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            if (IsActualTriggerButtonHit(hits[i]))
            {
                interactionHit = hits[i];
                return true;
            }
        }

        if (_isExploded)
        {
            for (int i = 0; i < hits.Length; i++)
            {
                if (TryGetInspectablePartIndex(hits[i], out _))
                {
                    interactionHit = hits[i];
                    return true;
                }

                if (allowCloseByClickingExplodedParts && IsHitOnExplodedPart(hits[i]))
                {
                    interactionHit = hits[i];
                    return true;
                }
            }
        }

        interactionHit = hits[0];
        return false;
    }

    private void CacheStartPositions()
    {
        _partStartLocalPositions.Clear();
        _partStartLocalRotations.Clear();
        _partStartLocalScales.Clear();

        for (int i = 0; i < parts.Count; i++)
        {
            Transform target = parts[i] != null ? parts[i].target : null;
            if (CanAnimatePart(target))
            {
                _partStartLocalPositions.Add(target.localPosition);
                _partStartLocalRotations.Add(target.localRotation);
                _partStartLocalScales.Add(target.localScale);
            }
            else
            {
                _partStartLocalPositions.Add(Vector3.zero);
                _partStartLocalRotations.Add(Quaternion.identity);
                _partStartLocalScales.Add(Vector3.one);
            }
        }

        _buttonStartLocalPosition = triggerButton != null ? triggerButton.localPosition : Vector3.zero;
    }

    private bool IsActualTriggerButtonHit(RaycastHit hit)
    {
        if (hit.collider == null)
            return false;

        if (triggerButton == null)
            return false;

        Transform hitTransform = hit.collider.transform;
        return hitTransform == triggerButton
               || hitTransform.IsChildOf(triggerButton)
               || triggerButton.IsChildOf(hitTransform);
    }

    private bool IsHitOnExplodedPart(RaycastHit hit)
    {
        if (hit.collider == null)
            return false;

        Transform hitTransform = hit.collider.transform;
        return hitTransform == transform || hitTransform.IsChildOf(transform);
    }

    private bool WasPrimaryInteractPressed()
    {
        bool mousePressed = useMouseClick && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        bool keyPressed = useKeyboardInteract && Keyboard.current != null && Keyboard.current[interactKey].wasPressedThisFrame;
        return mousePressed || keyPressed;
    }

    private IEnumerator AnimateExplodedState(bool targetExploded, bool animateButton)
    {
        _isAnimating = true;
        _activeInspectionPartIndex = -1;

        if (animateButton && triggerButton != null)
            yield return AnimateButtonPress();

        Vector3[] fromPositions = new Vector3[parts.Count];
        Vector3[] toPositions = new Vector3[parts.Count];
        Quaternion[] fromRotations = new Quaternion[parts.Count];
        Quaternion[] toRotations = new Quaternion[parts.Count];
        Vector3[] fromScales = new Vector3[parts.Count];
        Vector3[] toScales = new Vector3[parts.Count];

        for (int i = 0; i < parts.Count; i++)
        {
            ExplodedPart part = parts[i];
            if (part == null || !CanAnimatePart(part.target))
                continue;

            Vector3 closedLocalPosition = i < _partStartLocalPositions.Count
                ? _partStartLocalPositions[i]
                : part.target.localPosition;
            Quaternion closedLocalRotation = i < _partStartLocalRotations.Count
                ? _partStartLocalRotations[i]
                : part.target.localRotation;
            Vector3 closedLocalScale = i < _partStartLocalScales.Count
                ? _partStartLocalScales[i]
                : part.target.localScale;

            Vector3 explodedLocalPosition = closedLocalPosition + (part.explodeOffsetCm * 0.01f * explodeDistanceMultiplier);
            fromPositions[i] = part.target.localPosition;
            toPositions[i] = targetExploded ? explodedLocalPosition : closedLocalPosition;
            fromRotations[i] = part.target.localRotation;
            toRotations[i] = targetExploded
                ? Quaternion.Euler(part.explodeRotationEuler) * closedLocalRotation
                : closedLocalRotation;
            fromScales[i] = part.target.localScale;
            toScales[i] = targetExploded
                ? Vector3.Scale(closedLocalScale, SanitizeScaleMultiplier(part.explodeScaleMultiplier))
                : closedLocalScale;
        }

        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / animationDuration);
            float eased = EvaluateCurve(t);
            ApplyPartTransforms(fromPositions, toPositions, fromRotations, toRotations, fromScales, toScales, eased);
            yield return null;
        }

        ApplyPartTransforms(fromPositions, toPositions, fromRotations, toRotations, fromScales, toScales, 1f);
        _isExploded = targetExploded;
        _isAnimating = false;
    }

    private IEnumerator AnimateButtonPress()
    {
        Vector3 axis = buttonPressAxis.sqrMagnitude > 0.0001f ? buttonPressAxis.normalized : Vector3.down;
        Vector3 pressedLocalPosition = _buttonStartLocalPosition + axis * (buttonPressDistanceCm * 0.01f);

        yield return AnimateButtonPosition(_buttonStartLocalPosition, pressedLocalPosition);
        yield return AnimateButtonPosition(pressedLocalPosition, _buttonStartLocalPosition);
    }

    private IEnumerator AnimateButtonPosition(Vector3 from, Vector3 to)
    {
        if (triggerButton == null)
            yield break;

        float elapsed = 0f;
        while (elapsed < buttonPressDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / buttonPressDuration);
            triggerButton.localPosition = Vector3.LerpUnclamped(from, to, EvaluateCurve(t));
            yield return null;
        }

        triggerButton.localPosition = to;
    }

    private void ApplyPartTransforms(
        Vector3[] fromPositions,
        Vector3[] toPositions,
        Quaternion[] fromRotations,
        Quaternion[] toRotations,
        Vector3[] fromScales,
        Vector3[] toScales,
        float t)
    {
        for (int i = 0; i < parts.Count; i++)
        {
            ExplodedPart part = parts[i];
            if (part == null || !CanAnimatePart(part.target))
                continue;

            part.target.localPosition = Vector3.LerpUnclamped(fromPositions[i], toPositions[i], t);
            part.target.localRotation = Quaternion.SlerpUnclamped(fromRotations[i], toRotations[i], t);
            part.target.localScale = Vector3.LerpUnclamped(fromScales[i], toScales[i], t);
        }
    }

    private bool CanAnimatePart(Transform target)
    {
        if (target == null)
            return false;

        if (triggerButton == null)
            return true;

        return target != triggerButton
               && !target.IsChildOf(triggerButton)
               && !triggerButton.IsChildOf(target);
    }

    private float EvaluateCurve(float t)
    {
        if (animationCurve == null || animationCurve.length == 0)
            return t;

        return animationCurve.Evaluate(t);
    }

    private void UpdateInspectionRotation()
    {
        if (_activeInspectionPartIndex < 0)
            return;

        if (Mouse.current == null || !Mouse.current.leftButton.isPressed)
        {
            _activeInspectionPartIndex = -1;
            return;
        }

        if (_activeInspectionPartIndex >= parts.Count)
        {
            _activeInspectionPartIndex = -1;
            return;
        }

        ExplodedPart part = parts[_activeInspectionPartIndex];
        if (part == null || part.target == null || !part.inspectable)
        {
            _activeInspectionPartIndex = -1;
            return;
        }

        Vector2 delta = Mouse.current.delta.ReadValue();
        if (delta.sqrMagnitude <= 0.0001f)
            return;

        float sensitivity = part.inspectRotationSensitivity > 0f
            ? part.inspectRotationSensitivity
            : defaultInspectRotationSensitivity;

        float pitch = (invertInspectY ? delta.y : -delta.y) * sensitivity;
        float yaw = -delta.x * sensitivity;
        float roll = 0f;

        if (part.allowRotateZ)
        {
            bool rollPositive = Keyboard.current != null && Keyboard.current.eKey.isPressed;
            bool rollNegative = Keyboard.current != null && Keyboard.current.qKey.isPressed;
            if (rollPositive || rollNegative)
                roll = delta.x * sensitivity * (rollPositive ? 1f : -1f);
        }

        Vector3 eulerDelta = new Vector3(
            part.allowRotateX ? pitch : 0f,
            part.allowRotateY ? yaw : 0f,
            part.allowRotateZ ? roll : 0f);

        if (eulerDelta.sqrMagnitude <= 0.0001f)
            return;

        part.target.localRotation *= Quaternion.Euler(eulerDelta);
    }

    private bool TryStartInspection(RaycastHit hit)
    {
        if (!enablePartInspection || !_isExploded)
            return false;

        if (!TryGetInspectablePartIndex(hit, out int partIndex))
            return false;

        _activeInspectionPartIndex = partIndex;

        if (debugLogging)
            Debug.Log($"[ExplodedViewController] Inspecting part: {parts[partIndex].label}", this);

        return true;
    }

    private bool TryGetInspectablePartIndex(RaycastHit hit, out int partIndex)
    {
        partIndex = -1;

        if (hit.collider == null)
            return false;

        Transform hitTransform = hit.collider.transform;

        for (int i = 0; i < parts.Count; i++)
        {
            ExplodedPart part = parts[i];
            if (part == null || part.target == null || !part.inspectable)
                continue;

            if (hitTransform == part.target || hitTransform.IsChildOf(part.target) || part.target.IsChildOf(hitTransform))
            {
                partIndex = i;
                return true;
            }
        }

        return false;
    }

    private static Vector3 SanitizeScaleMultiplier(Vector3 scaleMultiplier)
    {
        return new Vector3(
            Mathf.Approximately(scaleMultiplier.x, 0f) ? 1f : scaleMultiplier.x,
            Mathf.Approximately(scaleMultiplier.y, 0f) ? 1f : scaleMultiplier.y,
            Mathf.Approximately(scaleMultiplier.z, 0f) ? 1f : scaleMultiplier.z);
    }
}
}
