// using System;
// using System.Collections;
// using System.Collections.Generic;
// using System.Text;
// using TMPro;
// using UnityEngine;
// using UnityEngine.Networking;

// /// <summary>
// /// Handles all LLM communication with Ollama.
// /// 
// /// Public coroutines:
// ///   GenerateScenario(roomIndex, onComplete)              — room intro, speaks via TTS
// ///   GenerateSolution(scenario, obj, conf, onComplete)    — object-based solution, speaks via TTS
// ///   GenerateAndSpeak(prompt, onComplete)                 — legacy / quick-test entry point
// /// </summary>
// public class OllamaDungeonMasterTtsClient : MonoBehaviour
// {
//     [Header("Speech Output")]
//     [SerializeField] private TTS projectTts;

//     [Header("Ollama Endpoint")]
//     [SerializeField] private string ollamaGenerateUrl = "http://localhost:11434/api/generate";

//     [Header("Request Settings")]
//     [SerializeField] private string ollamaModel           = "llama3.1:8b";
//     [SerializeField] private int    requestTimeoutSeconds = 60;
//     [SerializeField] private int    maxRetries            = 1;

//     [Header("Ollama Auto Start")]
//     [SerializeField] private bool   autoStartOllama    = true;
//     [SerializeField] private string ollamaExecutable   = "ollama";
//     [SerializeField] private string ollamaServeArgs    = "serve";
//     [SerializeField] private int    startupWaitSeconds = 12;
//     [SerializeField] private bool   allowOpenAiCompatibleFallback = true;

//     [Header("Model Auto-Pull")]
//     [Tooltip("Pull the model automatically if it is not present locally.")]
//     [SerializeField] private bool autoPullModel      = true;
//     [SerializeField] private int  pullTimeoutSeconds = 1800;

//     [Header("Prototype Status UI")]
//     [SerializeField] private TextMeshProUGUI ollamaStatusText;

//     // ── System prompts ────────────────────────────────────────────────────────

//     private const string ScenarioSystemPrompt =
//         "You are a Dungeon Master for a fantasy dungeon game. " +
//         "Generate a short, atmospheric room description (2-3 sentences). " +
//         "End with a clear obstacle or challenge the party must overcome. " +
//         "Be cinematic and easy to understand when spoken aloud.";

//     private const string SolutionSystemPrompt =
//         "You are a Dungeon Master for a fantasy dungeon game. " +
//         "The player has drawn an object to solve a room challenge. " +
//         "Describe in 2-3 sentences how the player cleverly uses that object to overcome the obstacle or utterly fail. " +
//         "Be creative, atmospheric, and satisfying. Easy to understand when spoken aloud.";

//     // ── Fallbacks ─────────────────────────────────────────────────────────────

//     private static readonly string[] FallbackScenarios =
//     {
//         "You enter a dimly lit chamber. Ancient runes pulse on the walls with an eerie red glow. A massive stone door blocks your path forward.",
//         "The corridor opens into a flooded hall. Strange creatures lurk beneath the murky water. You need to find a way across.",
//         "A thick fog fills the next room, making it impossible to see. You hear growling in the darkness. Something is waiting for you."
//     };

//     private static readonly string[] FallbackSolutions =
//     {
//         "With quick thinking, you put your item to use and the obstacle yields. The path forward is clear.",
//         "Your chosen tool proves surprisingly effective. The challenge crumbles before your ingenuity.",
//         "Against all odds, your solution works perfectly. The room is conquered."
//     };

//     // ── State ─────────────────────────────────────────────────────────────────

//     public string LastAiResponse { get; private set; }

//     private bool hasTriedAutoStart;
//     private bool isCheckingOllama;
//     private bool isOllamaReady;
//     private bool isModelReady;
//     private bool isPullingModel;

//     private int HttpTimeoutSeconds => 10;

//     // ── Lifecycle ─────────────────────────────────────────────────────────────

//     private void Awake()
//     {
//         if (projectTts == null)
//             projectTts = GetComponent<TTS>();
//         SetStatus("LLM: idle");
//     }

//     // =========================================================================
//     // Public API
//     // =========================================================================

//     /// <summary>
//     /// Generates an atmospheric room scenario for the given room index, speaks it,
//     /// and returns the text via callback. Falls back to a hardcoded scenario on failure.
//     /// </summary>
//     public IEnumerator GenerateScenario(int roomIndex, Action<string> onComplete)
//     {
//         SetStatus("LLM: generating scenario...");

//         string prompt = $"Generate a scenario for dungeon room {roomIndex + 1}. " +
//                         "Make it feel distinct from earlier rooms.";

//         string result = string.Empty;
//         yield return GenerateWithFallback(
//             prompt,
//             ScenarioSystemPrompt,
//             FallbackScenarios[roomIndex % FallbackScenarios.Length],
//             text => result = text);

//         LastAiResponse = result;
//         onComplete?.Invoke(result);
//         yield return SpeakText(result);
//         SetStatus("LLM: ready");
//     }

//     /// <summary>
//     /// Generates a solution narrative based on the current scenario, the player's drawn
//     /// object, and the recognition confidence. Speaks it and returns via callback.
//     /// Falls back to a hardcoded solution on failure.
//     /// </summary>
//     public IEnumerator GenerateSolution(
//         string scenario,
//         string predictedObject,
//         float  confidence,
//         Action<string> onComplete)
//     {
//         SetStatus("LLM: generating solution...");

//         string confidenceDesc = confidence >= 0.75f ? "confidently"
//                               : confidence >= 0.40f ? "hesitantly"
//                               : "desperately";

//         string prompt =
//             $"Room scenario: \"{scenario}\"\n\n" +
//             $"The player {confidenceDesc} draws a {predictedObject} " +
//             $"(recognition confidence: {confidence:P0}). " +
//             $"Describe how they use the {predictedObject} to overcome the obstacle or utterly fail.";

//         string result = string.Empty;
//         yield return GenerateWithFallback(
//             prompt,
//             SolutionSystemPrompt,
//             FallbackSolutions[UnityEngine.Random.Range(0, FallbackSolutions.Length)],
//             text => result = text);

//         LastAiResponse = result;
//         onComplete?.Invoke(result);
//         yield return SpeakText(result);
//         SetStatus("LLM: ready");
//     }

//     /// <summary>
//     /// Legacy / quick-test entry point. Kept for OllamaDmQuickTest compatibility.
//     /// </summary>
//     public IEnumerator GenerateAndSpeak(string userInput, Action<string> onComplete = null)
//     {
//         if (string.IsNullOrWhiteSpace(userInput))
//         {
//             SetStatus("LLM: idle");
//             onComplete?.Invoke(string.Empty);
//             yield break;
//         }

//         SetStatus("LLM: checking service...");
//         yield return EnsureOllamaReady();
//         if (!isOllamaReady)
//         {
//             SetStatus("LLM: service unavailable");
//             onComplete?.Invoke(string.Empty);
//             yield break;
//         }

//         SetStatus("LLM: checking model...");
//         yield return EnsureModelReady();
//         if (!isModelReady)
//         {
//             SetStatus("LLM: model unavailable");
//             onComplete?.Invoke(string.Empty);
//             yield break;
//         }

//         SetStatus("LLM: generating...");
//         string generatedText = string.Empty;
//         yield return RequestTextFromLocalLlm(userInput, null, text => generatedText = text);

//         if (string.IsNullOrWhiteSpace(generatedText))
//         {
//             SetStatus("LLM: request failed");
//             onComplete?.Invoke(string.Empty);
//             yield break;
//         }

//         LastAiResponse = generatedText.Trim();
//         onComplete?.Invoke(LastAiResponse);
//         yield return SpeakText(LastAiResponse);
//         SetStatus("LLM: ready");
//     }

//     // =========================================================================
//     // Internal generation helpers
//     // =========================================================================

//     private IEnumerator GenerateWithFallback(
//         string prompt,
//         string systemPrompt,
//         string fallbackText,
//         Action<string> onComplete)
//     {
//         yield return EnsureOllamaReady();
//         if (!isOllamaReady)
//         {
//             Debug.LogWarning("[OllamaDM] Service unavailable — using fallback.");
//             onComplete?.Invoke(fallbackText);
//             yield break;
//         }

//         yield return EnsureModelReady();
//         if (!isModelReady)
//         {
//             Debug.LogWarning("[OllamaDM] Model unavailable — using fallback.");
//             onComplete?.Invoke(fallbackText);
//             yield break;
//         }

//         string result  = string.Empty;
//         int    attempt = 0;

//         while (attempt <= maxRetries)
//         {
//             yield return RequestTextFromLocalLlm(prompt, systemPrompt, text => result = text);

//             if (!string.IsNullOrWhiteSpace(result))
//             {
//                 onComplete?.Invoke(result.Trim());
//                 yield break;
//             }

//             attempt++;
//             if (attempt <= maxRetries)
//             {
//                 Debug.LogWarning($"[OllamaDM] Attempt {attempt} failed, retrying...");
//                 yield return new WaitForSeconds(1f);
//             }
//         }

//         Debug.LogWarning("[OllamaDM] All attempts failed — using fallback.");
//         onComplete?.Invoke(fallbackText);
//     }

//     private IEnumerator SpeakText(string text)
//     {
//         if (projectTts == null || string.IsNullOrWhiteSpace(text))
//             yield break;
//         SetStatus("LLM: speaking...");
//         projectTts.SpeakText(text, "OllamaDmSession");
//     }

//     // =========================================================================
//     // Service & model readiness
//     // =========================================================================

//     private IEnumerator EnsureOllamaReady()
//     {
//         if (isOllamaReady) yield break;

//         if (isCheckingOllama)
//         {
//             while (isCheckingOllama) yield return null;
//             yield break;
//         }

//         isCheckingOllama = true;

//         bool reachable = false;
//         yield return CheckOllamaReachable(r => reachable = r);

//         if (reachable)
//         {
//             isOllamaReady    = true;
//             isCheckingOllama = false;
//             yield break;
//         }

//         if (autoStartOllama && !hasTriedAutoStart)
//         {
//             hasTriedAutoStart = true;
//             SetStatus("LLM: starting service...");
//             TryStartOllamaProcess();

//             float deadline = Time.realtimeSinceStartup + Mathf.Max(1, startupWaitSeconds);
//             while (Time.realtimeSinceStartup < deadline)
//             {
//                 yield return new WaitForSecondsRealtime(0.5f);
//                 bool started = false;
//                 yield return CheckOllamaReachable(r => started = r);
//                 if (started) { isOllamaReady = true; break; }
//             }
//         }

//         if (!isOllamaReady)
//             SetStatus("LLM: unavailable");

//         isCheckingOllama = false;
//     }

//     private IEnumerator CheckOllamaReachable(Action<bool> onChecked)
//     {
//         using (var req = UnityWebRequest.Get(BuildBaseUrl() + "/api/tags"))
//         {
//             req.timeout = 3;
//             yield return req.SendWebRequest();
//             if (req.result == UnityWebRequest.Result.Success) { onChecked?.Invoke(true); yield break; }
//         }
//         using (var req = UnityWebRequest.Get(BuildBaseUrl() + "/v1/models"))
//         {
//             req.timeout = 3;
//             yield return req.SendWebRequest();
//             onChecked?.Invoke(req.result == UnityWebRequest.Result.Success);
//         }
//     }

//     private IEnumerator EnsureModelReady()
//     {
//         if (isModelReady) yield break;

//         bool present = false;
//         yield return CheckModelPresent(r => present = r);
//         if (present) { isModelReady = true; yield break; }

//         if (!autoPullModel)
//         {
//             Debug.LogWarning($"[OllamaDM] Model '{ollamaModel}' not present and Auto Pull is disabled.");
//             yield break;
//         }

//         if (isPullingModel)
//         {
//             while (isPullingModel) yield return null;
//             yield break;
//         }

//         isPullingModel = true;
//         SetStatus($"LLM: pulling {ollamaModel}...");
//         Debug.Log($"[OllamaDM] Pulling model '{ollamaModel}'. This may take several minutes on first run.");

//         yield return PullModel();
//         yield return CheckModelPresent(r => isModelReady = r);

//         if (!isModelReady)
//             Debug.LogError($"[OllamaDM] Pull finished but model '{ollamaModel}' still not found.");

//         isPullingModel = false;
//     }

//     private IEnumerator CheckModelPresent(Action<bool> onChecked)
//     {
//         using var req = UnityWebRequest.Get(BuildBaseUrl() + "/api/tags");
//         req.timeout = HttpTimeoutSeconds;
//         yield return req.SendWebRequest();

//         if (req.result != UnityWebRequest.Result.Success) { onChecked?.Invoke(false); yield break; }

//         bool found = req.downloadHandler.text.IndexOf(
//             ollamaModel, StringComparison.OrdinalIgnoreCase) >= 0;
//         onChecked?.Invoke(found);
//     }

//     private IEnumerator PullModel()
//     {
//         string url   = BuildBaseUrl() + "/api/pull";
//         string body  = $"{{\"name\":\"{ollamaModel}\",\"stream\":false}}";
//         byte[] bytes = Encoding.UTF8.GetBytes(body);

//         using var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
//         req.uploadHandler   = new UploadHandlerRaw(bytes);
//         req.downloadHandler = new DownloadHandlerBuffer();
//         req.SetRequestHeader("Content-Type", "application/json");
//         req.timeout = pullTimeoutSeconds;

//         yield return req.SendWebRequest();

//         if (req.result == UnityWebRequest.Result.Success)
//             Debug.Log($"[OllamaDM] Pull response: {req.downloadHandler.text}");
//         else
//             Debug.LogError($"[OllamaDM] Pull failed (HTTP {req.responseCode}): {req.error}");
//     }

//     // =========================================================================
//     // HTTP request
//     // =========================================================================

//     private IEnumerator RequestTextFromLocalLlm(
//         string prompt,
//         string systemPromptOverride,
//         Action<string> onComplete)
//     {
//         List<string> candidateUrls = BuildCandidateGenerateUrls();

//         for (int i = 0; i < candidateUrls.Count; i++)
//         {
//             string url            = candidateUrls[i];
//             bool   useOpenAiStyle = url.IndexOf("/v1/chat/completions",
//                                         StringComparison.OrdinalIgnoreCase) >= 0;

//             string systemPrompt = string.IsNullOrEmpty(systemPromptOverride)
//                 ? ScenarioSystemPrompt
//                 : systemPromptOverride;

//             string json  = useOpenAiStyle
//                 ? BuildOpenAiChatRequestJson(prompt, systemPrompt)
//                 : BuildOllamaGenerateRequestJson(prompt, systemPrompt);

//             byte[] bytes = Encoding.UTF8.GetBytes(json);

//             using var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
//             req.uploadHandler   = new UploadHandlerRaw(bytes);
//             req.downloadHandler = new DownloadHandlerBuffer();
//             req.SetRequestHeader("Content-Type", "application/json");
//             req.timeout = requestTimeoutSeconds;

//             Debug.Log($"[OllamaDM] POST → {url}");
//             yield return req.SendWebRequest();

//             if (req.result == UnityWebRequest.Result.Success)
//             {
//                 if (TryExtractGeneratedText(req.downloadHandler.text, useOpenAiStyle, out string text))
//                 {
//                     onComplete?.Invoke(text);
//                     yield break;
//                 }
//                 Debug.LogError($"[OllamaDM] Parse failed for {url}. Raw: {req.downloadHandler.text}");
//                 continue;
//             }

//             long code = req.responseCode;
//             Debug.LogWarning($"[OllamaDM] {url} → HTTP {code}: {req.error}");
//             if (code != 404 && code != 405) break;
//         }

//         onComplete?.Invoke(string.Empty);
//     }

//     // =========================================================================
//     // JSON builders
//     // =========================================================================

//     private string BuildOllamaGenerateRequestJson(string prompt, string systemPrompt)
//     {
//         return JsonUtility.ToJson(new OllamaGenerateRequest
//         {
//             model  = ollamaModel,
//             system = systemPrompt,
//             prompt = prompt,
//             stream = false
//         });
//     }

//     private string BuildOpenAiChatRequestJson(string prompt, string systemPrompt)
//     {
//         return JsonUtility.ToJson(new OpenAiChatRequest
//         {
//             model    = ollamaModel,
//             messages = new[]
//             {
//                 new OpenAiChatMessage { role = "system", content = systemPrompt },
//                 new OpenAiChatMessage { role = "user",   content = prompt }
//             },
//             stream = false
//         });
//     }

//     private bool TryExtractGeneratedText(string rawJson, bool isOpenAiStyle, out string text)
//     {
//         text = string.Empty;
//         try
//         {
//             if (!isOpenAiStyle)
//             {
//                 var r = JsonUtility.FromJson<OllamaGenerateResponse>(rawJson);
//                 if (r != null && !string.IsNullOrWhiteSpace(r.response))
//                 { text = r.response; return true; }
//                 return false;
//             }

//             var oai = JsonUtility.FromJson<OpenAiChatResponse>(rawJson);
//             if (oai?.choices == null || oai.choices.Length == 0) return false;

//             var choice = oai.choices[0];
//             if (choice?.message != null && !string.IsNullOrWhiteSpace(choice.message.content))
//             { text = choice.message.content; return true; }
//             if (!string.IsNullOrWhiteSpace(choice?.text))
//             { text = choice.text; return true; }
//             return false;
//         }
//         catch (Exception ex)
//         {
//             Debug.LogError($"[OllamaDM] JSON parse error: {ex.Message}");
//             return false;
//         }
//     }

//     // =========================================================================
//     // URL helpers
//     // =========================================================================

//     private string BuildBaseUrl()
//     {
//         if (Uri.TryCreate(ollamaGenerateUrl, UriKind.Absolute, out Uri uri))
//             return $"{uri.Scheme}://{uri.Host}:{uri.Port}";
//         return "http://localhost:11434";
//     }

//     private List<string> BuildCandidateGenerateUrls()
//     {
//         var urls = new List<string>();
//         AddUrlIfMissing(urls, ollamaGenerateUrl);
//         AddUrlIfMissing(urls, BuildBaseUrl() + "/api/generate");
//         if (allowOpenAiCompatibleFallback)
//             AddUrlIfMissing(urls, BuildBaseUrl() + "/v1/chat/completions");
//         return urls;
//     }

//     private static void AddUrlIfMissing(List<string> list, string candidate)
//     {
//         if (string.IsNullOrWhiteSpace(candidate)) return;
//         string n = candidate.Trim();
//         if (!list.Contains(n)) list.Add(n);
//     }

//     // =========================================================================
//     // Process helpers
//     // =========================================================================

//     private void TryStartOllamaProcess()
//     {
//         try
//         {
//             var psi = new System.Diagnostics.ProcessStartInfo
//             {
//                 FileName        = ollamaExecutable,
//                 Arguments       = ollamaServeArgs,
//                 UseShellExecute = false,
//                 CreateNoWindow  = true,
//                 WindowStyle     = System.Diagnostics.ProcessWindowStyle.Hidden
//             };
//             System.Diagnostics.Process.Start(psi);
//             Debug.Log("[OllamaDM] Auto-started Ollama process.");
//         }
//         catch (Exception ex)
//         {
//             Debug.LogWarning($"[OllamaDM] Could not auto-start Ollama: {ex.Message}");
//         }
//     }

//     private void SetStatus(string message)
//     {
//         if (ollamaStatusText != null)
//             ollamaStatusText.text = message;
//     }

//     // =========================================================================
//     // Serializable types
//     // =========================================================================

//     [Serializable] private class OllamaGenerateRequest
//     {
//         public string model; public string system; public string prompt; public bool stream;
//     }
//     [Serializable] private class OllamaGenerateResponse  { public string response; }
//     [Serializable] private class OpenAiChatRequest
//     {
//         public string model; public OpenAiChatMessage[] messages; public bool stream;
//     }
//     [Serializable] private class OpenAiChatMessage       { public string role; public string content; }
//     [Serializable] private class OpenAiChatResponse      { public OpenAiChatChoice[] choices; }
//     [Serializable] private class OpenAiChatChoice
//     {
//         public OpenAiChatMessage message; public string text;
//     }
// }
