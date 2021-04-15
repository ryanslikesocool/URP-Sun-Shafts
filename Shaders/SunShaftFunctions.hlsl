float transformColor(float4 skyboxValue, float3 sunThreshold)
{
    return dot(max(skyboxValue.rgb - sunThreshold, 0), 1); // threshold and convert to greyscale
}