using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class OllamaDungeonMasterTtsClient : MonoBehaviour
{
    [Header("Speech Output")]
    [SerializeField] private TTS projectTts;

    [Header("Ollama Endpoint")]
    [SerializeField] private string ollamaGenerateUrl = "http://localhost:11434/api/generate";

    [Header("Request Settings")]
    [SerializeField] private string ollamaModel = "llama3.1:8b";
    [SerializeField] private int requestTimeoutSeconds = 15;

    private const string DungeonMasterSystemPrompt =
        "You are a Dungeon Master for a fantasy game. " +
        "Be creative, atmospheric, and cinematic. " +
        "Keep responses short (1-3 sentences) and easy to voice.";

    // Stores the most recent AI response text for easy debugging/inspection.
    public string LastAiResponse { get; private set; }

    private void Awake()
    {
        if (projectTts == null)
        {
            projectTts = GetComponent<TTS>();
        }
    }

    /// <summary>
    /// Main entry point: generates text with Ollama, then speaks it through the project's TTS component.
    /// Returns the generated text via callback.
    /// </summary>
    public IEnumerator GenerateAndSpeak(string userInput, Action<string> onComplete = null)
    {
        if (string.IsNullOrWhiteSpace(userInput))
        {
            Debug.LogWarning("GenerateAndSpeak called with empty userInput.");
            onComplete?.Invoke(string.Empty);
            yield break;
        }

        // Build Ollama payload with model, hardcoded DM system prompt, and player's input.
        OllamaGenerateRequest requestBody = new OllamaGenerateRequest
        {
            model = ollamaModel,
            system = DungeonMasterSystemPrompt,
            prompt = userInput,
            stream = false
        };

        string requestJson = JsonUtility.ToJson(requestBody);
        byte[] requestBytes = Encoding.UTF8.GetBytes(requestJson);

        using UnityWebRequest ollamaRequest = new UnityWebRequest(ollamaGenerateUrl, UnityWebRequest.kHttpVerbPOST);
        ollamaRequest.uploadHandler = new UploadHandlerRaw(requestBytes);
        ollamaRequest.downloadHandler = new DownloadHandlerBuffer();
        ollamaRequest.SetRequestHeader("Content-Type", "application/json");
        ollamaRequest.timeout = requestTimeoutSeconds;

        // Send text-generation request to local Ollama service.
        yield return ollamaRequest.SendWebRequest();

        if (ollamaRequest.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Ollama request failed: {ollamaRequest.error}. URL: {ollamaGenerateUrl}");
            onComplete?.Invoke(string.Empty);
            yield break;
        }

        string ollamaJson = ollamaRequest.downloadHandler.text;
        OllamaGenerateResponse ollamaResponse = null;

        try
        {
            ollamaResponse = JsonUtility.FromJson<OllamaGenerateResponse>(ollamaJson);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to parse Ollama JSON response: {ex.Message}. Raw: {ollamaJson}");
            onComplete?.Invoke(string.Empty);
            yield break;
        }

        if (ollamaResponse == null || string.IsNullOrWhiteSpace(ollamaResponse.response))
        {
            Debug.LogError($"Ollama response did not contain a usable 'response' field. Raw: {ollamaJson}");
            onComplete?.Invoke(string.Empty);
            yield break;
        }

        LastAiResponse = ollamaResponse.response.Trim();
        onComplete?.Invoke(LastAiResponse);

        if (projectTts == null)
        {
            Debug.LogError("No TTS component assigned/found. Add the TTS component and link it in the Inspector.");
            yield break;
        }

        projectTts.SpeakText(LastAiResponse, "OllamaDmSession");
    }

    [Serializable]
    private class OllamaGenerateRequest
    {
        public string model;
        public string system;
        public string prompt;
        public bool stream;
    }

    [Serializable]
    private class OllamaGenerateResponse
    {
        public string response;
    }

}
