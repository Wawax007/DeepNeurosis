// ──────────────────────────────────────────────────────────────
//  TextureTilingFromWorldScaleEditor.cs
//  Custom Inspector – avril 2025
// ──────────────────────────────────────────────────────────────
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TextureTilingFromWorldScale))]
public class TextureTilingFromWorldScaleEditor : Editor
{
    SerializedProperty pDesiredVisualTiling;
    SerializedProperty pReferenceScale;
    SerializedProperty pAutoDetectAxis;
    SerializedProperty pManualAxis;

    void OnEnable()
    {
        pDesiredVisualTiling = serializedObject.FindProperty(nameof(TextureTilingFromWorldScale.desiredVisualTiling));
        pReferenceScale      = serializedObject.FindProperty(nameof(TextureTilingFromWorldScale.referenceScale));
        pAutoDetectAxis      = serializedObject.FindProperty(nameof(TextureTilingFromWorldScale.autoDetectProjectionAxis));
        pManualAxis          = serializedObject.FindProperty(nameof(TextureTilingFromWorldScale.manualAxis));
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(pDesiredVisualTiling);
        EditorGUILayout.PropertyField(pReferenceScale);
        EditorGUILayout.PropertyField(pAutoDetectAxis);
        if (!pAutoDetectAxis.boolValue)
            EditorGUILayout.PropertyField(pManualAxis);

        serializedObject.ApplyModifiedProperties();

        GUILayout.Space(8);

        if (GUILayout.Button("📸 Capturer le tiling actuel", GUILayout.Height(24)))
        {
            foreach (Object o in targets)
            {
                if (o is TextureTilingFromWorldScale s)
                {
                    Undo.RecordObject(s, "Capture Tiling");
                    s.CaptureCurrentTiling();
                    s.ForceUpdateTiling();
                    EditorUtility.SetDirty(s);
                }
            }
        }

        if (pAutoDetectAxis.boolValue)
        {
            var first = (TextureTilingFromWorldScale)target;
            GUILayout.Label($"Axe détecté : <b>{first.DetectAxis()}</b>");
        }
    }
}
#endif
