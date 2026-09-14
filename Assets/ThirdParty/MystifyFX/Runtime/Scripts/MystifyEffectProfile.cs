using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace MystifyFX {

    public enum ColorEffect {
        None,
        LUT,
        Thermal,
        DirectionalTint
    }

    public enum WrapMode {
        Repeat,
        Clamp
    }

    public enum MappingMode {
        [InspectorName("2D")]
        _2D,
        [InspectorName("3D")]
        _3D
    }

    public enum TextureSource {
        Screen,
        Object
    }

    [CreateAssetMenu(fileName = "MystifyFXProfile", menuName = "Mystify FX Profile", order = 1200)]
    [ExecuteAlways]
    public class MystifyEffectProfile : ScriptableObject {
        // Reference to the MystifyEffect that owns this internal profile
        [HideInInspector]
        public MystifyEffect owner;

        const int BAKED_GRADIENTS_LENGTH = 256; // number of baked values for the gradients

        public CullMode cullMode = CullMode.Back;
        public WrapMode wrapMode = WrapMode.Repeat;
        public TextureSource textureSource = TextureSource.Screen;
        [Tooltip("If true, the effect will be rendered always on top of other objects in the scene.")]
        public bool renderOnTop;
        [Tooltip("If true, the effect will be rendered over the results from other Mystify FX objects.")]
        public bool grabPass;
        [Tooltip("If true, the effect will be rendered even if the renderer is disabled.")]
        public bool alwaysOn = true;
        [Tooltip("Can be used to control where the effects are applied. The alpha channel specifies the global effect intensity.")]
        public Texture2D maskTexture;
        [Tooltip("Pixels with alpha less than this value will be discarded.")]
        [Range(0, 1)]
        public float alphaCutoff;
        [Range(0, 1)]
        public float globalOpacity = 1f;
        [Tooltip("If true, effects with animation speed will get a stop motion effect.")]
        public bool useStopMotion;
        public float stopMotionInterval = 0.125f;

        public float scaleFactor = 1f;
        public float zoomFactor = 1f;
        public Vector2 sourceOffset;
        public Vector2 sourceScale = Vector2.one;
        public Vector2 uvOffset;
        public Vector2 uvScale = Vector2.one;
        public float vertexDistortion;
        public float vertexDistortionSpeed = 1f;
        [Range(0, 1)]
        public float blurIntensity;
        public ColorEffect lutEffect = ColorEffect.None;
        public Texture lutTexture;
        [Range(0, 1)]
        public float lutIntensity = 1f;
        [GradientUsage(hdr: true)]
        public Gradient tintGradient;
        public Vector3 tintPos1 = new Vector3(0, -0.5f, 0);
        public Vector3 tintPos2 = new Vector3(0, 0.5f, 0);
        public bool tintApplyAlphaToAll;
        [Range(1, 32)]
        public float noiseSize = 2;
        [Range(0, 1)]
        public float noiseAmount;
        public bool distortion;
        public Vector2 distortionAmplitude;
        public float distortionFrequency = 3;
        public float distortionAnimationSpeed = 1f;
        public float distortionFalloff = 5f;
        [Tooltip("Increase to limit the distortion to the edges of the object")]
        public float distortionRimPower;
        [Range(0, 255)]
        public int pixelate;

        [Range(0, 1)]
        public float rimIntensity;
        public float rimPower = 5f;
        [ColorUsage(showAlpha: false, hdr: true)]
        public Color rimColor = Color.yellow;
        public Texture2D rimTexture;
        [Range(0, 1)]
        public float rimTextureOpacity = 1f;
        public float rimTextureScale = 8f;
        [Range(0, 0.01f)]
        public float chromaticAberrationAmount;
        [Range(0, 1f)]
        public float chromaticAberrationEdgePower;

        [Range(0, 1)]
        public float scanLinesIntensity;
        public bool scanLinesScreenSpace;
        public int scanLinesSize = 4;
        public float scanLinesAnimationSpeed;
        [ColorUsage(showAlpha: false, hdr: true)]
        public Color scanLinesColor = Color.black;
        [Range(0.001f, 10f)]
        public float scanLinesMaxDistance = 2f;
        [Range(-180f, 180f)]
        public float scanLinesRotation;

        public float hazeIntensity;
        public float hazeThreshold = 0.5f;
        public MappingMode hazeMode = MappingMode._3D;
        [GradientUsage(hdr: false)]
        public Gradient hazeGradient;
        public Vector2 hazeScale = new Vector2(0.2f, 0.2f);
        public float hazeSpeed = 1f;

        public float frostIntensity;
        public float frostSpread = 2.5f;
        public Color frostColor = Color.white;

        [Range(0, 0.2f)]
        public float crystalize;
        public float crystalizeSpread;
        public Vector2 crystalizeAnimationSpeed;
        public Vector2 crystalizeScale = new Vector2(1f, 1f);

        public bool intersection;
        [Range(0.1f, 2)]
        public float intersectionThickness = 0.5f;
        [ColorUsage(showAlpha: true, hdr: true)]
        public Color intersectionColor = Color.white;
        public bool intersectionNoise = true;

        public bool rain;
        [Range(0, 1)]
        public float glassTinyDrops = 0.6f;
        [Range(48, 256)]
        public int glassTinyDropsGridSize = 64;
        [Range(0, 1)]
        public float glassLargeDrops = 0.7f;
        [Range(9, 24)]
        public int glassLargeDropsGridSize = 12;
        [Range(-10, 10)]
        public float glassLargeDropsSpeed = 1f;
        [Range(0, 1)]
        public float glassWaterDropIntensity = 1f;
        [Range(0, 1)]
        public float rainfall = 0.15f;
        [Range(-1, 1)]
        public float rainWind;
        [Range(-2f, 2f)]
        public float rainSpeed = 1f;

        public bool fakeLight;
        public float fakeLightIntensity;
        public float fakeLightFog = 0.15f;
        [GradientUsage(hdr: false)]
        public Gradient fakeLightGradient;
        [Range(0, 1)]
        public float fakeLightFalloff = 0.5f;
        public float fakeLightSpeed;

        public bool hexaGrid;
        public float hexaGridScale = 1f;
        [ColorUsage(showAlpha: true, hdr: true)]
        public Color hexaGridColor = new Color(1, 1, 1, 0.35f);
        public float hexaGridRimPower;
        public float hexaGridNoiseStrength = 5f;
        [ColorUsage(showAlpha: true, hdr: true)]
        public Color hexaGridNoiseColor = new Color(0.5f, 0.5f, 1f, 0.65f);
        [Range(0, 1)]
        public float hexaGridNoiseThreshold = 0.3f;
        public float hexaGridNoiseScale = 0.2f;
        public float hexaGridSweepAmount;
        public float hexaGridSweepSpeed = 1f;
        public float hexaGridSweepFrequency = 1.5f;

        [Range(0, 1)]
        public float hitIntensity;
        [ColorUsage(showAlpha: false, hdr: true)]
        public Color hitColor = Color.yellow;
        public float hitRadius = 0.2f;
        public float hitSpeed = 1f;
        [Range(0, 1f)]
        public float hitNoiseScale = 1f;
        [Range(0, 1f)]
        public float hitNoiseAmount = 1f;

        /*
                public Texture2D patternTexture;
                public float patternIntensity;
                public float patternScale = 1f;
                [ColorUsage(showAlpha: false, hdr: true)]
                public Color patternColor = Color.white;
                public Texture2D patternMask;
                [Range(0, 1)]
                public float patternMaskThreshold = 1f;
                public float patternMaskScale = 1f;
        */

        [Range(0, 2f)]
        public float brightness = 1f;
        [Range(0.5f, 1.5f)]
        public float contrast = 1f;
        [Range(-2, 3f)]
        public float vibrance;


        Color[] hazeColors;
        public Texture2D hazeGradientTex;

        Color[] tintColors;
        public Texture2D tintGradientTex;

        Color[] fakeLightColors;
        public Texture2D fakeLightGradientTex;


        void OnEnable () {
            ValidateSettings();
        }

        void OnValidate () {
            ValidateSettings();
            MystifyEffect[] effects = Misc.FindObjectsOfType<MystifyEffect>(true);
            foreach (MystifyEffect effect in effects) {
                // Use sharedProfile to avoid forcing per-instance clone during validation in Editor
                if (effect.sharedProfile == this) {
                    effect.UpdateMaterialProperties();
                }
            }
        }

        public void ValidateSettings () {
            vertexDistortion = Mathf.Max(vertexDistortion, 0f);
            vertexDistortionSpeed = Mathf.Max(vertexDistortionSpeed, 0f);
            stopMotionInterval = Mathf.Max(stopMotionInterval, 0.001f);
            scanLinesSize = Mathf.Max(scanLinesSize, 2);
            scaleFactor = Mathf.Max(scaleFactor, 1f);
            zoomFactor = Mathf.Max(zoomFactor, 1f);
            hazeIntensity = Mathf.Max(hazeIntensity, 0f);
            frostIntensity = Mathf.Max(frostIntensity, 0f);
            frostSpread = Mathf.Max(frostSpread, 0f);
            crystalize = Mathf.Max(crystalize, 0f);
            crystalizeSpread = Mathf.Max(crystalizeSpread, 0f);
            distortionFrequency = Mathf.Max(distortionFrequency, 0f);
            distortionFalloff = Mathf.Max(distortionFalloff, 0f);
            fakeLightIntensity = Mathf.Max(fakeLightIntensity, 0f);
            fakeLightFog = Mathf.Max(fakeLightFog, 0f);
            distortionRimPower = Mathf.Max(distortionRimPower, 0f);
            rimPower = Mathf.Max(rimPower, 1f);
            rimTextureScale = Mathf.Max(rimTextureScale, 0.0001f);
            hitRadius = Mathf.Max(hitRadius, 0f);
            hitSpeed = Mathf.Max(hitSpeed, 0f);
            hexaGridRimPower = Mathf.Max(hexaGridRimPower, 0f);
            hexaGridNoiseStrength = Mathf.Max(hexaGridNoiseStrength, 0f);
            hexaGridNoiseScale = Mathf.Max(hexaGridNoiseScale, 0f);
            hexaGridSweepAmount = Mathf.Max(hexaGridSweepAmount, 0f);
            hexaGridSweepFrequency = Mathf.Max(hexaGridSweepFrequency, 0.01f);
            // patternIntensity = Mathf.Max(patternIntensity, 0);
            // if (patternTexture == null) {
            //     patternTexture = Resources.Load<Texture2D>("Textures/DefaultPattern");
            // }
            CheckHazeColors();
            CheckTintColors();
            CheckFakeLightColors();
        }

        public void CheckHazeColors () {
            if (hazeGradient == null) {
                hazeGradient = new Gradient();
                Color baseColor = new Color(0.1f, 0, 0.5f);
                hazeGradient.colorKeys = new GradientColorKey[] { new GradientColorKey(baseColor, 0), new GradientColorKey(baseColor, 1) };
            }
            bool requiresUpdate = false;
            if (hazeGradientTex == null) {
                hazeGradientTex = new Texture2D(BAKED_GRADIENTS_LENGTH, 1, TextureFormat.RGBA32, mipChain: false);
                hazeGradientTex.wrapMode = TextureWrapMode.Clamp;
                requiresUpdate = true;
            }
            if (hazeColors == null || hazeColors.Length != BAKED_GRADIENTS_LENGTH) {
                hazeColors = new Color[BAKED_GRADIENTS_LENGTH];
                requiresUpdate = true;
            }
            for (int k = 0; k < BAKED_GRADIENTS_LENGTH; k++) {
                float t = (float)k / BAKED_GRADIENTS_LENGTH;
                Color color = hazeGradient.Evaluate(t);
                if (color != hazeColors[k]) {
                    hazeColors[k] = color;
                    requiresUpdate = true;
                }
            }
            if (requiresUpdate) {
                hazeGradientTex.SetPixels(hazeColors);
                hazeGradientTex.Apply();
            }
        }


        public void CheckTintColors () {
            if (tintGradient == null || tintGradient.colorKeys.Length == 0) {
                tintGradient = new Gradient();
                Color baseColor = new Color(1, 1, 1f, 0);
                tintGradient.colorKeys = new GradientColorKey[] { new GradientColorKey(baseColor, 0), new GradientColorKey(baseColor, 1) };
                tintGradient.alphaKeys = new GradientAlphaKey[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0, 1) };
            }
            bool requiresUpdate = false;
            if (tintGradientTex == null) {
                tintGradientTex = new Texture2D(BAKED_GRADIENTS_LENGTH, 1, TextureFormat.RGBA32, mipChain: false);
                tintGradientTex.wrapMode = TextureWrapMode.Clamp;
                requiresUpdate = true;
            }
            if (tintColors == null || tintColors.Length != BAKED_GRADIENTS_LENGTH) {
                tintColors = new Color[BAKED_GRADIENTS_LENGTH];
                requiresUpdate = true;
            }
            for (int k = 0; k < BAKED_GRADIENTS_LENGTH; k++) {
                float t = (float)k / (BAKED_GRADIENTS_LENGTH - 1);
                Color color = tintGradient.Evaluate(t);
                if (color != tintColors[k]) {
                    tintColors[k] = color;
                    requiresUpdate = true;
                }
            }
            if (requiresUpdate) {
                tintGradientTex.SetPixels(tintColors);
                tintGradientTex.Apply();
            }
        }

        public void CheckFakeLightColors () {
            if (fakeLightGradient == null || fakeLightGradient.colorKeys.Length == 0) {
                fakeLightGradient = new Gradient();
                Color baseColor = new Color(1, 1, 1f);
                fakeLightGradient.colorKeys = new GradientColorKey[] { new GradientColorKey(baseColor, 0), new GradientColorKey(baseColor, 1) };
            }
            bool requiresUpdate = false;
            if (fakeLightGradientTex == null) {
                fakeLightGradientTex = new Texture2D(BAKED_GRADIENTS_LENGTH, 1, TextureFormat.RGBA32, mipChain: false);
                fakeLightGradientTex.wrapMode = TextureWrapMode.Repeat;
                requiresUpdate = true;
            }
            if (fakeLightColors == null || fakeLightColors.Length != BAKED_GRADIENTS_LENGTH) {
                fakeLightColors = new Color[BAKED_GRADIENTS_LENGTH];
                requiresUpdate = true;
            }
            for (int k = 0; k < BAKED_GRADIENTS_LENGTH; k++) {
                float t = (float)k / BAKED_GRADIENTS_LENGTH;
                Color color = fakeLightGradient.Evaluate(t);
                if (color != fakeLightColors[k]) {
                    fakeLightColors[k] = color;
                    requiresUpdate = true;
                }
            }
            if (requiresUpdate) {
                fakeLightGradientTex.SetPixels(fakeLightColors);
                fakeLightGradientTex.Apply();
            }
        }
    }
}