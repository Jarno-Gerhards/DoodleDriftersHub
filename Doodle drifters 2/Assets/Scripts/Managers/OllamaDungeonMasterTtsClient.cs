using System;
using System.Collections;
using System.Text;
using TMPro;
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

    [Header("Ollama Auto Start (Prototype)")]
    [SerializeField] private bool autoStartOllama = true;
    [SerializeField] private string ollamaExecutable = "ollama";
    [SerializeField] private string ollamaServeArgs = "serve";
    [SerializeField] private int startupWaitSeconds = 12;

    [Header("Prototype Status UI")]
    [SerializeField] private TextMeshProUGUI ollamaStatusText;

    private const string DungeonMasterSystemPrompt =
        "You are a Dungeon Master for a fantasy game. " +
        "Be creative, atmospheric, and cinematic. " +
        "Keep responses short (1-3 sentences) and easy to voice.";

    // Stores the most recent AI response text for easy debugging/inspection.
    public string LastAiResponse { get; private set; }

    private bool hasTriedAutoStart;
    private bool isCheckingOllama;
    private bool isOllamaReady;

    private void Awake()
    {
        if (projectTts == null)
        {
            projectTts = GetComponent<TTS>();
        }

        SetStatus("LLM: idle");
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
            SetStatus("LLM: idle");
            onComplete?.Invoke(string.Empty);
            yield break;
        }

        SetStatus("LLM: checking...");
        yield return EnsureOllamaReady();
        if (!isOllamaReady)
        {
            Debug.LogError("Ollama is not reachable. Start it manually or enable auto-start settings on this component.");
            SetStatus("LLM: failed");
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
        SetStatus("LLM: generating...");
        yield return ollamaRequest.SendWebRequest();

        if (ollamaRequest.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Ollama request failed: {ollamaRequest.error}. URL: {ollamaGenerateUrl}");
            SetStatus("LLM: request failed");
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
            SetStatus("LLM: parse failed");
            onComplete?.Invoke(string.Empty);
            yield break;
        }

        if (ollamaResponse == null || string.IsNullOrWhiteSpace(ollamaResponse.response))
        {
            Debug.LogError($"Ollama response did not contain a usable 'response' field. Raw: {ollamaJson}");
            SetStatus("LLM: empty response");
            onComplete?.Invoke(string.Empty);
            yield break;
        }

        LastAiResponse = ollamaResponse.response.Trim();
        onComplete?.Invoke(LastAiResponse);

        if (projectTts == null)
        {
            Debug.LogError("No TTS component assigned/found. Add the TTS component and link it in the Inspector.");
            SetStatus("LLM: no TTS");
            yield break;
        }

        SetStatus("LLM: speaking...");
        projectTts.SpeakText(LastAiResponse, "OllamaDmSession");
        SetStatus("LLM: ready");
    }

    private IEnumerator EnsureOllamaReady()
    {
        if (isOllamaReady)
        {
            SetStatus("LLM: ready");
            yield break;
        }

        if (isCheckingOllama)
        {
            while (isCheckingOllama)
            {
                yield return null;
            }

            yield break;
        }

        isCheckingOllama = true;

        bool wasReachable = false;
        yield return CheckOllamaReachable(result => wasReachable = result);
        if (wasReachable)
        {
            isOllamaReady = true;
            isCheckingOllama = false;
            SetStatus("LLM: ready");
            yield break;
        }

        if (autoStartOllama && !hasTriedAutoStart)
        {
            hasTriedAutoStart = true;
            SetStatus("LLM: starting local service...");
            TryStartOllamaProcess();

            float deadline = Time.realtimeSinceStartup + Mathf.Max(1, startupWaitSeconds);
            while (Time.realtimeSinceStartup < deadline)
            {
                yield return new WaitForSecondsRealtime(0.5f);

                bool started = false;
                yield return CheckOllamaReachable(result => started = result);
                if (started)
                {
                    isOllamaReady = true;
                        SetStatus("LLM: ready");
                    break;
                }
            }
        }

        if (!isOllamaReady)
        {
            SetStatus("LLM: unavailable");
        }

        isCheckingOllama = false;
    }

    private IEnumerator CheckOllamaReachable(Action<bool> onChecked)
    {
        string healthUrl = BuildHealthUrl();
        using UnityWebRequest healthRequest = UnityWebRequest.Get(healthUrl);
        healthRequest.timeout = 2;
        yield return healthRequest.SendWebRequest();

        bool reachable = healthRequest.result == UnityWebRequest.Result.Success;
        onChecked?.Invoke(reachable);
    }

    private string BuildHealthUrl()
    {
        if (Uri.TryCreate(ollamaGenerateUrl, UriKind.Absolute, out Uri generateUri))
        {
            string baseUrl = $"{generateUri.Scheme}://{generateUri.Host}:{generateUri.Port}";
            return baseUrl + "/api/tags";
        }

        return "http://localhost:11434/api/tags";
    }

    private void TryStartOllamaProcess()
    {
        try
        {
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = ollamaExecutable,
                Arguments = ollamaServeArgs,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
            };

            System.Diagnostics.Process.Start(startInfo);
            Debug.Log("Attempted to start Ollama process automatically.");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Could not auto-start Ollama. Error: {ex.Message}");
        }
    }

    private void SetStatus(string message)
    {
        if (ollamaStatusText != null)
        {
            ollamaStatusText.text = message;
        }
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
