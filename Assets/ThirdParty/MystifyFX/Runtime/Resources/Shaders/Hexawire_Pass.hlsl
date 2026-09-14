
TEXTURE2D(_NoiseTex);

#define dot2(x) dot(x, x)

float4 _WireData;
#define VERTEX_DISTORTION_AMOUNT _WireData.x
#define VERTEX_DISTORTION_SPEED _WireData.y
#define HEXAGRID_SCALE _WireData.z
#define SCALE_FACTOR _WireData.w

float4 _WireData2;
#define RIM_POWER _WireData2.x
#define NOISE_STRENGTH _WireData2.y
#define NOISE_THRESHOLD _WireData2.z
#define NOISE_SCALE _WireData2.w

float4 _WireData3;
#define SWEEP_SPEED _WireData3.x
#define SWEEP_AMOUNT _WireData3.y
#define SWEEP_FREQUENCY _WireData3.z
half4 _NoiseColor;
half4 _WireColor;

float _AnimationTime;
#define ANIMATION_TIME _AnimationTime

struct Attributes
{
    float4 positionOS : POSITION;
    float2 uv : TEXCOORD0;
    float3 normalOS : NORMAL;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float3 positionWS : TEXCOORD0;
    float2 uv : TEXCOORD1;
    float3 normalWS : NORMAL;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

float3 getStaticRandom3(float2 p) {
    float3 p3 = frac(float3(p.xyx) * float3(0.1031, 0.103, 0.0973));
    p3 += dot(p3, p3.yxz + 33.33);
    return frac(p3.zyx * (p3.yzz + p3.xxy));
}


Varyings UnlitPassVertex(Attributes input)
{
    Varyings output = (Varyings)0;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    if (VERTEX_DISTORTION_AMOUNT > 0) {
        float3 rn = getStaticRandom3(input.uv.xy + ANIMATION_TIME * VERTEX_DISTORTION_SPEED);
        input.positionOS.xyz += normalize(input.normalOS) * (rn * VERTEX_DISTORTION_AMOUNT);
    }
    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz * SCALE_FACTOR * HEXAGRID_SCALE);

    output.positionCS  = vertexInput.positionCS;
    output.positionWS  = vertexInput.positionWS;
    output.normalWS    = GetVertexNormalInputs(input.normalOS).normalWS;
    output.uv          = input.uv;
    return output;
}


half4 UnlitPassFragment(Varyings input) : SV_Target0 {

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    float3 hazeRay = normalize(input.normalWS);
    float3 hazeUV = hazeRay * NOISE_SCALE;
    float wt = ANIMATION_TIME * 2.0;
    half n1 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_LinearRepeat, hazeUV.zy + wt).r;
    half n2 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_LinearRepeat, hazeUV.xz + wt).r;
    half n3 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_LinearRepeat, hazeUV.xy - wt).r;
    float3 triW = abs(hazeRay);
    float3 weights = triW / (triW.x + triW.y + triW.z);
    half haze = dot(half3(n1, n2, n3), weights);
    haze = max(0, haze - NOISE_THRESHOLD) * NOISE_STRENGTH;

    half4 color = _WireColor;
    color = color * (1.0 - haze) + haze * _NoiseColor;

    float rimThreshold = 1.0 - abs(dot(normalize(input.normalWS), normalize(_WorldSpaceCameraPos - input.positionWS)));
    float rimIntensity = pow(abs(rimThreshold), RIM_POWER);

    float sweep = frac( (input.uv.y - SWEEP_SPEED * ANIMATION_TIME) * SWEEP_FREQUENCY);
    color.rgb *= 1.0 + sweep * SWEEP_AMOUNT;

    color.a *= rimIntensity;
   

    color = max(0, color);
 
    return color;
}