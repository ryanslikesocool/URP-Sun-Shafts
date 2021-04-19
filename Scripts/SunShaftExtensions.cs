using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SunShaft
{
    public static class SunShaftExtensions
    {
        public static Color SaturateOpacity(this Color input)
        {
            input.a = Mathf.Clamp01(input.a);
            return input;
        }

        public static Color Opacity(this Color input, float opacity)
        {
            input.a = opacity;
            return input;
        }

        public static Vector4 SetW(this Vector4 input, float w)
        {
            input.w = w;
            return input;
        }

        public static void Render(this CommandBuffer cmd, Camera camera, RenderTargetIdentifier target, Material material, int pass)
        {
            cmd.SetRenderTarget(
                target,
                RenderBufferLoadAction.DontCare,
                RenderBufferStoreAction.Store
            );

            cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
            cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, material, 0, pass);
            cmd.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);
        }

        public static void RenderWithDepth(this CommandBuffer cmd, Camera camera, RenderTargetIdentifier target, Material material, int pass)
        {
            cmd.SetRenderTarget(
                target,
                RenderBufferLoadAction.DontCare,
                RenderBufferStoreAction.Store,
                target,
                RenderBufferLoadAction.DontCare,
                RenderBufferStoreAction.DontCare
            );

            cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
            cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, material, 0, pass);
            cmd.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);
        }

        public static void RenderAndSetTexture(this CommandBuffer cmd, Camera camera, string texString, RenderTargetIdentifier texture, RenderTargetIdentifier target, Material material, int pass)
        {
            cmd.SetGlobalTexture(texString, texture);
            cmd.Render(camera, target, material, pass);
        }
    }
}