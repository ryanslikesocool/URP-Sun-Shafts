// Made with love by Ryan Boyer http://ryanjboyer.com <3

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR;

namespace SunShaft
{
    public class SunShaftPass : ScriptableRenderPass
    {
        public static readonly int OpacityID = Shader.PropertyToID("_Opacity");
        public static readonly int BlurRadiusID = Shader.PropertyToID("_BlurRadius");
        public static readonly int SunColorID = Shader.PropertyToID("_SunColor");
        public static readonly int SunPositionID = Shader.PropertyToID("_SunPosition");
        public static readonly int SunThresholdID = Shader.PropertyToID("_SunThreshold");
        public static readonly int SkyboxID = Shader.PropertyToID("_Skybox");
        public static readonly int ColorBufferID = Shader.PropertyToID("_SunShaftColorBuffer");
        public static readonly int ColorTexID = Shader.PropertyToID("_ColorTexture");
        public static readonly int DepthThresholdID = Shader.PropertyToID("_DepthThreshold");
        public static readonly int CameraVPID = Shader.PropertyToID("_CameraVP");

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
            mat.SetFloat(DepthThresholdID, settings.depthThreshold);
            mat.SetFloat(OpacityID, settings.opacity);
            mat.SetVector(SunThresholdID, settings.sunColorThreshold);
            mat.SetVector(BlurRadiusID, Vector2.one * settings.sunBlurRadius);
            mat.SetVector(BlurRadiusID, Vector2.one * settings.Offset);
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            RenderTextureDescriptor cameraTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            rtW = cameraTargetDescriptor.width / settings.Resolution;
            rtH = cameraTargetDescriptor.height / settings.Resolution;
            RenderTextureDescriptor newTargetDescriptor;
            newTargetDescriptor =
                XRSettings.enabled ?
                XRSettings.eyeTextureDesc :
                new RenderTextureDescriptor(rtW, rtH, cameraTargetDescriptor.colorFormat, cameraTargetDescriptor.depthBufferBits);

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
            if (XRSettings.enabled)
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

#if ENABLE_VR && ENABLE_XR_MODULE
            int eyeCount = renderingData.cameraData.xr.enabled && renderingData.cameraData.xr.singlePassEnabled ? 2 : 1;
#else
            int eyeCount = 1;
#endif

            Matrix4x4[] cameraMatrices = new Matrix4x4[2];

            for (int eyeIndex = 0; eyeIndex < eyeCount; eyeIndex++)
            {
                Matrix4x4 view = renderingData.cameraData.GetViewMatrix(eyeIndex);
                Matrix4x4 proj = renderingData.cameraData.GetProjectionMatrix(eyeIndex);
                cameraMatrices[eyeIndex] = proj * view;
            }

            Material material = settings.sunShaftMaterial;

            Vector4 sunPosition = settings.sunPosition;
            sunPosition.w = settings.maxRadius;

            material.SetMatrixArray(CameraVPID, cameraMatrices);
            material.SetVector(SunPositionID, sunPosition);
            material.SetVector(SunColorID, settings.sunPosition.z >= 0 ? settings.SunColorIntensity : Vector4.zero);

            //if (settings.renderMode == SunShaftRenderMode.Depth)
            //{
            //    cmd.Render(camera, ColorBufferID, renderer.cameraColorTarget, sunBufferA, material, 2);
            //}
            //else
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
                    cmd.ClearRenderTarget(true, true, camera.backgroundColor);
                }

                //cmd.Blit(tmpBuffer, sunBufferA, material, 3);
                cmd.Render(camera, SkyboxID, tmpBuffer, sunBufferA, material, 3);
                RenderTexture.ReleaseTemporary(tmpBuffer);
            }

            float ofs = settings.Offset;

            for (int i = 0; i < settings.radialBlurIterations; i++)
            {
                //cmd.Blit(sunBufferA, sunBufferB, material, 1);
                cmd.Render(camera, ColorTexID, sunBufferA, sunBufferB, material, 1);
                material.SetVector(BlurRadiusID, Vector2.one * ofs * (i * 2 + 1) * 6);

                //cmd.Blit(sunBufferB, sunBufferA, material, 1);
                cmd.Render(camera, ColorTexID, sunBufferB, sunBufferA, material, 1);
                material.SetVector(BlurRadiusID, Vector2.one * ofs * (i * 2 + 2) * 6);
            }

            cmd.SetGlobalTexture(ColorBufferID, sunBufferA);
            //cmd.Blit(renderer.cameraColorTarget, renderer.cameraColorTarget, material, settings.BlendPass);
            cmd.Render(camera, ColorTexID, renderer.cameraColorTarget, renderer.cameraColorTarget, material, settings.BlendPass);

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