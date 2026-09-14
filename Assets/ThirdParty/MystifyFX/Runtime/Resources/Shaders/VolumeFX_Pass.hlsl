#pragma warning (disable : 3571) // pow(f, e) will not work for negative f

TEXTURE2D(_LUTTex);
TEXTURE3D(_LUT3DTex);
TEXTURE2D(_NoiseTex);
TEXTURE2D_X(_InputTex);
TEXTURE2D_X(_BlurTex);
TEXTURE2D(_FrostTex);
TEXTURE2D(_FrostNormals);
TEXTURE2D(_HazeGradientTex);
TEXTURE2D(_RimTexture);
TEXTURE2D(_PatternTex);
TEXTURE2D(_PatternMask);
TEXTURE2D(_MaskTex);
TEXTURE2D(_CutOutTex);
float4 _CutOutTex_ST;

float3 _FrostIntensity;
half4 _FrostTintColor;

float _AnimationTime;
#define ANIMATION_TIME _AnimationTime

float4 _ScaleOffset;
float4 _UVScaleOffset;
float4 _BlurTex_TexelSize;
float _AlphaCutoff;

float4 _FXData;
float4 _FXData2;
float4 _FXData3;
float4 _FXData4;
float4 _FXData5;
float4 _FXData6;
float4 _FXData7;
float4 _FXData8;
float4 _FXData9;
float4 _FXData10;
float4 _FXData11;
float4 _FXData12;
float4 _FXData13;
float4 _FXData14;

float4 _LUTTex_TexelSize;
float2 _LUT3DParams;

half4 _ScanLinesColor;
float4 _ScanLinesRotation;
half4 _FrostColor;
half4 _IntersectionColor;
half4 _RimColor;

#define LUT_INTENSITY _FXData.x
#define PIXELATE _FXData.y

#define DISTORTION_AMPLITUDE _FXData10.xy
#define DISTORTION_FREQUENCY _FXData.w
#define DISTORTION_FALLOFF _FXData10.z
#define DISTORTION_RIM_POWER _FXData10.w

#define NOISE_SIZE _FXData2.x
#define NOISE_AMOUNT _FXData2.y
#define BLUR_INTENSITY _FXData2.w

#define SCALE_FACTOR _FXData3.x
#define HAZE_INTENSITY _FXData3.y
#define HAZE_THRESHOLD _FXData3.z
#define DISTORTION_SPEED _FXData3.w

#define HAZE_SCALE _FXData4.xy
#define HAZE_SPEED _FXData4.z

#define FROST_INTENSITY _FXData5.x
#define FROST_SPREAD _FXData5.y

#define CRYSTALIZE _FXData5.z
#define CRYSTALIZE_SPREAD _FXData5.w
#define CRYSTALIZE_SCALE _FXData6.xy
#define CRYSTALIZE_ANIMATION_SPEED _FXData6.zw

#define SCAN_LINES_THRESHOLD _FXData7.x
#define SCAN_LINES_ANIMATION_SPEED _FXData7.y
#define SCAN_LINES_SCREEN_SPACE _FXData.z
#define ZOOM_FACTOR _FXData7.z
#define SCAN_LINES_MAX_DISTANCE _FXData11.w
#define SCAN_LINES_ROTATION_SIN _ScanLinesRotation.x
#define SCAN_LINES_ROTATION_COS _ScanLinesRotation.y
#define SCAN_LINES_OBJECT_ASPECT _ScanLinesRotation.z

#define INTERSECTION_NOISE _FXData7.w
#define INTERSECTION_THICKNESS _FXData2.z

TEXTURE2D(_TintGradientTex);
float3 _TintPos1;
float3 _TintPos2;
bool _TintApplyAlphaToAll;
#define TINT_START _TintPos1
#define TINT_END _TintPos2
#define TINT_APPLY_ALPHA_TO_ALL _TintApplyAlphaToAll

#define RIM_POWER _FXData8.x
#define RIM_COLOR _RimColor.rgb
#define RIM_INTENSITY _RimColor.a
#define RIM_TEXTURE_OPACITY _FXData8.y
#define RIM_TEXTURE_SCALE _FXData8.z
#define CHROMATIC_ABERRATION_AMOUNT _FXData8.w

#define GLASS_TINY_DROPS _FXData9.x
#define GLASS_TINY_DROPS_GRID_SIZE _FXData14.y
#define GLASS_LARGE_DROPS _FXData9.y
#define GLASS_LARGE_DROPS_GRID_SIZE _FXData14.z
#define GLASS_LARGE_DROPS_SPEED _FXData14.w
#define RAINFALL _FXData9.z
#define RAIN_WIND _FXData9.w
#define RAIN_SPEED _FXData14.x
#define RAIN_DROP_INTENSITY _FXData11.z

#define VERTEX_DISTORTION_AMOUNT _FXData11.x
#define VERTEX_DISTORTION_SPEED _FXData11.y

TEXTURE2D(_FakeLightGradientTex);
#define FAKE_LIGHT_INTENSITY _FXData12.x
#define FAKE_LIGHT_FOG _FXData12.y
#define FAKE_LIGHT_FALLOFF _FXData12.z
#define FAKE_LIGHT_SPEED _FXData12.w

/*
half4 _PatternColor;
#define PATTERN_INTENSITY _FXData13.x
#define PATTERN_SCALE _FXData13.y
#define PATTERN_MASK_SCALE _FXData13.z
#define PATTERN_MASK_THRESHOLD _FXData13.w
*/
#define CHROMATIC_ABERRATION_EDGE_POWER _FXData13.x

half4 _ColorBoost;
#define COLOR_BOOST_BRIGHTNESS _ColorBoost.x
#define COLOR_BOOST_CONTRAST _ColorBoost.y
#define COLOR_BOOST_VIBRANCE _ColorBoost.z
#define GLOBAL_INTENSITY _ColorBoost.w

#define GAMMA_TO_LINEAR(x) SRGBToLinear(x)
#define LINEAR_TO_GAMMA(x) LinearToSRGB(x)

#if _MYSTIFYFX_FX_WRAP_MODE
    #define sourceSampler sampler_LinearRepeat
#else
    #define sourceSampler sampler_LinearClamp
#endif

int _TextureSource;
#define TEXTURE_SOURCE_SCREEN 0
#define TEXTURE_SOURCE_OBJECT 1

#define dot2(x) dot(x, x)

struct Attributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT;
    float2 uv : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float3 normalWS : NORMAL;
    float4 positionNDC : TEXCOORD1;
    float2 uv : TEXCOORD2;
    float4 positionCO : TEXCOORD3;
    float3 positionWS : TEXCOORD4;
    float3 positionWS0 : TEXCOORD5;
    float3 positionOS : TEXCOORD6;
    float depth: TEXCOORD7;
    float2 objectUV : TEXCOORD8;
    #if _MYSTIFYFX_FX_RAIN
        float3 rainUV : TEXCOORD9;
    #endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

float getRandom(float2 uv) {
    return frac(sin(_Time.y + dot(uv, float2(12.9898, 78.233)))* 43758.5453) - 0.5;
}

float getStaticRandom(float2 uv) {
    return frac(sin(dot(uv, float2(12.9898, 78.233)))* 43758.5453);
}


float3 getStaticRandom3(float2 p) {
    float3 p3 = frac(float3(p.xyx) * float3(0.1031, 0.103, 0.0973));
    p3 += dot(p3, p3.yxz + 33.33);
    return frac(p3.zyx * (p3.yzz + p3.xxy));
}


float getLuma(float3 rgb) {
    const float3 lum = float3(0.299, 0.587, 0.114);
    return dot(rgb, lum);
}

float getBackRain(float2 uv, float ti) {
    ti *= RAIN_SPEED;
    uv.x -= ti * 5 * RAIN_WIND;
    const float2 GRID_SIZE = float2(128.0, 128.0 * 0.1);
    float2 accel = floor(float2(uv.x * GRID_SIZE.x, 0));
    float3 drop2 = getStaticRandom3(accel);
    ti *= 1.0 + drop2.x * 10;
    uv.y += ti;
    uv.x += (uv.y + drop2.y) * RAIN_WIND;
    float2 tile = floor(uv.xy * GRID_SIZE);
    float3 drop = getStaticRandom3(tile);
    if (drop.z < RAINFALL) {
        float2 tileCenter = tile + 0.5;
        tileCenter += 0.9 * (drop.xy - 0.5);
        float2 dispUV = uv - tileCenter / GRID_SIZE;
        float dropSize = 10 * (dispUV.y + 0.5 / GRID_SIZE.y);
        dispUV.y *= 0.15; // enlarge vertically
        float d = max(0, dropSize / GRID_SIZE.x - length(dispUV) );
        return dot2(d * 96 * drop.x);
    }
    return 0;
}


float getSmallDrops(float2 uv) {
    float GRID_SIZE = GLASS_TINY_DROPS_GRID_SIZE;
    uv.x += uv.y * RAIN_WIND;

    float2 tile = floor(uv * GRID_SIZE);
    float3 drop = getStaticRandom3(tile);
    if (drop.z < GLASS_TINY_DROPS) {
        float2 tileCenter = tile + 0.5;
        tileCenter += 0.9 * (drop.xy - 0.5);
        float2 dispUV = uv - tileCenter / GRID_SIZE;
        float ft = 1.0 - frac(ANIMATION_TIME + drop.x);
        float t = ft * 10 - 2.0 - 5.0 * drop.y;
        dispUV.y *= drop.z * 0.8; // enlarge vertically
        dispUV *=  1.0 - max(0, ft - 0.97) * 8.0; // hit
        float dropSize = 0.02 + drop.z * 0.4 * saturate(t);
        float d = max(0, dropSize / GRID_SIZE - length(dispUV));
        return dot2(d * 128);
    }
    return 0;
}

float getLargeDrops(float2 uv, float ti) {
    const float GRID_SIZE = GLASS_LARGE_DROPS_GRID_SIZE;
    float2 accel = floor(float2(uv.x * GRID_SIZE, 0));
    float3 drop2 = getStaticRandom3(accel);
    float sprint = sin( (uv.y + drop2.y) * 4.7);
    ti *= drop2.x;
    ti += sprint;
    uv.x += uv.y * drop2.y * RAIN_WIND * 0.5;
    uv.y += ti * 0.2;
    uv.x += 0.2 * sprint / GRID_SIZE;
    float2 tile = floor(uv * GRID_SIZE);
    float3 drop = getStaticRandom3(tile);
    if (drop.z < GLASS_LARGE_DROPS) {
        float2 tileCenter = tile + 0.5;
        tileCenter += 0.9 * (drop.xy - 0.5);
        float2 dispUV = uv - tileCenter / GRID_SIZE;
        float ft = 1.0 - frac(ANIMATION_TIME + drop.x);
        float t = ft * 14 - 2.0 - 5.0 * drop.y;
        dispUV.y *= 0.8 - sprint * 0.2; // enlarge vertically
        dispUV *= 1.0 - max(0, ft - 0.97) * 16.0; // hit
        float dropSize = 0.0125 + drop.z * 0.15 * saturate(t);
        float d = max(0, dropSize / GRID_SIZE - length(dispUV) );
        return dot2(d * 64);
    }
    return 0;
}

float GetHaze(float wt, float2 uv, float3 normalWS) {
    #if _MYSTIFYFX_FX_HAZE_WS
        float3 hazeRay = normalWS;
        float3 hazeUV = hazeRay * HAZE_SCALE.x;
        half n1 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_LinearRepeat, hazeUV.zy + wt).r;
        half n2 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_LinearRepeat, hazeUV.xz + wt).r;
        half n3 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_LinearRepeat, hazeUV.xy - wt).r;
        float3 triW = abs(hazeRay);
        float3 weights = triW / (triW.x + triW.y + triW.z);
        half haze = dot(half3(n1, n2, n3), weights);
    #else
        half2 waveDisp1 = half2(wt + cos(wt+uv.y * 32.0) * 0.125, 0) * 0.05;
        half fog1 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_LinearRepeat, (uv + waveDisp1) * 4 * HAZE_SCALE).r;
        wt *= 1.2;
        half2 waveDisp2 = half2(wt + cos(wt+uv.y * 8.0) * 0.5, 0) * 0.05;
        half fog2 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_LinearRepeat, (uv - waveDisp2) * 2 * HAZE_SCALE).r;
        half haze = (fog1 + fog2) * 0.5;
    #endif
    haze = saturate(haze * HAZE_INTENSITY - HAZE_THRESHOLD);
    return haze;
}

void RaySphereIntersect(float3 rayDirection, float3 sphereCenter, float radiusSqr, out float t0, out float t1) {
    float3 oc = _WorldSpaceCameraPos - sphereCenter;
    float a = dot(rayDirection, rayDirection);
    float b = 2.0 * dot(oc, rayDirection);
    float c = dot(oc, oc) - radiusSqr;
    float discriminant = b * b - 4.0 * a * c;
    float sqrtDiscriminant = sqrt(discriminant);
    t0 = (-b - sqrtDiscriminant) / (2.0 * a);
    t1 = (-b + sqrtDiscriminant) / (2.0 * a);
}



Varyings UnlitPassVertex(Attributes input)
{
    Varyings output = (Varyings)0;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    output.positionOS = input.positionOS.xyz;

    if (VERTEX_DISTORTION_AMOUNT > 0) {
        float3 rn = getStaticRandom3(input.uv.xy + ANIMATION_TIME * VERTEX_DISTORTION_SPEED);
        input.positionOS.xyz += normalize(input.normalOS) * (rn * VERTEX_DISTORTION_AMOUNT);
    }
    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz * SCALE_FACTOR);

    output.positionCS  = vertexInput.positionCS;
    #if UNITY_REVERSED_Z
        output.positionCS.z += 0.0001;
    #else
        output.positionCS.z -= 0.0001;
    #endif
        
    output.positionNDC = vertexInput.positionNDC;
    output.positionWS  = vertexInput.positionWS;
    output.depth = lerp(UNITY_Z_0_FAR_FROM_CLIPSPACE(output.positionCS.z), 1.0, UNITY_MATRIX_P[3][3]);

    output.normalWS    = GetVertexNormalInputs(input.normalOS).normalWS;
    float2 baseUV = input.uv;
    output.uv = baseUV * _UVScaleOffset.xy + _UVScaleOffset.zw;
    output.objectUV = baseUV * (_CutOutTex_ST.xy * _UVScaleOffset.xy) + _CutOutTex_ST.zw + _UVScaleOffset.zw;

    // rain uv: rotate mesh uv so world up maps to +v (fixes rotated/flipped uv islands)
    #if _MYSTIFYFX_FX_RAIN
        float2 rainUV = baseUV;
        float rainAligned = 0;
        float3 tangentWS = TransformObjectToWorldDir(input.tangentOS.xyz, false);
        float tangentLen = length(tangentWS);
        if (tangentLen > 0.0001) {
            tangentWS /= tangentLen;
            float3 bitangentWS = cross(output.normalWS, tangentWS) * (input.tangentOS.w * GetOddNegativeScale());
            float3 upWS = float3(0, 1, 0) - output.normalWS * output.normalWS.y;
            float2 upUV = float2(dot(upWS, tangentWS), dot(upWS, bitangentWS));
            float upLen = length(upUV);
            if (upLen > 0.001) {
                upUV /= upLen;
                rainUV = float2(upUV.y * baseUV.x - upUV.x * baseUV.y, upUV.x * baseUV.x + upUV.y * baseUV.y);
                rainAligned = 1;
            }
        }
        output.rainUV = float3(rainUV * _UVScaleOffset.xy + _UVScaleOffset.zw, rainAligned);
    #endif

    VertexPositionInputs vertexCenter = GetVertexPositionInputs(float3(0,0,0));
    output.positionWS0 = vertexCenter.positionWS;    
    output.positionCO = vertexCenter.positionNDC;

    return output;
}


half4 UnlitPassFragment(Varyings input) : SV_Target0 {

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    float2 objectUV = input.objectUV;

    const float aspect = _ScreenParams.x / _ScreenParams.y;
    float2 screenUV = input.positionNDC.xy / input.positionNDC.w;

    float2 uv = float2(screenUV.x, screenUV.y / aspect);

    uv.xy = uv.xy * _ScaleOffset.xy + _ScaleOffset.zw;

    float2 da = input.uv - 0.5;
    float dd = dot(da, da) * 2.0;

    float3 viewDir = normalize(_WorldSpaceCameraPos - input.positionWS);
    float3 normalWS = normalize(input.normalWS);
    float rimThreshold = 1.0 - abs(dot(normalWS, viewDir));

    #if _MYSTIFYFX_FX_FROST
        float fdd = saturate(pow(dd, FROST_SPREAD)) * FROST_INTENSITY;
        half4 frost = SAMPLE_TEXTURE2D(_FrostTex, sampler_LinearRepeat, input.uv);
        #if UNITY_COLORSPACE_GAMMA
            frost.rgb = SRGBToLinear(frost.rgb);
        #endif
    #endif

    // tinting
    #if _MYSTIFYFX_FX_DIRECTIONAL_TINT
        float3 tintDir = TINT_END - TINT_START;
        float t = saturate(dot(input.positionOS - TINT_START, tintDir) / dot(tintDir, tintDir));
        half4 tintColor = SAMPLE_TEXTURE2D(_TintGradientTex, sampler_LinearClamp, float2(t, 0));
        if (TINT_APPLY_ALPHA_TO_ALL) {
            RIM_INTENSITY *= tintColor.a;
            _ScanLinesColor.a *= tintColor.a;
            NOISE_AMOUNT *= tintColor.a;
            #if _MYSTIFYFX_FX_FROST
                fdd *= tintColor.a;
            #endif
            #if _MYSTIFYFX_FX_HAZE_OS || _MYSTIFYFX_FX_HAZE_WS
                HAZE_INTENSITY *= tintColor.a;
            #endif
            DISTORTION_AMPLITUDE *= tintColor.a;
            BLUR_INTENSITY *= tintColor.a;
            CRYSTALIZE *= tintColor.a;
        }
    #endif    

    #if _MYSTIFYFX_FX_CRYSTALIZE
        half4 norm = SAMPLE_TEXTURE2D(_FrostNormals, sampler_LinearRepeat, float2(input.uv * CRYSTALIZE_SCALE + CRYSTALIZE_ANIMATION_SPEED * ANIMATION_TIME));
        norm.rgb = UnpackNormal(norm);
        float cdd = saturate(pow(dd, CRYSTALIZE_SPREAD));
        float2 disp = norm.xy * CRYSTALIZE * cdd;
        uv += disp;
    #endif

    float2 cuv = saturate(input.positionCO.xy / input.positionCO.w);
    cuv.y /= aspect;
    cuv.xy = cuv.xy * _ScaleOffset.xy + _ScaleOffset.zw;

    // distortion
    float2 middlePoint = 0.5 * _UVScaleOffset.xy + _UVScaleOffset.zw;
    float cuvDist = length(input.uv - middlePoint);    
    float2 ripple = ((middlePoint - input.uv) / cuvDist) * saturate(1 / pow(1 + cuvDist, DISTORTION_FALLOFF)) * lerp(0.2, 0.5, 0.5 + 0.5 * sin(cuvDist * DISTORTION_FREQUENCY - ANIMATION_TIME * DISTORTION_SPEED));
    ripple *= pow(rimThreshold, DISTORTION_RIM_POWER);
    ripple *= DISTORTION_AMPLITUDE;
    input.uv -= ripple;
    uv -= ripple;
    screenUV -= ripple;

    // zoom
    float2 tuv = (uv - cuv) * ZOOM_FACTOR + cuv;

    // pixelate
    float2 pixelateUV = tuv;
    float2 objectPixelateUV = objectUV;
    if (PIXELATE > 0) {
        pixelateUV = floor(pixelateUV * PIXELATE + 0.5) / PIXELATE;
        objectPixelateUV = floor(objectPixelateUV * PIXELATE + 0.5) / PIXELATE;
    }
    pixelateUV.y *= aspect;

    half4 objectTex = SAMPLE_TEXTURE2D(_CutOutTex, sampler_LinearRepeat, objectPixelateUV);
    clip(objectTex.a - _AlphaCutoff);

    // rain
    #if _MYSTIFYFX_FX_RAIN
        float2 rainUV = input.rainUV.xy - ripple;
        if (input.rainUV.z == 0 && ddy(rainUV.y) < 0) rainUV.y = -rainUV.y;
        float goodRain = input.normalWS.y < 0.99;
        float drop  = getSmallDrops(rainUV);
        float drop2 = getLargeDrops(rainUV, ANIMATION_TIME * GLASS_LARGE_DROPS_SPEED);
        float mixedDrop = drop + drop2 * goodRain;
        mixedDrop *= RAIN_DROP_INTENSITY;
        float2 rainDisp = float2(ddx(mixedDrop), ddy(mixedDrop));
        pixelateUV += rainDisp;
        BLUR_INTENSITY *= 1-drop2;
    #endif

    // sample source
    bool useObjectTex = _TextureSource == TEXTURE_SOURCE_OBJECT;
    half4 c = useObjectTex ? half4(objectTex.rgb, objectTex.a) : SAMPLE_TEXTURE2D_X_LOD(_InputTex, sourceSampler, pixelateUV, 0);

    // chromatic aberration (reuses _MYSTIFYFX_FX_RIM keyword)
    #if _MYSTIFYFX_FX_RIM
        UNITY_BRANCH
        if (CHROMATIC_ABERRATION_AMOUNT > 0) {
            float edgeIntensity = pow(rimThreshold, CHROMATIC_ABERRATION_EDGE_POWER);
            if (edgeIntensity > 0) {
                float2 centerUV = cuv;
                float2 dir = pixelateUV - centerUV;
                float dist = length(dir);
                if (dist > 0.0001) {
                    float2 offsetDir = dir / dist;
                    float offsetAmount = CHROMATIC_ABERRATION_AMOUNT;
                    float2 offsetR = offsetDir * offsetAmount;
                    float2 offsetB = -offsetDir * offsetAmount;
                    
                    if (useObjectTex) {
                        float2 objOffsetR = offsetR / _ScaleOffset.xy;
                        float2 objOffsetB = offsetB / _ScaleOffset.xy;
                        half r = SAMPLE_TEXTURE2D_LOD(_CutOutTex, sampler_LinearRepeat, objectPixelateUV + objOffsetR, 0).r;
                        half b = SAMPLE_TEXTURE2D_LOD(_CutOutTex, sampler_LinearRepeat, objectPixelateUV + objOffsetB, 0).b;
                        c.r = lerp(c.r, r, edgeIntensity);
                        c.b = lerp(c.b, b, edgeIntensity);
                    } else {
                        half r = SAMPLE_TEXTURE2D_X_LOD(_InputTex, sourceSampler, pixelateUV + offsetR, 0).r;
                        half b = SAMPLE_TEXTURE2D_X_LOD(_InputTex, sourceSampler, pixelateUV + offsetB, 0).b;
                        c.r = lerp(c.r, r, edgeIntensity);
                        c.b = lerp(c.b, b, edgeIntensity);
                    }
                }
            }
        }
    #endif

    // blur
    #if _MYSTIFYFX_FX_BLUR
        half4 b = SAMPLE_TEXTURE2D_X(_BlurTex, sourceSampler, pixelateUV);
        c = lerp(c, b, BLUR_INTENSITY);
    #endif

    half3 rgbM = c.rgb;

    // saturate
    float maxComponent = max(rgbM.r, max(rgbM.g, rgbM.b));
    float minComponent = min(rgbM.r, min(rgbM.g, rgbM.b));
    float sat = saturate(maxComponent - minComponent);
    rgbM *= 1.0 + COLOR_BOOST_VIBRANCE * (1.0 - sat) * (rgbM - getLuma(rgbM));
    rgbM = (rgbM - 0.5.xxx) * COLOR_BOOST_CONTRAST + 0.5.xxx;
    rgbM *= COLOR_BOOST_BRIGHTNESS;    

    // back rain
    #if _MYSTIFYFX_FX_RAIN
        float rain = getBackRain(rainUV, ANIMATION_TIME * 20.0);
        rgbM += rain * (1.0 - BLUR_INTENSITY * 0.25) * goodRain;
    #endif
    
    // lut
    #if _MYSTIFYFX_FX_LUT || _MYSTIFYFX_FX_3DLUT

        #if _MYSTIFYFX_FX_LUT || _MYSTIFYFX_FX_3DLUT
            #if !UNITY_COLORSPACE_GAMMA
                rgbM = LINEAR_TO_GAMMA(rgbM);
            #endif

            #if _MYSTIFYFX_FX_3DLUT
                float3 xyz = rgbM * _LUT3DParams.y + _LUT3DParams.x;
                float3 lut = SAMPLE_TEXTURE3D(_LUT3DTex, sampler_LinearClamp, xyz).rgb;
            #else
                float3 lutST = float3(_LUTTex_TexelSize.x, _LUTTex_TexelSize.y, _LUTTex_TexelSize.w - 1);
                float3 lookUp = saturate(rgbM) * lutST.zzz;
                lookUp.xy = lutST.xy * (lookUp.xy + 0.5);
                float slice = floor(lookUp.z);
                lookUp.x += slice * lutST.y;
                float2 lookUpNextSlice = float2(lookUp.x + lutST.y, lookUp.y);
                float3 lut = lerp(SAMPLE_TEXTURE2D(_LUTTex, sampler_LinearClamp, lookUp.xy).rgb, SAMPLE_TEXTURE2D(_LUTTex, sampler_LinearClamp, lookUpNextSlice).rgb, lookUp.z - slice);
            #endif
            rgbM = lerp(rgbM, lut, LUT_INTENSITY);

            #if !UNITY_COLORSPACE_GAMMA
                rgbM = GAMMA_TO_LINEAR(rgbM);
            #endif
        #endif

    #endif

    #if _MYSTIFYFX_FX_THERMAL
        half lumaM = getLuma(rgbM);
        float3 tv0 = lerp(float3(0.0,0.0,1.0), float3(1.0,1.0,0.0), lumaM * 2.0);
        float3 tv1 = lerp(float3(1.0,1.0,0.0), float3(1.0,0.0,0.0), lumaM * 2.0 - 1.0);
        rgbM = lerp(tv0, tv1, (float)(lumaM >= 0.5));
    #endif

    #if _MYSTIFYFX_FX_DIRECTIONAL_TINT
        rgbM = lerp(rgbM, tintColor.rgb, tintColor.a);    
    #endif

    // rim (reuses _MYSTIFYFX_FX_RIM keyword, works independently from chromatic)
    #if _MYSTIFYFX_FX_RIM
        if (RIM_INTENSITY > 0) {
            float rimIntensity = pow(rimThreshold, RIM_POWER);
            half rimTexture = SAMPLE_TEXTURE2D(_RimTexture, sampler_LinearRepeat, input.uv * RIM_TEXTURE_SCALE).r;
            rimIntensity *= 1.0 + rimTexture * RIM_TEXTURE_OPACITY;
            rgbM = lerp(rgbM, RIM_COLOR, rimIntensity * RIM_INTENSITY);
        }
    #endif
    
    // scan lines
    if (SCAN_LINES_THRESHOLD) {
        float z = saturate(SCAN_LINES_MAX_DISTANCE / input.depth);
        float2 scanLinesUV = lerp(input.uv, screenUV, SCAN_LINES_SCREEN_SPACE);
        float scanAspect = lerp(SCAN_LINES_OBJECT_ASPECT, aspect, SCAN_LINES_SCREEN_SPACE);
        float scanLinesValue = SCAN_LINES_ROTATION_SIN * scanLinesUV.x * scanAspect + SCAN_LINES_ROTATION_COS * scanLinesUV.y;
        rgbM = lerp(rgbM, _ScanLinesColor.rgb, z * z * _ScanLinesColor.a * (0.2 + frac(floor((scanLinesValue + SCAN_LINES_ANIMATION_SPEED * ANIMATION_TIME) *_ScreenParams.y) * SCAN_LINES_THRESHOLD) < 0.4));
    }

    // noise     
    rgbM *= 1.0 + getRandom(floor(uv * NOISE_SIZE) / NOISE_SIZE) * NOISE_AMOUNT;					

    // frost
    #if _MYSTIFYFX_FX_FROST
        frost.rgb *= fdd;
        rgbM = frost.rgb * _FrostColor.rgb + rgbM * (1.0 - frost.g);
    #endif

    // haze
    #if _MYSTIFYFX_FX_HAZE_OS || _MYSTIFYFX_FX_HAZE_WS
        half wt = ANIMATION_TIME * HAZE_SPEED;
        half haze = GetHaze(wt, input.uv, normalWS);
        half4 hazeColor = SAMPLE_TEXTURE2D(_HazeGradientTex, sampler_LinearClamp, float2(haze, 0));
        haze *= hazeColor.a * HAZE_INTENSITY;    
        rgbM = rgbM * (1.0 - haze) + haze * hazeColor.rgb;
    #endif

    #if _MYSTIFYFX_FX_INTERSECTION || _MYSTIFYFX_FX_FAKE_LIGHT
        float depth = SampleSceneDepth(screenUV);
        #if !UNITY_REVERSED_Z
            depth = depth * 2.0 - 1.0;
        #endif
        float3 wpos = ComputeWorldSpacePosition(screenUV, depth, unity_MatrixInvVP);
    #endif

    /*
    // pattern
    half4 pattern = SAMPLE_TEXTURE2D(_PatternTex, sampler_LinearRepeat, input.uv * PATTERN_SCALE);
    half4 patternMask = SAMPLE_TEXTURE2D(_PatternMask, sampler_LinearRepeat, input.uv * PATTERN_MASK_SCALE);
    pattern.rgb *= lumaM;
    half patternCutout = patternMask.r <= PATTERN_MASK_THRESHOLD;
    rgbM = lerp(rgbM, pattern.rgb, pattern.a * PATTERN_INTENSITY * patternCutout);
*/        
    // intersection
    #if _MYSTIFYFX_FX_INTERSECTION
        float gapRandom = (0.5 + getRandom(screenUV * 0.001)) * INTERSECTION_NOISE;
        gapRandom += 0.0005;
        gapRandom *= INTERSECTION_THICKNESS;
        float intersection = saturate( gapRandom / (0.00001 + abs(dot2(input.positionWS.xyz - wpos) - 0.001 * INTERSECTION_THICKNESS)));
        rgbM += (intersection * _IntersectionColor.a) * _IntersectionColor.rgb;
    #endif

    // spectral light
    #if _MYSTIFYFX_FX_FAKE_LIGHT
        float radiusSqr = dot2(input.positionWS0 - input.positionWS) + 0.001;
        float3 dwpos = wpos - input.positionWS0;
        float distSqr = dot2(dwpos);
        float intensity = distSqr / radiusSqr;
        intensity = smoothstep(1.0, FAKE_LIGHT_FALLOFF, intensity);
        float fakeCast = intensity * intensity;

        float3 rayDir = normalize(input.positionWS - _WorldSpaceCameraPos);
        float t0, t1;
        RaySphereIntersect(rayDir, input.positionWS0, radiusSqr, t0, t1);
        t0 = max(t0, 0);
        t1 = max(t0, t1);
        t1 = min(t1, distance(_WorldSpaceCameraPos, wpos));
        float3 mp = _WorldSpaceCameraPos + ((t0 + t1) * 0.5) * rayDir;
        float3 dmp = input.positionWS0 - mp;
        float dmpSqr = dot2(dmp);
        float dmp0 = sqrt(dmpSqr);
        float li = dmpSqr / radiusSqr;
        li = smoothstep(1.0, FAKE_LIGHT_FALLOFF, li);
        float fakeFog = li * li;

        float gt = sqrt(dmpSqr/ radiusSqr);

        float fakeLight = fakeFog + fakeCast;
        half4 fakeLightColor = SAMPLE_TEXTURE2D(_FakeLightGradientTex, sampler_LinearRepeat, float2(gt - ANIMATION_TIME * FAKE_LIGHT_SPEED, 0));

        fakeLight = fakeFog * FAKE_LIGHT_FOG + fakeCast * FAKE_LIGHT_INTENSITY;
        fakeLight *= fakeLightColor.a;
        rgbM = lerp(rgbM, fakeLightColor.rgb * (0.5 + rgbM), fakeLight);
    #endif

    c.rgb = max(0, rgbM);

    c.a = SAMPLE_TEXTURE2D(_MaskTex, sampler_LinearRepeat, input.uv).a;
    c.a *= GLOBAL_INTENSITY;

    return c;
}