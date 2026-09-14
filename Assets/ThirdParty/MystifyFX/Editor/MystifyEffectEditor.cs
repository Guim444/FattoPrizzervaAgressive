using UnityEngine;
using UnityEditor;
using System.IO;

namespace MystifyFX {

    [CustomEditor(typeof(MystifyEffect))]
    public class MystifyFXEffectEditor : Editor {

        SerializedProperty meshType;
        SerializedProperty includeType, customRenderers, includeLayerMask;
        SerializedProperty sharedProfile;
        MystifyEffectProfile cachedProfile;
        Editor cachedProfileEditor;
        static GUIStyle boxStyle;

        void OnEnable () {
            sharedProfile = serializedObject.FindProperty("_sharedProfile");
            meshType = serializedObject.FindProperty("meshType");
            includeType = serializedObject.FindProperty("includeType");
            customRenderers = serializedObject.FindProperty("customRenderers");
            includeLayerMask = serializedObject.FindProperty("includeLayerMask");
        }


        public override void OnInspectorGUI () {

            if (boxStyle == null) {
                boxStyle = new GUIStyle(GUI.skin.box);
                boxStyle.padding = new RectOffset(15, 10, 5, 5);
            }

            if (!MystifyFXRendererFeature.installed) {
                EditorGUILayout.HelpBox("Mystify FX Render Feature is not installed in the current URP asset.", MessageType.Error);
                if (GUILayout.Button("Go to URP asset", EditorStyles.miniButton)) {
                    var pipe = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
                    if (pipe != null) {
                        Selection.activeObject = pipe;
                    }
                }
            }

            if (GUILayout.Button("Online Resources & Support")) {
                ContactUsWindow.ShowScreen();
            }

            serializedObject.Update();

            EditorGUILayout.PropertyField(meshType);
            if (meshType.enumValueIndex == (int)MeshType.Existing) {
                EditorGUILayout.PropertyField(includeType, new GUIContent("Include"));
                if (includeType.enumValueIndex == (int)IncludeType.Custom) {
                    EditorGUILayout.PropertyField(customRenderers, new GUIContent("Renderers"));
                } else if (includeType.enumValueIndex == (int)IncludeType.ByLayer) {
                    EditorGUILayout.PropertyField(includeLayerMask, new GUIContent("Layer Mask"));
                }
            }
            MystifyEffect effect = target as MystifyEffect;

            if (effect.sharedProfile != null && effect.sharedProfile.alwaysOn && effect.renderers != null) {
                foreach (var renderer in effect.renderers) {
                    if (renderer == null) continue;
                    if (!renderer.enabled && renderer is SkinnedMeshRenderer) {
                        EditorGUILayout.HelpBox("Skinned Mesh Renderer must be enabled to play animation. Use a transparent material on the Skinned Mesh Renderer if you only want to show the effects.", MessageType.Info);
                    }
                }
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(sharedProfile, new GUIContent("Profile"));
            if (GUILayout.Button("Refresh", GUILayout.Width(80))) {
                (target as MystifyEffect).Refresh();
                EditorUtility.SetDirty(target);
            }
            EditorGUILayout.EndHorizontal();


            if (sharedProfile.objectReferenceValue != null) {
                if (cachedProfile != sharedProfile.objectReferenceValue) {
                    cachedProfile = null;
                }
                if (cachedProfile == null) {
                    cachedProfile = (MystifyEffectProfile)sharedProfile.objectReferenceValue;
                    cachedProfileEditor = CreateEditor(sharedProfile.objectReferenceValue);
                }

                // Drawing the profile editor
                EditorGUILayout.BeginVertical(boxStyle);
                cachedProfileEditor.OnInspectorGUI();
                EditorGUILayout.EndVertical();
            }
            else {
                EditorGUILayout.HelpBox("Create or assign a profile.", MessageType.Info);
                if (GUILayout.Button("New Profile")) {
                    CreateProfile();
                }
            }

            if (serializedObject.ApplyModifiedProperties()) {
                (target as MystifyEffect).UpdateMaterialPropertiesNow();
            }
        }

        void CreateProfile () {

            string path = "Assets";
            foreach (Object obj in Selection.GetFiltered(typeof(Object), SelectionMode.Assets)) {
                path = AssetDatabase.GetAssetPath(obj);
                if (File.Exists(path)) {
                    path = Path.GetDirectoryName(path);
                }
                break;
            }
            MystifyEffectProfile fp = CreateInstance<MystifyEffectProfile>();
            fp.name = "New Mystify FX Profile";
            AssetDatabase.CreateAsset(fp, path + "/" + fp.name + ".asset");
            AssetDatabase.SaveAssets();
            sharedProfile.objectReferenceValue = fp;
            EditorGUIUtility.PingObject(fp);
        }

        [MenuItem("GameObject/Effects/Mystify FX", false, 100)]
        static void CreateMystifyEffectGameObject () {
            GameObject mystifyObject = new GameObject("MystifyEffect");
            MystifyEffect m = mystifyObject.AddComponent<MystifyEffect>();
            m.meshType = MeshType.Quad;
            Undo.RegisterCreatedObjectUndo(mystifyObject, "Create Mystify FX GameObject");
            Selection.activeGameObject = mystifyObject;
        }

    }

}