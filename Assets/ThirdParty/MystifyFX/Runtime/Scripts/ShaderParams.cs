using UnityEngine;

namespace MystifyFX {

    static class ShaderParams {
        public static int mainTex = Shader.PropertyToID("_MainTex");
        public static int inputTex = Shader.PropertyToID("_InputTex");
        public static int cameraOpaqueTex = Shader.PropertyToID("_CameraOpaqueTexture");
        public static int blurRT = Shader.PropertyToID("_BlurTex");
        public static int rimTexture = Shader.PropertyToID("_RimTexture");
        public static int decalTexture = Shader.PropertyToID("_DecalTex");
        public static int hazeGradientTex = Shader.PropertyToID("_HazeGradientTex");
        public static int fakeLightGradientTex = Shader.PropertyToID("_FakeLightGradientTex");
        public static int patternTex = Shader.PropertyToID("_PatternTex");
        public static int patternMask = Shader.PropertyToID("_PatternMask");
        public static int maskTex = Shader.PropertyToID("_MaskTex");
        public static int cutOutTex = Shader.PropertyToID("_CutOutTex");
        public static int cutOutTexST = Shader.PropertyToID("_CutOutTex_ST");
        public static int alphaCutoff = Shader.PropertyToID("_AlphaCutoff");
        
        public static int CullMode = Shader.PropertyToID("_CullMode");
        public static int ZWrite = Shader.PropertyToID("_ZWrite");
        public static int ZTest = Shader.PropertyToID("_ZTest");
        public static int textureSource = Shader.PropertyToID("_TextureSource");

        public static int animationTime = Shader.PropertyToID("_AnimationTime");
        public static int scaleOffset = Shader.PropertyToID("_ScaleOffset");
        public static int uvScaleOffset = Shader.PropertyToID("_UVScaleOffset");
        public static int fxData = Shader.PropertyToID("_FXData");
        public static int fxData2 = Shader.PropertyToID("_FXData2");
        public static int fxData3 = Shader.PropertyToID("_FXData3");
        public static int fxData4 = Shader.PropertyToID("_FXData4");
        public static int fxData5 = Shader.PropertyToID("_FXData5");
        public static int fxData6 = Shader.PropertyToID("_FXData6");
        public static int fxData7 = Shader.PropertyToID("_FXData7");
        public static int fxData8 = Shader.PropertyToID("_FXData8");
        public static int fxData9 = Shader.PropertyToID("_FXData9");
        public static int fxData10 = Shader.PropertyToID("_FXData10");
        public static int fxData11 = Shader.PropertyToID("_FXData11");
        public static int fxData12 = Shader.PropertyToID("_FXData12");
        public static int fxData13 = Shader.PropertyToID("_FXData13");
        public static int fxData14 = Shader.PropertyToID("_FXData14");

        public static int wireData = Shader.PropertyToID("_WireData");
        public static int wireData2 = Shader.PropertyToID("_WireData2");
        public static int wireData3 = Shader.PropertyToID("_WireData3");
        public static int wireColor = Shader.PropertyToID("_WireColor");
        public static int noiseColor = Shader.PropertyToID("_NoiseColor");
        public static int lutTex = Shader.PropertyToID("_LUTTex");
        public static int lut3DTexture = Shader.PropertyToID("_LUT3DTex");
        public static int lut3DParams = Shader.PropertyToID("_LUT3DParams");
        public static int colorBoost = Shader.PropertyToID("_ColorBoost");

        public static int tintGradientTex = Shader.PropertyToID("_TintGradientTex");
        public static int tintPos1 = Shader.PropertyToID("_TintPos1");
        public static int tintPos2 = Shader.PropertyToID("_TintPos2");
        public static int tintApplyAlphaToAll = Shader.PropertyToID("_TintApplyAlphaToAll");

        public static int blurScale = Shader.PropertyToID("_BlurScale");
        public static int highlightIntensity = Shader.PropertyToID("_HighlightIntensity");
        public static int highlightThreshold = Shader.PropertyToID("_HighlightThreshold");
        public static int tempBlurRT = Shader.PropertyToID("_LocalBlurFXTempBlurRT");
        public static int tempBlurDownscaling = Shader.PropertyToID("_LocalBlurFXTempBlurDownscaling");

        public static int frostColor = Shader.PropertyToID("_FrostColor");
        public static int scanLinesColor = Shader.PropertyToID("_ScanLinesColor");
        public static int scanLinesRotation = Shader.PropertyToID("_ScanLinesRotation");
        public static int intersectionColor = Shader.PropertyToID("_IntersectionColor");
        public static int rimColor = Shader.PropertyToID("_RimColor");
        public static int patternColor = Shader.PropertyToID("_PatternColor");
        
        public static int hitData = Shader.PropertyToID("_HitData");
        public static int hitPosition = Shader.PropertyToID("_HitPosition");
        public static int hitColor = Shader.PropertyToID("_HitColor");
        public static int scale = Shader.PropertyToID("_Scale");

        public const string SKW_BLUR = "_MYSTIFYFX_FX_BLUR";
        public const string SKW_LUT = "_MYSTIFYFX_FX_LUT";
        public const string SKW_LUT3D = "_MYSTIFYFX_FX_3DLUT";
        public const string SKW_THERMAL = "_MYSTIFYFX_FX_THERMAL";
        public const string SKW_DIRECTIONAL_TINT = "_MYSTIFYFX_FX_DIRECTIONAL_TINT";
        public const string SKW_HAZE_OS = "_MYSTIFYFX_FX_HAZE_OS";
        public const string SKW_HAZE_WS = "_MYSTIFYFX_FX_HAZE_WS";
        public const string SKW_FROST = "_MYSTIFYFX_FX_FROST";
        public const string SKW_CRYSTALIZE = "_MYSTIFYFX_FX_CRYSTALIZE";
        public const string SKW_RIM = "_MYSTIFYFX_FX_RIM";
        public const string SKW_WRAP_MODE = "_MYSTIFYFX_FX_WRAP_MODE";
        public const string SKW_INTERSECTION = "_MYSTIFYFX_FX_INTERSECTION";
        public const string SKW_RAIN = "_MYSTIFYFX_FX_RAIN";
        public const string SKW_FAKE_LIGHT = "_MYSTIFYFX_FX_FAKE_LIGHT";
    }
}