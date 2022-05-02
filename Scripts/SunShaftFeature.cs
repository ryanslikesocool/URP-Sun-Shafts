// Developed With Love by Ryan Boyer http://ryanjboyer.com <3

using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SunShaft {
    public class SunShaftFeature : ScriptableRendererFeature {
        public Settings settings = new Settings();

        private SunShaftPass pass;

        public override void Create() {
            pass = new SunShaftPass(settings, name);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
            renderer.EnqueuePass(pass);
        }
    }
}