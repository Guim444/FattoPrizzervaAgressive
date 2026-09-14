using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace MystifyFX {

    public enum IncludeType {
        OnlyThisObject,
        IncludeChildren,
        Custom,
        ByLayer
    }

    public enum MeshType {
        Existing,
        Quad,
        Disc,
        Sphere,
        Hexasphere,
        Cylinder
    }

    [ExecuteAlways]
    [HelpURL("https://kronnect.com/docs/mystify-fx/")]
    public class MystifyEffect : MonoBehaviour {

        public IncludeType includeType = IncludeType.OnlyThisObject;
        public MeshType meshType = MeshType.Existing;
        public LayerMask includeLayerMask;
        [UnityEngine.Serialization.FormerlySerializedAs("profile")]
        [SerializeField]
        private MystifyEffectProfile _sharedProfile;

        // Direct shared profile reference. Mirrors Renderer.sharedMaterial semantics:
        // - If an instance has been created via `profile`, this returns that instance
        // - Setting this assigns the shared asset and clears any per-instance clone
        public MystifyEffectProfile sharedProfile {
            get {
                if (_profileInstance != null && _profileInstance.owner == this) return _profileInstance;
                return _sharedProfile;
            }
            set {
                if (_sharedProfile == value) return;
                // Assign new shared asset and drop any internal instance
                if (_profileInstance != null) {
                    DestroyImmediate(_profileInstance);
                    _profileInstance = null;
                }
                _sharedProfile = value;
				Refresh();
            }
        }

        // Per-instance profile that clones from sharedProfile when needed
        MystifyEffectProfile _profileInstance;

        // Public property to get/set the per-instance profile
        public MystifyEffectProfile profile {
            get {
                if (_profileInstance != null) {
                    if (_profileInstance.owner == this) return _profileInstance;
                    // If owner mismatch, destroy and recreate
                    DestroyImmediate(_profileInstance);
                    _profileInstance = null;
                }
                if (_sharedProfile == null) return null;
                _profileInstance = Instantiate(_sharedProfile);
                _profileInstance.owner = this;
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
#endif
                return _profileInstance;
            }
            set {
                if (value == _profileInstance) return;
                if (_profileInstance != null) {
                    DestroyImmediate(_profileInstance);
                    _profileInstance = null;
                }
                if (value != null) {
                    _profileInstance = Instantiate(value);
                    _profileInstance.owner = this;
                }
				Refresh();
            }
        }

        public readonly static List<MystifyEffect> instances = new List<MystifyEffect>();

        [NonSerialized]
        public readonly List<HitRequest> hitRequests = new List<HitRequest>();

        [SerializeField]
        public List<Renderer> customRenderers;

        [NonSerialized]
        public List<Renderer> renderers;

        [NonSerialized]
        public Material mat;

        [NonSerialized]
        public bool writesToDepth;

        [NonSerialized]
        public bool needsUpdateMaterial;

        static readonly List<string> keywords = new List<string>();
        static string[] keywordsArray;

        void OnEnable () {
            if (!instances.Contains(this)) {
                instances.Add(this);
            }
            needsUpdateMaterial = true;
        }

        void OnDisable () {
            if (instances.Contains(this)) {
                instances.Remove(this);
            }
        }


        void OnDestroy () {
            if (mat != null) {
                DestroyImmediate(mat);
            }
			if (_profileInstance != null) {
				DestroyImmediate(_profileInstance);
				_profileInstance = null;
			}
        }

        void OnValidate () {
            Refresh();
        }

        public void Refresh () {
            renderers = null;
            MystifyEffectProfile p = sharedProfile; // do not force instantiation
            if (p != null) {
                p.ValidateSettings();
            }
            needsUpdateMaterial = true;
        }

        public virtual void GetRenderers () {
            if (renderers != null) {
                renderers.Clear();
            }
            else {
                renderers = new List<Renderer>();
            }
            switch (includeType) {
                case IncludeType.OnlyThisObject:
                    Renderer r = GetComponent<Renderer>();
                    if (r == null) {
                        includeType = IncludeType.IncludeChildren;
                        GetRenderers();
                        return;
                    }
                    else {
                        renderers.Add(r);
                    }
                    break;
                case IncludeType.ByLayer:
                    GetRenderersByLayer();
                    break;
                case IncludeType.IncludeChildren:
                    GetComponentsInChildren(true, renderers);
                    break;
                case IncludeType.Custom:
                    renderers.AddRange(customRenderers);
                    break;
            }
        }

        public void GetRenderersByLayer () {
            renderers.Clear();
            foreach (Renderer renderer in Misc.FindObjectsOfType<Renderer>(true)) {
                if ((1 << renderer.gameObject.layer & includeLayerMask) != 0) {
                    renderers.Add(renderer);
                }
            }
        }

        public void UpdateMaterialProperties () {
            needsUpdateMaterial = true;
        }

        public void UpdateMaterialPropertiesNow () {

			MystifyEffectProfile p = sharedProfile; // use current assigned profile without forcing instantiation
            if (p == null || !needsUpdateMaterial) return;
            needsUpdateMaterial = false;

            if (mat == null) {
                mat = Resources.Load<Material>("Materials/MystifyFX");
                mat = Instantiate(mat);
            }

            // Rendering settings
            mat.SetInt(ShaderParams.CullMode, (int)p.cullMode);
            mat.SetInt(ShaderParams.ZTest, p.renderOnTop ? (int)CompareFunction.Always : (int)CompareFunction.LessEqual);

            bool uses3Deffects = p.intersection || p.fakeLightIntensity > 0;
            if (uses3Deffects) {
                MystifyFXRendererFeature.usesCameraDepthTexture = true;
            }

            writesToDepth = p.intersection;
            mat.SetInt(ShaderParams.ZWrite, writesToDepth ? 1 : 0);
            mat.SetTexture(ShaderParams.maskTex, p.maskTexture);
            mat.SetInt(ShaderParams.textureSource, (int)p.textureSource);
            mat.SetFloat(ShaderParams.alphaCutoff, meshType == MeshType.Existing ? p.alphaCutoff : 0);

            keywords.Clear();

            // Wrap mode
            if (p.wrapMode == WrapMode.Repeat) {
                keywords.Add(ShaderParams.SKW_WRAP_MODE);
            }

            // Blur
            if (p.blurIntensity > 0) {
                keywords.Add(ShaderParams.SKW_BLUR);
            }

            // LUT
            switch (p.lutEffect) {
                case ColorEffect.LUT:
                    bool hasLut = p.lutIntensity > 0 && p.lutTexture != null;
                    bool hasLut3D = hasLut && p.lutTexture is Texture3D;

                    if (hasLut || hasLut3D) {
                        if (hasLut3D) {
                            mat.SetTexture(ShaderParams.lut3DTexture, p.lutTexture);
                            float x = 1f / p.lutTexture.width;
                            float y = p.lutTexture.width - 1f;
                            mat.SetVector(ShaderParams.lut3DParams, new Vector4(x * 0.5f, x * y, 0, 0));
                            keywords.Add(ShaderParams.SKW_LUT3D);
                        }
                        else {
                            mat.SetTexture(ShaderParams.lutTex, p.lutTexture);
                            keywords.Add(ShaderParams.SKW_LUT);
                        }
                    }
                    break;
                case ColorEffect.Thermal:
                    keywords.Add(ShaderParams.SKW_THERMAL);
                    break;
                case ColorEffect.DirectionalTint:
                    if (p.tintGradientTex == null) {
                        p.CheckTintColors();
                    }
                    mat.SetTexture(ShaderParams.tintGradientTex, p.tintGradientTex == null ? Texture2D.whiteTexture : p.tintGradientTex);
                    mat.SetVector(ShaderParams.tintPos1, p.tintPos1);
                    mat.SetVector(ShaderParams.tintPos2, p.tintPos2);
                    mat.SetInt(ShaderParams.tintApplyAlphaToAll, p.tintApplyAlphaToAll ? 1 : 0);
                    keywords.Add(ShaderParams.SKW_DIRECTIONAL_TINT);
                    break;
            }

            // Color boost
            mat.SetVector(ShaderParams.colorBoost, new Vector4(p.brightness, p.contrast, p.vibrance, p.globalOpacity));
            mat.SetVector(ShaderParams.scaleOffset, new Vector4(p.sourceScale.x, p.sourceScale.y, p.sourceOffset.x, p.sourceOffset.y));
            mat.SetVector(ShaderParams.uvScaleOffset, new Vector4(p.uvScale.x, p.uvScale.y, p.uvOffset.x, p.uvOffset.y));
            mat.SetVector(ShaderParams.fxData, new Vector4(p.lutIntensity, p.pixelate > 0 ? Screen.width / (1 + p.pixelate) : 0, p.scanLinesScreenSpace ? 1 : 0, p.distortionFrequency * Mathf.PI));
            mat.SetVector(ShaderParams.fxData2, new Vector4(Screen.width / p.noiseSize, p.noiseAmount, p.intersectionThickness, p.blurIntensity));
            mat.SetVector(ShaderParams.fxData3, new Vector4(p.scaleFactor, p.hazeIntensity, p.hazeThreshold, p.distortionAnimationSpeed * 60f));
            mat.SetVector(ShaderParams.fxData7, new Vector4(p.scanLinesIntensity > 0f ? 1f / p.scanLinesSize : 0, p.scanLinesAnimationSpeed * 20f, 1f / p.zoomFactor, p.intersectionNoise ? 0.002f : 0));
            mat.SetVector(ShaderParams.fxData11, new Vector4(p.vertexDistortion * 0.1f, p.vertexDistortionSpeed, p.glassWaterDropIntensity, p.scanLinesMaxDistance));

            float distortion = p.distortion ? 1f : 0f;
            mat.SetVector(ShaderParams.fxData10, new Vector4(p.distortionAmplitude.x * distortion, p.distortionAmplitude.y * distortion, p.distortionFalloff, p.distortionRimPower));

            if (p.scanLinesIntensity > 0) {
                mat.SetVector(ShaderParams.scanLinesColor, new Vector4(p.scanLinesColor.r, p.scanLinesColor.g, p.scanLinesColor.b, p.scanLinesIntensity));
                float scanLinesRotationRad = p.scanLinesRotation * Mathf.Deg2Rad;
                Vector3 scale = transform.lossyScale;
                float objectAspect = scale.x / Mathf.Max(scale.y, 0.0001f);
                mat.SetVector(ShaderParams.scanLinesRotation, new Vector4(Mathf.Sin(scanLinesRotationRad), Mathf.Cos(scanLinesRotationRad), objectAspect, 0));
            }

            if (p.hazeIntensity > 0) {
                mat.SetVector(ShaderParams.fxData4, new Vector4(p.hazeScale.x, p.hazeScale.y, p.hazeMode == MappingMode._2D ? p.hazeSpeed * 20f : p.hazeSpeed * 2f, 0));
                if (p.hazeGradientTex == null) {
                    p.CheckHazeColors();
                }
                mat.SetTexture(ShaderParams.hazeGradientTex, p.hazeGradientTex == null ? Texture2D.whiteTexture : p.hazeGradientTex);
                if (p.hazeMode == MappingMode._2D) {
                    keywords.Add(ShaderParams.SKW_HAZE_OS);
                }
                else {
                    keywords.Add(ShaderParams.SKW_HAZE_WS);
                }
            }

            if (p.rimIntensity > 0 || p.chromaticAberrationAmount > 0) {
                mat.SetColor(ShaderParams.rimColor, new Color(p.rimColor.r, p.rimColor.g, p.rimColor.b, p.rimIntensity));
                mat.SetVector(ShaderParams.fxData8, new Vector4(p.rimPower, p.rimTextureOpacity * 32f, p.rimTextureScale, p.chromaticAberrationAmount));
                if (p.chromaticAberrationAmount > 0) {
                    mat.SetVector(ShaderParams.fxData13, new Vector4(p.chromaticAberrationEdgePower, 0, 0, 0));
                }
                if (p.rimTexture != null) {
                    mat.SetTexture(ShaderParams.rimTexture, p.rimTexture);
                }
                keywords.Add(ShaderParams.SKW_RIM);
            }


            mat.SetVector(ShaderParams.fxData5, new Vector4(p.frostIntensity, p.frostSpread, p.crystalize, p.crystalizeSpread));
            if (p.frostIntensity > 0 || p.crystalize > 0) {
                if (p.crystalize > 0) {
                    mat.SetVector(ShaderParams.fxData6, new Vector4(p.crystalizeScale.x, p.crystalizeScale.y, p.crystalizeAnimationSpeed.x * 20f, p.crystalizeAnimationSpeed.y * 20f));
                    keywords.Add(ShaderParams.SKW_CRYSTALIZE);
                }
                if (p.frostIntensity > 0) {
                    mat.SetColor(ShaderParams.frostColor, p.frostColor);
                    keywords.Add(ShaderParams.SKW_FROST);
                }
            }

            if (p.intersection) {
                mat.SetColor(ShaderParams.intersectionColor, p.intersectionColor);
                keywords.Add(ShaderParams.SKW_INTERSECTION);
            }

            if (p.fakeLight) {
                if (p.fakeLightGradientTex == null) {
                    p.CheckFakeLightColors();
                }
                mat.SetVector(ShaderParams.fxData12, new Vector4(p.fakeLightIntensity, p.fakeLightFog, (1f - p.fakeLightFalloff) * 0.99f, p.fakeLightSpeed * 20f));
                mat.SetTexture(ShaderParams.fakeLightGradientTex, p.fakeLightGradientTex == null ? Texture2D.whiteTexture : p.fakeLightGradientTex);
                keywords.Add(ShaderParams.SKW_FAKE_LIGHT);
            }

            if (p.rain) {
                mat.SetVector(ShaderParams.fxData9, new Vector4(p.glassTinyDrops, p.glassLargeDrops, p.rainfall, p.rainWind));
                mat.SetVector(ShaderParams.fxData14, new Vector4(p.rainSpeed, p.glassTinyDropsGridSize, p.glassLargeDropsGridSize, p.glassLargeDropsSpeed * 20f));
                keywords.Add(ShaderParams.SKW_RAIN);
            }

            int keywordsCount = keywords.Count;
            if (keywordsArray == null || keywordsArray.Length < keywordsCount) {
                keywordsArray = new string[keywordsCount];
            }
            int keywordsArrayCount = keywordsArray.Length;
            for (int k = 0; k < keywordsArrayCount; k++) {
                if (k < keywordsCount) {
                    keywordsArray[k] = keywords[k];
                }
                else {
                    keywordsArray[k] = "";
                }
            }
            mat.shaderKeywords = keywordsArray;
        }


        public void HitFX (Vector3 position, float intensity = 0, Color color = default, float radius = 0, float speed = 0, float noiseAmount = 0, float noiseScale = 0) {
            MystifyEffectProfile p = sharedProfile;
            if (p == null) return;
            if (intensity <= 0) {
                intensity = p.hitIntensity;
            }
            if (intensity <= 0) return;
            if (color == default) {
                color = p.hitColor;
            }
            if (radius <= 0) {
                radius = p.hitRadius;
            }
            if (speed <= 0) {
                speed = p.hitSpeed;
            }
            if (noiseAmount <= 0) {
                noiseAmount = p.hitNoiseAmount;
            }
            if (noiseScale <= 0) {
                noiseScale = p.hitNoiseScale;
            }

            hitRequests.Add(new HitRequest {
                intensity = intensity,
                position = position,
                radius = radius,
                color = color,
                speed = speed,
                startTime = Time.timeSinceLevelLoad,
                noiseScale = noiseScale * 5f,
                noiseAmount = noiseAmount * 0.2f
            });
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected () {
            if (!enabled || meshType == MeshType.Existing) return;

            Gizmos.color = new Color(0, 1, 0, 0.5f);
            Matrix4x4 matrix = transform.localToWorldMatrix;

            switch (meshType) {
                case MeshType.Quad:
                    DrawQuadGizmo(matrix);
                    break;
                case MeshType.Disc:
                    DrawDiscGizmo(matrix);
                    break;
                case MeshType.Sphere:
                case MeshType.Hexasphere:
                    DrawSphereGizmo(matrix);
                    break;
                case MeshType.Cylinder:
                    DrawCylinderGizmo(matrix);
                    break;
            }
        }

        private void DrawQuadGizmo (Matrix4x4 matrix) {
            Vector3[] corners = new Vector3[] {
                new Vector3(-0.5f, -0.5f, 0),
                new Vector3(0.5f, -0.5f, 0),
                new Vector3(0.5f, 0.5f, 0),
                new Vector3(-0.5f, 0.5f, 0)
            };

            for (int i = 0; i < 4; i++) {
                Vector3 start = matrix.MultiplyPoint(corners[i]);
                Vector3 end = matrix.MultiplyPoint(corners[(i + 1) % 4]);
                Gizmos.DrawLine(start, end);
            }

            // Only show warning if shared profile exists and culling is enabled (avoid cloning in Editor)
            MystifyEffectProfile p = _sharedProfile;
            if (p != null && p.cullMode != CullMode.Off) {
                Vector3 quadNormal = matrix.MultiplyVector(Vector3.forward);
                Vector3 toCam = SceneView.lastActiveSceneView.camera.transform.position - transform.position;
                bool isBackface = Vector3.Dot(quadNormal, toCam) > 0;

                // Show warning if looking at back face with back culling, or front face with front culling
                if ((isBackface && p.cullMode == CullMode.Back) || (!isBackface && p.cullMode == CullMode.Front)) {
                    GUIStyle style = new GUIStyle();
                    style.normal.textColor = new Color(0, 0, 0, 0.8f);
                    style.fontSize = 18;
                    style.alignment = TextAnchor.MiddleCenter;
                    style.richText = true;

                    Matrix4x4 prevMatrix = Handles.matrix;
                    Handles.matrix = matrix;
                    Handles.Label(new Vector3(0, 0.15f, 0), "THIS SIDE IS HIDDEN", style);
                    Handles.matrix = prevMatrix;
                }
            }
        }

        private void DrawDiscGizmo (Matrix4x4 matrix) {
            int segments = 32;
            float step = 360f / segments;

            for (int i = 0; i < segments; i++) {
                float angle1 = step * i * Mathf.Deg2Rad;
                float angle2 = step * (i + 1) * Mathf.Deg2Rad;

                Vector3 pos1 = new Vector3(Mathf.Cos(angle1) * 0.5f, Mathf.Sin(angle1) * 0.5f, 0);
                Vector3 pos2 = new Vector3(Mathf.Cos(angle2) * 0.5f, Mathf.Sin(angle2) * 0.5f, 0);

                Gizmos.DrawLine(matrix.MultiplyPoint(pos1), matrix.MultiplyPoint(pos2));
            }
        }

        private void DrawSphereGizmo (Matrix4x4 matrix) {
            Gizmos.matrix = matrix;
            Gizmos.DrawWireSphere(Vector3.zero, 0.5f);
            Gizmos.matrix = Matrix4x4.identity;
        }

        private void DrawCylinderGizmo (Matrix4x4 matrix) {
            Gizmos.matrix = matrix * Matrix4x4.Scale(new Vector3(1, 2, 1));
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
            Gizmos.matrix = Matrix4x4.identity;
        }

#endif

    }

}