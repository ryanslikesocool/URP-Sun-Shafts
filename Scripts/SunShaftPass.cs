// Developed With Love by Ryan Boyer http://ryanjboyer.com <3

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SunShaft {
    public class SunShaftPass : ScriptableRenderPass {
        private static readonly int[] RenderTargetIDs = new int[2] {
            Shader.PropertyToID("_SunBuffer0"),
            Shader.PropertyToID("_SunBuffer1")
        };
        private static readonly int TemporaryRenderTargetID = Shader.PropertyToID("_TemporaryRenderTarget");

        private List<ShaderTagId> shaderTagIDs = new List<ShaderTagId>();

        private RenderTargetIdentifier[] renderTargetIdentifiers = null;
        private RenderTargetIdentifier tempRenderTargetIdentifier;
        private FilteringSettings filteringSettings;
        private RenderStateBlock renderStateBlock;

        private Settings settings;

        private Material material;

        public SunShaftPass(Settings settings, string tag) {
            profilingSampler = new ProfilingSampler(tag);
            filteringSettings = new FilteringSettings(null, settings.layerMask);

            shaderTagIDs.Add(new ShaderTagId("SRPDefaultUnlit"));
            shaderTagIDs.Add(new ShaderTagId("UniversalForward"));
            shaderTagIDs.Add(new ShaderTagId("UniversalForwardOnly"));
            shaderTagIDs.Add(new ShaderTagId("LightweightForward"));

            renderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);

            this.renderPassEvent = settings.renderPassEvent;
            this.settings = settings;

            renderTargetIdentifiers = new RenderTargetIdentifier[2];

            material = new Material(settings.shader);
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData) {
            RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;

            //descriptor.colorFormat = RenderTextureFormat.ARGB32;
            //descriptor.useDynamicScale = true;

            { // temporary render target
                //descriptor.depthBufferBits = 0;

                cmd.GetTemporaryRT(TemporaryRenderTargetID, descriptor);
                tempRenderTargetIdentifier = new RenderTargetIdentifier(TemporaryRenderTargetID);
                ConfigureTarget(tempRenderTargetIdentifier);
            }

            { // sun buffer targets
                //descriptor.depthBufferBits = 8;
                descriptor.width /= (int)settings.resolution;
                descriptor.height /= (int)settings.resolution;

                for (int i = 0; i < 2; i++) {
                    cmd.GetTemporaryRT(RenderTargetIDs[i], descriptor);
                    renderTargetIdentifiers[i] = new RenderTargetIdentifier(RenderTargetIDs[i]);
                    ConfigureTarget(renderTargetIdentifiers[i], depthAttachment);
                    ConfigureClear(ClearFlag.Color, Color.clear);
                }
            }
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
            SortingCriteria sortingCriteria = SortingCriteria.CommonOpaque;
            DrawingSettings drawingSettings = CreateDrawingSettings(shaderTagIDs, ref renderingData, sortingCriteria);

            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, profilingSampler)) {
                ScriptableRenderer renderer = renderingData.cameraData.renderer;
                Camera camera = renderingData.cameraData.camera;

                // material properties

                Vector3 sunViewportPosition = new Vector3(0.5f, 0.5f, 0.0f);

                material.SetVector("_BlurRadius4", new Vector4(1.0f, 1.0f, 0.0f, 0.0f) * settings.blurRadius);
                material.SetVector("_SunPosition", new Vector4(sunViewportPosition.x, sunViewportPosition.y, sunViewportPosition.z, settings.maxRadius));
                material.SetVector("_SunThreshold", settings.threshold);

                // clear background

                if (settings.backgroundMode == BackgroundMode.Skybox) {
                    var format = camera.allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;

                    cmd.SetRenderTarget(tempRenderTargetIdentifier, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare);
                    GL.ClearWithSkybox(false, camera);

                    //material.SetTexture("_Skybox", tempRenderTargetIdentifier);
                    cmd.SetGlobalTexture("_Skybox", tempRenderTargetIdentifier);
                    cmd.Blit(renderer.cameraColorTarget, renderTargetIdentifiers[0], material, 3);
                } else {
                    cmd.Blit(renderer.cameraColorTarget, renderTargetIdentifiers[0], material, 2);
                }

                // initial blur

                float offset = settings.blurRadius * (1.0f / 768.0f);

                material.SetVector("_BlurRadius4", new Vector4(offset, offset, 0.0f, 0.0f));
                material.SetVector("_SunPosition", new Vector4(sunViewportPosition.x, sunViewportPosition.y, sunViewportPosition.z, settings.maxRadius));

                // blur loop

                for (int i = 0; i < settings.radialBlurIterations; i++) {
                    // each iteration takes 2 * 6 samples
                    // update _BlurRadius each time to cheaply get a smooth look

                    //renderTargetIdentifiers[1] = RenderTexture.GetTemporary(rtW, rtH, 0);
                    cmd.Blit(renderTargetIdentifiers[0], renderTargetIdentifiers[1], material, 1);
                    //RenderTexture.ReleaseTemporary(lrBufferA);
                    offset = settings.blurRadius * (((i * 2.0f + 1.0f) * 6.0f)) / 768.0f;
                    material.SetVector("_BlurRadius4", new Vector4(offset, offset, 0.0f, 0.0f));

                    //renderTargetIdentifiers[0] = RenderTexture.GetTemporary(rtW, rtH, 0);
                    cmd.Blit(renderTargetIdentifiers[1], renderTargetIdentifiers[0], material, 1);
                    //RenderTexture.ReleaseTemporary(lrBufferB);
                    offset = settings.blurRadius * (((i * 2.0f + 2.0f) * 6.0f)) / 768.0f;
                    material.SetVector("_BlurRadius4", new Vector4(offset, offset, 0.0f, 0.0f));
                }

                // combine

                if (sunViewportPosition.z >= 0.0f) {
                    material.SetVector("_SunColor", settings.sunColor * settings.intensity);
                } else {
                    material.SetVector("_SunColor", Vector4.zero); // no backprojection
                }
                //material.SetTexture("_ColorBuffer", renderTargetIdentifiers[0]);
                cmd.SetGlobalTexture("_ColorBuffer", renderTargetIdentifiers[0]);

                cmd.Blit(renderer.cameraColorTarget, tempRenderTargetIdentifier, material, (int)settings.blendMode);
                cmd.Blit(tempRenderTargetIdentifier, renderer.cameraColorTarget, null);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void FrameCleanup(CommandBuffer cmd) {
            if (cmd == null) {
                throw new System.ArgumentNullException("cmd");
            }

            cmd.ReleaseTemporaryRT(TemporaryRenderTargetID);
            for (int i = 0; i < 2; i++) {
                cmd.ReleaseTemporaryRT(RenderTargetIDs[i]);
            }
        }
    }
}