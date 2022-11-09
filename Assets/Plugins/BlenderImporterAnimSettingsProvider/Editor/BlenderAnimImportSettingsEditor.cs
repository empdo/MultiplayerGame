using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BlenderAnimImportSettings))]
public class BlenderAnimImportSettingsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var settings = (BlenderAnimImportSettings)target;
        var scriptSettingsQueryResult = BlenderAnimImportSettings.LastFileQueryResult;
        if (!scriptSettingsQueryResult.IsValid)
        {
            GUILayout.Label(scriptSettingsQueryResult.Error);
        }
        else
        {
            EditorGUILayout.HelpBox("Adjusts the built-in .blend import script to the desired way of importing animation clips", MessageType.None);
            GUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("importType"),
                new GUIContent(
                    "Clip import Type",
                    $"How animations are imported, '{nameof(BlenderAnimImportSettings.AnimationImportTypes.DefaultAsSceneClip)}' is what the new bad unity default is which ignores actions and dumps everything into one clip, '{nameof(BlenderAnimImportSettings.AnimationImportTypes.SplitByAction)}' is the old better alternative that respectively separates by Action"));
            if (scriptSettingsQueryResult.ScriptSettingType != settings.ImportType)
            {
                var previousColor = GUI.backgroundColor;
                GUI.backgroundColor = Color.green;
                if (GUILayout.Button("Apply", GUILayout.ExpandWidth(false)))
                {
                    BlenderAnimImportSettings.ApplySettingsToScriptFile(settings.ImportType);
                    BlenderAnimImportSettings.RefreshScriptInfo();
                }
                GUI.backgroundColor = previousColor;
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Trigger reimport of .blend files", GUILayout.ExpandWidth(false)))
            {
                EditorApplication.delayCall += BlenderAnimImportSettings.ReimportBlenderAssets;
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.HelpBox("Automatically applies the importer patch when the settings are loaded (usually on editor start).\nUseful if this setting should be shared in team-projects or when updating unity versions", MessageType.Info);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("autoApply"),new GUIContent("Auto Apply"));
            EditorGUILayout.EndVertical();
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}