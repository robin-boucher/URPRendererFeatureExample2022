using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class FullscreenBlitBlitterRendererFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class FullscreenBlitBlitterSettings
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        public Material material = null;
    }

    public class FullscreenBlitBlitterRenderPass : ScriptableRenderPass
    {
        private Material material;

        // Temporary RTHandle for blit
        private RTHandle tempRTHandle;

#if UNITY_EDITOR
        private RTHandle editorCameraColorTargetHandle;
#endif

        public FullscreenBlitBlitterRenderPass(RenderPassEvent renderPassEvent, Material material)
        {
            this.profilingSampler = new ProfilingSampler(nameof(FullscreenBlitBlitterRendererFeature));

            this.material = material;

            this.renderPassEvent = renderPassEvent;
        }

#if UNITY_EDITOR
        public void SetEditorCameraColorTarget(RTHandle cameraColorTargetRTHandle)
        {
            this.editorCameraColorTargetHandle = cameraColorTargetRTHandle;
        }
#endif

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            // Create temporary RTHandle with RenderingUtils.ReAllocateIfNeeded
            RenderTextureDescriptor desc = cameraTextureDescriptor;
            desc.depthBufferBits = 0;
            RenderingUtils.ReAllocateIfNeeded(ref this.tempRTHandle, desc, FilterMode.Point, TextureWrapMode.Clamp, false, 1, 0, "_TempRT");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (this.material == null) {
                return;
            }

            // Run (blit)
            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, this.profilingSampler)) {
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                // Camera color render target
#if UNITY_EDITOR
                RTHandle cameraColorTargetRTHandle = renderingData.cameraData.cameraType == CameraType.Game
                    ? renderingData.cameraData.renderer.cameraColorTargetHandle
                    : this.editorCameraColorTargetHandle;
                    
#else
                RTHandle cameraColorTargetRTHandle = renderingData.cameraData.renderer.cameraColorTargetHandle;
#endif

                // Use Blitter API to blit
                // Blit cameraColor -> tempRT with material
                Blitter.BlitCameraTexture(cmd, cameraColorTargetRTHandle, this.tempRTHandle, this.material, 0);
                // Blit tempRT -> cameraColor
                Blitter.BlitCameraTexture(cmd, this.tempRTHandle, cameraColorTargetRTHandle);
            }
            context.ExecuteCommandBuffer(cmd);

            CommandBufferPool.Release(cmd);
        }

        public void Dispose()
        {
            // Release temporary RTHandle
            this.tempRTHandle?.Release();
        }
    }

    [SerializeField] private FullscreenBlitBlitterSettings settings = new FullscreenBlitBlitterSettings();

    private FullscreenBlitBlitterRenderPass renderPass;

    public override void Create()
    {
        this.renderPass = new FullscreenBlitBlitterRenderPass(
            this.settings.renderPassEvent,
            this.settings.material
        );
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(this.renderPass);
    }

#if UNITY_EDITOR
    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType != CameraType.Game) {
            this.renderPass.SetEditorCameraColorTarget(renderer.cameraColorTargetHandle);
        }
    }
#endif

    protected override void Dispose(bool disposing)
    {
        // Use Dispose for cleanup

        this.renderPass.Dispose();
    }
}