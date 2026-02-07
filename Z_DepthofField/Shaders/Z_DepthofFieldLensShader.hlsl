Texture2D InputTexture : register(t0);
SamplerState InputSampler : register(s0);

cbuffer constants : register(b0)
{
    float radius;
    float brightness;
    float edgeStrength;
    float limit;
};

static float pi = 3.141592;
static float goldenAngle = 2.399963;

float4 main(
    float4 pos : SV_POSITION,
    float4 posScene : SCENE_POSITION,
    float4 uv0 : TEXCOORD0
) : SV_Target
{
    if (radius <= 0.1)
        return InputTexture.Sample(InputSampler, uv0.xy);

    float4 result = float4(0, 0, 0, 0);
    float gamma = 1 + brightness;
    
    float samples = clamp(limit * limit * pi, 1.0, 1024.0);
    float radiusScaling = radius / sqrt(samples);
    float4 totalGain = 0;

    [loop]
    for (float i = 0; i < samples; i++)
    {
        float r = radiusScaling * sqrt(i);
        float t = goldenAngle * i;
        float2 delta = float2(r * cos(t), r * sin(t));

        float2 uv = uv0.xy + delta * uv0.zw;
        float gain = (edgeStrength <= 0 || samples <= 1) ? 1.0 : pow(abs(r / radius), edgeStrength);
        
        float4 color = InputTexture.SampleLevel(InputSampler, uv, 0);
        result += pow(abs(color), gamma) * gain;
        totalGain += gain;
    }
    return pow(abs(result / max(totalGain, 0.0001)), 1.0 / gamma);
}