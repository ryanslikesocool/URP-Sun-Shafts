// Made with love by Ryan Boyer http://ryanjboyer.com <3

using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SunShaft
{
    [System.Serializable]
    public class SunShaftSettings
    {
        [Tooltip("When in the frame should the effect be renderered?  Leave at Before Rendering Post Processing for best results.")] public RenderPassEvent passEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        [Tooltip("The material to use for the effect.  Assign this with a material that uses the URPSunShafts shader.")] public Material sunShaftMaterial = null;

        [Space, Tooltip("What background will produce the effect?\nDepth mode requires the URP Asset's depth toggle to be on.")] public SunShaftRenderMode renderMode = SunShaftRenderMode.Color;
        [Tooltip("Lower resolution produces a blurrier effect in the foreground but will lose resolution in the background.")] public SunShaftResolution resolution = SunShaftResolution.Normal;
        [Tooltip("Screen is subtler, while Add is more prominent")] public SunShaftBlendMode blendMode = SunShaftBlendMode.Screen;
        [Range(0, 1), Tooltip("Some recommended presets:\nDepth - 0.06\nSkybox/Color - 0.2")] public float depthThreshold = 0.018f;
        [Range(0, 1), Tooltip("How opaque is the effect?")] public float opacity = 1;

        [Space, Tooltip("The \"sun\" position in world space.")] public Vector3 sunPosition = Vector3.forward * 10;
        [Range(1, 4), Tooltip("More iterations will execute extra passes but produce a nicer effect.")] public int radialBlurIterations = 2;
        [ColorUsage(false), Tooltip("The color of the sun and shafts.")] public Color sunColor = Color.white;
        [Tooltip("How intense is the sunlight?")] public float sunIntensity = 1.15f;
        [ColorUsage(false), Tooltip("Additional blending")] public Color sunColorThreshold = Color.black;
        [Tooltip("How far are the shafts from the objects that produce them?")] public float sunBlurRadius = 2.5f;
        [Range(0, 1), Tooltip("Additional blending")] public float maxRadius = 0.75f;

        public float Offset => sunBlurRadius * 0.00130f;
        public Vector4 SunColorIntensity => (sunColor * sunIntensity);
        public int BlendPass => (int)blendMode;
        public int Resolution => (int)resolution;
    }
}