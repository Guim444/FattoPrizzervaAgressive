TEXTURE2D(_NoiseTex);

float4 _HitData;
#define HIT_RADIUS_SQR _HitData.x
#define HIT_SPEED _HitData.y
#define HIT_START_TIME _HitData.z
#define HIT_NOISE_AMOUNT _HitData.w

half3 _HitColor;
#define HIT_COLOR _HitColor
float4 _HitPosition;
#define HIT_POSITION _HitPosition.xyz
#define HIT_NOISE_SCALE _HitPosition.w
float _Scale;

float _AnimationTime;
#define ANIMATION_TIME _AnimationTime

#define dot2(x) dot(x, x)

struct Attributes
{
    float4 positionOS : POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float3 positionWS : TEXCOORD0;
    float4 positionNDC : TEXCOORD1;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};


float3 ApplyHitEffect(float3 positionWS, float radiusSqr, float2 screenUV, float startTime, float time, float3 hitPos, float3 hitColor) {
    float hitDist = dot2(positionWS - hitPos) / radiusSqr;
    half hitNoise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_LinearRepeat, screenUV * HIT_NOISE_SCALE).r * HIT_NOISE_AMOUNT;
    hitDist += hitNoise;
    float expa = (time - startTime) * HIT_SPEED;
    hitDist = hitDist - expa;
    hitDist = 1 / abs(hitDist * 50);
    hitDist = saturate(hitDist);
    return hitColor * saturate(hitDist - expa * 3.5);
}


Varyings UnlitPassVertex(Attributes input)
{
    Varyings output = (Varyings)0;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz * _Scale);

    output.positionCS  = vertexInput.positionCS;
    output.positionNDC = vertexInput.positionNDC;
    output.positionWS  = vertexInput.positionWS;

    return output;
}

half4 UnlitPassFragment(Varyings input) : SV_Target0 {

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    float2 screenUV = input.positionNDC.xy / input.positionNDC.w;

    half3 rgbM = ApplyHitEffect(input.positionWS, HIT_RADIUS_SQR, screenUV, HIT_START_TIME, ANIMATION_TIME * 20, HIT_POSITION, HIT_COLOR);

    return half4(rgbM, 0);
}