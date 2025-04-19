using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

[CustomEditor(typeof(CreatureBehavior))]
public class CreatureBehaviorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        CreatureBehavior creature = (CreatureBehavior)target;

        List<int> allTraitIds = TraitManager.GetAllTraitIds();
        List<string> traitNames = allTraitIds.Select(id => TraitManager.GetTraitName(id)).ToList();
        traitNames.Insert(0, "None");

        SerializedProperty traitIdsProp = serializedObject.FindProperty("traitIds");

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Add Trait", EditorStyles.boldLabel);

        int selectedIndex = EditorGUILayout.Popup("New Trait", 0, traitNames.ToArray());
        if (selectedIndex > 0)
        {
            int selectedTraitId = allTraitIds[selectedIndex - 1];
            Type traitType = TraitManager.GetTraitType(selectedTraitId);

            if (creature.GetComponent(traitType) == null)
            {
                creature.gameObject.AddComponent(traitType);
                traitIdsProp.arraySize++;
                traitIdsProp.GetArrayElementAtIndex(traitIdsProp.arraySize - 1).intValue = selectedTraitId;
                serializedObject.ApplyModifiedProperties();
            }
            else
            {
                EditorUtility.DisplayDialog("Warning", $"{TraitManager.GetTraitName(selectedTraitId)} is already assigned.", "OK");
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Assigned Traits", EditorStyles.boldLabel);

        for (int i = 0; i < traitIdsProp.arraySize; i++)
        {
            int traitId = traitIdsProp.GetArrayElementAtIndex(i).intValue;
            string traitName = TraitManager.GetTraitName(traitId);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(traitName, GUILayout.Width(200));

            if (GUILayout.Button("Remove"))
            {
                Type traitType = TraitManager.GetTraitType(traitId);
                if (traitType != null)
                {
                    var component = creature.GetComponent(traitType);
                    if (component != null)
                    {
                        DestroyImmediate(component);
                    }
                }
                traitIdsProp.DeleteArrayElementAtIndex(i);
                serializedObject.ApplyModifiedProperties();
                break;
            }
            EditorGUILayout.EndHorizontal();
        }

        serializedObject.ApplyModifiedProperties();
    }
}