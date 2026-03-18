using System;
using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] private bool allowOpenAiCompatibleFallback = true;

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
        SetStatus("LLM: generating...");
        string generatedText = string.Empty;
        yield return RequestTextFromLocalLlm(userInput, text => generatedText = text);
        if (string.IsNullOrWhiteSpace(generatedText))
        {
            SetStatus("LLM: request failed");
            onComplete?.Invoke(string.Empty);
            yield break;
        }

        LastAiResponse = generatedText.Trim();
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
        bool reachable = false;

        string healthUrl = BuildHealthUrl();
        using (UnityWebRequest tagsRequest = UnityWebRequest.Get(healthUrl))
        {
            tagsRequest.timeout = 2;
            yield return tagsRequest.SendWebRequest();
            reachable = tagsRequest.result == UnityWebRequest.Result.Success;
        }

        if (!reachable)
        {
            string modelsUrl = BuildOpenAiModelsUrl();
            using UnityWebRequest modelsRequest = UnityWebRequest.Get(modelsUrl);
            modelsRequest.timeout = 2;
            yield return modelsRequest.SendWebRequest();
            reachable = modelsRequest.result == UnityWebRequest.Result.Success;
        }

        onChecked?.Invoke(reachable);
    }

    private IEnumerator RequestTextFromLocalLlm(string userInput, Action<string> onComplete)
    {
        List<string> candidateUrls = BuildCandidateGenerateUrls();
        for (int i = 0; i < candidateUrls.Count; i++)
        {
            string candidateUrl = candidateUrls[i];
            bool useOpenAiPayload = candidateUrl.Contains("/v1/chat/completions", StringComparison.OrdinalIgnoreCase);

            string requestJson = useOpenAiPayload
                ? BuildOpenAiChatRequestJson(userInput)
                : BuildOllamaGenerateRequestJson(userInput);

            byte[] requestBytes = Encoding.UTF8.GetBytes(requestJson);

            using UnityWebRequest request = new UnityWebRequest(candidateUrl, UnityWebRequest.kHttpVerbPOST);
            request.uploadHandler = new UploadHandlerRaw(requestBytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = requestTimeoutSeconds;

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseText = request.downloadHandler.text;
                if (TryExtractGeneratedText(responseText, useOpenAiPayload, out string extractedText))
                {
                    onComplete?.Invoke(extractedText);
                    yield break;
                }

                Debug.LogError($"LLM response parse failed for endpoint {candidateUrl}. Raw: {responseText}");
                continue;
            }

            long statusCode = request.responseCode;
            Debug.LogWarning($"LLM request failed on {candidateUrl} with status {statusCode}: {request.error}");

            if (statusCode != 404 && statusCode != 405)
            {
                break;
            }
        }

        Debug.LogError(
            $"All local LLM endpoint attempts failed. Tried base URL from: {ollamaGenerateUrl}. " +
            "If you test in browser, note that /api/generate is POST-only so GET can show 405.");
        onComplete?.Invoke(string.Empty);
    }

    private string BuildOllamaGenerateRequestJson(string userInput)
    {
        OllamaGenerateRequest requestBody = new OllamaGenerateRequest
        {
            model = ollamaModel,
            system = DungeonMasterSystemPrompt,
            prompt = userInput,
            stream = false
        };

        return JsonUtility.ToJson(requestBody);
    }

    private string BuildOpenAiChatRequestJson(string userInput)
    {
        OpenAiChatRequest requestBody = new OpenAiChatRequest
        {
            model = ollamaModel,
            messages = new[]
            {
                new OpenAiChatMessage { role = "system", content = DungeonMasterSystemPrompt },
                new OpenAiChatMessage { role = "user", content = userInput }
            },
            stream = false
        };

        return JsonUtility.ToJson(requestBody);
    }

    private bool TryExtractGeneratedText(string rawJson, bool isOpenAiStyle, out string text)
    {
        text = string.Empty;

        try
        {
            if (!isOpenAiStyle)
            {
                OllamaGenerateResponse ollamaResponse = JsonUtility.FromJson<OllamaGenerateResponse>(rawJson);
                if (ollamaResponse != null && !string.IsNullOrWhiteSpace(ollamaResponse.response))
                {
                    text = ollamaResponse.response;
                    return true;
                }

                return false;
            }

            OpenAiChatResponse openAiResponse = JsonUtility.FromJson<OpenAiChatResponse>(rawJson);
            if (openAiResponse?.choices == null || openAiResponse.choices.Length == 0)
            {
                return false;
            }

            OpenAiChatChoice firstChoice = openAiResponse.choices[0];
            if (firstChoice?.message != null && !string.IsNullOrWhiteSpace(firstChoice.message.content))
            {
                text = firstChoice.message.content;
                return true;
            }

            if (!string.IsNullOrWhiteSpace(firstChoice?.text))
            {
                text = firstChoice.text;
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to parse LLM response JSON: {ex.Message}");
            return false;
        }
    }

    private string BuildHealthUrl()
    {
        return BuildBaseUrl() + "/api/tags";
    }

    private string BuildOpenAiModelsUrl()
    {
        return BuildBaseUrl() + "/v1/models";
    }

    private List<string> BuildCandidateGenerateUrls()
    {
        List<string> urls = new List<string>();
        AddUrlIfMissing(urls, ollamaGenerateUrl);
        AddUrlIfMissing(urls, BuildBaseUrl() + "/api/generate");

        if (allowOpenAiCompatibleFallback)
        {
            AddUrlIfMissing(urls, BuildBaseUrl() + "/v1/chat/completions");
        }

        return urls;
    }

    private void AddUrlIfMissing(List<string> urls, string candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return;
        }

        string normalized = candidate.Trim();
        if (!urls.Contains(normalized))
        {
            urls.Add(normalized);
        }
    }

    private string BuildBaseUrl()
    {
        if (Uri.TryCreate(ollamaGenerateUrl, UriKind.Absolute, out Uri generateUri))
        {
            return $"{generateUri.Scheme}://{generateUri.Host}:{generateUri.Port}";
        }

        return "http://localhost:11434";
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

    [Serializable]
    private class OpenAiChatRequest
    {
        public string model;
        public OpenAiChatMessage[] messages;
        public bool stream;
    }

    [Serializable]
    private class OpenAiChatMessage
    {
        public string role;
        public string content;
    }

    [Serializable]
    private class OpenAiChatResponse
    {
        public OpenAiChatChoice[] choices;
    }

    [Serializable]
    private class OpenAiChatChoice
    {
        public OpenAiChatMessage message;
        public string text;
    }

}
