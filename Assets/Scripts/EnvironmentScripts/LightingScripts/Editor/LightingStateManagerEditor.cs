using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[CustomEditor(typeof(LightingStateManager))]
public class LightingStateManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        LightingStateManager manager = (LightingStateManager)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Transiciones", EditorStyles.boldLabel);

        GUI.enabled = Application.isPlaying;

        if (GUILayout.Button("→ Tapat (snap)"))
            manager.SnapToState(LightingStateManager.LightingState.Tapat);

        if (GUILayout.Button("→ Radiografia"))
            manager.TransitionTo(LightingStateManager.LightingState.Radiografia);

        if (GUILayout.Button("→ Lluna"))
            manager.TransitionTo(LightingStateManager.LightingState.Lluna);

        if (GUILayout.Button("→ Azul (snap)"))
            manager.SnapToState(LightingStateManager.LightingState.Blue);

        if (!Application.isPlaying)
            EditorGUILayout.HelpBox("Entra en Play Mode para usar los botones.", MessageType.Info);

        GUI.enabled = true;

        DrawBakePreparationControls();
    }

    private void DrawBakePreparationControls()
    {
        EditorGUILayout.Space(14);
        EditorGUILayout.LabelField("Preparar bake (Edit Mode)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Configura los grupos de luces y sus intensidades objetivo sin aplicar lightmaps "
            + "ni reproducir transiciones. Warm2 utiliza el estado final Lluna.",
            MessageType.Info);

        using (new EditorGUI.DisabledScope(Application.isPlaying || Lightmapping.isRunning))
        {
            if (GUILayout.Button("Preparar Warm1 / Tapat"))
                PrepareBakeState(LightingStateManager.LightingState.Tapat, "Warm1 / Tapat");

            if (GUILayout.Button("Preparar Warm2 / Lluna"))
                PrepareBakeState(LightingStateManager.LightingState.Lluna, "Warm2 / Lluna");

            if (GUILayout.Button("Preparar Blue"))
                PrepareBakeState(LightingStateManager.LightingState.Blue, "Blue");
        }

        if (Lightmapping.isRunning)
        {
            EditorGUILayout.HelpBox(
                "Los controles están bloqueados mientras Unity genera la iluminación.",
                MessageType.Warning);
        }
    }

    private void PrepareBakeState(LightingStateManager.LightingState state, string label)
    {
        serializedObject.Update();

        SerializedProperty statesProperty = serializedObject.FindProperty("states");
        int stateIndex = (int)state;

        if (statesProperty == null
            || stateIndex < 0
            || stateIndex >= statesProperty.arraySize)
        {
            EditorUtility.DisplayDialog(
                "No se puede preparar el bake",
                $"No existe la configuración del estado {state}.",
                "Aceptar");
            return;
        }

        SerializedProperty targetEntries = statesProperty
            .GetArrayElementAtIndex(stateIndex)
            .FindPropertyRelative("entries");

        if (targetEntries == null || targetEntries.arraySize == 0)
        {
            EditorUtility.DisplayDialog(
                "No se puede preparar el bake",
                $"El estado {state} no contiene luces configuradas.",
                "Aceptar");
            return;
        }

        var allManagedLights = new HashSet<Light>();
        var targetIntensities = new Dictionary<Light, float>();
        int missingReferences = 0;

        for (int i = 0; i < statesProperty.arraySize; i++)
        {
            SerializedProperty entries = statesProperty
                .GetArrayElementAtIndex(i)
                .FindPropertyRelative("entries");

            if (entries == null)
                continue;

            for (int j = 0; j < entries.arraySize; j++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(j);
                var sceneLight = entry.FindPropertyRelative("light").objectReferenceValue as Light;

                if (sceneLight != null)
                    allManagedLights.Add(sceneLight);
            }
        }

        for (int i = 0; i < targetEntries.arraySize; i++)
        {
            SerializedProperty entry = targetEntries.GetArrayElementAtIndex(i);
            var sceneLight = entry.FindPropertyRelative("light").objectReferenceValue as Light;

            if (sceneLight == null)
            {
                missingReferences++;
                continue;
            }

            targetIntensities[sceneLight] = entry
                .FindPropertyRelative("targetIntensity")
                .floatValue;
        }

        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName($"Preparar bake {label}");

        var dirtyScenes = new HashSet<Scene>();
        bool isBlue = state == LightingStateManager.LightingState.Blue;

        SetActiveWithUndo(GetGameObjectProperty("redLightsObject"), !isBlue, label, dirtyScenes);
        SetActiveWithUndo(GetGameObjectProperty("blueLightsObject"), isBlue, label, dirtyScenes);
        SetActiveWithUndo(GetGameObjectProperty("redSource"), !isBlue, label, dirtyScenes);
        SetActiveWithUndo(GetGameObjectProperty("blueSource"), isBlue, label, dirtyScenes);

        foreach (Light sceneLight in allManagedLights)
        {
            bool belongsToTarget = targetIntensities.TryGetValue(sceneLight, out float intensity);
            SetActiveWithUndo(sceneLight.gameObject, belongsToTarget, label, dirtyScenes);

            if (!belongsToTarget)
                continue;

            Undo.RecordObject(sceneLight, $"Preparar bake {label}");
            sceneLight.intensity = intensity;
            EditorUtility.SetDirty(sceneLight);
            PrefabUtility.RecordPrefabInstancePropertyModifications(sceneLight);
            dirtyScenes.Add(sceneLight.gameObject.scene);
        }

        foreach (Scene dirtyScene in dirtyScenes)
        {
            if (dirtyScene.IsValid() && dirtyScene.isLoaded)
                EditorSceneManager.MarkSceneDirty(dirtyScene);
        }

        Undo.CollapseUndoOperations(undoGroup);
        SceneView.RepaintAll();

        string missingText = missingReferences > 0
            ? $" Hay {missingReferences} referencias de Light vacías en este estado."
            : string.Empty;

        Debug.Log(
            $"[{nameof(LightingStateManagerEditor)}] Bake {label} preparado con "
            + $"{targetIntensities.Count} luces. Revisa la escena y después pulsa Generate Lighting."
            + missingText,
            target);
    }

    private GameObject GetGameObjectProperty(string propertyName)
    {
        return serializedObject.FindProperty(propertyName)?.objectReferenceValue as GameObject;
    }

    private static void SetActiveWithUndo(
        GameObject targetObject,
        bool active,
        string label,
        HashSet<Scene> dirtyScenes)
    {
        if (targetObject == null || targetObject.activeSelf == active)
            return;

        Undo.RecordObject(targetObject, $"Preparar bake {label}");
        targetObject.SetActive(active);
        EditorUtility.SetDirty(targetObject);
        PrefabUtility.RecordPrefabInstancePropertyModifications(targetObject);
        dirtyScenes.Add(targetObject.scene);
    }
}
