using UnityEngine;

namespace SunShaft
{
    public static class SunShaftExtensions
    {
        public static Color SaturateOpacity(this Color input)
        {
            input.a = Mathf.Clamp01(input.a);
            return input;
        }

        public static Color Opacity(this Color input, float opacity)
        {
            input.a = opacity;
            return input;
        }

        public static Vector4 SetW(this Vector4 input, float w)
        {
            input.w = w;
            return input;
        }
    }
}