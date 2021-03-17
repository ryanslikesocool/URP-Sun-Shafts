using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SunShaft
{
    public class SunShaftPass : ScriptableRenderPass
    {
        private const string OPACITY_PROP = "_Opacity";
        private const string BLUR_RADIUS_PROP = "_BlurRadius4";
        private const string SUN_COLOR_PROP = "_SunColor";
        private const string SUN_POSITION_PROP = "_SunPosition";
        private const string SUN_THRESHOLD_PROP = "_SunThreshold";
        private const string SKYBOX_PROP = "_Skybox";
        private const string COLOR_BUFFER_PROP = "_ColorBuffer";

        private string profilerTag;

        private SunShaftFeature.Settings settings;

        private ScriptableRenderer renderer;

        private int rtW;
        private int rtH;

        private readonly int sunBufferAId = Shader.PropertyToID("_SunBufferA");
        private readonly int sunBufferBId = Shader.PropertyToID("_SunBufferB");
        private RenderTargetIdentifier sunBufferA;
        private RenderTargetIdentifier sunBufferB;

        private RenderTexture colorBufferRT;

        public SunShaftPass(string profilerTag, SunShaftFeature.Settings settings)
        {
            this.profilerTag = profilerTag;
            this.settings = settings;
            renderPassEvent = settings.passEvent;
        }

        public void Setup(ScriptableRenderer renderer)
        {
            this.renderer = renderer;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            RenderTextureDescriptor blitTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;

            int divider = (int)settings.resolution;

            rtW = blitTargetDescriptor.width / divider;
            rtH = blitTargetDescriptor.height / divider;

            cmd.GetTemporaryRT(sunBufferAId, rtW, rtH, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB32);
            cmd.GetTemporaryRT(sunBufferBId, rtW, rtH, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB32);

            sunBufferA = new RenderTargetIdentifier(sunBufferAId);
            sunBufferB = new RenderTargetIdentifier(sunBufferBId);

            ConfigureTarget(sunBufferA);
            ConfigureTarget(sunBufferB);

            colorBufferRT = RenderTexture.GetTemporary(rtW, rtH, 0);
            colorBufferRT.name = "_ColorBufferRT"; //id render texture for frame debugger
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(profilerTag);

            //start rendering

            Camera camera = Camera.main;
            if (settings.useDepthTexture)
            {
                camera.depthTextureMode |= DepthTextureMode.Depth;
            }

            Vector3 v = Vector3.one * 0.5f;
            if (settings.sunPosition != Vector3.zero)
            {
                v = camera.WorldToViewportPoint(settings.sunPosition);
            }
            else
            {
                v.z = 0;
            }

            Material mat = settings.sunShaftMaterial;
            mat.SetFloat(OPACITY_PROP, settings.opacity);
            mat.SetVector(BLUR_RADIUS_PROP, new Vector4(1, 1, 0, 0) * settings.sunBlurRadius);
            mat.SetVector(SUN_POSITION_PROP, new Vector4(v.x, v.y, v.z, settings.maxRadius));
            mat.SetVector(SUN_THRESHOLD_PROP, settings.sunThreshold);

            if (!settings.useDepthTexture)
            {
                var format = camera.allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;
                RenderTexture tmpBuffer = RenderTexture.GetTemporary(rtW, rtH, 0, format);
                RenderTexture.active = tmpBuffer;
                GL.ClearWithSkybox(false, camera);

                mat.SetTexture(SKYBOX_PROP, tmpBuffer);
                cmd.Blit(renderer.cameraColorTarget, sunBufferA, mat, 3);
                RenderTexture.ReleaseTemporary(tmpBuffer);
            }
            else
            {
                cmd.Blit(renderer.cameraColorTarget, sunBufferA, mat, 2);
            }

            float ofs = settings.sunBlurRadius * (1 / 768f);

            mat.SetVector(BLUR_RADIUS_PROP, new Vector4(ofs, ofs, 0, 0));

            for (int i = 0; i < settings.radialBlurIterations; i++)
            {
                cmd.Blit(sunBufferA, sunBufferB, mat, 1);
                ofs = settings.sunBlurRadius * ((i * 2 + 1) * 6) / 768f;
                mat.SetVector(BLUR_RADIUS_PROP, new Vector4(ofs, ofs, 0, 0));

                cmd.Blit(sunBufferB, sunBufferA, mat, 1);
                ofs = settings.sunBlurRadius * ((i * 2 + 2) * 6) / 768f;
                mat.SetVector(BLUR_RADIUS_PROP, new Vector4(ofs, ofs, 0, 0));
            }

            if (v.z >= 0)
            {
                mat.SetVector(SUN_COLOR_PROP, new Vector4(settings.sunColor.r, settings.sunColor.g, settings.sunColor.b, settings.sunColor.a) * settings.sunIntensity);
            }
            else
            {
                mat.SetVector(SUN_COLOR_PROP, Vector4.zero);
            }

            cmd.Blit(sunBufferA, colorBufferRT, mat, 1);
            mat.SetTexture(COLOR_BUFFER_PROP, colorBufferRT);

            cmd.Blit(renderer.cameraColorTarget, renderer.cameraColorTarget, mat, (settings.blendMode == SunShaftBlendMode.Screen) ? 0 : 4);

            //end rendering

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(sunBufferAId);
            cmd.ReleaseTemporaryRT(sunBufferBId);
            RenderTexture.ReleaseTemporary(colorBufferRT);
        }
    }
}