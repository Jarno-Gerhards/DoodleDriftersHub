using System;
using UnityEngine;
using Unity.InferenceEngine;
using TMPro;

/// <summary>
/// Runs the doodle classification model and exposes the result both via UI
/// and via a callback (label + confidence) for the GameLoopOrchestrator.
/// </summary>
public class DoodleInference : MonoBehaviour
{
    [Header("Model")]
    public ModelAsset modelAsset;

    [Header("References")]
    public DoodleDrawer drawingInput;

    [Header("UI")]
    public TextMeshProUGUI resultText;

    private Worker   _worker;
    private string[] _classLabels = { "Bandage", "Compass", "Hammer", "Ladder", "Lantern", "Sword" };

    // ── Result cache (readable by orchestrator without callback) ─────────────
    public string LastPredictedLabel { get; private set; } = string.Empty;
    public float  LastConfidence     { get; private set; } = 0f;

    // ── Callback set by orchestrator before Submit is pressed ────────────────
    /// <summary>
    /// Optional. Set this before the player presses Submit.
    /// Called with (label, confidence) once Predict() finishes.
    /// </summary>
    public Action<string, float> OnPredictionComplete;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Start()
    {
        Model model = ModelLoader.Load(modelAsset);
        _worker = new Worker(model, BackendType.GPUCompute);
    }

    private void OnDestroy()
    {
        _worker?.Dispose();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Called by the Submit button (and by the orchestrator indirectly via that button).
    /// Runs inference, updates UI, caches result, and fires OnPredictionComplete.
    /// </summary>
    public void Predict()
    {
        Texture2D drawing = drawingInput.GetTexture();

        using Tensor<float> inputTensor = TextureToTensor(drawing);
        _worker.Schedule(inputTensor);

        Tensor<float> outputGPU = _worker.PeekOutput() as Tensor<float>;
        Tensor<float> output     = outputGPU.ReadbackAndClone();

        // Get predicted class and confidence
        int   predictedIndex = ArgMax(output);
        float confidence     = Softmax(output, predictedIndex);

        output.Dispose();

        string label = _classLabels[predictedIndex];

        // Cache
        LastPredictedLabel = label;
        LastConfidence     = confidence;

        // UI
        if (resultText != null)
            resultText.text = $"I think this is a {label} ({confidence:P0})";

        Debug.Log($"[DoodleInference] Predicted: {label} (confidence: {confidence:P1})");

        // Notify orchestrator (if listening)
        OnPredictionComplete?.Invoke(label, confidence);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private Tensor<float> TextureToTensor(Texture2D texture)
    {
        const int W = 28, H = 28;
        Texture2D resized = Resize(texture, W, H);

        var tensor = new Tensor<float>(new TensorShape(1, H, W, 1));
        Color[] pixels = resized.GetPixels();

        for (int y = 0; y < H; y++)
        for (int x = 0; x < W; x++)
        {
            float grayscale = 1f - pixels[y * W + x].grayscale;
            tensor[0, y, x, 0] = grayscale;
        }

        return tensor;
    }

    private int ArgMax(Tensor<float> tensor)
    {
        int   maxIndex = 0;
        float maxValue = tensor[0];

        for (int i = 1; i < tensor.shape[1]; i++)
        {
            if (tensor[i] > maxValue)
            {
                maxValue = tensor[i];
                maxIndex = i;
            }
        }

        return maxIndex;
    }

    /// <summary>Returns the softmax probability of one class index.</summary>
    private float Softmax(Tensor<float> tensor, int targetIndex)
    {
        int n = tensor.shape[1];

        float max = tensor[0];
        for (int i = 1; i < n; i++)
            if (tensor[i] > max) max = tensor[i];

        float sumExp    = 0f;
        float targetExp = 0f;

        for (int i = 0; i < n; i++)
        {
            float e = Mathf.Exp(tensor[i] - max);
            sumExp += e;
            if (i == targetIndex) targetExp = e;
        }

        return sumExp > 0f ? targetExp / sumExp : 0f;
    }

    private Texture2D Resize(Texture2D source, int width, int height)
    {
        RenderTexture rt = RenderTexture.GetTemporary(width, height);
        Graphics.Blit(source, rt);

        RenderTexture previous  = RenderTexture.active;
        RenderTexture.active    = rt;

        Texture2D result = new Texture2D(width, height);
        result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        result.Apply();

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);

        return result;
    }
}
