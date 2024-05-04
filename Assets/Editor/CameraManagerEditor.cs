using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CameraManager))]
public class CameraManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        CameraManager cameraManager = (CameraManager)target;

        EditorGUILayout.PropertyField(serializedObject.FindProperty("intro"));
        if (serializedObject.FindProperty("intro").isExpanded)
        {
            SerializedProperty plansProperty = serializedObject.FindProperty("intro").FindPropertyRelative("plans");

            for (int i = 0; i < plansProperty.arraySize; i++)
            {
                SerializedProperty planProperty = plansProperty.GetArrayElementAtIndex(i);
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("Cinematic Plan " + (i + 1));
                EditorGUILayout.PropertyField(planProperty);

                if (GUILayout.Button("Set To View"))
                {
                    Cinematic.CinematicPlan plan = cameraManager.intro.plans[i];
                    plan.SetotView();
                }

                if (GUILayout.Button("Get To View"))
                {
                    Cinematic.CinematicPlan plan = cameraManager.intro.plans[i];
                    plan.GetToView();
                }

                EditorGUILayout.EndVertical();
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}