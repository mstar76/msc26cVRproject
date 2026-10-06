using UnityEngine;

namespace Track11.EuropeanWindowPackVol01
{
/// <summary>
/// Simple dependency-free tooltip trigger based on a SphereCollider.
/// Creates a TextMesh at runtime and fades it in while the player stays inside the trigger.
/// </summary>
[RequireComponent(typeof(SphereCollider))]
public sealed class TooltipTrigger3D : MonoBehaviour
{
    [Header("Text")]
    [TextArea]
    [SerializeField] private string tooltipText = "Info";
    [SerializeField] private int fontSize = 24;
    [SerializeField] private float characterSize = 0.025f;
    [SerializeField] private Color textColor = new Color32(0, 255, 0, 255);
    [SerializeField] private TextAnchor textAnchor = TextAnchor.MiddleCenter;
    [SerializeField] private TextAlignment textAlignment = TextAlignment.Left;

    [Header("Visibility")]
    [SerializeField][Range(0f, 1f)] private float minimumVisibility = 0.15f;
    [SerializeField][Range(0f, 1f)] private float maximumVisibility = 1f;
    [SerializeField] private float fadeSpeed = 4f;

    [Header("Placement")]
    [SerializeField] private Vector3 localOffset = Vector3.zero;
    [SerializeField] private bool faceCamera = true;
    [SerializeField] private bool onlyShowInsideTrigger = true;

    [Header("Connector Line")]
    [SerializeField] private bool showConnectorLine = false;
    [SerializeField] private Color lineColor = Color.white;
    [SerializeField] private float lineWidth = 0.01f;

    [Header("Detection")]
    [SerializeField] private string playerTag = "Player";

    private SphereCollider _sphereCollider;
    private TextMesh _textMesh;
    private Transform _textTransform;
    private LineRenderer _lineRenderer;
    private Color _currentColor;
    private bool _playerInside;
    private Font _runtimeFont;

    private void Reset()
    {
        _sphereCollider = GetComponent<SphereCollider>();
        if (_sphereCollider != null)
            _sphereCollider.isTrigger = true;
    }

    private void Awake()
    {
        _sphereCollider = GetComponent<SphereCollider>();
        _sphereCollider.isTrigger = true;

        _runtimeFont = LoadBuiltinFont();
        CreateTextObject();
        SetVisibilityInstant(onlyShowInsideTrigger ? 0f : minimumVisibility);
    }

    private void OnValidate()
    {
        minimumVisibility = Mathf.Clamp01(minimumVisibility);
        maximumVisibility = Mathf.Clamp(maximumVisibility, minimumVisibility, 1f);
        fadeSpeed = Mathf.Max(0f, fadeSpeed);
        lineWidth = Mathf.Max(0.0001f, lineWidth);

        if (_sphereCollider == null)
            _sphereCollider = GetComponent<SphereCollider>();

        if (_sphereCollider != null)
            _sphereCollider.isTrigger = true;
    }

    private void Update()
    {
        if (_textTransform == null || _textMesh == null) return;

        _textTransform.position = transform.position + localOffset;

        if (faceCamera && Camera.main != null)
        {
            Vector3 toCamera = _textTransform.position - Camera.main.transform.position;
            if (toCamera.sqrMagnitude > 0.0001f)
                _textTransform.rotation = Quaternion.LookRotation(toCamera.normalized, Vector3.up);
        }

        float targetAlpha = _playerInside ? maximumVisibility : (onlyShowInsideTrigger ? 0f : minimumVisibility);
        float currentAlpha = Mathf.MoveTowards(_currentColor.a, targetAlpha, fadeSpeed * Time.deltaTime);
        _currentColor.a = currentAlpha;
        _textMesh.color = _currentColor;

        UpdateConnectorLine(currentAlpha);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsPlayer(other))
            _playerInside = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsPlayer(other))
            _playerInside = false;
    }

    private bool IsPlayer(Collider other)
    {
        if (other == null) return false;
        if (!string.IsNullOrWhiteSpace(playerTag) && other.CompareTag(playerTag)) return true;
        return other.GetComponentInParent<CharacterController>() != null;
    }

    private void CreateTextObject()
    {
        GameObject go = new GameObject("TooltipText");
        go.transform.SetParent(transform, false);
        _textTransform = go.transform;
        _textTransform.localPosition = localOffset;

        _textMesh = go.AddComponent<TextMesh>();
        _textMesh.text = tooltipText;
        _textMesh.anchor = textAnchor;
        _textMesh.alignment = textAlignment;
        _textMesh.fontSize = fontSize;
        _textMesh.characterSize = characterSize;
        _textMesh.font = _runtimeFont;

        MeshRenderer renderer = go.GetComponent<MeshRenderer>();
        if (renderer != null && _runtimeFont != null && _runtimeFont.material != null)
            renderer.sharedMaterial = _runtimeFont.material;

        _currentColor = textColor;
        _textMesh.color = _currentColor;

        CreateConnectorLine(go);
    }

    private void SetVisibilityInstant(float alpha)
    {
        if (_textMesh == null) return;
        _currentColor = textColor;
        _currentColor.a = Mathf.Clamp01(alpha);
        _textMesh.color = _currentColor;
        UpdateConnectorLine(_currentColor.a);
    }

    private static Font LoadBuiltinFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font != null) return font;
        return Resources.GetBuiltinResource<Font>("Arial.ttf");
    }

    private void CreateConnectorLine(GameObject parentObject)
    {
        if (!showConnectorLine) return;

        _lineRenderer = parentObject.AddComponent<LineRenderer>();
        _lineRenderer.useWorldSpace = true;
        _lineRenderer.positionCount = 2;
        _lineRenderer.startWidth = lineWidth;
        _lineRenderer.endWidth = lineWidth;
        _lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _lineRenderer.receiveShadows = false;
        _lineRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        _lineRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");

        if (shader != null)
            _lineRenderer.material = new Material(shader);
    }

    private void UpdateConnectorLine(float alpha)
    {
        if (_lineRenderer == null) return;

        _lineRenderer.enabled = showConnectorLine && alpha > 0.001f;
        if (!_lineRenderer.enabled) return;

        _lineRenderer.startWidth = lineWidth;
        _lineRenderer.endWidth = lineWidth;
        _lineRenderer.SetPosition(0, transform.position);
        _lineRenderer.SetPosition(1, _textTransform.position);

        Color currentLineColor = lineColor;
        currentLineColor.a *= alpha;
        _lineRenderer.startColor = currentLineColor;
        _lineRenderer.endColor = currentLineColor;
    }
}
}
