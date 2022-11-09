using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

internal class BlenderAnimImportSettings : ScriptableObject
{
    const string ScriptFileName = "Unity-BlenderToFBX.py";
    const string ImportKeyword = "bake_anim_use_all_actions";

    [SerializeField] AnimationImportTypes importType;

    [Tooltip("Useful in case of shared projects or when updating unity versions, automatically applies the settings patch on load")]
    [SerializeField]
    bool autoApply;

    public AnimationImportTypes ImportType => importType;

    public static ScriptFileSettingQueryResult LastFileQueryResult { get; private set; }

    public readonly struct ScriptFileSettingQueryResult
    {
        public readonly bool IsValid;
        public readonly AnimationImportTypes ScriptSettingType;
        public readonly string Error;

        public ScriptFileSettingQueryResult(bool isValid, AnimationImportTypes scriptSettingType, string error)
        {
            IsValid = isValid;
            ScriptSettingType = scriptSettingType;
            Error = error;
        }
    }

    public enum AnimationImportTypes
    {
        DefaultAsSceneClip,
        SplitByAction
    }

    void OnEnable()
    {
        RefreshScriptInfo();
        AutoApplyIfNecessary();
    }

    void OnValidate()
    {
        RefreshScriptInfo();
    }

    static string GetDefaultSettingsAssetPath()
    {
        var asset = CreateInstance<BlenderAnimImportSettings>();
        var monoscript = MonoScript.FromScriptableObject(asset);
        var path = AssetDatabase.GetAssetPath(monoscript);
        DestroyImmediate(asset, false);
        path = new FileInfo(path).Directory?.FullName;
        if (string.IsNullOrEmpty(path)) return "Assets/Editor/" + nameof(BlenderAnimImportSettings) + ".asset";
        return Path.Combine("Assets" + path.Substring(Application.dataPath.Length), nameof(BlenderAnimImportSettings) + ".asset");
    }

    void AutoApplyIfNecessary()
    {
        if (!autoApply) return;
        var foundAssets = AssetDatabase.FindAssets($"t:{nameof(BlenderAnimImportSettings)}");
        if (foundAssets.Length > 1)
        {
            Debug.LogError(
                "Cannot auto-apply animation clip type patch to .blend importer, more than 1 settings file exists in project (potentially ambiguous value to apply), please make sure only one such file exists, found assets:");
            foreach (var assetPath in foundAssets.Select(AssetDatabase.GUIDToAssetPath))
            {
                var asset = AssetDatabase.LoadAssetAtPath<BlenderAnimImportSettings>(assetPath);
                Debug.Log(asset.name, asset);
            }
            return;
        }
        if (!LastFileQueryResult.IsValid) return;
        if (LastFileQueryResult.ScriptSettingType == importType) return;
        ApplySettingsToScriptFile(importType);
    }

    public static BlenderAnimImportSettings GetOrCreateSettings()
    {
        var defaultAssetPath = GetDefaultSettingsAssetPath();
        var settings = AssetDatabase.LoadAssetAtPath<BlenderAnimImportSettings>(defaultAssetPath);
        if (settings == null) //find first existing
        {
            var assets = AssetDatabase.FindAssets($"t:{nameof(BlenderAnimImportSettings)}");
            if (assets.Length > 1)
            {
                Debug.LogWarning($"More than one settings asset ({nameof(BlenderAnimImportSettings)}) found, using first available");
            }
            var firstExistingAssetPath = assets.Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault();
            if (!string.IsNullOrEmpty(firstExistingAssetPath))
            {
                settings = AssetDatabase.LoadAssetAtPath<BlenderAnimImportSettings>(firstExistingAssetPath);
            }
        }
        if (settings == null) //create new
        {
            settings = CreateInstance<BlenderAnimImportSettings>();
            AssetDatabase.CreateAsset(settings, defaultAssetPath);
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssetIfDirty(settings);
        }
        return settings;
    }

    static string GetImportScriptPath()
    {
        var p = new FileInfo(EditorApplication.applicationPath).Directory?.FullName;
        if (string.IsNullOrEmpty(p)) return null;
        p = Path.Combine(p, "Data", "Tools", ScriptFileName);
        return p;
    }

    public static void RefreshScriptInfo()
    {
        var scriptPath = GetImportScriptPath();
        if (string.IsNullOrEmpty(scriptPath) || !File.Exists(scriptPath))
        {
            LastFileQueryResult = new ScriptFileSettingQueryResult(false, default, $"Importer file path is invalid ('{scriptPath}')");
        }
        LastFileQueryResult = GetScriptFileImportType(scriptPath);
    }

    static ScriptFileSettingQueryResult GetScriptFileImportType(string scriptFilePath)
    {
        string[] lines;
        try
        {
            lines = File.ReadAllLines(scriptFilePath);
        }
        catch (Exception ex)
        {
            return new ScriptFileSettingQueryResult(false, default, $"Error reading importer script content: {ex.Message}");
        }
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].IndexOf(ImportKeyword, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                var parts = lines[i].Split('=');
                if (parts.Length != 2)
                {
                    return new ScriptFileSettingQueryResult(false, default, "Unexpected content in importer script file, cannot read current setting");
                }
                for (int j = 1; j < parts.Length; j++)
                {
                    if (parts[j].IndexOf("True", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return new ScriptFileSettingQueryResult(true, AnimationImportTypes.SplitByAction, string.Empty);
                    }
                    if (parts[j].IndexOf("False", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return new ScriptFileSettingQueryResult(true, AnimationImportTypes.DefaultAsSceneClip, string.Empty);
                    }
                }
            }
        }
        return new ScriptFileSettingQueryResult(false, default, "Expected setting keyword not found in import script file");
    }

    public static void ReimportBlenderAssets()
    {
        var affectedAssets = AssetDatabase.FindAssets("t:Model").Select(AssetDatabase.GUIDToAssetPath).Where(p => p.EndsWith(".blend", StringComparison.OrdinalIgnoreCase)).ToList();
        try
        {
            for (int i = 0; i < affectedAssets.Count; i++)
            {
                if (EditorUtility.DisplayCancelableProgressBar("Reimporting Assets", affectedAssets[i], Mathf.Clamp01((float)i / affectedAssets.Count)))
                {
                    EditorUtility.ClearProgressBar();
                    break;
                }
                AssetDatabase.ImportAsset(affectedAssets[i], ImportAssetOptions.ForceUpdate);
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    public static void ApplySettingsToScriptFile(AnimationImportTypes type)
    {
        var scriptFilePath = GetImportScriptPath();
        string[] lines;
        try
        {
            lines = File.ReadAllLines(scriptFilePath);
        }
        catch (Exception ex)
        {
            Debug.LogException(new AggregateException(new Exception("Error reading importer script file, not applying modifications"), ex));
            return;
        }
        if (!TryApplyPatch(lines, type, out var error))
        {
            Debug.LogError($"Error applying patch ({error}), no modifications applied");
            return;
        }
        try
        {
            var backupFilePath = scriptFilePath + ".bak";
            if (!File.Exists(backupFilePath))
            {
                File.Copy(scriptFilePath, backupFilePath);
            }
            File.WriteAllLines(scriptFilePath, lines);
            Debug.Log("Import script patch applied");
        }
        catch (Exception ex)
        {
            Debug.LogException(new AggregateException(new Exception("Unable to apply patch to import script"), ex));
            return;
        }
        if (EditorUtility.DisplayDialog("Reimport required", "Automatically reimport all affected assets?\n(this operation can take a while)", "Yes", "No"))
        {
            ReimportBlenderAssets();
        }
        else
        {
            Debug.Log("Import settings changed, affected files will need to be reimported for changes to take effect");
        }
        bool TryApplyPatch(string[] scriptLines, AnimationImportTypes replacedValue, out string errorText)
        {
            for (int i = 0; i < scriptLines.Length; i++)
            {
                if (scriptLines[i].IndexOf(ImportKeyword, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var parts = scriptLines[i].Split('=');
                    if (parts.Length != 2)
                    {
                        errorText = "Unexpected content in importer script file";
                        return false;
                    }
                    var valueSet = false;
                    for (int j = 1; j < parts.Length; j++)
                    {
                        if (ReplaceKeywordValue(ref parts[j], "True", replacedValue) || ReplaceKeywordValue(ref parts[j], "False", replacedValue))
                        {
                            valueSet = true;
                            break;
                        }
                    }
                    if (!valueSet)
                    {
                        errorText = "Error parsing importer script file";
                        return false;
                    }
                    scriptLines[i] = string.Join("=", parts);
                    errorText = string.Empty;
                    return true;
                }
            }
            errorText = "Setting to replace not found in importer script file";
            return false;

            bool ReplaceKeywordValue(ref string part, string searchValue, AnimationImportTypes replacedValueType)
            {
                if (part.IndexOf(searchValue, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    part = part.Replace(searchValue, (replacedValueType != AnimationImportTypes.DefaultAsSceneClip).ToString());
                    return true;
                }
                return false;
            }
        }
    }
}