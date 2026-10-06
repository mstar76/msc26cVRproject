using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Track11.EuropeanWindowPackVol01
{
/// <summary>
/// Applies per-material overrides via MaterialPropertyBlock without creating material instances.
/// Attach to the FBX root. Renderer entries can be auto-collected in Edit Mode and then tuned in the Inspector.
/// </summary>
[ExecuteAlways]
public sealed class ColorChooser3D : MonoBehaviour
{
    [Serializable]
    private sealed class PresetData
    {
        public bool autoCollectOnValidate = true;
        public bool includeInactiveChildren = true;
        public bool showVariants = false;
        public List<PresetEntryData> materialOverrides = new List<PresetEntryData>();
    }

    [Serializable]
    private sealed class PresetEntryData
    {
        public string label = "Entry";
        public string targetRendererPath = string.Empty;
        public int materialIndex = 0;
        public string materialNameContains = string.Empty;
        public bool applyToAllMatchingRenderers = true;
        public bool overrideColor = false;
        public Color colorOverride = Color.white;
        public bool overrideAlpha = false;
        public float alphaOverride = 1f;
        public bool overrideMetallic = false;
        public float metallicOverride = 0f;
        public bool overrideSmoothness = false;
        public float smoothnessOverride = 0.5f;
        public bool overrideSpecularColor = false;
        public Color specularColorOverride = Color.white;
        public bool overrideEmissionColor = false;
        public Color emissionColorOverride = Color.black;
        public bool overrideBumpScale = false;
        public float bumpScaleOverride = 1f;
        public bool overrideOcclusionStrength = false;
        public float occlusionStrengthOverride = 1f;
        public bool enabled = true;
    }

    [Serializable]
    private sealed class MaterialOverrideEntry
    {
        public string label = "Entry";
        public Renderer targetRenderer;
        public int materialIndex = 0;
        public string materialNameContains = "Alu_EV1";
        public bool applyToAllMatchingRenderers = true;
        public bool overrideColor = false;
        public Color colorOverride = Color.white;
        public bool overrideAlpha = false;
        [Range(0f, 1f)] public float alphaOverride = 1f;
        public bool overrideMetallic = false;
        [Range(0f, 1f)] public float metallicOverride = 0f;
        public bool overrideSmoothness = false;
        [Range(0f, 1f)] public float smoothnessOverride = 0.5f;
        public bool overrideSpecularColor = false;
        public Color specularColorOverride = Color.white;
        public bool overrideEmissionColor = false;
        public Color emissionColorOverride = Color.black;
        public bool overrideBumpScale = false;
        [Range(0f, 10f)] public float bumpScaleOverride = 1f;
        public bool overrideOcclusionStrength = false;
        [Range(0f, 1f)] public float occlusionStrengthOverride = 1f;
        public bool enabled = true;
    }

    private struct RendererSlot
    {
        public Renderer renderer;
        public int materialIndex;

        public RendererSlot(Renderer renderer, int materialIndex)
        {
            this.renderer = renderer;
            this.materialIndex = materialIndex;
        }
    }

    [Header("Auto Collect")]
    [SerializeField] private bool autoCollectOnValidate = true;
    [SerializeField] private bool includeInactiveChildren = true;
    [SerializeField] private bool showVariants = false;

    [Header("Material Overrides")]
    [SerializeField] private List<MaterialOverrideEntry> materialOverrides = new List<MaterialOverrideEntry>();

    private readonly List<RendererSlot> _controlledRendererSlots = new List<RendererSlot>();
    private MaterialPropertyBlock _propertyBlock;
    [SerializeField, HideInInspector] private bool _lastCollectedShowVariants;

    private void OnEnable()
    {
        bool modeChanged = HandleModeSwitchIfNeeded();

        if (autoCollectOnValidate && (modeChanged || materialOverrides == null || materialOverrides.Count == 0))
            CollectRendererEntries();

        ApplyOverrides();
    }

    private void OnDisable()
    {
        ClearControlledOverrides();
    }

    private void OnValidate()
    {
        bool modeChanged = HandleModeSwitchIfNeeded();

        if (autoCollectOnValidate && (modeChanged || materialOverrides == null || materialOverrides.Count == 0))
            CollectRendererEntries();

        ApplyOverrides();
    }

    [ContextMenu("Collect Renderer Entries")]
    public void CollectRendererEntries()
    {
        Dictionary<string, MaterialOverrideEntry> existingEntries = new Dictionary<string, MaterialOverrideEntry>(StringComparer.Ordinal);

        for (int i = 0; i < materialOverrides.Count; i++)
        {
            MaterialOverrideEntry entry = materialOverrides[i];
            if (entry == null)
                continue;

            if (showVariants && entry.targetRenderer == null)
                continue;

            string key = BuildEntryKey(entry);
            if (string.IsNullOrEmpty(key))
                continue;

            if (!existingEntries.ContainsKey(key))
                existingEntries.Add(key, entry);
        }

        List<MaterialOverrideEntry> collectedEntries = new List<MaterialOverrideEntry>();
        Renderer[] renderers = GetComponentsInChildren<Renderer>(includeInactiveChildren);

        for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
        {
            Renderer renderer = renderers[rendererIndex];
            if (renderer == null)
                continue;

            Material[] sharedMaterials = renderer.sharedMaterials;
            if (sharedMaterials == null || sharedMaterials.Length == 0)
                continue;

            for (int materialIndex = 0; materialIndex < sharedMaterials.Length; materialIndex++)
            {
                Material sharedMaterial = sharedMaterials[materialIndex];
                if (sharedMaterial == null)
                    continue;

                string materialName = sharedMaterial.name;
                string key = showVariants
                    ? BuildVariantEntryKey(renderer, materialIndex)
                    : BuildMaterialEntryKey(materialName);

                if (existingEntries.TryGetValue(key, out MaterialOverrideEntry existingEntry))
                {
                    existingEntry.label = showVariants
                        ? BuildVariantLabel(renderer, materialIndex, sharedMaterial)
                        : BuildMaterialLabel(sharedMaterial);
                    existingEntry.materialNameContains = materialName;
                    existingEntry.materialIndex = materialIndex;
                    existingEntry.targetRenderer = showVariants ? renderer : null;
                    existingEntry.applyToAllMatchingRenderers = !showVariants;
                    collectedEntries.Add(existingEntry);
                    continue;
                }

                if (!showVariants && ContainsMaterialEntry(collectedEntries, materialName))
                    continue;

                collectedEntries.Add(new MaterialOverrideEntry
                {
                    label = showVariants
                        ? BuildVariantLabel(renderer, materialIndex, sharedMaterial)
                        : BuildMaterialLabel(sharedMaterial),
                    targetRenderer = showVariants ? renderer : null,
                    materialIndex = materialIndex,
                    materialNameContains = materialName,
                    applyToAllMatchingRenderers = !showVariants
                });
            }
        }

        materialOverrides = collectedEntries;
        _lastCollectedShowVariants = showVariants;
    }

    [ContextMenu("Apply Overrides")]
    public void ApplyOverrides()
    {
        EnsurePropertyBlock();
        ClearControlledOverrides();

        if (materialOverrides == null || materialOverrides.Count == 0)
            return;

        for (int i = 0; i < materialOverrides.Count; i++)
        {
            MaterialOverrideEntry entry = materialOverrides[i];
            if (entry == null || !entry.enabled)
                continue;

            if (!showVariants)
            {
                ApplyToAllMatchingRenderers(entry);
                continue;
            }

            if (!IsRendererEntryUsable(entry, out Material sharedMaterial))
                continue;

            _propertyBlock.Clear();
            ApplyPropertyOverrides(entry, sharedMaterial, _propertyBlock);
            entry.targetRenderer.SetPropertyBlock(_propertyBlock, entry.materialIndex);
            _controlledRendererSlots.Add(new RendererSlot(entry.targetRenderer, entry.materialIndex));
        }
    }

    private void ApplyToAllMatchingRenderers(MaterialOverrideEntry entry)
    {
        if (entry == null || string.IsNullOrWhiteSpace(entry.materialNameContains))
            return;

        Renderer[] renderers = GetComponentsInChildren<Renderer>(includeInactiveChildren);
        for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
        {
            Renderer renderer = renderers[rendererIndex];
            if (renderer == null)
                continue;

            Material[] sharedMaterials = renderer.sharedMaterials;
            if (sharedMaterials == null || sharedMaterials.Length == 0)
                continue;

            for (int materialIndex = 0; materialIndex < sharedMaterials.Length; materialIndex++)
            {
                Material sharedMaterial = sharedMaterials[materialIndex];
                if (sharedMaterial == null)
                    continue;

                if (sharedMaterial.name.IndexOf(entry.materialNameContains, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                _propertyBlock.Clear();
                ApplyPropertyOverrides(entry, sharedMaterial, _propertyBlock);
                renderer.SetPropertyBlock(_propertyBlock, materialIndex);
                _controlledRendererSlots.Add(new RendererSlot(renderer, materialIndex));
            }
        }
    }

    private void EnsurePropertyBlock()
    {
        if (_propertyBlock == null)
            _propertyBlock = new MaterialPropertyBlock();
    }

    private void ClearControlledOverrides()
    {
        if (_controlledRendererSlots.Count == 0)
            return;

        EnsurePropertyBlock();
        _propertyBlock.Clear();

        for (int i = 0; i < _controlledRendererSlots.Count; i++)
        {
            RendererSlot slot = _controlledRendererSlots[i];
            if (slot.renderer == null)
                continue;

            slot.renderer.SetPropertyBlock(_propertyBlock, slot.materialIndex);
        }

        _controlledRendererSlots.Clear();
    }

    private static void ApplyPropertyOverrides(MaterialOverrideEntry entry, Material sharedMaterial, MaterialPropertyBlock block)
    {
        if (entry == null || block == null || sharedMaterial == null)
            return;

        Color baseColor = GetFirstAvailableColor(sharedMaterial, Color.white, "_BaseColor", "_Color");
        Color finalColor = entry.overrideColor ? entry.colorOverride : baseColor;

        if (entry.overrideAlpha)
            finalColor.a = entry.alphaOverride;

        if (entry.overrideColor)
            SetColorIfPresent(sharedMaterial, block, finalColor, "_BaseColor", "_Color");

        if (!entry.overrideColor && entry.overrideAlpha)
            SetColorIfPresent(sharedMaterial, block, finalColor, "_BaseColor", "_Color");

        if (entry.overrideMetallic)
            SetFloatIfPresent(sharedMaterial, block, entry.metallicOverride, "_Metallic");

        if (entry.overrideSmoothness)
            SetFloatIfPresent(sharedMaterial, block, entry.smoothnessOverride, "_Smoothness", "_Glossiness");

        if (entry.overrideSpecularColor)
            SetColorIfPresent(sharedMaterial, block, entry.specularColorOverride, "_SpecColor", "_SpecularColor");

        if (entry.overrideEmissionColor)
            SetColorIfPresent(sharedMaterial, block, entry.emissionColorOverride, "_EmissionColor");

        if (entry.overrideBumpScale)
            SetFloatIfPresent(sharedMaterial, block, entry.bumpScaleOverride, "_BumpScale");

        if (entry.overrideOcclusionStrength)
            SetFloatIfPresent(sharedMaterial, block, entry.occlusionStrengthOverride, "_OcclusionStrength");
    }

    private static void SetColorIfPresent(Material sharedMaterial, MaterialPropertyBlock block, Color value, params string[] propertyNames)
    {
        if (sharedMaterial == null || block == null || propertyNames == null)
            return;

        for (int i = 0; i < propertyNames.Length; i++)
        {
            string propertyName = propertyNames[i];
            if (!string.IsNullOrWhiteSpace(propertyName) && sharedMaterial.HasProperty(propertyName))
                block.SetColor(propertyName, value);
        }
    }

    private static void SetFloatIfPresent(Material sharedMaterial, MaterialPropertyBlock block, float value, params string[] propertyNames)
    {
        if (sharedMaterial == null || block == null || propertyNames == null)
            return;

        for (int i = 0; i < propertyNames.Length; i++)
        {
            string propertyName = propertyNames[i];
            if (!string.IsNullOrWhiteSpace(propertyName) && sharedMaterial.HasProperty(propertyName))
                block.SetFloat(propertyName, value);
        }
    }

    private static Color GetFirstAvailableColor(Material sharedMaterial, Color fallback, params string[] propertyNames)
    {
        if (sharedMaterial == null || propertyNames == null)
            return fallback;

        for (int i = 0; i < propertyNames.Length; i++)
        {
            string propertyName = propertyNames[i];
            if (!string.IsNullOrWhiteSpace(propertyName) && sharedMaterial.HasProperty(propertyName))
                return sharedMaterial.GetColor(propertyName);
        }

        return fallback;
    }

    private bool IsRendererEntryUsable(MaterialOverrideEntry entry, out Material sharedMaterial)
    {
        sharedMaterial = null;

        if (entry == null || entry.targetRenderer == null)
            return false;

        Material[] sharedMaterials = entry.targetRenderer.sharedMaterials;
        if (sharedMaterials == null || entry.materialIndex < 0 || entry.materialIndex >= sharedMaterials.Length)
            return false;

        sharedMaterial = sharedMaterials[entry.materialIndex];
        if (sharedMaterial == null)
            return false;

        if (string.IsNullOrWhiteSpace(entry.materialNameContains))
            return false;

        return sharedMaterial.name.IndexOf(entry.materialNameContains, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool ContainsMaterialEntry(List<MaterialOverrideEntry> entries, string materialName)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            MaterialOverrideEntry entry = entries[i];
            if (entry == null)
                continue;

            if (string.Equals(entry.materialNameContains, materialName, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static string BuildEntryKey(MaterialOverrideEntry entry)
    {
        if (entry == null)
            return string.Empty;

        return entry.applyToAllMatchingRenderers
            ? BuildMaterialEntryKey(entry.materialNameContains)
            : BuildVariantEntryKey(entry.targetRenderer, entry.materialIndex);
    }

    private static string BuildMaterialEntryKey(string materialName)
    {
        return $"material|{materialName}";
    }

    private static string BuildVariantEntryKey(Renderer renderer, int materialIndex)
    {
        if (renderer == null)
            return string.Empty;

        return $"{GetTransformPath(renderer.transform)}|{materialIndex}";
    }

    private static string BuildMaterialLabel(Material material)
    {
        string materialName = material != null ? material.name : "Material";
        return $"Material: {materialName}";
    }

    private static string BuildVariantLabel(Renderer renderer, int materialIndex, Material material)
    {
        string materialName = material != null ? material.name : "Material";
        string meshName = renderer != null ? renderer.name : "Mesh";
        return $"Material:{materialName} @ Mesh:{meshName} Slot [{materialIndex}]";
    }

    private static string GetTransformPath(Transform target)
    {
        if (target == null)
            return string.Empty;

        List<string> names = new List<string>();
        Transform current = target;

        while (current != null)
        {
            names.Add(current.name);
            current = current.parent;
        }

        names.Reverse();
        return string.Join("/", names);
    }

    private bool HandleModeSwitchIfNeeded()
    {
        if (_lastCollectedShowVariants == showVariants)
            return false;

        materialOverrides = new List<MaterialOverrideEntry>();
        _lastCollectedShowVariants = showVariants;
        return true;
    }

#if UNITY_EDITOR
    public void SavePresetToTextFile()
    {
        string defaultName = $"{name}_ColorChooserPreset.txt";
        string path = EditorUtility.SaveFilePanel("Save ColorChooser3D Preset", Application.dataPath, defaultName, "txt");
        if (string.IsNullOrWhiteSpace(path))
            return;

        PresetData presetData = BuildPresetData();
        string json = JsonUtility.ToJson(presetData, true);
        System.IO.File.WriteAllText(path, json);
        AssetDatabase.Refresh();
    }

    public void LoadPresetFromTextFile()
    {
        string path = EditorUtility.OpenFilePanel("Load ColorChooser3D Preset", Application.dataPath, "txt");
        if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path))
            return;

        string json = System.IO.File.ReadAllText(path);
        if (string.IsNullOrWhiteSpace(json))
            return;

        PresetData presetData = JsonUtility.FromJson<PresetData>(json);
        if (presetData == null)
            return;

        Undo.RecordObject(this, "Load ColorChooser3D Preset");
        ApplyPresetData(presetData);
        ApplyOverrides();
        EditorUtility.SetDirty(this);
    }

    public void ApplyEntryFieldToAllInScene(int sourceEntryIndex, string fieldName)
    {
        if (materialOverrides == null || sourceEntryIndex < 0 || sourceEntryIndex >= materialOverrides.Count)
            return;

        MaterialOverrideEntry sourceEntry = materialOverrides[sourceEntryIndex];
        if (sourceEntry == null)
            return;

        ColorChooser3D[] allChoosers = FindObjectsByType<ColorChooser3D>(FindObjectsSortMode.None);
        for (int i = 0; i < allChoosers.Length; i++)
        {
            ColorChooser3D chooser = allChoosers[i];
            if (chooser == null)
                continue;

            Undo.RecordObject(chooser, $"Apply {fieldName} To All ColorChooser3D");
            chooser.ApplyFieldFromSourceEntry(sourceEntry, fieldName);
            chooser.ApplyOverrides();
            EditorUtility.SetDirty(chooser);
        }
    }

    public void ApplyEntryGroupToAllInScene(int sourceEntryIndex, string groupName)
    {
        if (materialOverrides == null || sourceEntryIndex < 0 || sourceEntryIndex >= materialOverrides.Count)
            return;

        switch (groupName)
        {
            case "enabled":
                ApplyEntryFieldToAllInScene(sourceEntryIndex, "enabled");
                break;
            case "color":
                ApplyEntryFieldToAllInScene(sourceEntryIndex, "overrideColor");
                ApplyEntryFieldToAllInScene(sourceEntryIndex, "colorOverride");
                break;
            case "alpha":
                ApplyEntryFieldToAllInScene(sourceEntryIndex, "overrideAlpha");
                ApplyEntryFieldToAllInScene(sourceEntryIndex, "alphaOverride");
                break;
            case "metallic":
                ApplyEntryFieldToAllInScene(sourceEntryIndex, "overrideMetallic");
                ApplyEntryFieldToAllInScene(sourceEntryIndex, "metallicOverride");
                break;
            case "smoothness":
                ApplyEntryFieldToAllInScene(sourceEntryIndex, "overrideSmoothness");
                ApplyEntryFieldToAllInScene(sourceEntryIndex, "smoothnessOverride");
                break;
            case "specular":
                ApplyEntryFieldToAllInScene(sourceEntryIndex, "overrideSpecularColor");
                ApplyEntryFieldToAllInScene(sourceEntryIndex, "specularColorOverride");
                break;
            case "emission":
                ApplyEntryFieldToAllInScene(sourceEntryIndex, "overrideEmissionColor");
                ApplyEntryFieldToAllInScene(sourceEntryIndex, "emissionColorOverride");
                break;
            case "bump":
                ApplyEntryFieldToAllInScene(sourceEntryIndex, "overrideBumpScale");
                ApplyEntryFieldToAllInScene(sourceEntryIndex, "bumpScaleOverride");
                break;
            case "occlusion":
                ApplyEntryFieldToAllInScene(sourceEntryIndex, "overrideOcclusionStrength");
                ApplyEntryFieldToAllInScene(sourceEntryIndex, "occlusionStrengthOverride");
                break;
        }
    }

    public void ApplyWholeEntryToAllInScene(int sourceEntryIndex)
    {
        if (materialOverrides == null || sourceEntryIndex < 0 || sourceEntryIndex >= materialOverrides.Count)
            return;

        MaterialOverrideEntry sourceEntry = materialOverrides[sourceEntryIndex];
        if (sourceEntry == null)
            return;

        ColorChooser3D[] allChoosers = FindObjectsByType<ColorChooser3D>(FindObjectsSortMode.None);
        for (int i = 0; i < allChoosers.Length; i++)
        {
            ColorChooser3D chooser = allChoosers[i];
            if (chooser == null)
                continue;

            Undo.RecordObject(chooser, "Apply Whole Entry To All ColorChooser3D");
            chooser.ApplyWholeEntryFromSource(sourceEntry);
            chooser.ApplyOverrides();
            EditorUtility.SetDirty(chooser);
        }
    }

    public void ResetAllToStockInScene()
    {
        ColorChooser3D[] allChoosers = FindObjectsByType<ColorChooser3D>(FindObjectsSortMode.None);
        for (int i = 0; i < allChoosers.Length; i++)
        {
            ColorChooser3D chooser = allChoosers[i];
            if (chooser == null)
                continue;

            Undo.RecordObject(chooser, "Reset All ColorChooser3D To Stock");
            chooser.ResetToStock();
            chooser.ApplyOverrides();
            EditorUtility.SetDirty(chooser);
        }
    }

    public void ResetToStock()
    {
        if (materialOverrides == null)
            materialOverrides = new List<MaterialOverrideEntry>();

        for (int i = 0; i < materialOverrides.Count; i++)
        {
            MaterialOverrideEntry entry = materialOverrides[i];
            if (entry == null)
                continue;

            entry.enabled = true;
            entry.applyToAllMatchingRenderers = !showVariants;
            entry.overrideColor = false;
            entry.colorOverride = Color.white;
            entry.overrideAlpha = false;
            entry.alphaOverride = 1f;
            entry.overrideMetallic = false;
            entry.metallicOverride = 0f;
            entry.overrideSmoothness = false;
            entry.smoothnessOverride = 0.5f;
            entry.overrideSpecularColor = false;
            entry.specularColorOverride = Color.white;
            entry.overrideEmissionColor = false;
            entry.emissionColorOverride = Color.black;
            entry.overrideBumpScale = false;
            entry.bumpScaleOverride = 1f;
            entry.overrideOcclusionStrength = false;
            entry.occlusionStrengthOverride = 1f;
        }
    }

    private PresetData BuildPresetData()
    {
        PresetData presetData = new PresetData
        {
            autoCollectOnValidate = autoCollectOnValidate,
            includeInactiveChildren = includeInactiveChildren,
            showVariants = showVariants,
            materialOverrides = new List<PresetEntryData>()
        };

        if (materialOverrides == null)
            return presetData;

        for (int i = 0; i < materialOverrides.Count; i++)
        {
            MaterialOverrideEntry entry = materialOverrides[i];
            if (entry == null)
                continue;

            presetData.materialOverrides.Add(new PresetEntryData
            {
                label = entry.label,
                targetRendererPath = GetRelativeRendererPath(entry.targetRenderer),
                materialIndex = entry.materialIndex,
                materialNameContains = entry.materialNameContains,
                applyToAllMatchingRenderers = entry.applyToAllMatchingRenderers,
                overrideColor = entry.overrideColor,
                colorOverride = entry.colorOverride,
                overrideAlpha = entry.overrideAlpha,
                alphaOverride = entry.alphaOverride,
                overrideMetallic = entry.overrideMetallic,
                metallicOverride = entry.metallicOverride,
                overrideSmoothness = entry.overrideSmoothness,
                smoothnessOverride = entry.smoothnessOverride,
                overrideSpecularColor = entry.overrideSpecularColor,
                specularColorOverride = entry.specularColorOverride,
                overrideEmissionColor = entry.overrideEmissionColor,
                emissionColorOverride = entry.emissionColorOverride,
                overrideBumpScale = entry.overrideBumpScale,
                bumpScaleOverride = entry.bumpScaleOverride,
                overrideOcclusionStrength = entry.overrideOcclusionStrength,
                occlusionStrengthOverride = entry.occlusionStrengthOverride,
                enabled = entry.enabled
            });
        }

        return presetData;
    }

    private void ApplyPresetData(PresetData presetData)
    {
        if (presetData == null)
            return;

        autoCollectOnValidate = presetData.autoCollectOnValidate;
        includeInactiveChildren = presetData.includeInactiveChildren;
        showVariants = presetData.showVariants;
        materialOverrides = new List<MaterialOverrideEntry>();

        if (presetData.materialOverrides != null)
        {
            for (int i = 0; i < presetData.materialOverrides.Count; i++)
            {
                PresetEntryData presetEntry = presetData.materialOverrides[i];
                if (presetEntry == null)
                    continue;

                materialOverrides.Add(new MaterialOverrideEntry
                {
                    label = presetEntry.label,
                    targetRenderer = ResolveRendererFromPath(presetEntry.targetRendererPath),
                    materialIndex = presetEntry.materialIndex,
                    materialNameContains = presetEntry.materialNameContains,
                    applyToAllMatchingRenderers = presetEntry.applyToAllMatchingRenderers,
                    overrideColor = presetEntry.overrideColor,
                    colorOverride = presetEntry.colorOverride,
                    overrideAlpha = presetEntry.overrideAlpha,
                    alphaOverride = presetEntry.alphaOverride,
                    overrideMetallic = presetEntry.overrideMetallic,
                    metallicOverride = presetEntry.metallicOverride,
                    overrideSmoothness = presetEntry.overrideSmoothness,
                    smoothnessOverride = presetEntry.smoothnessOverride,
                    overrideSpecularColor = presetEntry.overrideSpecularColor,
                    specularColorOverride = presetEntry.specularColorOverride,
                    overrideEmissionColor = presetEntry.overrideEmissionColor,
                    emissionColorOverride = presetEntry.emissionColorOverride,
                    overrideBumpScale = presetEntry.overrideBumpScale,
                    bumpScaleOverride = presetEntry.bumpScaleOverride,
                    overrideOcclusionStrength = presetEntry.overrideOcclusionStrength,
                    occlusionStrengthOverride = presetEntry.occlusionStrengthOverride,
                    enabled = presetEntry.enabled
                });
            }
        }

        _lastCollectedShowVariants = showVariants;
    }

    private string GetRelativeRendererPath(Renderer renderer)
    {
        return renderer == null ? string.Empty : GetRelativeTransformPath(renderer.transform);
    }

    private string GetRelativeTransformPath(Transform target)
    {
        if (target == null)
            return string.Empty;

        List<string> names = new List<string>();
        Transform current = target;

        while (current != null && current != transform)
        {
            names.Add(current.name);
            current = current.parent;
        }

        if (current != transform)
            return string.Empty;

        names.Reverse();
        return string.Join("/", names);
    }

    private Renderer ResolveRendererFromPath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return null;

        Transform target = transform.Find(relativePath);
        return target != null ? target.GetComponent<Renderer>() : null;
    }

#endif

    private void ApplyFieldFromSourceEntry(MaterialOverrideEntry sourceEntry, string fieldName)
    {
        if (sourceEntry == null)
            return;

        if (materialOverrides == null)
            materialOverrides = new List<MaterialOverrideEntry>();

        bool anyMatched = false;

        for (int i = 0; i < materialOverrides.Count; i++)
        {
            MaterialOverrideEntry targetEntry = materialOverrides[i];
            if (!EntriesMatch(sourceEntry, targetEntry))
                continue;

            CopySingleField(sourceEntry, targetEntry, fieldName);
            anyMatched = true;
        }

        if (!anyMatched && sourceEntry.applyToAllMatchingRenderers)
        {
            MaterialOverrideEntry newEntry = CreateMaterialEntryFromSource(sourceEntry);
            CopySingleField(sourceEntry, newEntry, fieldName);
            materialOverrides.Add(newEntry);
        }
    }

    private void ApplyWholeEntryFromSource(MaterialOverrideEntry sourceEntry)
    {
        if (sourceEntry == null)
            return;

        if (materialOverrides == null)
            materialOverrides = new List<MaterialOverrideEntry>();

        for (int i = 0; i < materialOverrides.Count; i++)
        {
            MaterialOverrideEntry targetEntry = materialOverrides[i];
            if (!EntriesMatch(sourceEntry, targetEntry))
                continue;

            CopyWholeEntry(sourceEntry, targetEntry);
            return;
        }

        MaterialOverrideEntry newEntry = sourceEntry.applyToAllMatchingRenderers
            ? CreateMaterialEntryFromSource(sourceEntry)
            : CreateVariantEntryFromSource(sourceEntry);

        CopyWholeEntry(sourceEntry, newEntry);
        materialOverrides.Add(newEntry);
    }

    private static bool EntriesMatch(MaterialOverrideEntry sourceEntry, MaterialOverrideEntry targetEntry)
    {
        if (sourceEntry == null || targetEntry == null)
            return false;

        if (sourceEntry.applyToAllMatchingRenderers)
        {
            return string.Equals(
                sourceEntry.materialNameContains,
                targetEntry.materialNameContains,
                StringComparison.OrdinalIgnoreCase);
        }

        return string.Equals(sourceEntry.label, targetEntry.label, StringComparison.OrdinalIgnoreCase);
    }

    private static MaterialOverrideEntry CreateMaterialEntryFromSource(MaterialOverrideEntry sourceEntry)
    {
        return new MaterialOverrideEntry
        {
            label = $"Material: {sourceEntry.materialNameContains}",
            materialNameContains = sourceEntry.materialNameContains,
            applyToAllMatchingRenderers = true,
            enabled = true
        };
    }

    private static MaterialOverrideEntry CreateVariantEntryFromSource(MaterialOverrideEntry sourceEntry)
    {
        return new MaterialOverrideEntry
        {
            label = sourceEntry.label,
            targetRenderer = sourceEntry.targetRenderer,
            materialIndex = sourceEntry.materialIndex,
            materialNameContains = sourceEntry.materialNameContains,
            applyToAllMatchingRenderers = false,
            enabled = true
        };
    }

    private static void CopyWholeEntry(MaterialOverrideEntry sourceEntry, MaterialOverrideEntry targetEntry)
    {
        if (sourceEntry == null || targetEntry == null)
            return;

        targetEntry.label = sourceEntry.label;
        targetEntry.targetRenderer = sourceEntry.targetRenderer;
        targetEntry.materialIndex = sourceEntry.materialIndex;
        targetEntry.materialNameContains = sourceEntry.materialNameContains;
        targetEntry.applyToAllMatchingRenderers = sourceEntry.applyToAllMatchingRenderers;
        targetEntry.overrideColor = sourceEntry.overrideColor;
        targetEntry.colorOverride = sourceEntry.colorOverride;
        targetEntry.overrideAlpha = sourceEntry.overrideAlpha;
        targetEntry.alphaOverride = sourceEntry.alphaOverride;
        targetEntry.overrideMetallic = sourceEntry.overrideMetallic;
        targetEntry.metallicOverride = sourceEntry.metallicOverride;
        targetEntry.overrideSmoothness = sourceEntry.overrideSmoothness;
        targetEntry.smoothnessOverride = sourceEntry.smoothnessOverride;
        targetEntry.overrideSpecularColor = sourceEntry.overrideSpecularColor;
        targetEntry.specularColorOverride = sourceEntry.specularColorOverride;
        targetEntry.overrideEmissionColor = sourceEntry.overrideEmissionColor;
        targetEntry.emissionColorOverride = sourceEntry.emissionColorOverride;
        targetEntry.overrideBumpScale = sourceEntry.overrideBumpScale;
        targetEntry.bumpScaleOverride = sourceEntry.bumpScaleOverride;
        targetEntry.overrideOcclusionStrength = sourceEntry.overrideOcclusionStrength;
        targetEntry.occlusionStrengthOverride = sourceEntry.occlusionStrengthOverride;
        targetEntry.enabled = sourceEntry.enabled;
    }

    private static void CopySingleField(MaterialOverrideEntry sourceEntry, MaterialOverrideEntry targetEntry, string fieldName)
    {
        if (sourceEntry == null || targetEntry == null || string.IsNullOrWhiteSpace(fieldName))
            return;

        switch (fieldName)
        {
            case "enabled":
                targetEntry.enabled = sourceEntry.enabled;
                break;
            case "overrideColor":
                targetEntry.overrideColor = sourceEntry.overrideColor;
                break;
            case "colorOverride":
                targetEntry.colorOverride = sourceEntry.colorOverride;
                break;
            case "overrideAlpha":
                targetEntry.overrideAlpha = sourceEntry.overrideAlpha;
                break;
            case "alphaOverride":
                targetEntry.alphaOverride = sourceEntry.alphaOverride;
                break;
            case "overrideMetallic":
                targetEntry.overrideMetallic = sourceEntry.overrideMetallic;
                break;
            case "metallicOverride":
                targetEntry.metallicOverride = sourceEntry.metallicOverride;
                break;
            case "overrideSmoothness":
                targetEntry.overrideSmoothness = sourceEntry.overrideSmoothness;
                break;
            case "smoothnessOverride":
                targetEntry.smoothnessOverride = sourceEntry.smoothnessOverride;
                break;
            case "overrideSpecularColor":
                targetEntry.overrideSpecularColor = sourceEntry.overrideSpecularColor;
                break;
            case "specularColorOverride":
                targetEntry.specularColorOverride = sourceEntry.specularColorOverride;
                break;
            case "overrideEmissionColor":
                targetEntry.overrideEmissionColor = sourceEntry.overrideEmissionColor;
                break;
            case "emissionColorOverride":
                targetEntry.emissionColorOverride = sourceEntry.emissionColorOverride;
                break;
            case "overrideBumpScale":
                targetEntry.overrideBumpScale = sourceEntry.overrideBumpScale;
                break;
            case "bumpScaleOverride":
                targetEntry.bumpScaleOverride = sourceEntry.bumpScaleOverride;
                break;
            case "overrideOcclusionStrength":
                targetEntry.overrideOcclusionStrength = sourceEntry.overrideOcclusionStrength;
                break;
            case "occlusionStrengthOverride":
                targetEntry.occlusionStrengthOverride = sourceEntry.occlusionStrengthOverride;
                break;
        }
    }

}
}
