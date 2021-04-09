using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SunShaft
{
    public class SunShaftFeature : ScriptableRendererFeature
    {
        public SunShaftSettings settings = new SunShaftSettings();

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