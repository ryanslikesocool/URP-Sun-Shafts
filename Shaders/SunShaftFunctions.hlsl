#if defined(USING_STEREO_MATRICES)
    #define unity_eyeIndex unity_StereoEyeIndex
#else
    #define unity_eyeIndex 0
#endif

half4x4 _CameraVP[2];

half transformColor(half4 skyboxValue, half3 sunThreshold)
{
    return dot(max(skyboxValue.rgb - sunThreshold, 0), 1); // threshold and convert to greyscale
}

half3 worldToScreenPosition(half3 pnt)
{
    half4x4 camVP = _CameraVP[unity_eyeIndex];

    half3 result;
    result.x = camVP._m00 * pnt.x + camVP._m01 * pnt.y + camVP._m02 * pnt.z + camVP._m03;
    result.y = camVP._m10 * pnt.x + camVP._m11 * pnt.y + camVP._m12 * pnt.z + camVP._m13;
    result.z = camVP._m20 * pnt.x + camVP._m21 * pnt.y + camVP._m22 * pnt.z + camVP._m23;
    half num = camVP._m30 * pnt.x + camVP._m31 * pnt.y + camVP._m32 * pnt.z + camVP._m33;
    num = 1.0 / num;
    result.x *= num;
    result.y *= num;
    result.z = num;

    result.x = result.x * 0.5 + 0.5;
    result.y = result.y * 0.5 + 0.5;

    return result;
}