using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SunShaft
{
    public class SunShaftFeature : ScriptableRendererFeature
    {
        public SunShaftSettings settings = new SunShaftSettings();

        private SunShaftPass sunShaftPass;

        public SunShaftBlendMode BlendMode
        {
            get => settings.blendMode;
            set => settings.blendMode = value;
        }
        public float DepthThreshold
        {
            get => settings.depthThreshold;
            set
            {
                settings.depthThreshold = Mathf.Clamp01(value);
                settings.sunShaftMaterial.SetFloat(SunShaftPass.DEPTH_THRESHOLD_PROP, settings.depthThreshold);
            }
        }
        public float Opacity
        {
            get => settings.opacity;
            set
            {
                settings.opacity = Mathf.Clamp01(value);
                settings.sunShaftMaterial.SetFloat(SunShaftPass.OPACITY_PROP, settings.opacity);
            }
        }
        public Vector3 Position
        {
            get => settings.sunPosition;
            set => settings.sunPosition = value;
        }
        public int RadialBlurIterations
        {
            get => settings.radialBlurIterations;
            set => settings.radialBlurIterations = Mathf.Clamp(value, 1, 4);
        }
        public Color Color
        {
            get => settings.sunColor;
            set => settings.sunColor = value;
        }
        public float Intensity
        {
            get => settings.sunIntensity;
            set => settings.sunIntensity = value;
        }
        public Color ColorThreshold
        {
            get => settings.sunColorThreshold;
            set
            {
                settings.sunColorThreshold = value;
                settings.sunShaftMaterial.SetVector(SunShaftPass.SUN_THRESHOLD_PROP, settings.sunColorThreshold);
            }
        }
        public float BlurRadius
        {
            get => settings.sunBlurRadius;
            set
            {
                settings.sunBlurRadius = value;
                settings.sunShaftMaterial.SetVector(SunShaftPass.BLUR_RADIUS_PROP, new Vector4(settings.sunBlurRadius, settings.sunBlurRadius));
            }
        }
        public float MaxRadius
        {
            get => settings.maxRadius;
            set => settings.maxRadius = value;
        }

        public override void Create()
        {
            sunShaftPass = new SunShaftPass(name, settings);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (settings.sunShaftMaterial == null) { return; }
            renderer.EnqueuePass(sunShaftPass);
        }

        public void ApplySettings()
        {
            sunShaftPass.ApplySettings();
        }
    }
}