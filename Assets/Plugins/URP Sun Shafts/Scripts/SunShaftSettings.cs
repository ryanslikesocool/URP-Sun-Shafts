using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SunShaft
{
    [System.Serializable]
    public class SunShaftSettings
    {
        public RenderPassEvent passEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        public Material sunShaftMaterial = null;

        [Space] public SunShaftRenderMode renderMode = SunShaftRenderMode.DepthTexture;
        public SunShaftResolution resolution = SunShaftResolution.Normal;
        public SunShaftBlendMode blendMode = SunShaftBlendMode.Screen;
        [Range(0, 1)] public float depthThreshold = 0.018f;
        [Range(0, 1)] public float opacity = 1;

        [Space] public Vector3 sunPosition = Vector3.forward * 10;
        [Range(1, 4)] public int radialBlurIterations = 2;
        [ColorUsage(false)] public Color sunColor = Color.white;
        [ColorUsage(false)] public Color sunThreshold = new Color(0.87f, 0.74f, 0.65f);
        public float sunBlurRadius = 2.5f;
        public float sunIntensity = 1.15f;
        [Range(0, 2)] public float maxRadius = 0.75f;
    }
}