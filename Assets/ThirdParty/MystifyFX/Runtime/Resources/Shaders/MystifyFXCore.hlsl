#ifndef MystifyFX_CORE_FX
#define MystifyFX_CORE_FX

#pragma warning (disable : 3571)

    TEXTURE2D_X(_MainTex);
    SAMPLER(sampler_MainTex);
    float4 _MainTex_TexelSize;

    #include "MystifyFXCommon.hlsl"
  

	VaryingsSimple VertMystifyFX(AttributesSimple input) {
	    VaryingsSimple output;
        UNITY_SETUP_INSTANCE_ID(input);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
        output.positionCS = input.positionOS;
        output.positionCS.y *= _ProjectionParams.x;
        output.uv = input.uv.xy;
    	return output;
	}

	half4 FragCopy (VaryingsSimple i) : SV_Target {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
        float2 uv     = UnityStereoTransformScreenSpaceTex(i.uv);
        #if defined(USE_BILINEAR)
            return clamp(SAMPLE_TEXTURE2D_X(_MainTex, sampler_LinearClamp, uv), 0, 1e6);
        #else
		    return SAMPLE_TEXTURE2D_X(_MainTex, sampler_PointClamp, uv);
        #endif
	}


#endif // MystifyFX_CORE_FX
