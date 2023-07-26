==== Unity 2022.3 LTS Renderer Feature example for custom RenderTarget and fullscreen blit

This is an example of a Renderer Feature in Unity 2022.3 LTS which includes rendering to a custom RenderTarget with RTHandles, and a fullscreen blit using the Blitter API.  These must be implemented differently from Unity 2021.

This example is of a common use case which occurs when rendering sRGB UI in Linear color space.

Since the alpha blending result is different between Gamma and Linear color space, the final UI coloring will appear off (i.e not what the UI artist intended) when viewed in Linear color space.

We address this by implementing custom Renderer Features to perform the alpha blending manually.  The steps are as follows:
1. Render the UI into a custom RenderTarget named _UITexture.  If the UI texture is sRGB, we perform a color conversion.
2. Manually blend _UITexture on top of the camera RenderTarget, via a fullscreen blit.

Scenes
- Assets/Scenes/Scene_Default.unity: Default scene, without any Renderer Features.  You can see that the UI coloration will change when switching between Gamma/Linear color space.
- Assets/Scenes/Scene_WithRendererFeature.unity: Scene with Renderer Features.  The UI coloration when viewed in Linear color space will be the same as the coloration of Scene_Default in Gamma color space.

Shaders
- Assets/Shaders/UI.shader: Custom UI shader.  Performs color conversion when in Linear color space.  Only used on Image components using a sRGB texture.
- Assets/Shaders/UITextureAlphaBlend: Alpha blend shader.  Blends _UITexture on top of the camera texture.  Utilizes Blit.hlsl.

Renderer Features
- Assets/Scripts/DrawRenderersCustomRTRendererFeature.cs: Renderer Feature which draws renderers to a custom RenderTarget.  Uses RTHandles.
- Assets/Scripts/FullscreenBlitBlitterRendererFeature.cs: Renderer Feature which performs a fullscreen blit using the Blitter API.