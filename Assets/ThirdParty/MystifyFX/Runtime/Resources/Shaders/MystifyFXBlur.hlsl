#ifndef MystifyFX_PPSLUM_FX
#define MystifyFX_PPSLUM_FX

	// Copyright 2020-2021 Kronnect - All Rights Reserved.
    #include "MystifyFXCommon.hlsl"

	TEXTURE2D_X(_MainTex);
	float4    _MainTex_TexelSize;
	float4    _MainTex_ST;
	TEXTURE2D_X(_MaskTex);
	float _BlurScale;
	float _HighlightThreshold;
	float _HighlightIntensity;

	struct VaryingsCross {
	    float4 positionCS : SV_POSITION;
	    float2 uv: TEXCOORD0;
        VERTEX_CROSS_UV_DATA
        UNITY_VERTEX_OUTPUT_STEREO
	};

	struct VaryingsLum {
		float4 positionCS : SV_POSITION;
		float2 uv: TEXCOORD0;
        UNITY_VERTEX_OUTPUT_STEREO
	};

	struct VaryingsCrossLum {
		float4 positionCS : SV_POSITION;
		float2 uv: TEXCOORD0;
        VERTEX_CROSS_UV_DATA
        UNITY_VERTEX_OUTPUT_STEREO
	};


	VaryingsLum VertLum(AttributesSimple input) {

	    VaryingsLum output;
        UNITY_SETUP_INSTANCE_ID(input);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
        output.positionCS = input.positionOS;
		output.positionCS.y *= _ProjectionParams.x;
        output.uv = input.uv;

        return output;
	}

   	VaryingsCross VertCross(AttributesSimple v) {
    	VaryingsCross o;
        UNITY_SETUP_INSTANCE_ID(v);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

		o.positionCS = v.positionOS;
		o.positionCS.y *= _ProjectionParams.x;
        o.uv = v.uv;
        VERTEX_OUTPUT_CROSS_UV(o)

		return o;
	}

	  	
	VaryingsCross VertBlur(AttributesSimple v) {
    	VaryingsCross o;
        UNITY_SETUP_INSTANCE_ID(v);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

		o.positionCS = v.positionOS;
		o.positionCS.y *= _ProjectionParams.x;
    	o.uv = v.uv;
        VERTEX_OUTPUT_GAUSSIAN_UV(o)

    	return o;
	}
	
	float4 FragBlur (VaryingsCross i): SV_Target {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
        i.uv = UnityStereoTransformScreenSpaceTex(i.uv);
        FRAG_SETUP_GAUSSIAN_UV(i)

		float4 cp = SAMPLE_TEXTURE2D_X(_MainTex, sampler_LinearClamp, i.uv);
		float bp = dot(cp.rgb, 1.0);
		cp.rgb += max(0, bp - _HighlightThreshold) * _HighlightIntensity;

		float4 pixel = cp * 0.2270270270
					+ (SAMPLE_TEXTURE2D_X(_MainTex, sampler_LinearClamp, uv1) + SAMPLE_TEXTURE2D_X(_MainTex, sampler_LinearClamp, uv2)) * 0.3162162162
					+ (SAMPLE_TEXTURE2D_X(_MainTex, sampler_LinearClamp, uv3) + SAMPLE_TEXTURE2D_X(_MainTex, sampler_LinearClamp, uv4)) * 0.0702702703;
   		return pixel;
	}	

#endif