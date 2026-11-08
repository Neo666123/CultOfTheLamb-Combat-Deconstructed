using System.Collections.Generic;
using MIeMie.Combat;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WeaponDefinitionSO))]
public sealed class WeaponDefinitionSOEditor : Editor
{
    private readonly Dictionary<int, UnityEditor.Editor> segmentEditors = new Dictionary<int, UnityEditor.Editor>();
    private readonly HashSet<int> expandedSegments = new HashSet<int>();

    private void OnDisable()
    {
        foreach (UnityEditor.Editor segmentEditor in segmentEditors.Values)
        {
            if (segmentEditor != null)
                DestroyImmediate(segmentEditor);
        }
        segmentEditors.Clear();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawPropertiesExcluding(serializedObject, "m_Script", "combos");

        SerializedProperty combos = serializedObject.FindProperty("combos");
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("普通攻击段（展开后直接编辑对应文件）", EditorStyles.boldLabel);

        int requestedSize = Mathf.Max(0, EditorGUILayout.IntField("攻击段数量", combos.arraySize));
        if (requestedSize != combos.arraySize)
            combos.arraySize = requestedSize;

        for (int index = 0; index < combos.arraySize; index++)
            DrawSegment(combos.GetArrayElementAtIndex(index), index);

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawSegment(SerializedProperty reference, int index)
    {
        AttackSegmentData segment = reference.objectReferenceValue as AttackSegmentData;
        int key = segment != null ? segment.GetHashCode() : -1000 - index;
        bool expanded = expandedSegments.Contains(key);
        string label = segment != null
            ? "第 " + (index + 1) + " 段 — " + segment.displayName
            : "第 " + (index + 1) + " 段 — 未指定";

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.BeginHorizontal();
        bool nextExpanded = EditorGUILayout.Foldout(expanded, label, true);
        EditorGUILayout.PropertyField(reference, GUIContent.none);
        EditorGUILayout.EndHorizontal();

        if (nextExpanded != expanded)
        {
            if (nextExpanded)
                expandedSegments.Add(key);
            else
                expandedSegments.Remove(key);
        }

        if (nextExpanded && segment != null)
        {
            EditorGUILayout.Space(3f);
            EditorGUI.indentLevel++;
            GetSegmentEditor(segment).OnInspectorGUI();
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndVertical();
    }

    private UnityEditor.Editor GetSegmentEditor(AttackSegmentData segment)
    {
        int key = segment.GetHashCode();
        UnityEditor.Editor segmentEditor;
        if (!segmentEditors.TryGetValue(key, out segmentEditor) || segmentEditor == null)
        {
            segmentEditor = CreateEditor(segment);
            segmentEditors[key] = segmentEditor;
        }
        return segmentEditor;
    }
}

