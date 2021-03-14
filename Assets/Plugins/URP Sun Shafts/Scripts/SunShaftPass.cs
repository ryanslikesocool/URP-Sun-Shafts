using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SunShaft
{
    public class SunShaftPass : ScriptableRenderPass
    {
        private string profilerTag;

        private SunShaftFeature.Settings settings;

        private ScriptableRenderer renderer;

        private int rtW;
        private int rtH;

        private readonly int sunBufferAId = Shader.PropertyToID("_SunBufferA");
        private readonly int sunBufferBId = Shader.PropertyToID("_SunBufferB");
        private RenderTargetIdentifier sunBufferA;
        private RenderTargetIdentifier sunBufferB;

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
            //blitTargetDescriptor.depthBufferBits = 16;

            int divider = 1;
            switch (settings.resolution)
            {
                case SunShaftResolution.Low:
                    divider = 4;
                    break;
                case SunShaftResolution.Normal:
                    divider = 2;
                    break;
            }

            rtW = blitTargetDescriptor.width / divider;
            rtH = blitTargetDescriptor.height / divider;

            cmd.GetTemporaryRT(sunBufferAId, rtW, rtH, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB32);
            cmd.GetTemporaryRT(sunBufferBId, rtW, rtH, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB32);

            sunBufferA = new RenderTargetIdentifier(sunBufferAId);
            sunBufferB = new RenderTargetIdentifier(sunBufferBId);

            ConfigureTarget(sunBufferA);
            ConfigureTarget(sunBufferB);
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
            mat.SetFloat("_Opacity", settings.opacity);
            mat.SetVector("_BlurRadius4", new Vector4(1, 1, 0, 0) * settings.sunBlurRadius);
            mat.SetVector("_SunPosition", new Vector4(v.x, v.y, v.z, settings.maxRadius));
            mat.SetVector("_SunThreshold", settings.sunThreshold);

            if (!settings.useDepthTexture)
            {
                var format = camera.allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;
                RenderTexture tmpBuffer = RenderTexture.GetTemporary(rtW, rtH, 0, format);
                RenderTexture.active = tmpBuffer;
                GL.ClearWithSkybox(false, camera);

                mat.SetTexture("_Skybox", tmpBuffer);
                cmd.Blit(renderer.cameraColorTarget, sunBufferA, mat, 3);
                RenderTexture.ReleaseTemporary(tmpBuffer);
            }
            else
            {
                cmd.Blit(renderer.cameraColorTarget, sunBufferA, mat, 2);
            }

            float ofs = settings.sunBlurRadius * (1 / 768f);

            mat.SetVector("_BlurRadius4", new Vector4(ofs, ofs, 0, 0));
            mat.SetVector("_SunPosition", new Vector4(v.x, v.y, v.z, settings.maxRadius));

            for (int i = 0; i < settings.radialBlurIterations; i++)
            {
                cmd.Blit(sunBufferA, sunBufferB, mat, 1);
                ofs = settings.sunBlurRadius * ((i * 2 + 1) * 6) / 768f;
                mat.SetVector("_BlurRadius4", new Vector4(ofs, ofs, 0, 0));

                cmd.Blit(sunBufferB, sunBufferA, mat, 1);
                ofs = settings.sunBlurRadius * ((i * 2 + 2) * 6) / 768f;
                mat.SetVector("_BlurRadius4", new Vector4(ofs, ofs, 0, 0));
            }

            if (v.z >= 0)
            {
                mat.SetVector("_SunColor", new Vector4(settings.sunColor.r, settings.sunColor.g, settings.sunColor.b, settings.sunColor.a) * settings.sunIntensity);
            }
            else
            {
                mat.SetVector("_SunColor", Vector4.zero);
            }

            RenderTexture renderTexture = RenderTexture.GetTemporary(rtW, rtH, 0);
            renderTexture.name = "_ColorBufferRT";
            cmd.Blit(sunBufferA, renderTexture, mat, 1);
            mat.SetTexture("_ColorBuffer", renderTexture);
            RenderTexture.ReleaseTemporary(renderTexture);

            cmd.Blit(renderer.cameraColorTarget, renderer.cameraColorTarget, mat, (settings.blendMode == SunShaftBlendMode.Screen) ? 0 : 4);

            //end rendering
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(sunBufferAId);
            cmd.ReleaseTemporaryRT(sunBufferBId);
        }
    }
}