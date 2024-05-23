using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(CameraManager))]
public class CameraManagerEditor : Editor
{
    private ReorderableList introReorderableList;
    private ReorderableList tutorialReorderableList;
    private ReorderableList kindergardenReorderableList;
    private ReorderableList escapeReorderableList;

    private void OnEnable()
    {
        ///////////
        // INTRO //
        ///////////

        // Get the serialized property for intro plans
        SerializedProperty introProperty = serializedObject.FindProperty("intro");
        SerializedProperty introPlansProperty = introProperty.FindPropertyRelative("plans");

        introReorderableList = new ReorderableList(serializedObject, introPlansProperty, true, true, true, true);

        introReorderableList.drawHeaderCallback = (Rect rect) =>
        {
            EditorGUI.LabelField(rect, "Cinematic Plans");
        };

        introReorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            SerializedProperty element = introReorderableList.serializedProperty.GetArrayElementAtIndex(index);
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
                plan.SetToView();
            }

            // Draw GetToView button
            if (GUI.Button(new Rect(rect.x + rect.width / 2 + padding, rect.y, rect.width / 2 - padding, singleLineHeight), "Get To View"))
            {
                plan.GetToView();
            }

            rect.y += singleLineHeight + padding;
        };

        introReorderableList.elementHeightCallback = (int index) =>
        {
            SerializedProperty element = introReorderableList.serializedProperty.GetArrayElementAtIndex(index);
            float propertyHeight = EditorGUI.GetPropertyHeight(element);
            return propertyHeight + EditorGUIUtility.singleLineHeight * 2 + 10f; // Adjusted height for properties and buttons
        };

        //////////////
        // TUTORIAL //
        //////////////

        // Get the serialized property for intro plans
        SerializedProperty tutorialProperty = serializedObject.FindProperty("tutorial");
        SerializedProperty tutorialPlansProperty = tutorialProperty.FindPropertyRelative("plans");

        tutorialReorderableList = new ReorderableList(serializedObject, tutorialPlansProperty, true, true, true, true);

        tutorialReorderableList.drawHeaderCallback = (Rect rect) =>
        {
            EditorGUI.LabelField(rect, "Cinematic Plans");
        };

        tutorialReorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            SerializedProperty element = tutorialReorderableList.serializedProperty.GetArrayElementAtIndex(index);
            CameraManager cameraManager = (CameraManager)target;
            Cinematic.CinematicPlan plan = cameraManager.tutorial.plans[index];

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
                plan.SetToView();
            }

            // Draw GetToView button
            if (GUI.Button(new Rect(rect.x + rect.width / 2 + padding, rect.y, rect.width / 2 - padding, singleLineHeight), "Get To View"))
            {
                plan.GetToView();
            }

            rect.y += singleLineHeight + padding;
        };

        tutorialReorderableList.elementHeightCallback = (int index) =>
        {
            SerializedProperty element = tutorialReorderableList.serializedProperty.GetArrayElementAtIndex(index);
            float propertyHeight = EditorGUI.GetPropertyHeight(element);
            return propertyHeight + EditorGUIUtility.singleLineHeight * 2 + 10f; // Adjusted height for properties and buttons
        };

        //////////////////
        // KINDERGARDEN //
        //////////////////

        SerializedProperty kindergardenProperty = serializedObject.FindProperty("kindergarden");
        SerializedProperty kindergardenPlansProperty = kindergardenProperty.FindPropertyRelative("plans");

        kindergardenReorderableList = new ReorderableList(serializedObject, kindergardenPlansProperty, true, true, true, true);

        kindergardenReorderableList.drawHeaderCallback = (Rect rect) =>
        {
            EditorGUI.LabelField(rect, "Cinematic Plans");
        };

        kindergardenReorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            SerializedProperty element = kindergardenReorderableList.serializedProperty.GetArrayElementAtIndex(index);
            CameraManager cameraManager = (CameraManager)target;
            Cinematic.CinematicPlan plan = cameraManager.kindergarden.plans[index];

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
                plan.SetToView();
            }

            // Draw GetToView button
            if (GUI.Button(new Rect(rect.x + rect.width / 2 + padding, rect.y, rect.width / 2 - padding, singleLineHeight), "Get To View"))
            {
                plan.GetToView();
            }

            rect.y += singleLineHeight + padding;
        };

        kindergardenReorderableList.elementHeightCallback = (int index) =>
        {
            SerializedProperty element = kindergardenReorderableList.serializedProperty.GetArrayElementAtIndex(index);
            float propertyHeight = EditorGUI.GetPropertyHeight(element);
            return propertyHeight + EditorGUIUtility.singleLineHeight * 2 + 10f; // Adjusted height for properties and buttons
        };

        ////////////
        // ESCAPE //
        ////////////

        SerializedProperty escapeProperty = serializedObject.FindProperty("escape");
        SerializedProperty escapePlansProperty = escapeProperty.FindPropertyRelative("plans");

        escapeReorderableList = new ReorderableList(serializedObject, escapePlansProperty, true, true, true, true);

        escapeReorderableList.drawHeaderCallback = (Rect rect) =>
        {
            EditorGUI.LabelField(rect, "Cinematic Plans");
        };

        escapeReorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            SerializedProperty element = escapeReorderableList.serializedProperty.GetArrayElementAtIndex(index);
            CameraManager cameraManager = (CameraManager)target;
            Cinematic.CinematicPlan plan = cameraManager.escape.plans[index];

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
                plan.SetToView();
            }

            // Draw GetToView button
            if (GUI.Button(new Rect(rect.x + rect.width / 2 + padding, rect.y, rect.width / 2 - padding, singleLineHeight), "Get To View"))
            {
                plan.GetToView();
            }

            rect.y += singleLineHeight + padding;
        };

        escapeReorderableList.elementHeightCallback = (int index) =>
        {
            SerializedProperty element = escapeReorderableList.serializedProperty.GetArrayElementAtIndex(index);
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
                    introReorderableList.DoLayoutList();
                }
            }
            else if (iterator.name == "tutorial")
            {
                EditorGUILayout.PropertyField(iterator, new GUIContent("Tutorial"), false);
                if (iterator.isExpanded)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(iterator.FindPropertyRelative("startTransition"));
                    EditorGUILayout.PropertyField(iterator.FindPropertyRelative("endTransition"));
                    EditorGUILayout.PropertyField(iterator.FindPropertyRelative("startDuration"));
                    EditorGUILayout.PropertyField(iterator.FindPropertyRelative("endDuration"));

                    EditorGUILayout.Space();
                    tutorialReorderableList.DoLayoutList();
                }
            }
            else if (iterator.name == "kindergarden")
            {
                EditorGUILayout.PropertyField(iterator, new GUIContent("Kindergarden"), false);
                if (iterator.isExpanded)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(iterator.FindPropertyRelative("startTransition"));
                    EditorGUILayout.PropertyField(iterator.FindPropertyRelative("endTransition"));
                    EditorGUILayout.PropertyField(iterator.FindPropertyRelative("startDuration"));
                    EditorGUILayout.PropertyField(iterator.FindPropertyRelative("endDuration"));

                    EditorGUILayout.Space();
                    kindergardenReorderableList.DoLayoutList();
                }
            }
            else if (iterator.name == "escape")
            {
                EditorGUILayout.PropertyField(iterator, new GUIContent("Escape"), false);
                if (iterator.isExpanded)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(iterator.FindPropertyRelative("startTransition"));
                    EditorGUILayout.PropertyField(iterator.FindPropertyRelative("endTransition"));
                    EditorGUILayout.PropertyField(iterator.FindPropertyRelative("startDuration"));
                    EditorGUILayout.PropertyField(iterator.FindPropertyRelative("endDuration"));

                    EditorGUILayout.Space();
                    escapeReorderableList.DoLayoutList();
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