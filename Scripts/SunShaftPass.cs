using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

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
        public const string COLOR_TEX_PROP = "_ColorTexture";
        public const string DEPTH_THRESHOLD_PROP = "_DepthThreshold";

        private string profilerTag;

        private SunShaftSettings settings;

        private int rtW;
        private int rtH;

        private readonly int sunBufferAId = Shader.PropertyToID("_SunBufferA");
        private readonly int sunBufferBId = Shader.PropertyToID("_SunBufferB");
        private RenderTargetIdentifier sunBufferA;
        private RenderTargetIdentifier sunBufferB;

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
            mat.SetVector(BLUR_RADIUS_PROP, Vector2.one * settings.Offset);
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            RenderTextureDescriptor cameraTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            rtW = cameraTargetDescriptor.width / settings.Resolution;
            rtH = cameraTargetDescriptor.height / settings.Resolution;
            RenderTextureDescriptor newTargetDescriptor = new RenderTextureDescriptor(rtW, rtH, cameraTargetDescriptor.colorFormat, cameraTargetDescriptor.depthBufferBits);

            cmd.GetTemporaryRT(sunBufferAId, newTargetDescriptor);
            cmd.GetTemporaryRT(sunBufferBId, newTargetDescriptor);

            sunBufferA = new RenderTargetIdentifier(sunBufferAId);
            sunBufferB = new RenderTargetIdentifier(sunBufferBId);

            ConfigureTarget(sunBufferA);
            ConfigureTarget(sunBufferB);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(profilerTag);
            CameraData cameraData = renderingData.cameraData;
            ScriptableRenderer renderer = cameraData.renderer;
            Camera camera = cameraData.camera;
            if (camera == null) { return; }
            if (XRGraphics.enabled)
            {
                context.StartMultiEye(camera);
            }
            // start rendering

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                ApplySettings();
            }
#endif

            Material material = settings.sunShaftMaterial;

            Vector4 sunScreenPosition = camera.WorldToViewportPoint(settings.sunPosition);

            material.SetVector(SUN_POSITION_PROP, sunScreenPosition.SetW(settings.maxRadius));
            material.SetVector(SUN_COLOR_PROP, sunScreenPosition.z >= 0 ? settings.SunColorIntensity : Vector4.zero);

            if (false)// settings.renderMode == SunShaftRenderMode.Depth)
            {
                cmd.RenderAndSetTexture(camera, COLOR_TEX_PROP, renderer.cameraColorTarget, sunBufferA, material, 2);
            }
            else
            {
                RenderTextureFormat format = camera.allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;

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

                cmd.RenderAndSetTexture(camera, SKYBOX_PROP, tmpBuffer, sunBufferA, material, 3);
                RenderTexture.ReleaseTemporary(tmpBuffer);
            }

            float ofs = settings.Offset;

            for (int i = 0; i < settings.radialBlurIterations; i++)
            {
                cmd.RenderAndSetTexture(camera, COLOR_TEX_PROP, sunBufferA, sunBufferB, material, 1);
                material.SetVector(BLUR_RADIUS_PROP, Vector2.one * ofs * (i * 2 + 1) * 6);

                cmd.RenderAndSetTexture(camera, COLOR_TEX_PROP, sunBufferB, sunBufferA, material, 1);
                material.SetVector(BLUR_RADIUS_PROP, Vector2.one * ofs * (i * 2 + 2) * 6);
            }

            cmd.SetGlobalTexture(COLOR_BUFFER_PROP, sunBufferA);
            cmd.RenderAndSetTexture(camera, COLOR_TEX_PROP, renderer.cameraColorTarget, renderer.cameraColorTarget, material, settings.BlendPass);

            // end rendering
            if (XRGraphics.enabled)
            {
                context.StopMultiEye(camera);
            }
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