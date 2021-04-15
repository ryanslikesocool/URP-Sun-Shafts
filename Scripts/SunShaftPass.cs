using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if UNITY_XR
using UnityEngine.XR;
#endif

namespace SunShaft
{
    public class SunShaftPass : ScriptableRenderPass
    {
        public const string OPACITY_PROP = "_Opacity";
        public const string BLUR_RADIUS_PROP = "_BlurRadius";
        public const string SUN_COLOR_PROP = "_SunColor";
        public const string SUN_POSITION_PROP = "_SunPosition";
        public const string SUN_THRESHOLD_PROP = "_SunThreshold";
        public const string SKYBOX_PROP = "_Skybox";
        public const string COLOR_BUFFER_PROP = "_SunShaftColorBuffer";
        public const string DEPTH_THRESHOLD_PROP = "_DepthThreshold";

        private string profilerTag;

        private SunShaftSettings settings;

        private int rtW;
        private int rtH;

        private readonly int sunBufferAId = Shader.PropertyToID("_SunBufferA");
        private readonly int sunBufferBId = Shader.PropertyToID("_SunBufferB");
        private RenderTargetIdentifier sunBufferA;
        private RenderTargetIdentifier sunBufferB;

#if UNITY_XR
        private Vector2 eyeSize;
#endif

        public SunShaftPass(string profilerTag, SunShaftSettings settings)
        {
            this.profilerTag = profilerTag;
            this.settings = settings;
            renderPassEvent = settings.passEvent;

            ApplySettings();
        }

        public void ApplySettings()
        {
            Material mat = settings.sunShaftMaterial;
            mat.SetFloat(DEPTH_THRESHOLD_PROP, settings.depthThreshold);
            mat.SetFloat(OPACITY_PROP, settings.opacity);
            mat.SetVector(SUN_THRESHOLD_PROP, settings.sunColorThreshold);
            mat.SetVector(BLUR_RADIUS_PROP, Vector2.one * settings.sunBlurRadius);

            float ofs = settings.Offset;
            mat.SetVector(BLUR_RADIUS_PROP, Vector2.one * ofs);
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
#if UNITY_XR
            RenderTextureDescriptor blitTargetDescriptor = XRSettings.eyeTextureDesc;
            if (blitTargetDescriptor.Equals(default(RenderTextureDescriptor)))
            {
                blitTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            }
            else
            {
                eyeSize = new Vector2(XRSettings.eyeTextureWidth, XRSettings.eyeTextureHeight);
            }
#else
            RenderTextureDescriptor blitTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
#endif
            blitTargetDescriptor.width /= settings.Resolution;
            blitTargetDescriptor.height /= settings.Resolution;

            rtW = blitTargetDescriptor.width;
            rtH = blitTargetDescriptor.height;

            cmd.GetTemporaryRT(sunBufferAId, blitTargetDescriptor);
            cmd.GetTemporaryRT(sunBufferBId, blitTargetDescriptor);

            sunBufferA = new RenderTargetIdentifier(sunBufferAId);
            sunBufferB = new RenderTargetIdentifier(sunBufferBId);

            ConfigureTarget(sunBufferA);
            ConfigureTarget(sunBufferB);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(profilerTag);
            ScriptableRenderer renderer = renderingData.cameraData.renderer;
            Camera camera = Camera.main;
            if (camera == null) { return; }
            // start rendering

#if UNITY_EDITOR
            ApplySettings();
#endif

            Material mat = settings.sunShaftMaterial;

            Vector4 sunScreenPosition = camera.WorldToViewportPoint(settings.sunPosition);

            mat.SetVector(SUN_POSITION_PROP, sunScreenPosition.SetW(settings.maxRadius));
            mat.SetVector(SUN_COLOR_PROP, sunScreenPosition.z >= 0 ? settings.SunColorIntensity : Vector4.zero);

            if (settings.renderMode == SunShaftRenderMode.Depth)
            {
                cmd.Blit(renderer.cameraColorTarget, sunBufferA, mat, 2);
            }
            else
            {
#if UNITY_XR
                RenderTextureFormat format = XRSettings.eyeTextureDesc.colorFormat;
                if (format.Equals(default(RenderTextureFormat)))
                {
                    format = camera.allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;
                }
#else
                RenderTextureFormat format = camera.allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;
#endif
                RenderTexture tmpBuffer = RenderTexture.GetTemporary(rtW, rtH, 0, format);
                RenderTexture.active = tmpBuffer;

                if (settings.renderMode == SunShaftRenderMode.Skybox)
                {
                    GL.ClearWithSkybox(false, camera);
                }
                else
                {
                    cmd.ClearRenderTarget(false, true, camera.backgroundColor);
                }

                mat.SetTexture(SKYBOX_PROP, tmpBuffer);
                cmd.Blit(renderer.cameraColorTarget, sunBufferA, mat, 3);
                RenderTexture.ReleaseTemporary(tmpBuffer);
            }

            float ofs = settings.Offset;

            for (int i = 0; i < settings.radialBlurIterations; i++)
            {
                cmd.Blit(sunBufferA, sunBufferB, mat, 1);
                mat.SetVector(BLUR_RADIUS_PROP, Vector2.one * ofs * (i * 2 + 1) * 6);

                cmd.Blit(sunBufferB, sunBufferA, mat, 1);
                mat.SetVector(BLUR_RADIUS_PROP, Vector2.one * ofs * (i * 2 + 2) * 6);
            }

            cmd.SetGlobalTexture(COLOR_BUFFER_PROP, sunBufferA);
            cmd.Blit(renderer.cameraColorTarget, renderer.cameraColorTarget, mat, settings.BlendPass);

            // end rendering
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