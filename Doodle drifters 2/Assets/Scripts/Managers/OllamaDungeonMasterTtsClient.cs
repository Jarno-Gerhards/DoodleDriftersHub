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
    [SerializeField] private int requestTimeoutSeconds = 60;

    [Header("Ollama Auto Start")]
    [SerializeField] private bool autoStartOllama = true;
    [SerializeField] private string ollamaExecutable = "ollama";
    [SerializeField] private string ollamaServeArgs = "serve";
    [SerializeField] private int startupWaitSeconds = 12;
    [SerializeField] private bool allowOpenAiCompatibleFallback = true;

    [Header("Model Auto-Pull")]
    [Tooltip("If the model is not present locally, pull it automatically at runtime.")]
    [SerializeField] private bool autoPullModel = true;
    [Tooltip("Timeout in seconds for the model pull request. Large models take a while.")]
    [SerializeField] private int pullTimeoutSeconds = 1800; // 30 minutes

    [Header("Prototype Status UI")]
    [SerializeField] private TextMeshProUGUI ollamaStatusText;

    private const string DungeonMasterSystemPrompt =
        "You are a Dungeon Master for a fantasy game. " +
        "Be creative, atmospheric, and cinematic. " +
        "Keep responses short (1-3 sentences) and easy to voice.";

    /// <summary>Stores the most recent AI response text for easy debugging/inspection.</summary>
    public string LastAiResponse { get; private set; }

    private bool _hasTriedAutoStart;
    private bool _isCheckingOllama;
    private bool _isOllamaReady;
    private bool _isModelReady;
    private bool _isPullingModel;

    // ── Lifecycle ────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (projectTts == null)
            projectTts = GetComponent<TTS>();

        SetStatus("LLM: idle");
    }

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Main entry point: ensures Ollama is running and the model is available,
    /// generates text, then speaks it through the TTS component.
    /// Returns the generated text via callback.
    /// </summary>
    public IEnumerator GenerateAndSpeak(string userInput, Action<string> onComplete = null)
    {
        if (string.IsNullOrWhiteSpace(userInput))
        {
            Debug.LogWarning("[OllamaDM] GenerateAndSpeak called with empty userInput.");
            SetStatus("LLM: idle");
            onComplete?.Invoke(string.Empty);
            yield break;
        }

        // 1. Make sure the Ollama service is up
        SetStatus("LLM: checking service...");
        yield return EnsureOllamaReady();
        if (!_isOllamaReady)
        {
            Debug.LogError("[OllamaDM] Ollama is not reachable. " +
                           "Start it manually or enable Auto Start on this component.");
            SetStatus("LLM: service unavailable");
            onComplete?.Invoke(string.Empty);
            yield break;
        }

        // 2. Make sure the model is pulled
        SetStatus("LLM: checking model...");
        yield return EnsureModelReady();
        if (!_isModelReady)
        {
            Debug.LogError($"[OllamaDM] Model '{ollamaModel}' is not available. " +
                           "Enable Auto Pull Model or run 'ollama pull <model>' manually.");
            SetStatus("LLM: model unavailable");
            onComplete?.Invoke(string.Empty);
            yield break;
        }

        // 3. Generate text
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

        // 4. Speak
        if (projectTts == null)
        {
            Debug.LogError("[OllamaDM] No TTS component assigned/found.");
            SetStatus("LLM: no TTS");
            yield break;
        }

        SetStatus("LLM: speaking...");
        projectTts.SpeakText(LastAiResponse, "OllamaDmSession");
        SetStatus("LLM: ready");
    }

    // ── Service readiness ────────────────────────────────────────────────────

    private IEnumerator EnsureOllamaReady()
    {
        if (_isOllamaReady)
        {
            yield break;
        }

        // If another coroutine is already checking, wait for it
        if (_isCheckingOllama)
        {
            while (_isCheckingOllama)
                yield return null;
            yield break;
        }

        _isCheckingOllama = true;

        bool reachable = false;
        yield return CheckOllamaReachable(result => reachable = result);

        if (reachable)
        {
            _isOllamaReady    = true;
            _isCheckingOllama = false;
            SetStatus("LLM: ready");
            yield break;
        }

        if (autoStartOllama && !_hasTriedAutoStart)
        {
            _hasTriedAutoStart = true;
            SetStatus("LLM: starting service...");
            TryStartOllamaProcess();

            float deadline = Time.realtimeSinceStartup + Mathf.Max(1, startupWaitSeconds);
            while (Time.realtimeSinceStartup < deadline)
            {
                yield return new WaitForSecondsRealtime(0.5f);

                bool started = false;
                yield return CheckOllamaReachable(result => started = result);
                if (started)
                {
                    _isOllamaReady = true;
                    SetStatus("LLM: ready");
                    break;
                }
            }
        }

        if (!_isOllamaReady)
            SetStatus("LLM: unavailable");

        _isCheckingOllama = false;
    }

    private IEnumerator CheckOllamaReachable(Action<bool> onChecked)
    {
        // Primary: Ollama native health endpoint
        using (var req = UnityWebRequest.Get(BuildBaseUrl() + "/api/tags"))
        {
            req.timeout = 3;
            yield return req.SendWebRequest();
            if (req.result == UnityWebRequest.Result.Success)
            {
                onChecked?.Invoke(true);
                yield break;
            }
        }

        // Fallback: OpenAI-compatible models endpoint
        using (var req = UnityWebRequest.Get(BuildBaseUrl() + "/v1/models"))
        {
            req.timeout = 3;
            yield return req.SendWebRequest();
            onChecked?.Invoke(req.result == UnityWebRequest.Result.Success);
        }
    }

    // ── Model readiness ──────────────────────────────────────────────────────

    private IEnumerator EnsureModelReady()
    {
        if (_isModelReady)
            yield break;

        // Check if model is already listed in /api/tags
        bool present = false;
        yield return CheckModelPresent(result => present = result);

        if (present)
        {
            _isModelReady = true;
            yield break;
        }

        // Optionally auto-pull
        if (!autoPullModel)
        {
            Debug.LogWarning($"[OllamaDM] Model '{ollamaModel}' is not present and Auto Pull is disabled.");
            yield break;
        }

        // Prevent concurrent pulls
        if (_isPullingModel)
        {
            while (_isPullingModel)
                yield return null;
            yield break;
        }

        _isPullingModel = true;
        SetStatus($"LLM: pulling {ollamaModel}...");
        Debug.Log($"[OllamaDM] Pulling model '{ollamaModel}'. This may take several minutes on first run.");

        yield return PullModel();

        // Verify after pull
        yield return CheckModelPresent(result => _isModelReady = result);

        if (!_isModelReady)
            Debug.LogError($"[OllamaDM] Model pull completed but '{ollamaModel}' still not found in /api/tags.");

        _isPullingModel = false;
    }

    private IEnumerator CheckModelPresent(Action<bool> onChecked)
    {
        using var req = UnityWebRequest.Get(BuildBaseUrl() + "/api/tags");
        req.timeout = HttpTimeoutSeconds;
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            onChecked?.Invoke(false);
            yield break;
        }

        // Simple string search — avoids a full JSON parse
        bool found = req.downloadHandler.text.IndexOf(
            ollamaModel, StringComparison.OrdinalIgnoreCase) >= 0;
        onChecked?.Invoke(found);
    }

    /// <summary>
    /// Pulls a model by POSTing to /api/pull with stream:false.
    /// Ollama streams NDJSON by default; with stream:false it returns one final JSON object.
    /// The request timeout is set to pullTimeoutSeconds to handle large models.
    /// </summary>
    private IEnumerator PullModel()
    {
        string url  = BuildBaseUrl() + "/api/pull";
        string body = $"{{\"name\":\"{ollamaModel}\",\"stream\":false}}";
        byte[] bodyBytes = Encoding.UTF8.GetBytes(body);

        using var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
        req.uploadHandler   = new UploadHandlerRaw(bodyBytes);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.timeout = pullTimeoutSeconds;

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            Debug.Log($"[OllamaDM] Pull response: {req.downloadHandler.text}");
        }
        else
        {
            Debug.LogError($"[OllamaDM] Pull request failed (status {req.responseCode}): {req.error}");
        }
    }

    // ── Text generation ──────────────────────────────────────────────────────

    private IEnumerator RequestTextFromLocalLlm(string userInput, Action<string> onComplete)
    {
        List<string> candidateUrls = BuildCandidateGenerateUrls();

        for (int i = 0; i < candidateUrls.Count; i++)
        {
            string url            = candidateUrls[i];
            bool   useOpenAiStyle = url.IndexOf("/v1/chat/completions",
                                        StringComparison.OrdinalIgnoreCase) >= 0;

            string json  = useOpenAiStyle
                ? BuildOpenAiChatRequestJson(userInput)
                : BuildOllamaGenerateRequestJson(userInput);

            byte[] bytes = Encoding.UTF8.GetBytes(json);

            using var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
            req.uploadHandler   = new UploadHandlerRaw(bytes);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.timeout = requestTimeoutSeconds;

            Debug.Log($"[OllamaDM] Sending request to {url}");
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                string raw = req.downloadHandler.text;
                if (TryExtractGeneratedText(raw, useOpenAiStyle, out string text))
                {
                    onComplete?.Invoke(text);
                    yield break;
                }

                Debug.LogError($"[OllamaDM] Response parse failed for {url}.\nRaw: {raw}");
                continue; // try next candidate
            }

            long code = req.responseCode;
            Debug.LogWarning($"[OllamaDM] Request to {url} failed (HTTP {code}): {req.error}");

            // Only continue to next URL on "not found" / "method not allowed"
            if (code != 404 && code != 405)
                break;
        }

        Debug.LogError(
            "[OllamaDM] All endpoint attempts failed.\n" +
            $"Base URL: {ollamaGenerateUrl}\n" +
            "Tip: /api/generate only accepts POST — a 405 in browser is normal.");
        onComplete?.Invoke(string.Empty);
    }

    // ── JSON builders ────────────────────────────────────────────────────────

    private string BuildOllamaGenerateRequestJson(string userInput)
    {
        var body = new OllamaGenerateRequest
        {
            model  = ollamaModel,
            system = DungeonMasterSystemPrompt,
            prompt = userInput,
            stream = false
        };
        return JsonUtility.ToJson(body);
    }

    private string BuildOpenAiChatRequestJson(string userInput)
    {
        var body = new OpenAiChatRequest
        {
            model    = ollamaModel,
            messages = new[]
            {
                new OpenAiChatMessage { role = "system", content = DungeonMasterSystemPrompt },
                new OpenAiChatMessage { role = "user",   content = userInput }
            },
            stream = false
        };
        return JsonUtility.ToJson(body);
    }

    private bool TryExtractGeneratedText(string rawJson, bool isOpenAiStyle, out string text)
    {
        text = string.Empty;
        try
        {
            if (!isOpenAiStyle)
            {
                var r = JsonUtility.FromJson<OllamaGenerateResponse>(rawJson);
                if (r != null && !string.IsNullOrWhiteSpace(r.response))
                {
                    text = r.response;
                    return true;
                }
                return false;
            }

            var oai = JsonUtility.FromJson<OpenAiChatResponse>(rawJson);
            if (oai?.choices == null || oai.choices.Length == 0) return false;

            var choice = oai.choices[0];
            if (choice?.message != null && !string.IsNullOrWhiteSpace(choice.message.content))
            {
                text = choice.message.content;
                return true;
            }
            if (!string.IsNullOrWhiteSpace(choice?.text))
            {
                text = choice.text;
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[OllamaDM] JSON parse error: {ex.Message}");
            return false;
        }
    }

    // ── URL helpers ──────────────────────────────────────────────────────────

    private string BuildBaseUrl()
    {
        if (Uri.TryCreate(ollamaGenerateUrl, UriKind.Absolute, out Uri uri))
            return $"{uri.Scheme}://{uri.Host}:{uri.Port}";
        return "http://localhost:11434";
    }

    private List<string> BuildCandidateGenerateUrls()
    {
        var urls = new List<string>();
        AddUrlIfMissing(urls, ollamaGenerateUrl);
        AddUrlIfMissing(urls, BuildBaseUrl() + "/api/generate");
        if (allowOpenAiCompatibleFallback)
            AddUrlIfMissing(urls, BuildBaseUrl() + "/v1/chat/completions");
        return urls;
    }

    private static void AddUrlIfMissing(List<string> list, string candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate)) return;
        string n = candidate.Trim();
        if (!list.Contains(n)) list.Add(n);
    }

    // ── Process helpers ──────────────────────────────────────────────────────

    private void TryStartOllamaProcess()
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName        = ollamaExecutable,
                Arguments       = ollamaServeArgs,
                UseShellExecute = false,
                CreateNoWindow  = true,
                WindowStyle     = System.Diagnostics.ProcessWindowStyle.Hidden
            };
            System.Diagnostics.Process.Start(psi);
            Debug.Log("[OllamaDM] Auto-started Ollama process.");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[OllamaDM] Could not auto-start Ollama: {ex.Message}");
        }
    }

    // ── UI helper ────────────────────────────────────────────────────────────

    private void SetStatus(string message)
    {
        if (ollamaStatusText != null)
            ollamaStatusText.text = message;
    }

    // Avoid hard-coding the constant so the compiler doesn't complain about unused private fields
    private int HttpTimeoutSeconds => 10;

    // ── Serializable types ───────────────────────────────────────────────────

    [Serializable] private class OllamaGenerateRequest
    {
        public string model;
        public string system;
        public string prompt;
        public bool   stream;
    }

    [Serializable] private class OllamaGenerateResponse
    {
        public string response;
    }

    [Serializable] private class OpenAiChatRequest
    {
        public string             model;
        public OpenAiChatMessage[] messages;
        public bool               stream;
    }

    [Serializable] private class OpenAiChatMessage
    {
        public string role;
        public string content;
    }

    [Serializable] private class OpenAiChatResponse
    {
        public OpenAiChatChoice[] choices;
    }

    [Serializable] private class OpenAiChatChoice
    {
        public OpenAiChatMessage message;
        public string            text;
    }
}