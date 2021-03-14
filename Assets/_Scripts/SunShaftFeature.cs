using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;

namespace SunShaft
{
    public class SunShaftFeature : ScriptableRendererFeature
    {
        [System.Serializable]
        public class Settings
        {
            public RenderPassEvent passEvent = RenderPassEvent.AfterRenderingOpaques;
            public Material sunShaftMaterial = null;

            [Space, Range(0, 1)] public float opacity = 1;

            [Space] public SunShaftResolution resolution = SunShaftResolution.Normal;
            public SunShaftBlendMode blendMode = SunShaftBlendMode.Screen;

            [Space] public Vector3 sunPosition = Vector3.forward * 10;
            [Range(1, 4)] public int radialBlurIterations = 2;
            public Color sunColor = Color.white;
            public Color sunThreshold = new Color(0.87f, 0.74f, 0.65f);
            public float sunBlurRadius = 2.5f;
            public float sunIntensity = 1.15f;

            [Space] public float maxRadius = 0.75f;

            [Space] public bool useDepthTexture = true;
        }

        public Settings settings = new Settings();

        private SunShaftPass sunShaftPass;

        public override void Create()
        {
            sunShaftPass = new SunShaftPass(name, settings);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (settings.sunShaftMaterial == null) { return; }

            sunShaftPass.Setup(renderer);
            renderer.EnqueuePass(sunShaftPass);
        }
    }
}