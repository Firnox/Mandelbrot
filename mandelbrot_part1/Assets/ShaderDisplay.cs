using UnityEngine;

public class ShaderDisplay : MonoBehaviour {
  [SerializeField] private ComputeShader computeShader;

  // Controls for rendering.
  private RenderTexture renderTexture;
  private bool needsUpdate = true;

  private void UpdateRenderTexture(int width, int height) {
    // On first run create the new render texture.
    if (renderTexture == null) {
      renderTexture = new(width, height, 24) {
        // We need to set this for compute shaders.
        enableRandomWrite = true
      };
      renderTexture.Create();
    }

    // Find our kernel and set render texture.
    int kernel = computeShader.FindKernel("Mandelbrot");
    computeShader.SetTexture(kernel, "result", renderTexture);

    // General parameters
    computeShader.SetFloat("width", width);
    computeShader.SetFloat("height", height);

    // Define image to render.
    computeShader.SetFloat("img_real", 0f);
    computeShader.SetFloat("img_imag", 0f);
    computeShader.SetFloat("pixel_size", 4f / height);

    // The compute shader works in (8, 8, 1) threads.
    computeShader.Dispatch(kernel, width / 8, height / 8, 1);
  }

  private void OnRenderImage(RenderTexture source, RenderTexture destination) {
    // Only update the shader (as can be expensive) if anything has changed.
    if (needsUpdate) {
      UpdateRenderTexture(source.width, source.height);
      needsUpdate = false;
    }

    // Set our render to be our renderTexture.
    Graphics.Blit(renderTexture, destination);
  }
}
