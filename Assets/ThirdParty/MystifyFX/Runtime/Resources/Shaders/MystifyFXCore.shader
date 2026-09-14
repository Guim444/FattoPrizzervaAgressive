Shader "Hidden/Kronnect/MystifyFX" {

Subshader {	

    Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
    ZWrite Off ZTest Always Blend Off Cull Off

    HLSLINCLUDE
    #pragma target 3.0
    #pragma prefer_hlslcc gles
    #pragma exclude_renderers d3d11_9x
    
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/Shaders/PostProcessing/Common.hlsl"
    ENDHLSL

  Pass { // 0 
      Name "Raw Copy (Point Filtering)"
      HLSLPROGRAM
      #pragma vertex VertOS
      #pragma fragment FragCopy
      #include "MystifyFXCore.hlsl"
      ENDHLSL
  }

  Pass { // 1
      Name "Blur horizontally"
      HLSLPROGRAM
      #pragma vertex VertBlur
      #pragma fragment FragBlur
      #define BLUR_HORIZ
      #include "MystifyFXBlur.hlsl"
      ENDHLSL
  }    
      
  Pass { // 2 
      Name "Blur vertically"
	  HLSLPROGRAM
      #pragma vertex VertBlur
      #pragma fragment FragBlur
      #include "MystifyFXBlur.hlsl"
      ENDHLSL
  }    
}
FallBack Off
}
