// Developed With Love by Ryan Boyer http://ryanjboyer.com <3

using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SunShaft {
    [Serializable]
    public class Settings {
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        public LayerMask layerMask = -1;

        [Tooltip("The shader to use for the effect.  Assign this with the SunShafts shader.")] public Shader shader = null;

        public Resolution resolution = Resolution.Normal;
        public BlendMode blendMode = BlendMode.Screen;
        public BackgroundMode backgroundMode = BackgroundMode.Depth;

        [Range(1, 4)] public int radialBlurIterations = 2;
        public Color sunColor = Color.white;
        public Color threshold = new Color(0.87f, 0.74f, 0.65f);
        public float blurRadius = 2.5f;
        public float intensity = 1.15f;

        public float maxRadius = 0.75f;
    }
}