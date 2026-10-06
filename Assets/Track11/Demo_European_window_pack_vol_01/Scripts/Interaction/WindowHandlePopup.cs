using System.Collections;
using UnityEngine;

namespace Track11.EuropeanWindowPackVol01
{
/// <summary>
/// Interactive handle/popup controller for window prefabs.
/// Spawns a small 3D menu next to the clicked handle and drives turn/tilt/close animations.
/// The menu is built from real meshes and colliders so it can be used with a normal Physics raycast.
/// </summary>
public class WindowHandlePopup : MonoBehaviour
{
    public enum Axis { X, Y, Z }
    private enum Mode { Closed, TurnOpen, TiltOpen, SlideOpen }
    public enum InitialMode { Closed, TurnOpen, TiltOpen, SlideOpen }

    [Header("Pivots")]
    public Transform tiltPivot;
    public Transform turnPivot;
    public Transform handleBarPivot;
    public Transform mechanicPivot;
    public Transform slidePivot;
    public Transform liftPivot;

    [Header("French Window")]
    [Tooltip("Marks this WHP as the primary sash/door of a French window pair. Only TurnOpen releases the secondary.")]
    public bool frenchDoorPrimary;
    [Tooltip("Marks this WHP as the secondary sash/door of a French window pair. Secondary can only open if the primary is already TurnOpen.")]
    public bool frenchWindowSecondary;

    [Header("Rotation Axes")]
    public Axis tiltAxis = Axis.X;
    public Axis turnAxis = Axis.Y;
    public Axis handleAxis = Axis.Z;
    public Axis mechanicTurnAxis = Axis.Y;
    public Axis mechanicTiltAxis = Axis.Y;
    public Axis slideAxis = Axis.X;
    public Axis liftAxis = Axis.Y;

    [Header("Angles")]
    public float tiltOpenAngle = -5f;
    public float turnOpenAngle = 90f;
    public float handleAngleClosed = 0f;
    public float handleAngleTurn = -90f;
    public float handleAngleTilt = 180f;
    public float handleAngleSlide = 180f;
    public float mechanicTurnAngle = 90f;
    public float mechanicTiltAngle = 25f;

    [Header("Animation")]
    public float rotateSpeed = 220f;
    public float turnRotateSpeed = 220f;
    public float tiltRotateSpeed = 220f;
    public float mechanicTurnSpeedMultiplier = 1f;
    public float mechanicTiltSpeedMultiplier = 0.5f;
    public AnimationCurve mechanicTiltCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Initial State")]
    public InitialMode initialMode = InitialMode.Closed;

    [Header("Slide / HSE")]
    public string slideLabel = "Slide";
    public float slideDistanceCm = 80f;
    public float liftDistanceCm = 6f;
    public float slideSpeed = 120f;
    public float liftSpeed = 80f;
    public AnimationCurve slideCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Audio")]
    public bool enableAudio = false;
    public Transform handleSoundAnchor;
    public Transform frameSoundAnchor;
    public AudioClip handleTurn90Sound;
    public AudioClip handleTurn180Sound;
    public AudioClip frameOpenSound;
    public AudioClip frameCloseSound;
    [Range(0f, 1f)] public float handleVolume = 1f;
    [Range(0f, 1f)] public float frameVolume = 1f;

    [Header("3D Menu Content")]
    public bool showTitle = false;
    public string titleText = "Window";
    public string turnLabel = "Turn";
    public string tiltLabel = "Tilt";
    public string closeLabel = "Close";
    public bool showCloseX = true;
    public string closeXLabel = "X";

    [Header("3D Menu Placement")]
    public Vector3 worldOffsetCm = new Vector3(-8f, 0f, 8f);

    [Tooltip("Distance to push the menu away from the hit surface along the hit normal (in centimeters).")]
    public float panelNormalDistanceCm = 12f;

    [Tooltip("Additional offset applied in menu-local space after orientation (x=right, y=up, z=forward), in centimeters.")]
    public Vector3 panelLocalOffsetCm = Vector3.zero;

    [Tooltip("If enabled the menu billboards toward the camera on the horizontal plane.")]
    public bool faceCamera = true;

    [Tooltip("Flips the menu forward direction toward or away from the camera.")]
    public bool flipTowardsCamera = false;

    [Tooltip("Extra menu rotation in local Euler angles. Use this to flip the whole menu by 180 degrees if needed.")]
    public Vector3 menuRotationOffsetEuler = Vector3.zero;

    [Header("3D Menu Size")]
    public float menuWidthCm = 30f;
    public float menuHeightCm = 34f;
    public float menuDepthCm = 1.6f;
    public float buttonWidthCm = 22f;
    public float buttonHeightCm = 14f;
    public float buttonDepthCm = 1.2f;
    public float buttonInsetCm = 1.2f;
    public float spacingCm = 1.8f;
    public float paddingCm = -12f;
    public float titleHeightCm = 10f;
    public float headerButtonWidthCm = 10f;

    [Header("3D Menu Visuals")]
    public Color panelColor = new Color(0.08f, 0.08f, 0.08f, 0.88f);
    public Color buttonColor = new Color(0.20f, 0.20f, 0.20f, 1f);
    public Color buttonHoverColor = new Color(0.33f, 0.33f, 0.33f, 1f);
    public Color buttonPressedColor = new Color(0.45f, 0.45f, 0.45f, 1f);
    public Color textColor = Color.white;
    public Color titleColor = Color.white;

    [Header("3D Menu Text")]
    public int titleFontSize = 40;
    public int buttonFontSize = 36;
    public float titleCharacterSizeCm = 3.5f;
    public float buttonCharacterSizeCm = 14f;
    public float textDepthOffsetCm = 0.2f;

    [Header("3D Menu Hover")]
    public bool enableHoverScale = true;
    public float hoverScaleMultiplier = 1.2f;

    public static bool AnyPopupOpen => _openCount > 0;
    public bool IsPopupOpen => _popupOpen;
    private static int _openCount;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        _openCount = 0;
    }

    private const string FrenchPrimaryMarkerName = "__WHP_FrenchPrimaryOpen";
    private const string FrenchSecondaryMarkerName = "__WHP_FrenchSecondaryOpen";

    private Quaternion _tiltClosedLocalRot;
    private Quaternion _turnClosedLocalRot;
    private Quaternion _handleClosedLocalRot;
    private Quaternion _mechanicClosedLocalRot;
    private Vector3 _slideClosedLocalPos;
    private Vector3 _liftClosedLocalPos;

    private Mode _mode = Mode.Closed;
    private Coroutine _transitionRoutine;
    private bool _popupOpen;

    private Transform _menuRoot;
    private Transform _panelRoot;
    private Popup3DButton _currentHover;
    private Font _runtimeFont;
    private AudioSource _handleAudioSource;
    private AudioSource _frameAudioSource;

    private void Awake()
    {
        NormalizeFrenchFlags();
        CacheClosedRotations();
        CacheAudioSources();
        _runtimeFont = LoadBuiltinFont();
        ApplyInitialState();
    }

    private void OnDestroy()
    {
        if (_popupOpen) _openCount = Mathf.Max(0, _openCount - 1);
        if (_menuRoot != null) Destroy(_menuRoot.gameObject);
    }

    public static void CloseAll()
    {
        var all = FindObjectsByType<WindowHandlePopup>(FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++) all[i].Close();
    }

    /// <summary>
    /// Opens or closes the menu using a surface hit point and hit normal to place it in world space.
    /// </summary>
    public void Toggle(RaycastHit hit)
    {
        if (!CanOpenPopup())
            return;

        if (_popupOpen)
        {
            HidePopupInstant();
            return;
        }

        Vector3 spawnPos = hit.point + hit.normal * Cm(panelNormalDistanceCm) + Cm(worldOffsetCm);
        ShowMenuAt(spawnPos);
    }

    /// <summary>
    /// Fallback overload for compatibility with simple SendMessage("Toggle") calls without RaycastHit.
    /// </summary>
    public void Toggle()
    {
        if (!CanOpenPopup())
            return;

        if (_popupOpen)
        {
            HidePopupInstant();
            return;
        }

        ShowMenuAt(transform.position + Vector3.up * Cm(80f));
    }

    public void Close()
    {
        HidePopupInstant();
    }

    public void CloseWindowState()
    {
        HidePopupInstant();
        StartModeTransition(Mode.Closed);
    }

    public void SetHoveredButton(Popup3DButton button)
    {
        if (_currentHover == button) return;

        if (_currentHover != null)
            _currentHover.SetHovered(false);

        _currentHover = button;

        if (_currentHover != null)
            _currentHover.SetHovered(true);
    }

    public void ClearHoveredButton(Popup3DButton button)
    {
        if (_currentHover != button) return;

        _currentHover.SetHovered(false);
        _currentHover = null;
    }

    public void ClearHoveredButton()
    {
        if (_currentHover == null) return;

        _currentHover.SetHovered(false);
        _currentHover = null;
    }

    public void ExecuteButtonAction(Popup3DButton.ActionType action)
    {
        switch (action)
        {
            case Popup3DButton.ActionType.HideMenu:
                HidePopupInstant();
                break;
            case Popup3DButton.ActionType.OpenTurn:
                HidePopupInstant();
                StartModeTransition(Mode.TurnOpen);
                break;
            case Popup3DButton.ActionType.OpenTilt:
                HidePopupInstant();
                StartModeTransition(Mode.TiltOpen);
                break;
            case Popup3DButton.ActionType.OpenSlide:
                HidePopupInstant();
                StartModeTransition(Mode.SlideOpen);
                break;
            case Popup3DButton.ActionType.CloseWindow:
                CloseWindowState();
                break;
        }
    }

    public Color GetButtonColor(Popup3DButton.VisualState state)
    {
        switch (state)
        {
            case Popup3DButton.VisualState.Hovered: return buttonHoverColor;
            case Popup3DButton.VisualState.Pressed: return buttonPressedColor;
            default: return buttonColor;
        }
    }

    public float GetHoverScaleMultiplier() => enableHoverScale ? Mathf.Max(1f, hoverScaleMultiplier) : 1f;

    private void CacheClosedRotations()
    {
        if (tiltPivot) _tiltClosedLocalRot = tiltPivot.localRotation;
        if (turnPivot) _turnClosedLocalRot = turnPivot.localRotation;
        if (handleBarPivot) _handleClosedLocalRot = handleBarPivot.localRotation;
        if (mechanicPivot) _mechanicClosedLocalRot = mechanicPivot.localRotation;
        if (slidePivot) _slideClosedLocalPos = slidePivot.localPosition;
        if (liftPivot) _liftClosedLocalPos = liftPivot.localPosition;
    }

    private void CacheAudioSources()
    {
        if (handleSoundAnchor == null)
            handleSoundAnchor = handleBarPivot != null ? handleBarPivot : transform;

        _handleAudioSource = EnsureAudioSource(handleSoundAnchor);

        if (frameSoundAnchor == null)
            frameSoundAnchor = GetDefaultFrameSoundAnchor();

        _frameAudioSource = EnsureAudioSource(frameSoundAnchor);
    }

    private Transform GetDefaultFrameSoundAnchor()
    {
        if (turnPivot != null) return turnPivot;
        if (tiltPivot != null) return tiltPivot;
        if (slidePivot != null) return slidePivot;
        return transform;
    }

    private static AudioSource EnsureAudioSource(Transform anchor)
    {
        if (anchor == null)
            return null;

        AudioSource source = anchor.GetComponent<AudioSource>();
        if (source == null)
            source = anchor.gameObject.AddComponent<AudioSource>();

        source.playOnAwake = false;
        source.spatialBlend = 1f;
        return source;
    }

    private void ApplyInitialState()
    {
        switch (initialMode)
        {
            case InitialMode.TurnOpen:
                if (!SupportsTurn()) break;
                ApplyClosedTransforms();
                ApplyTurnStateImmediate();
                SetFrenchMarkerStateForMode(Mode.TurnOpen);
                _mode = Mode.TurnOpen;
                break;

            case InitialMode.TiltOpen:
                if (!SupportsTilt()) break;
                ApplyClosedTransforms();
                ApplyTiltStateImmediate();
                SetFrenchMarkerStateForMode(Mode.TiltOpen);
                _mode = Mode.TiltOpen;
                break;

            case InitialMode.SlideOpen:
                if (!SupportsSlide()) break;
                ApplyClosedTransforms();
                ApplySlideStateImmediate();
                SetFrenchMarkerStateForMode(Mode.SlideOpen);
                _mode = Mode.SlideOpen;
                break;

            default:
                ApplyClosedTransforms();
                SetFrenchMarkerStateForMode(Mode.Closed);
                _mode = Mode.Closed;
                break;
        }
    }

    private void ApplyClosedTransforms()
    {
        if (turnPivot) turnPivot.localRotation = _turnClosedLocalRot;
        if (tiltPivot) tiltPivot.localRotation = _tiltClosedLocalRot;
        if (handleBarPivot) handleBarPivot.localRotation = _handleClosedLocalRot;
        if (mechanicPivot) mechanicPivot.localRotation = _mechanicClosedLocalRot;
        if (slidePivot) slidePivot.localPosition = _slideClosedLocalPos;
        if (liftPivot) liftPivot.localPosition = _liftClosedLocalPos;
    }

    private void ApplyTurnStateImmediate()
    {
        if (handleBarPivot)
            handleBarPivot.localRotation = ApplyAxis(_handleClosedLocalRot, handleAxis, handleAngleTurn);

        if (turnPivot)
            turnPivot.localRotation = ApplyAxis(_turnClosedLocalRot, turnAxis, turnOpenAngle);

        if (mechanicPivot)
            mechanicPivot.localRotation = ApplyAxis(_mechanicClosedLocalRot, mechanicTurnAxis, mechanicTurnAngle);
    }

    private void ApplyTiltStateImmediate()
    {
        if (handleBarPivot)
            handleBarPivot.localRotation = ApplyAxis(_handleClosedLocalRot, handleAxis, handleAngleTilt);

        if (tiltPivot)
            tiltPivot.localRotation = ApplyAxis(_tiltClosedLocalRot, tiltAxis, tiltOpenAngle);

        if (mechanicPivot)
            mechanicPivot.localRotation = ApplyAxis(_mechanicClosedLocalRot, mechanicTiltAxis, mechanicTiltAngle);
    }

    private void ApplySlideStateImmediate()
    {
        if (handleBarPivot)
            handleBarPivot.localRotation = ApplyAxis(_handleClosedLocalRot, handleAxis, handleAngleSlide);

        if (liftPivot)
            liftPivot.localPosition = ApplyAxisOffset(_liftClosedLocalPos, liftAxis, Cm(liftDistanceCm));

        if (slidePivot)
        {
            Vector3 basePosition = _slideClosedLocalPos;
            if (slidePivot == liftPivot && liftPivot != null)
                basePosition = ApplyAxisOffset(basePosition, liftAxis, Cm(liftDistanceCm));

            slidePivot.localPosition = ApplyAxisOffset(basePosition, slideAxis, Cm(slideDistanceCm));
        }
    }

    private void Ensure3DMenu()
    {
        if (_menuRoot != null) return;

        GameObject root = new GameObject($"WindowHandlePopup3D_{GetInstanceID()}");
        _menuRoot = root.transform;
        Rebuild3DMenuGeometry();
        _menuRoot.gameObject.SetActive(false);
    }

    private void Rebuild3DMenuGeometry()
    {
        if (_menuRoot == null) return;

        for (int i = _menuRoot.childCount - 1; i >= 0; i--)
            DestroyMenuObject(_menuRoot.GetChild(i).gameObject);

        GameObject panel = CreatePrimitive("Panel", PrimitiveType.Cube, _menuRoot);
        _panelRoot = panel.transform;
        _panelRoot.localScale = new Vector3(Cm(menuWidthCm), Cm(menuHeightCm), Cm(menuDepthCm));

        var panelRenderer = panel.GetComponent<Renderer>();
        panelRenderer.sharedMaterial = CreateRuntimeMaterial(panelColor, !IsEffectivelyOpaque(panelColor));
        Destroy(panel.GetComponent<Collider>());

        float frontZ = (_panelRoot.localScale.z * 0.5f) + Cm(buttonInsetCm);
        float contentTop = (_panelRoot.localScale.y * 0.5f) - Cm(paddingCm);
        float cursorY = contentTop;
        float safeTextOffset = GetStableTextDepthOffset();

        if (showTitle)
        {
            float titleHeight = Cm(titleHeightCm);
            float titleY = cursorY - (titleHeight * 0.5f);
            CreateTextObject("TitleText", titleText, _panelRoot, new Vector3(0f, titleY, -frontZ - safeTextOffset),
                Cm(titleCharacterSizeCm), titleFontSize, titleColor, TextAnchor.MiddleCenter);
            cursorY -= titleHeight + Cm(spacingCm);
        }

        if (showCloseX)
        {
            float headerButtonWidth = Cm(headerButtonWidthCm);
            float buttonHeight = Cm(buttonHeightCm);
            float buttonDepth = Cm(buttonDepthCm);
            float xPos = (_panelRoot.localScale.x * 0.5f) - Cm(paddingCm) - (headerButtonWidth * 0.5f);
            float xY = contentTop - (buttonHeight * 0.5f);
            CreateButton("CloseX", closeXLabel, Popup3DButton.ActionType.HideMenu,
                new Vector3(xPos, xY, frontZ), new Vector3(headerButtonWidth, buttonHeight, buttonDepth));

            if (!showTitle)
                cursorY -= buttonHeight + Cm(spacingCm);
        }

        if (SupportsTurn())
            CreateButtonRow(ref cursorY, "Turn", turnLabel, Popup3DButton.ActionType.OpenTurn, frontZ);

        if (SupportsTilt())
            CreateButtonRow(ref cursorY, "Tilt", tiltLabel, Popup3DButton.ActionType.OpenTilt, frontZ);

        if (SupportsSlide())
            CreateButtonRow(ref cursorY, "Slide", slideLabel, Popup3DButton.ActionType.OpenSlide, frontZ);

        if (SupportsClose())
            CreateButtonRow(ref cursorY, "Close", closeLabel, Popup3DButton.ActionType.CloseWindow, frontZ);
    }

    private void CreateButtonRow(ref float cursorY, string objectName, string label, Popup3DButton.ActionType action, float frontZ)
    {
        float maxWidth = _panelRoot.localScale.x - (Cm(paddingCm) * 2f);
        float width = Mathf.Clamp(Cm(buttonWidthCm), Cm(4f), maxWidth);
        float buttonHeight = Cm(buttonHeightCm);
        float buttonDepth = Cm(buttonDepthCm);
        float y = cursorY - (buttonHeight * 0.5f);
        CreateButton(objectName, label, action, new Vector3(0f, y, frontZ), new Vector3(width, buttonHeight, buttonDepth));
        cursorY -= buttonHeight + Cm(spacingCm);
    }

    private void CreateButton(string objectName, string label, Popup3DButton.ActionType action, Vector3 localPos, Vector3 size)
    {
        GameObject button = CreatePrimitive(objectName, PrimitiveType.Cube, _panelRoot);
        button.transform.localPosition = localPos;
        button.transform.localScale = size;

        var renderer = button.GetComponent<Renderer>();
        renderer.sharedMaterial = CreateRuntimeMaterial(buttonColor, !IsEffectivelyOpaque(buttonColor));

        Popup3DButton popupButton = button.AddComponent<Popup3DButton>();
        popupButton.Setup(this, action, renderer);

        float safeTextOffset = GetStableTextDepthOffset();
        CreateTextObject($"{objectName}Text", label, button.transform,
            new Vector3(0f, 0f, -(size.z * 0.5f) - safeTextOffset),
            Cm(buttonCharacterSizeCm), buttonFontSize, textColor, TextAnchor.MiddleCenter);
    }

    private TextMesh CreateTextObject(string objectName, string text, Transform parent, Vector3 localPos,
        float characterSize, int fontSize, Color color, TextAnchor anchor)
    {
        GameObject go = new GameObject(objectName);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.identity;

        TextMesh tm = go.AddComponent<TextMesh>();
        tm.text = text;
        tm.anchor = anchor;
        tm.alignment = TextAlignment.Center;
        tm.characterSize = characterSize;
        tm.fontSize = fontSize;
        tm.color = color;
        tm.font = _runtimeFont != null ? _runtimeFont : LoadBuiltinFont();

        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        if (mr != null && tm.font != null && tm.font.material != null)
        {
            Material textMaterial = CreateRuntimeTextMaterial(tm.font);
            mr.sharedMaterial = textMaterial != null ? textMaterial : tm.font.material;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            mr.allowOcclusionWhenDynamic = false;
        }

        return tm;
    }

    private void ShowMenuAt(Vector3 worldPos)
    {
        Ensure3DMenu();
        if (_menuRoot == null) return;

        RefreshMenuVisuals();

        _popupOpen = true;
        _openCount++;

        _menuRoot.position = worldPos;
        OrientMenu();
        ApplyLocalOffset();
        _menuRoot.gameObject.SetActive(true);
    }

    private void RefreshMenuVisuals()
    {
        if (_menuRoot == null) return;
        Rebuild3DMenuGeometry();
    }

    private void OrientMenu()
    {
        if (_menuRoot == null) return;

        Quaternion rotation = Quaternion.identity;
        Camera cam = Camera.main;
        bool orientUsingCamera = faceCamera || flipTowardsCamera;

        if (orientUsingCamera && cam != null)
        {
            Vector3 toCam = cam.transform.position - _menuRoot.position;
            toCam.y = 0f;

            if (toCam.sqrMagnitude > 0.0001f)
            {
                Vector3 forward = flipTowardsCamera ? toCam.normalized : -toCam.normalized;
                rotation = Quaternion.LookRotation(forward, Vector3.up);
            }
        }

        rotation *= Quaternion.Euler(menuRotationOffsetEuler);
        _menuRoot.rotation = rotation;
    }

    private void ApplyLocalOffset()
    {
        if (_menuRoot == null || panelLocalOffsetCm == Vector3.zero) return;

        Transform t = _menuRoot;
        Vector3 panelLocalOffset = Cm(panelLocalOffsetCm);
        t.position += t.right * panelLocalOffset.x;
        t.position += t.up * panelLocalOffset.y;
        t.position += t.forward * panelLocalOffset.z;
    }

    private void HidePopupInstant()
    {
        if (!_popupOpen) return;

        _popupOpen = false;
        _openCount = Mathf.Max(0, _openCount - 1);

        if (_currentHover != null)
        {
            _currentHover.SetHovered(false);
            _currentHover = null;
        }

        if (_menuRoot != null)
            _menuRoot.gameObject.SetActive(false);
    }

    private void StartModeTransition(Mode targetMode)
    {
        if (_transitionRoutine != null) StopCoroutine(_transitionRoutine);
        _transitionRoutine = StartCoroutine(TransitionRoutine(targetMode));
    }

    /// <summary>
    /// Ensures turn and tilt states remain mutually exclusive and animates pivots and handle to the target mode.
    /// </summary>
    private IEnumerator TransitionRoutine(Mode targetMode)
    {
        if (targetMode == _mode && targetMode != Mode.Closed)
            yield break;

        if (!CanTransitionTo(targetMode))
            yield break;

        Mode sourceMode = _mode;

        if (targetMode == Mode.TurnOpen)
        {
            if (_mode == Mode.TiltOpen) yield return CloseTiltStateOnly();
            else if (_mode == Mode.SlideOpen) yield return CloseSlideStateOnly();
            else yield return RotateTo(tiltPivot, _tiltClosedLocalRot, Mathf.Max(0.01f, tiltRotateSpeed));

            yield return RotateHandle(handleAngleTurn);
            PlayFrameOpenSequence();
            yield return RotatePrimaryWithMechanic(
                turnPivot,
                _turnClosedLocalRot,
                turnAxis,
                turnOpenAngle,
                mechanicPivot,
                _mechanicClosedLocalRot,
                mechanicTurnAxis,
                mechanicTurnAngle,
                Mathf.Max(0.01f, turnRotateSpeed),
                Mathf.Max(0.01f, mechanicTurnSpeedMultiplier),
                null);
            SetFrenchMarkerStateForMode(Mode.TurnOpen);
            _mode = Mode.TurnOpen;
            yield break;
        }

        if (targetMode == Mode.TiltOpen)
        {
            if (_mode == Mode.TurnOpen) yield return CloseTurnStateOnly();
            else if (_mode == Mode.SlideOpen) yield return CloseSlideStateOnly();
            else yield return RotateTo(turnPivot, _turnClosedLocalRot, Mathf.Max(0.01f, turnRotateSpeed));

            yield return RotateHandle(handleAngleTilt);
            PlayFrameOpenSequence();
            yield return RotatePrimaryWithMechanic(
                tiltPivot,
                _tiltClosedLocalRot,
                tiltAxis,
                tiltOpenAngle,
                mechanicPivot,
                _mechanicClosedLocalRot,
                mechanicTiltAxis,
                mechanicTiltAngle,
                Mathf.Max(0.01f, tiltRotateSpeed),
                Mathf.Max(0.01f, mechanicTiltSpeedMultiplier),
                mechanicTiltCurve);
            SetFrenchMarkerStateForMode(Mode.TiltOpen);
            _mode = Mode.TiltOpen;
            yield break;
        }

        if (targetMode == Mode.SlideOpen)
        {
            if (_mode == Mode.TurnOpen) yield return CloseTurnStateOnly();
            else if (_mode == Mode.TiltOpen) yield return CloseTiltStateOnly();
            else if (_mode == Mode.SlideOpen) yield return CloseSlideStateOnly();

            yield return OpenSlideState();
            SetFrenchMarkerStateForMode(Mode.SlideOpen);
            _mode = Mode.SlideOpen;
            yield break;
        }

        yield return CloseCurrentOpenState();
        yield return RotateHandle(handleAngleClosed, sourceMode);
        PlayFrameClip(frameCloseSound);
        SetFrenchMarkerStateForMode(Mode.Closed);
        _mode = Mode.Closed;
    }

    private IEnumerator CloseCurrentOpenState()
    {
        switch (_mode)
        {
            case Mode.TurnOpen:
                yield return CloseTurnStateOnly();
                break;
            case Mode.TiltOpen:
                yield return CloseTiltStateOnly();
                break;
            case Mode.SlideOpen:
                yield return CloseSlideStateOnly();
                break;
            default:
                yield return RotateTargetsToClosed(Mathf.Max(0.01f, Mathf.Max(turnRotateSpeed, tiltRotateSpeed)));
                break;
        }
    }

    private IEnumerator CloseTurnStateOnly()
    {
        bool done;
        float speed = Mathf.Max(0.01f, turnRotateSpeed);
        float mechanicSpeed = speed * Mathf.Max(0.01f, mechanicTurnSpeedMultiplier);

        do
        {
            done = true;
            done &= RotateTowardsTarget(turnPivot, _turnClosedLocalRot, speed);
            done &= RotateTowardsTarget(mechanicPivot, _mechanicClosedLocalRot, mechanicSpeed);

            if (!done)
                yield return null;
        }
        while (!done);

        _mode = Mode.Closed;
    }

    private IEnumerator OpenSlideState()
    {
        Quaternion handleTarget = _handleClosedLocalRot;
        bool hasHandle = handleBarPivot != null;
        if (hasHandle)
            handleTarget = ApplyAxis(_handleClosedLocalRot, handleAxis, handleAngleSlide);

        Vector3 liftTarget = _liftClosedLocalPos;
        bool hasLift = liftPivot != null && !Mathf.Approximately(liftDistanceCm, 0f);
        if (hasLift)
            liftTarget = ApplyAxisOffset(_liftClosedLocalPos, liftAxis, Cm(liftDistanceCm));

        bool done;
        float liftUnitsPerSecond = Cm(Mathf.Max(0.01f, liftSpeed));

        do
        {
            done = true;
            if (hasHandle)
                done &= RotateTowardsTarget(handleBarPivot, handleTarget, rotateSpeed);

            if (hasLift)
                done &= MoveTowardsTarget(liftPivot, liftTarget, liftUnitsPerSecond);

            if (!done)
                yield return null;
        }
        while (!done);

        PlayFrameOpenSequence();
        yield return MoveSlideToOpen();
    }

    private IEnumerator CloseSlideStateOnly()
    {
        float slideUnitsPerSecond = Cm(Mathf.Max(0.01f, slideSpeed));
        float liftUnitsPerSecond = Cm(Mathf.Max(0.01f, liftSpeed));

        if (slidePivot)
            yield return MoveTo(slidePivot, _slideClosedLocalPos, slideUnitsPerSecond);

        Quaternion handleTarget = _handleClosedLocalRot;
        bool hasHandle = handleBarPivot != null;
        bool hasLift = liftPivot != null;
        bool done;

        do
        {
            done = true;
            if (hasHandle)
                done &= RotateTowardsTarget(handleBarPivot, handleTarget, rotateSpeed);

            if (hasLift)
                done &= MoveTowardsTarget(liftPivot, _liftClosedLocalPos, liftUnitsPerSecond);

            if (!done)
                yield return null;
        }
        while (!done);

        _mode = Mode.Closed;
    }

    private IEnumerator CloseTiltStateOnly()
    {
        bool done;
        float speed = Mathf.Max(0.01f, tiltRotateSpeed);
        float mechanicSpeed = speed * Mathf.Max(0.01f, mechanicTiltSpeedMultiplier);

        do
        {
            done = true;
            done &= RotateTowardsTarget(tiltPivot, _tiltClosedLocalRot, speed);
            done &= RotateTowardsTarget(mechanicPivot, _mechanicClosedLocalRot, mechanicSpeed);

            if (!done)
                yield return null;
        }
        while (!done);

        _mode = Mode.Closed;
    }

    private IEnumerator RotateHandle(float targetAngle)
    {
        yield return RotateHandle(targetAngle, _mode);
    }

    private IEnumerator RotateHandle(float targetAngle, Mode sourceMode)
    {
        if (!handleBarPivot) yield break;
        PlayHandleClipForTarget(targetAngle, sourceMode);
        Quaternion target = ApplyAxis(_handleClosedLocalRot, handleAxis, targetAngle);
        yield return RotateTo(handleBarPivot, target);
    }

    private void PlayFrameOpenSequence()
    {
        PlayFrameClip(frameOpenSound);
    }

    private void PlayHandleClipForTarget(float targetAngle, Mode sourceMode)
    {
        if (!enableAudio || _handleAudioSource == null)
            return;

        AudioClip clip = null;
        if (Mathf.Approximately(targetAngle, handleAngleTurn))
            clip = handleTurn90Sound;
        else if (Mathf.Approximately(targetAngle, handleAngleTilt) || Mathf.Approximately(targetAngle, handleAngleSlide))
            clip = handleTurn180Sound;
        else if (Mathf.Approximately(targetAngle, handleAngleClosed))
            clip = sourceMode == Mode.TurnOpen ? handleTurn90Sound : handleTurn180Sound;

        if (clip != null)
            _handleAudioSource.PlayOneShot(clip, handleVolume);
    }

    private void PlayFrameClip(AudioClip clip)
    {
        if (!enableAudio || _frameAudioSource == null || clip == null)
            return;

        _frameAudioSource.PlayOneShot(clip, frameVolume);
    }

    private IEnumerator RotateTo(Transform target, Quaternion targetLocal)
    {
        if (!target) yield break;

        while (Quaternion.Angle(target.localRotation, targetLocal) > 0.1f)
        {
            target.localRotation = Quaternion.RotateTowards(target.localRotation, targetLocal, rotateSpeed * Time.deltaTime);
            yield return null;
        }

        target.localRotation = targetLocal;
    }

    private IEnumerator RotateTargetsToClosed(float primarySpeed)
    {
        bool done;

        do
        {
            done = true;
            done &= RotateTowardsTarget(turnPivot, _turnClosedLocalRot, primarySpeed);
            done &= RotateTowardsTarget(tiltPivot, _tiltClosedLocalRot, primarySpeed);
            done &= RotateTowardsTarget(mechanicPivot, _mechanicClosedLocalRot, primarySpeed);

            if (!done)
                yield return null;
        }
        while (!done);
    }

    private IEnumerator RotatePrimaryWithMechanic(
        Transform primaryPivot,
        Quaternion primaryClosedRot,
        Axis primaryAxis,
        float primaryOpenAngle,
        Transform secondaryPivot,
        Quaternion secondaryClosedRot,
        Axis secondaryAxis,
        float secondaryOpenAngle,
        float primarySpeed,
        float secondarySpeedMultiplier,
        AnimationCurve secondaryCurve)
    {
        if (!primaryPivot)
        {
            if (secondaryPivot)
            {
                Quaternion secondaryTargetOnly = ApplyAxis(secondaryClosedRot, secondaryAxis, secondaryOpenAngle);
                yield return RotateTo(secondaryPivot, secondaryTargetOnly, primarySpeed * secondarySpeedMultiplier);
            }
            yield break;
        }

        Quaternion primaryTarget = ApplyAxis(primaryClosedRot, primaryAxis, primaryOpenAngle);
        float startPrimaryAngle = Mathf.Max(0.0001f, Quaternion.Angle(primaryClosedRot, primaryTarget));
        AnimationCurve curve = secondaryCurve;

        bool primaryDone;
        bool secondaryDone;

        do
        {
            primaryDone = RotateTowardsTarget(primaryPivot, primaryTarget, primarySpeed);

            float primaryProgress = 1f - (Quaternion.Angle(primaryPivot.localRotation, primaryTarget) / startPrimaryAngle);
            primaryProgress = Mathf.Clamp01(primaryProgress);

            secondaryDone = true;
            if (secondaryPivot)
            {
                float curvedProgress = curve != null ? Mathf.Clamp01(curve.Evaluate(primaryProgress)) : primaryProgress;
                Quaternion secondaryTarget = ApplyAxis(secondaryClosedRot, secondaryAxis, secondaryOpenAngle * curvedProgress);
                secondaryDone = RotateTowardsTarget(secondaryPivot, secondaryTarget, primarySpeed * secondarySpeedMultiplier);
            }

            if (!primaryDone || !secondaryDone)
                yield return null;
        }
        while (!primaryDone || !secondaryDone);

        if (secondaryPivot)
        {
            Quaternion finalSecondaryTarget = ApplyAxis(secondaryClosedRot, secondaryAxis, secondaryOpenAngle);
            yield return RotateTo(secondaryPivot, finalSecondaryTarget, primarySpeed * secondarySpeedMultiplier);
        }
    }

    private IEnumerator RotateTo(Transform target, Quaternion targetLocal, float speed)
    {
        if (!target) yield break;

        while (Quaternion.Angle(target.localRotation, targetLocal) > 0.1f)
        {
            target.localRotation = Quaternion.RotateTowards(target.localRotation, targetLocal, speed * Time.deltaTime);
            yield return null;
        }

        target.localRotation = targetLocal;
    }

    private static bool RotateTowardsTarget(Transform target, Quaternion targetLocal, float speed)
    {
        if (!target) return true;

        if (Quaternion.Angle(target.localRotation, targetLocal) <= 0.1f)
        {
            target.localRotation = targetLocal;
            return true;
        }

        target.localRotation = Quaternion.RotateTowards(target.localRotation, targetLocal, speed * Time.deltaTime);
        return Quaternion.Angle(target.localRotation, targetLocal) <= 0.1f;
    }

    private IEnumerator MoveSlideToOpen()
    {
        if (!slidePivot || Mathf.Approximately(slideDistanceCm, 0f))
            yield break;

        Vector3 basePosition = _slideClosedLocalPos;
        if (slidePivot == liftPivot && liftPivot != null)
            basePosition = ApplyAxisOffset(basePosition, liftAxis, Cm(liftDistanceCm));

        Vector3 target = ApplyAxisOffset(basePosition, slideAxis, Cm(slideDistanceCm));
        float speed = Cm(Mathf.Max(0.01f, slideSpeed));
        yield return MoveToWithCurve(slidePivot, target, speed, slideCurve);
    }

    private IEnumerator MoveTo(Transform target, Vector3 targetLocalPos, float unitsPerSecond)
    {
        if (!target) yield break;

        while (Vector3.Distance(target.localPosition, targetLocalPos) > 0.0001f)
        {
            target.localPosition = Vector3.MoveTowards(target.localPosition, targetLocalPos, unitsPerSecond * Time.deltaTime);
            yield return null;
        }

        target.localPosition = targetLocalPos;
    }

    private IEnumerator MoveToWithCurve(Transform target, Vector3 targetLocalPos, float unitsPerSecond, AnimationCurve curve)
    {
        if (!target) yield break;

        Vector3 start = target.localPosition;
        float distance = Vector3.Distance(start, targetLocalPos);
        if (distance <= 0.0001f)
        {
            target.localPosition = targetLocalPos;
            yield break;
        }

        float duration = Mathf.Max(0.0001f, distance / unitsPerSecond);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float curvedT = curve != null ? Mathf.Clamp01(curve.Evaluate(t)) : t;
            target.localPosition = Vector3.LerpUnclamped(start, targetLocalPos, curvedT);
            yield return null;
        }

        target.localPosition = targetLocalPos;
    }

    private static bool MoveTowardsTarget(Transform target, Vector3 targetLocalPos, float unitsPerSecond)
    {
        if (!target) return true;

        if (Vector3.Distance(target.localPosition, targetLocalPos) <= 0.0001f)
        {
            target.localPosition = targetLocalPos;
            return true;
        }

        target.localPosition = Vector3.MoveTowards(target.localPosition, targetLocalPos, unitsPerSecond * Time.deltaTime);
        return Vector3.Distance(target.localPosition, targetLocalPos) <= 0.0001f;
    }

    private static Quaternion ApplyAxis(Quaternion baseRot, Axis axis, float angleDeg)
    {
        Vector3 e = baseRot.eulerAngles;
        switch (axis)
        {
            case Axis.X: e.x = angleDeg; break;
            case Axis.Y: e.y = angleDeg; break;
            case Axis.Z: e.z = angleDeg; break;
        }
        return Quaternion.Euler(e);
    }

    private static Vector3 ApplyAxisOffset(Vector3 basePos, Axis axis, float offset)
    {
        switch (axis)
        {
            case Axis.X: basePos.x += offset; break;
            case Axis.Y: basePos.y += offset; break;
            case Axis.Z: basePos.z += offset; break;
        }

        return basePos;
    }

    private static GameObject CreatePrimitive(string objectName, PrimitiveType primitiveType, Transform parent)
    {
        GameObject go = GameObject.CreatePrimitive(primitiveType);
        go.name = objectName;
        go.transform.SetParent(parent, false);
        return go;
    }

    private static Font LoadBuiltinFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font != null) return font;
        return Resources.GetBuiltinResource<Font>("Arial.ttf");
    }

    private float GetStableTextDepthOffset()
    {
        return Cm(Mathf.Max(textDepthOffsetCm, 0.35f));
    }

    private static Material CreateRuntimeMaterial(Color color, bool transparent)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Diffuse");

        Material material = new Material(shader);
        ConfigureMaterial(material, color, transparent);
        return material;
    }

    private static Material CreateRuntimeTextMaterial(Font font)
    {
        if (font == null || font.material == null)
            return null;

        Material material = new Material(font.material);

        if (material.HasProperty("_Color"))
            material.SetColor("_Color", Color.white);

        if (material.HasProperty("_Cull"))
            material.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);

        if (material.HasProperty("_ZWrite"))
            material.SetFloat("_ZWrite", 0f);

        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + 100;
        return material;
    }

    private static bool IsEffectivelyOpaque(Color color)
    {
        return color.a >= 0.999f;
    }

    private static void ConfigureMaterial(Material material, Color color, bool transparent)
    {
        material.color = color;

        if (!transparent) return;

        if (material.HasProperty("_Surface"))
        {
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
        else if (material.HasProperty("_Mode"))
        {
            material.SetFloat("_Mode", 3f);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
    }

    private static float Cm(float centimeters)
    {
        return centimeters * 0.01f;
    }

    private static Vector3 Cm(Vector3 centimeters)
    {
        return centimeters * 0.01f;
    }

    private static void DestroyMenuObject(Object obj)
    {
        if (obj == null) return;

        if (Application.isPlaying) Destroy(obj);
        else DestroyImmediate(obj);
    }

    private bool CanOpenPopup()
    {
        if (frenchWindowSecondary)
            return HasFrenchPrimaryOpenMarker();

        return true;
    }

    private bool CanTransitionTo(Mode targetMode)
    {
        if (frenchDoorPrimary)
        {
            if (HasFrenchSecondaryOpenMarker() && targetMode != Mode.TurnOpen)
                return false;
        }

        if (frenchWindowSecondary)
        {
            if (targetMode == Mode.TurnOpen)
                return HasFrenchPrimaryOpenMarker();

            if (targetMode == Mode.TiltOpen || targetMode == Mode.SlideOpen)
                return false;
        }

        return true;
    }

    private void SetFrenchMarkerStateForMode(Mode mode)
    {
        if (frenchDoorPrimary)
            SetFrenchMarker(FrenchPrimaryMarkerName, mode == Mode.TurnOpen);

        if (frenchWindowSecondary)
            SetFrenchMarker(FrenchSecondaryMarkerName, mode == Mode.TurnOpen);
    }

    private bool HasFrenchPrimaryOpenMarker()
    {
        return FindFrenchMarker(FrenchPrimaryMarkerName) != null;
    }

    private bool HasFrenchSecondaryOpenMarker()
    {
        return FindFrenchMarker(FrenchSecondaryMarkerName) != null;
    }

    private Transform FindFrenchMarker(string markerName)
    {
        Transform root = transform.root;
        if (root == null) return null;
        return root.Find(markerName);
    }

    private void SetFrenchMarker(string markerName, bool enabled)
    {
        Transform marker = FindFrenchMarker(markerName);

        if (enabled)
        {
            if (marker != null) return;

            GameObject go = new GameObject(markerName);
            go.transform.SetParent(transform.root, false);
            return;
        }

        if (marker != null)
            DestroyMenuObject(marker.gameObject);
    }

    private void NormalizeFrenchFlags()
    {
        if (frenchDoorPrimary && frenchWindowSecondary)
            frenchWindowSecondary = false;
    }

    private bool SupportsTurn()
    {
        return turnPivot != null && !Mathf.Approximately(turnOpenAngle, 0f);
    }

    private bool SupportsTilt()
    {
        return tiltPivot != null && !Mathf.Approximately(tiltOpenAngle, 0f);
    }

    private bool SupportsClose()
    {
        return SupportsTurn() || SupportsTilt() || SupportsSlide();
    }

    private bool SupportsSlide()
    {
        return slidePivot != null && !Mathf.Approximately(slideDistanceCm, 0f);
    }

    private void OnValidate()
    {
        NormalizeFrenchFlags();
    }
}
}
