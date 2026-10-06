#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Track11.EuropeanWindowPackVol01
{
/// <summary>
/// Runntime Script for ColorChooser3D Version 0.1
/// Needs to lay in folder called "Editor" somewhere in breach
/// </summary>

[CustomEditor(typeof(ColorChooser3D))]
public sealed class ColorChooser3DEditor : Editor
{
    private enum DeferredAction
    {
        None,
        RefreshMaterials,
        ReapplyOverrides,
        SaveTxtPreset,
        LoadTxtPreset,
        ResetAllToStock
    }

    private const float ToggleWidth = 18f;
    private const float NameWidth = 78f;
    private const float HexWidth = 95f;
    private const float ColorPickerWidth = 90f;
    private const float ApplyButtonWidth = 92f;
    private const float SliderWidth = 145f;
    private const float NumberFieldWidth = 40f;
    private const float InlineFieldSpacingCompensation = 4f;
    private const float ValueAreaWidth = HexWidth + ColorPickerWidth + InlineFieldSpacingCompensation;

    private bool _showProSettings;
    private DeferredAction _deferredAction = DeferredAction.None;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawScriptField();
        DrawTopLevelSettings();
        DrawMaterialOverrides();

        serializedObject.ApplyModifiedProperties();
        ExecuteDeferredAction();
    }

    private void DrawScriptField()
    {
        using (new EditorGUI.DisabledScope(true))
        {
            MonoScript script = MonoScript.FromMonoBehaviour((ColorChooser3D)target);
            EditorGUILayout.ObjectField("Script", script, typeof(MonoScript), false);
        }
    }

    private void DrawTopLevelSettings()
    {
        SerializedProperty autoCollectProperty = serializedObject.FindProperty("autoCollectOnValidate");
        SerializedProperty includeInactiveProperty = serializedObject.FindProperty("includeInactiveChildren");
        SerializedProperty showVariantsProperty = serializedObject.FindProperty("showVariants");
        _showProSettings = EditorGUILayout.ToggleLeft("Pro Settings", _showProSettings);
        if (_showProSettings)
        {
            EditorGUILayout.BeginVertical("box");

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Refresh Materials List"))
                    QueueDeferredAction(DeferredAction.RefreshMaterials);

                if (GUILayout.Button("Reapply Overrides"))
                    QueueDeferredAction(DeferredAction.ReapplyOverrides);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Save TXT Preset"))
                    QueueDeferredAction(DeferredAction.SaveTxtPreset);

                if (GUILayout.Button("Load TXT Preset"))
                    QueueDeferredAction(DeferredAction.LoadTxtPreset);
            }

            EditorGUILayout.Space(4f);
            if (GUILayout.Button("Reset All To Stock In Scene"))
            {
                bool confirmed = EditorUtility.DisplayDialog(
                    "Reset All To Stock?",
                    "This will reset all ColorChooser3D overrides in the current scene back to their stock values. Do you want to continue?",
                    "Reset All",
                    "Cancel");

                if (confirmed)
                    QueueDeferredAction(DeferredAction.ResetAllToStock);
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Auto Collect Materials", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (autoCollectProperty != null)
                    autoCollectProperty.boolValue = EditorGUILayout.ToggleLeft("On Validate", autoCollectProperty.boolValue, GUILayout.Width(110f));

                if (includeInactiveProperty != null)
                    includeInactiveProperty.boolValue = EditorGUILayout.ToggleLeft("Incl. Inactive Meshes", includeInactiveProperty.boolValue);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(8f);
        }

        EditorGUILayout.LabelField("Override Mode", EditorStyles.boldLabel);
        int modeIndex = showVariantsProperty != null && showVariantsProperty.boolValue ? 1 : 0;
        int newModeIndex = GUILayout.Toolbar(modeIndex, new[] { "Main Materials", "Materials by Variants" });
        if (showVariantsProperty != null && newModeIndex != modeIndex)
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "Switch Override Mode?",
                "Switching between Main Materials and Materials by Variants will rebuild the material entries and reset the current entry values. Do you want to continue?",
                "Switch Mode",
                "Cancel");

            if (confirmed)
                showVariantsProperty.boolValue = newModeIndex == 1;
        }

        EditorGUILayout.Space(6f);
    }

    private void DrawMaterialOverrides()
    {
        SerializedProperty materialOverrides = serializedObject.FindProperty("materialOverrides");
        SerializedProperty showVariantsProperty = serializedObject.FindProperty("showVariants");
        bool showVariants = showVariantsProperty != null && showVariantsProperty.boolValue;
        if (materialOverrides == null)
            return;

        EditorGUILayout.LabelField("Material Overrides", EditorStyles.boldLabel);

        if (!materialOverrides.isArray || materialOverrides.arraySize == 0)
        {
            EditorGUILayout.HelpBox("No material override entries collected yet.", MessageType.Info);
            return;
        }

        for (int i = 0; i < materialOverrides.arraySize; i++)
        {
            SerializedProperty entry = materialOverrides.GetArrayElementAtIndex(i);
            if (entry == null)
                continue;

            SerializedProperty labelProperty = entry.FindPropertyRelative("label");
            string label = labelProperty != null && !string.IsNullOrWhiteSpace(labelProperty.stringValue)
                ? labelProperty.stringValue
                : $"Entry {i}";

            entry.isExpanded = EditorGUILayout.Foldout(entry.isExpanded, label, true);
            if (!entry.isExpanded)
                continue;

            EditorGUILayout.BeginVertical("box");
            DrawProperty(entry, "enabled");

            DrawProperty(entry, "label");
            DrawProperty(entry, "targetRenderer");
            DrawProperty(entry, "materialNameContains");

            EditorGUILayout.Space(4f);
            DrawSeparator();
            EditorGUILayout.LabelField("Overrides:", EditorStyles.boldLabel);
            EditorGUILayout.Space(2f);

            DrawColorOverrideRow(entry, i, "overrideColor", "Color", "colorOverride", "color");
            DrawOverrideRow(entry, i, "overrideAlpha", "Alpha", "alphaOverride", "alpha");
            DrawOverrideRow(entry, i, "overrideMetallic", "Metallic", "metallicOverride", "metallic");
            DrawOverrideRow(entry, i, "overrideSmoothness", "Smoothness", "smoothnessOverride", "smoothness");
            DrawColorOverrideRow(entry, i, "overrideSpecularColor", "Specular", "specularColorOverride", "specular");
            DrawColorOverrideRow(entry, i, "overrideEmissionColor", "Emission", "emissionColorOverride", "emission");
            DrawOverrideRow(entry, i, "overrideBumpScale", "Normal Strength", "bumpScaleOverride", "bump");
            DrawOverrideRow(entry, i, "overrideOcclusionStrength", "Occlusion", "occlusionStrengthOverride", "occlusion");

            EditorGUILayout.Space(6f);
            if (GUILayout.Button("Apply Whole Entry To All", GUILayout.Height(22f)))
                ApplyWholeEntry(i);

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(6f);
        }
    }

    private void DrawOverrideRow(SerializedProperty entry, int entryIndex, string toggleField, string displayName, string valueField, string groupName)
    {
        SerializedProperty toggleProperty = entry.FindPropertyRelative(toggleField);
        SerializedProperty valueProperty = entry.FindPropertyRelative(valueField);
        if (toggleProperty == null || valueProperty == null)
            return;

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.PropertyField(toggleProperty, GUIContent.none, GUILayout.Width(ToggleWidth));
            EditorGUILayout.LabelField(displayName, GUILayout.Width(NameWidth));
            DrawFixedWidthValueField(valueProperty, valueField);

            GUIStyle miniButton = new GUIStyle(GUI.skin.button);
            miniButton.fontSize = 10;
            miniButton.fixedHeight = 18f;
            if (GUILayout.Button("Apply To All", miniButton, GUILayout.Width(ApplyButtonWidth)))
                ApplyGroup(entryIndex, groupName);
        }
    }

    private void DrawColorOverrideRow(SerializedProperty entry, int entryIndex, string toggleField, string displayName, string valueField, string groupName)
    {
        SerializedProperty toggleProperty = entry.FindPropertyRelative(toggleField);
        SerializedProperty valueProperty = entry.FindPropertyRelative(valueField);
        if (toggleProperty == null || valueProperty == null)
            return;

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.PropertyField(toggleProperty, GUIContent.none, GUILayout.Width(ToggleWidth));
            EditorGUILayout.LabelField(displayName, GUILayout.Width(NameWidth));

            Color currentColor = valueProperty.colorValue;
            string hex = "#" + ColorUtility.ToHtmlStringRGBA(currentColor);
            string newHex = EditorGUILayout.TextField(hex, GUILayout.Width(HexWidth));
            if (!string.Equals(newHex, hex, System.StringComparison.Ordinal))
            {
                if (!string.IsNullOrWhiteSpace(newHex))
                {
                    string normalized = newHex.StartsWith("#", System.StringComparison.Ordinal) ? newHex : "#" + newHex;
                    if (ColorUtility.TryParseHtmlString(normalized, out Color parsedColor))
                        valueProperty.colorValue = parsedColor;
                }
            }

            EditorGUILayout.PropertyField(valueProperty, GUIContent.none, GUILayout.Width(ColorPickerWidth));

            GUIStyle miniButton = new GUIStyle(GUI.skin.button);
            miniButton.fontSize = 10;
            miniButton.fixedHeight = 18f;
            if (GUILayout.Button("Apply To All", miniButton, GUILayout.Width(ApplyButtonWidth)))
                ApplyGroup(entryIndex, groupName);
        }
    }

    private void DrawFixedWidthValueField(SerializedProperty valueProperty, string valueField)
    {
        if (valueProperty == null)
            return;

        if (TryGetSliderRange(valueField, out float minValue, out float maxValue))
        {
            float currentValue = valueProperty.floatValue;
            float newValue = EditorGUILayout.Slider(currentValue, minValue, maxValue, GUILayout.Width(SliderWidth));
            valueProperty.floatValue = newValue;
            valueProperty.floatValue = EditorGUILayout.FloatField(valueProperty.floatValue, GUILayout.Width(NumberFieldWidth));
            return;
        }

        if (valueProperty.propertyType == SerializedPropertyType.ObjectReference)
        {
            EditorGUILayout.PropertyField(valueProperty, GUIContent.none, GUILayout.Width(ValueAreaWidth));
            return;
        }

        EditorGUILayout.PropertyField(valueProperty, GUIContent.none, GUILayout.Width(ValueAreaWidth));
    }

    private static bool TryGetSliderRange(string valueField, out float minValue, out float maxValue)
    {
        switch (valueField)
        {
            case "alphaOverride":
            case "metallicOverride":
            case "smoothnessOverride":
            case "occlusionStrengthOverride":
                minValue = 0f;
                maxValue = 1f;
                return true;
            case "bumpScaleOverride":
                minValue = 0f;
                maxValue = 10f;
                return true;
            default:
                minValue = 0f;
                maxValue = 0f;
                return false;
        }
    }

    private static void DrawSeparator()
    {
        Rect rect = EditorGUILayout.GetControlRect(false, 1f);
        EditorGUI.DrawRect(rect, new Color(0.25f, 0.25f, 0.25f, 1f));
    }

    private void DrawProperty(string propertyName)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
            EditorGUILayout.PropertyField(property, true);
    }

    private static void DrawProperty(SerializedProperty parent, string relativeName)
    {
        SerializedProperty property = parent.FindPropertyRelative(relativeName);
        if (property != null)
            EditorGUILayout.PropertyField(property, true);
    }

    private void ApplyGroup(int entryIndex, string groupName)
    {
        if (!ConfirmApply(groupName, false))
            return;

        serializedObject.ApplyModifiedProperties();

        ColorChooser3D chooser = (ColorChooser3D)target;
        chooser.ApplyEntryGroupToAllInScene(entryIndex, groupName);

        serializedObject.Update();
    }

    private void ApplyWholeEntry(int entryIndex)
    {
        if (!ConfirmApply("whole entry", true))
            return;

        serializedObject.ApplyModifiedProperties();

        ColorChooser3D chooser = (ColorChooser3D)target;
        chooser.ApplyWholeEntryToAllInScene(entryIndex);

        serializedObject.Update();
    }

    private void InvokeChooser(System.Action<ColorChooser3D> action)
    {
        serializedObject.ApplyModifiedProperties();
        action?.Invoke((ColorChooser3D)target);
        serializedObject.Update();
    }

    private void QueueDeferredAction(DeferredAction action)
    {
        if (action == DeferredAction.None)
            return;

        _deferredAction = action;
    }

    private void ExecuteDeferredAction()
    {
        if (_deferredAction == DeferredAction.None)
            return;

        DeferredAction actionToRun = _deferredAction;
        _deferredAction = DeferredAction.None;
        ColorChooser3D chooser = (ColorChooser3D)target;

        EditorApplication.delayCall += () =>
        {
            if (chooser == null)
                return;

            switch (actionToRun)
            {
                case DeferredAction.RefreshMaterials:
                    chooser.CollectRendererEntries();
                    break;
                case DeferredAction.ReapplyOverrides:
                    chooser.ApplyOverrides();
                    break;
                case DeferredAction.SaveTxtPreset:
                    chooser.SavePresetToTextFile();
                    break;
                case DeferredAction.LoadTxtPreset:
                    chooser.LoadPresetFromTextFile();
                    break;
                case DeferredAction.ResetAllToStock:
                    chooser.ResetAllToStockInScene();
                    break;
            }

            EditorUtility.SetDirty(chooser);
        };
    }

    private static bool ConfirmApply(string targetLabel, bool wholeEntry)
    {
        string title = wholeEntry ? "Apply Whole Entry To All?" : "Apply To All?";
        string message = wholeEntry
            ? "This will overwrite the full material override entry on all other ColorChooser3D components in the current scene. Do you want to continue?"
            : $"This will overwrite the '{targetLabel}' setting on all other ColorChooser3D components in the current scene. Do you want to continue?";

        return EditorUtility.DisplayDialog(title, message, "Apply", "Cancel");
    }
}
}
#endif
