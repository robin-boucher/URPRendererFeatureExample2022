using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

using System.Collections.Generic;

// Draws renderers into a custom RenderTarget
public class DrawRenderersCustomRTRendererFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class DrawRenderersCustomRTSettings
    {
        public string customRenderTargetName = "_CustomTargetTexture";
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        public LayerMask layerMask = -1;
    }

    public class DrawRenderersCustomRTRenderPass : ScriptableRenderPass
    {
        private string customRenderTargetName;

        // RTHandle for custom render target
        private RTHandle customRTHandle;

        protected FilteringSettings filteringSettings;

        protected List<ShaderTagId> shaderTagIds;

        public DrawRenderersCustomRTRenderPass(string customRenderTargetName, RenderPassEvent renderPassEvent, LayerMask layerMask)
        {
            this.profilingSampler = new ProfilingSampler(nameof(DrawRenderersCustomRTRendererFeature));

            this.customRenderTargetName = customRenderTargetName;

            this.renderPassEvent = renderPassEvent;

            this.filteringSettings = new FilteringSettings(RenderQueueRange.all, layerMask);

            // Target material shader pass names
            this.shaderTagIds = new List<ShaderTagId>();
            this.shaderTagIds.Add(new ShaderTagId("SRPDefaultUnlit"));
            this.shaderTagIds.Add(new ShaderTagId("UniversalForward"));
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            // Create RTHandle with RenderingUtils.ReAllocateIfNeeded
            RenderTextureDescriptor desc = cameraTextureDescriptor;
            desc.colorFormat = RenderTextureFormat.ARGB32; // For alpha
            desc.depthBufferBits = 0;
            RenderingUtils.ReAllocateIfNeeded(ref this.customRTHandle, desc, FilterMode.Point, TextureWrapMode.Clamp, false, 1, 0, this.customRenderTargetName);

            // Set render target
            ConfigureTarget(this.customRTHandle);

            // Clear
            ConfigureClear(ClearFlag.All, new Color(0f, 0f, 0f, 0f));

            // SetGlobalTexture to use in shader
            // This is required because the name is set automatically with appended information in ReAllocateIfNeeded
            cmd.SetGlobalTexture(this.customRenderTargetName, this.customRTHandle);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // Sorting criteria (default transparent)
            SortingCriteria sortingCriteria = SortingCriteria.CommonTransparent;

            // DrawingSettings
            DrawingSettings drawingSettings = CreateDrawingSettings(this.shaderTagIds, ref renderingData, sortingCriteria);
            drawingSettings.overrideMaterial = null;  // No override material

            // Run (DrawRenderers)
            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, this.profilingSampler)) {
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                // Draw
                context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref this.filteringSettings);
            }
            context.ExecuteCommandBuffer(cmd);

            CommandBufferPool.Release(cmd);
        }

        public void Dispose()
        {
            // Release RTHandle
            this.customRTHandle?.Release();
        }
    }

    [SerializeField] private DrawRenderersCustomRTSettings settings = new DrawRenderersCustomRTSettings();

    private DrawRenderersCustomRTRenderPass renderPass;

    public override void Create()
    {
        this.renderPass = new DrawRenderersCustomRTRenderPass(
            this.settings.customRenderTargetName,
            this.settings.renderPassEvent,
            this.settings.layerMask
        );
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(this.renderPass);
    }

    protected override void Dispose(bool disposing)
    {
        // Use Dispose for cleanup

        this.renderPass.Dispose();
    }
}