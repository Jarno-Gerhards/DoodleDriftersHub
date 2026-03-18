using UnityEngine;
using Unity.InferenceEngine;
using TMPro;

public class DoodleInference : MonoBehaviour
{
    public ModelAsset modelAsset;
    public DoodleDrawer drawingInput;
    public TextMeshProUGUI resultText;

    private Worker worker;
    private string[] classLabels = new string[] { "Bandage" , "Compass" , "Hammer" , "Ladder" , "Lantern" , "Sword" };

    void Start()
    {
        //RunModel();
        Model model = ModelLoader.Load(modelAsset);
        worker = new Worker(model, BackendType.GPUCompute);
    }

    public void Predict()
    {
        Texture2D drawing = drawingInput.GetTexture();
        // Convert image to tensor
        Tensor<float> inputTensor = TextureToTensor(drawing);

        // Run inference
        worker.Schedule(inputTensor);

        // Get output
        Tensor<float> outputGPU = worker.PeekOutput() as Tensor<float>;
        Tensor<float> output = outputGPU.ReadbackAndClone();
        
        int predictedClass = ArgMax(output);
        
        output.Dispose();

        //Debug.Log("Predicted class: " + classLabels[predictedClass]);
        resultText.text = $"I think this is a {classLabels[predictedClass]}";

        inputTensor.Dispose();
        output.Dispose();
    }

    Tensor<float> TextureToTensor(Texture2D texture)
    {
        int width = 28;
        int height = 28;

        Texture2D resized = Resize(texture, width, height);

        Tensor<float> tensor = new Tensor<float>(new TensorShape(1, height, width, 1));

        Color[] pixels = resized.GetPixels();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float grayscale = 1f - pixels[y * width + x].grayscale;
                tensor[0, y, x, 0] = grayscale;
            }
        }

        return tensor;
    }

int ArgMax(Tensor<float> tensor)
{
    int maxIndex = 0;
    float maxValue = tensor[0];

    for (int i = 1; i < tensor.shape[1]; i++)
    {
        float val = tensor[i];
        if (val > maxValue)
        {
            maxValue = val;
            maxIndex = i;
        }
    }

    return maxIndex;
}

    Texture2D Resize(Texture2D source, int width, int height)
    {
        RenderTexture rt = RenderTexture.GetTemporary(width, height);
        Graphics.Blit(source, rt);

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D result = new Texture2D(width, height);
        result.ReadPixels(new Rect(0,0,width,height),0,0);
        result.Apply();

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);

        return result;
    }
}