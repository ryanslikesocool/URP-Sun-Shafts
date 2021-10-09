// Made with love by Ryan Boyer http://ryanjboyer.com <3

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SunShaft
{
    public static class SunShaftExtensions
    {
        public static void Render(this CommandBuffer cmd, Camera camera, int texID, RenderTargetIdentifier texture, RenderTargetIdentifier target, Material material, int pass)
        {
            cmd.SetGlobalTexture(texID, texture);

            cmd.SetRenderTarget(
                target,
                RenderBufferLoadAction.DontCare,
                RenderBufferStoreAction.Store
            );

            cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
            cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, material, 0, pass);
        }
    }
}