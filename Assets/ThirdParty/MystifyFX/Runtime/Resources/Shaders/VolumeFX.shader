Shader "Kronnect/MystifyFX/VolumeFX"
{
    Properties
    {
        [MainTexture] _BaseMap ("Texture", 2D) = "white" {}
        [MainColor] _BaseColor("Color", Color) = (1, 1, 1, 1)
        _MaskTex ("Mask Texture", 2D) = "white" {}
        _NoiseTex ("Noise Texture", 2D) = "white" {}
        _FrostTex ("Frost Texture", 2D) = "white" {}
        _RimTexture ("Rim Texture", 2D) = "black" {}
        _FrostNormals ("Frost Normals", 2D) = "bump" {}
        _LUTTex("LUT Texture", 2D) = "white" {}
        _LUT3DTex("LUT 3D Texture", 3D) = "" {}
        _HazeGradientTex("Haze Gradient Texture", 2D) = "white" {}
        _FrostIntensity("Frost Intensity", Vector) = (0, 0, 0)
        _FrostTintColor("Frost Tint Color", Color) = (1, 1, 1, 1)
        _UVScaleOffset("Scale Offset", Vector) = (1, 1, 0, 0)
        _ScaleOffset("Scale Offset", Vector) = (1, 1, 0, 0)
        _AlphaCutoff ("Alpha Cutoff", Range(0, 1)) = 0
        _FXData("FX Data", Vector) = (0, 0, 0, 0)
        _FXData2("FX Data 2", Vector) = (0, 0, 0, 0)
        _FXData3("FX Data 3", Vector) = (0, 0, 0, 0)
        _FXData4("FX Data 4", Vector) = (0, 0, 0, 0)
        _FXData5("FX Data 5", Vector) = (0, 0, 0, 0)
        _FXData6("FX Data 6", Vector) = (0, 0, 0, 0)
        _FXData7("FX Data 7", Vector) = (0, 0, 0, 0)
        _FXData8("FX Data 8", Vector) = (0, 0, 0, 0)
        _FXData9("FX Data 9", Vector) = (0, 0, 0, 0)
        _FXData10("FX Data 10", Vector) = (0, 0, 0, 0)
        _FXData11("FX Data 11", Vector) = (0, 0, 1, 0)
        _FXData12("FX Data 12", Vector) = (0, 0, 0, 0)
        _FXData13("FX Data 13", Vector) = (0, 0, 0, 0)
        _FXData14("FX Data 14", Vector) = (0, 0, 0, 0)
        _LUT3DParams("LUT 3D Params", Vector) = (0, 0, 0, 0)
        _ScanLinesColor("Scan Lines Color", Color) = (1, 1, 1, 1)
        _ScanLinesRotation("Scan Lines Rotation", Vector) = (0, 1, 0, 0)
        _FrostColor("Frost Color", Color) = (1, 1, 1, 1)
        _IntersectionColor("Intersection Color", Color) = (1, 1, 1, 1)
        _RimColor("Rim Color", Color) = (1, 1, 1, 1)
        _TintGradientTex("Tint Gradient Texture", 2D) = "white" {}
        _TintPos1("Tint Position 1", Vector) = (0, 0, 0)
        _TintPos2("Tint Position 2", Vector) = (0, 0, 0)
        _TintApplyAlphaToAll("Tint Apply Alpha To All", Float) = 0
        _FakeLightGradientTex("Fake Light Gradient Texture", 2D) = "white" {}
        _PatternTex("Pattern Texture", 2D) = "black" {}
        _PatternColor("Pattern Color", Color) = (1, 1, 1, 1)
        _PatternMask("Pattern Mask", 2D) = "white" {}
        _PatternMaskThreshold("Pattern Mask Threshold", Float) = 1
        _ColorBoost("Color Boost", Vector) = (1, 1, 1, 0)
        _ZWrite ("Z Write", Int) = 0
        _TextureSource("Texture Source", Int) = 0
        [Enum(UnityEngine.Rendering.CullMode)] _CullMode("Cull Mode", Int) = 2
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("Z Test", Int) = 4
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite [_ZWrite]
        Cull [_CullMode]
        ZTest [_ZTest]
        Pass
        {
            Name "Volume FX Pass"
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex UnlitPassVertex
            #pragma fragment UnlitPassFragment
            #pragma multi_compile_local_fragment _ _MYSTIFYFX_FX_BLUR
            #pragma multi_compile_local_fragment _ _MYSTIFYFX_FX_LUT _MYSTIFYFX_FX_3DLUT _MYSTIFYFX_FX_THERMAL _MYSTIFYFX_FX_DIRECTIONAL_TINT
            #pragma multi_compile_local_fragment _ _MYSTIFYFX_FX_HAZE_OS _MYSTIFYFX_FX_HAZE_WS
            #pragma multi_compile_local_fragment _ _MYSTIFYFX_FX_FROST
            #pragma multi_compile_local_fragment _ _MYSTIFYFX_FX_CRYSTALIZE
            #pragma multi_compile_local_fragment _ _MYSTIFYFX_FX_WRAP_MODE
            #pragma multi_compile_local_fragment _ _MYSTIFYFX_FX_INTERSECTION
            #pragma multi_compile_local_fragment _ _MYSTIFYFX_FX_RIM
            #pragma multi_compile_local _ _MYSTIFYFX_FX_RAIN
            #pragma multi_compile_local_fragment _ _MYSTIFYFX_FX_FAKE_LIGHT
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            #include "VolumeFX_Pass.hlsl"

            ENDHLSL
        }
    }
    
}
