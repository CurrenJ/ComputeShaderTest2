using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PaletteModule))]
public class PaletteModuleEditor : Editor
{
    private SerializedProperty palettes;
    private void OnEnable()
    {
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        if (serializedObject.FindProperty("palettes") != null)
        {
            palettes = serializedObject.FindProperty("palettes");
            int y = 8;
            for (int p = 0; p < palettes.arraySize; p++)
            {
                SerializedProperty palette = palettes.GetArrayElementAtIndex(p);
                int height = 16;
                if (palette.isExpanded)
                    height += 18;

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(palette);
                DrawColorSwatches(124, y, 16, height, palette);
                EditorGUILayout.EndHorizontal();

                if (palette.isExpanded)
                {
                    y += 20;
                    if (palette.FindPropertyRelative("palette").isExpanded)
                    {
                        y += 20 * 5;
                    }
                }
                y += 20;
            }
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("-"))
            {
                palettes.DeleteArrayElementAtIndex(palettes.arraySize - 1);
            }
            if (GUILayout.Button("+"))
            {
                palettes.InsertArrayElementAtIndex(palettes.arraySize);
            }
            EditorGUILayout.EndHorizontal();
        }
        serializedObject.ApplyModifiedProperties();
    }

    public void DrawColorSwatches(int x, int y, int width, int height, SerializedProperty palette)
    {
        EditorGUI.DrawRect(new Rect(124, y, 16, height), palette.FindPropertyRelative("palette").GetArrayElementAtIndex(0).colorValue);
        EditorGUI.DrawRect(new Rect(148, y, 16, height), palette.FindPropertyRelative("palette").GetArrayElementAtIndex(1).colorValue);
        EditorGUI.DrawRect(new Rect(172, y, 16, height), palette.FindPropertyRelative("palette").GetArrayElementAtIndex(2).colorValue);
        EditorGUI.DrawRect(new Rect(196, y, 16, height), palette.FindPropertyRelative("palette").GetArrayElementAtIndex(3).colorValue);
    }
}
