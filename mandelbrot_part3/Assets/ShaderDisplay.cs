using UnityEngine;

public class ShaderDisplay : MonoBehaviour {
  [SerializeField] private ComputeShader computeShader;

  // Controls for rendering.
  private RenderTexture renderTexture;
  private bool needsUpdate = true;

  [Header("Location")]
  // These denote the coordinates of the centre of our drawing.
  [SerializeField] private double img_real = 0.0;
  [SerializeField] private double img_imag = 0.0;
  [SerializeField] private double pixel_size = 4.0 / 2160;

  [Header("Colour setup")]
  [Range(1, 256)]
  [SerializeField] private int iterationsPerGroup = 256;
  [Range(1,10)]
  [SerializeField] private int numGroups = 1;
  [SerializeField] private Gradient gradient;
  // Texture to store our gradient that we'll pass to the shader.
  private Texture2D gradientTexture;

  private void UpdateGradient() {
    // Make sure the gradient texture is up to date.
    for (int i = 0; i < iterationsPerGroup; i++) {
      float percent = (float)i / (iterationsPerGroup - 1);
      gradientTexture.SetPixel(i, 1, gradient.Evaluate(percent));
    }
    gradientTexture.Apply();
  }

  private void UpdateRenderTexture(int width, int height) {
    // On first run create the new render texture.
    if (renderTexture == null) {
      renderTexture = new(width, height, 24) {
        // We need to set this for compute shaders.
        enableRandomWrite = true
      };
      renderTexture.Create();
    }

    // If we've changed the number of iterations resize the texture.
    if ((gradientTexture == null) ||  (iterationsPerGroup != gradientTexture.width)) {
      gradientTexture = new(iterationsPerGroup, 1);
    }
    UpdateGradient();

    // Find our kernel and set render texture.
    int kernel = computeShader.FindKernel("Mandelbrot");
    computeShader.SetTexture(kernel, "result", renderTexture);

    // General parameters
    computeShader.SetFloat("width", width);
    computeShader.SetFloat("height", height);

    // Define image to render.
    computeShader.SetFloat("img_real", (float)img_real);
    computeShader.SetFloat("img_imag", (float)img_imag);
    computeShader.SetFloat("pixel_size", (float)pixel_size);

    // Parameters to control how we render the output.
    computeShader.SetInt("iterations_per_group", iterationsPerGroup);
    computeShader.SetInt("num_groups", numGroups);
    computeShader.SetTexture(kernel, "gradient_texture", gradientTexture);

    // The compute shader works in (8, 8, 1) threads.
    computeShader.Dispatch(kernel, width / 8, height / 8, 1);
  }

  private void OnValidate() {
    // When we change values in the inspector update the shader output.
    needsUpdate = true;
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

  private void Update() {
    // Arrow keys to move around
    if (Input.GetKey(KeyCode.LeftArrow)) { Move(-1, 0); }
    if (Input.GetKey(KeyCode.RightArrow)) { Move(1, 0); }
    if (Input.GetKey(KeyCode.UpArrow)) { Move(0, 1); }
    if (Input.GetKey(KeyCode.DownArrow)) { Move(0, -1); }

    // +/- to zoom in and out.
    if (Input.GetKey(KeyCode.KeypadPlus)) { Zoom(-1); }
    if (Input.GetKey(KeyCode.KeypadMinus)) { Zoom(1); }

    // Side mouse buttons for zoom.
    if (Input.GetMouseButton(3)) { Zoom(1); }
    if (Input.GetMouseButton(4)) { Zoom(-1); }

    // Centre screen on location of left mouse click.
    if (Input.GetMouseButtonDown(0)) { CentreOnMouse(); }
  }

  void CentreOnMouse() {
    // Offset by half screen width as we want 0,0 to be in the centre.
    double mouseX = Input.mousePosition.x - (Screen.width / 2.0);
    double mouseY = Input.mousePosition.y - (Screen.height / 2.0);

    // Convert from pixels to complex units.
    img_real += mouseX * pixel_size;
    img_imag += mouseY * pixel_size;

    // Mark that we need to rerun the compute shader.
    needsUpdate = true;
  }

  private void Move(double real, double imag) {
    // How much we move is dependant on the zoom level and the time elapsed.
    double moveSpeed = Screen.height / 5; // Essentially pixels/second
    double moveAmount = moveSpeed * pixel_size * Time.deltaTime;

    // Update the centre position.
    img_real += real * moveAmount;
    img_imag += imag * moveAmount;
    // Note that we need to update the compute shader.
    needsUpdate = true;
  }

  private void Zoom(int direction) {
    // Double the zoom every second.
    pixel_size += direction * pixel_size * Mathf.Min(Time.deltaTime, 0.5f);
    // Note that we need to update the compute shader.
    needsUpdate = true;
  }
}
