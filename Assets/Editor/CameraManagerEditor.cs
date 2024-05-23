using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(CameraManager))]
public class CameraManagerEditor : Editor
{
    private ReorderableList reorderableList;

    private void OnEnable()
    {
        // Get the serialized property for intro plans
        SerializedProperty introProperty = serializedObject.FindProperty("intro");
        SerializedProperty plansProperty = introProperty.FindPropertyRelative("plans");

        reorderableList = new ReorderableList(serializedObject, plansProperty, true, true, true, true);

        reorderableList.drawHeaderCallback = (Rect rect) =>
        {
            EditorGUI.LabelField(rect, "Cinematic Plans");
        };

        reorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            SerializedProperty element = reorderableList.serializedProperty.GetArrayElementAtIndex(index);
            CameraManager cameraManager = (CameraManager)target;
            Cinematic.CinematicPlan plan = cameraManager.intro.plans[index];

            // Draw the properties of the element
            rect.y += 2;
            float singleLineHeight = EditorGUIUtility.singleLineHeight;
            float padding = 2f;

            EditorGUI.PropertyField(
                new Rect(rect.x, rect.y, rect.width, singleLineHeight),
                element, new GUIContent($"Cinematic Plan {index + 1}"), true);

            rect.y += EditorGUI.GetPropertyHeight(element) + padding;

            // Draw SetToView button
            if (GUI.Button(new Rect(rect.x, rect.y, rect.width / 2 - padding, singleLineHeight), "Set To View"))
            {
                plan.SetotView();
            }

            // Draw GetToView button
            if (GUI.Button(new Rect(rect.x + rect.width / 2 + padding, rect.y, rect.width / 2 - padding, singleLineHeight), "Get To View"))
            {
                plan.GetToView();
            }

            rect.y += singleLineHeight + padding;
        };

        reorderableList.elementHeightCallback = (int index) =>
        {
            SerializedProperty element = reorderableList.serializedProperty.GetArrayElementAtIndex(index);
            float propertyHeight = EditorGUI.GetPropertyHeight(element);
            return propertyHeight + EditorGUIUtility.singleLineHeight * 2 + 10f; // Adjusted height for properties and buttons
        };
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Draw all properties except intro
        SerializedProperty iterator = serializedObject.GetIterator();
        bool enterChildren = true;
        while (iterator.NextVisible(enterChildren))
        {
            if (iterator.name == "intro")
            {
                EditorGUILayout.PropertyField(iterator, new GUIContent("Intro"), false);
                if (iterator.isExpanded)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(iterator.FindPropertyRelative("startTransition"));
                    EditorGUILayout.PropertyField(iterator.FindPropertyRelative("endTransition"));
                    EditorGUILayout.PropertyField(iterator.FindPropertyRelative("startDuration"));
                    EditorGUILayout.PropertyField(iterator.FindPropertyRelative("endDuration"));

                    EditorGUILayout.Space();
                    reorderableList.DoLayoutList();
                }
            }
            else
            {
                EditorGUILayout.PropertyField(iterator, true);
            }
            enterChildren = false;
        }

        serializedObject.ApplyModifiedProperties();
    }
}
