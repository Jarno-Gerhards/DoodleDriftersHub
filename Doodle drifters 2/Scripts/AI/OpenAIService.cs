using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// OpenAI API integration for image analysis and narrative generation.
/// Equivalent to OpenAiApi.js.
/// Uses UnityWebRequest for HTTP calls.
/// </summary>
public class OpenAIService : MonoBehaviour
{
    private static OpenAIService _instance;
    public static OpenAIService Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("OpenAIService");
                _instance = go.AddComponent<OpenAIService>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    private string ApiKey => GameConfig.Instance.OpenAIApiKey;
    private string Model => GameConfig.Instance.OpenAIModel;
    private bool HasApiKey => !string.IsNullOrEmpty(ApiKey);

    private const string API_URL = "https://api.openai.com/v1/chat/completions";

    // ============================================================
    // Image Analysis
    // ============================================================

    /// <summary>
    /// Analyze a drawing and return a short description of what was drawn.
    /// </summary>
    public void AnalyzeDrawing(string imageBase64, Action<string> onComplete)
    {
        if (!HasApiKey)
        {
            onComplete?.Invoke("A generic racing gadget");
            return;
        }

        StartCoroutine(AnalyzeDrawingCoroutine(imageBase64, onComplete));
    }

    private IEnumerator AnalyzeDrawingCoroutine(string imageBase64, Action<string> onComplete)
    {
        string imageUrl = imageBase64.StartsWith("data:") ? imageBase64 : $"data:image/png;base64,{imageBase64}";

        string requestBody = JsonUtility.ToJson(new OpenAIVisionRequest
        {
            model = Model,
            max_tokens = 200,
            messages = new OpenAIVisionMessage[]
            {
                new OpenAIVisionMessage
                {
                    role = "user",
                    content = new OpenAIVisionContent[]
                    {
                        new OpenAIVisionContent
                        {
                            type = "text",
                            text = @"You analyze rough, low-quality doodles and describe what objects appear in them.
Follow these rules:
- Interpret the doodle flexibly; assume it was drawn quickly or roughly.
- Abstract, imaginative, and exaggerated objects are allowed.
- Respond with a very short description (max 20 characters including spaces).
- Use no periods, punctuation, or filler words.
- Do not say ""drawing of"", ""sketch of"", ""doodle of"", etc.
- State the object(s) directly.
- Try to describe the entire scene, not only one isolated part.
- Avoid vague terms like ""shapes"" or ""lines""; be specific.
- Be as descriptive as possible within the character limit.
- If nothing is recognizable, respond with ""weird mess"" or ""unknown object"".
- If more than one interpretation is possible, choose the single most likely one.
- Do not list alternatives. Never use ""or""."
                        },
                        new OpenAIVisionContent
                        {
                            type = "image_url",
                            image_url = new OpenAIImageUrl { url = imageUrl, detail = "low" }
                        }
                    }
                }
            }
        });

        // JsonUtility can't handle nested arrays of polymorphic types well,
        // so we build JSON manually
        string json = BuildVisionRequestJson(imageUrl);

        using (var request = new UnityWebRequest(API_URL, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {ApiKey}");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[OpenAI] Vision API error: {request.error}");
                onComplete?.Invoke("Unknown object");
                yield break;
            }

            string response = request.downloadHandler.text;
            string content = ExtractContentFromResponse(response);

            if (content.Length > 30)
            {
                Debug.LogWarning($"[OpenAI] Vision response too long: {content}");
                onComplete?.Invoke("Weird mess");
            }
            else
            {
                onComplete?.Invoke(content);
            }
        }
    }

    // ============================================================
    // Narrative Generation
    // ============================================================

    /// <summary>
    /// Generate the next part of the race narrative.
    /// </summary>
    public void GenerateNarrative(NarrativeContext context, Action<string> onComplete)
    {
        if (!HasApiKey)
        {
            string mockNarrative = "The race continues with high intensity as everyone speeds forward!";
            context.History.Add(mockNarrative);
            onComplete?.Invoke(mockNarrative);
            return;
        }

        StartCoroutine(GenerateNarrativeCoroutine(context, onComplete));
    }

    private IEnumerator GenerateNarrativeCoroutine(NarrativeContext context, Action<string> onComplete)
    {
        string systemPrompt = BuildSystemPrompt();
        string userPrompt = BuildUserPrompt(context);

        string json = BuildChatRequestJson(systemPrompt, userPrompt);

        using (var request = new UnityWebRequest(API_URL, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {ApiKey}");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[OpenAI] Narrative API error: {request.error}");
                string fallback = "The race continues wildly as the drivers speed towards the finish line!";
                context.History.Add(fallback);
                onComplete?.Invoke(fallback);
                yield break;
            }

            string response = request.downloadHandler.text;
            string content = ExtractContentFromResponse(response);

            context.History.Add(content);
            onComplete?.Invoke(content);
        }
    }

    // ============================================================
    // Prompt Building
    // ============================================================

    private string BuildSystemPrompt()
    {
        return @"You are the narrator of ""Doodle Drifters"" -- a chaotic, comedic racing game inspired by Wacky Races and Mario Kart. Your job is to craft an exciting, continuous story about this wild race.

## GAME THEME & TONE
- The race features eccentric characters in ridiculous, gadget-filled vehicles
- The tone is playful, exaggerated, and family-friendly absurd (think Saturday morning cartoons)
- Racers constantly sabotage each other with bizarre items they've drawn
- Mishaps, betrayals, and unexpected comebacks are the norm
- Physics and logic are optional -- embrace the chaos!

## STORYTELLING RULES

### Continuity & Flow
- This is ONE continuous story told across multiple rounds -- treat it like chapters
- Reference events from previous rounds naturally (callbacks, running gags, consequences)
- Characters can hold grudges, form temporary alliances, or have rivalries
- Each snippet should flow from the last, not start fresh

### Round Awareness
- **First round**: Set the scene! Introduce the chaotic atmosphere, the starting line energy
- **Middle rounds**: Escalate the action, build on previous events
- **Final round**: Build to a climax! Make it dramatic, the finish line is in sight

### Item Usage (CRITICAL)
- The item with the MOST VOTES must be prominently featured -- it's the star of this round
- Other voted items can appear as secondary actions or failed attempts
- Items come from player doodles analyzed by AI, so they can be WEIRD
- Be creative! A ""weird blob"" could be slime that makes the track slippery

### Positions & Drama
- Racers overtaking others is dramatic -- describe HOW it happened
- Someone falling to last place is humiliating -- milk it!
- The leader defending their position creates tension

### Writing Style
- Keep it PUNCHY -- short, vivid sentences mixed with occasional longer dramatic ones
- Use varied vocabulary -- NEVER repeat the same adjective twice in one snippet
- Sound effects are welcome (ZOOM! SPLAT! WHOOOOSH!)
- Character names should appear frequently
- Stay within the word limit provided
- Don't use emojis or em-dashes in the narrative.

### What to AVOID
- Generic racing commentary
- Repetitive sentence structures
- Ignoring the voted items
- Treating each round as isolated
- Being too wordy

## OUTPUT FORMAT
Respond ONLY with the narrative snippet. No explanations, no meta-commentary, no quotation marks around the response.";
    }

    private string BuildUserPrompt(NarrativeContext context)
    {
        int currentRound = context.CurrentRound;
        int totalRounds = context.TotalRounds;
        bool isFirst = currentRound == 1;
        bool isFinal = context.Finished || currentRound == totalRounds;

        // Current standings
        var currentPositions = context.Positions.Count > 0
            ? context.Positions[context.Positions.Count - 1]
            : new List<PlayerPositionSnapshot>();

        var standings = new StringBuilder();
        for (int i = 0; i < currentPositions.Count; i++)
        {
            var p = currentPositions[i];
            standings.AppendLine($"{i + 1}. {p.Name}{(p.Crashed ? " (CRASHED)" : "")}");
        }

        // Voting results
        var votingResults = FormatVotingResults(currentPositions);

        // Position history
        var positionHistory = FormatPositionHistory(context.Positions);

        // Previous narrative
        string previousNarrative = context.History.Count > 0
            ? string.Join("\n\n", context.History)
            : "No previous narrative -- this is the start of the race!";

        int wordLimit = 30 + UnityEngine.Random.Range(0, 31);

        string roundContext;
        if (isFirst) roundContext = "THIS IS THE FIRST ROUND -- Set the scene, introduce the chaos!";
        else if (isFinal) roundContext = "THIS IS THE FINAL ROUND -- Build to a dramatic finish!";
        else roundContext = $"Round {currentRound} of {totalRounds} -- Keep the momentum going!";

        return $@"{roundContext}

## CURRENT STANDINGS
{standings}

## VOTING RESULTS THIS ROUND
{votingResults}

## POSITION CHANGES
{positionHistory}

## STORY SO FAR
{previousNarrative}

---
Write the next {wordLimit} words of the race narrative. Feature the most-voted item prominently!";
    }

    private string FormatPositionHistory(List<List<PlayerPositionSnapshot>> positions)
    {
        if (positions == null || positions.Count < 2)
            return "First round -- no position changes yet.";

        var prevRound = positions[positions.Count - 2];
        var currRound = positions[positions.Count - 1];

        var sb = new StringBuilder();
        for (int newPos = 0; newPos < currRound.Count; newPos++)
        {
            var player = currRound[newPos];
            int oldPos = prevRound.FindIndex(p => p.Id == player.Id);
            int diff = oldPos - newPos;

            string movement;
            if (player.Crashed) movement = "CRASHED OUT";
            else if (diff > 0) movement = $"up {diff} position{(diff > 1 ? "s" : "")}";
            else if (diff < 0) movement = $"down {Math.Abs(diff)} position{(Math.Abs(diff) > 1 ? "s" : "")}";
            else movement = "held position";

            sb.AppendLine($"{player.Name}: {movement}");
        }
        return sb.ToString();
    }

    private string FormatVotingResults(List<PlayerPositionSnapshot> positions)
    {
        if (positions == null || positions.Count == 0)
            return "No voting data available.";

        var sorted = positions
            .Where(p => !p.Crashed)
            .OrderByDescending(p => p.VotesInLastRound)
            .ToList();

        if (sorted.Count == 0) return "All players crashed!";

        int maxVotes = sorted[0].VotesInLastRound;

        var sb = new StringBuilder();
        foreach (var p in sorted)
        {
            int votes = p.VotesInLastRound;
            string item = string.IsNullOrEmpty(p.Item) ? "unknown gadget" : p.Item;
            string prefix = (votes == maxVotes && votes > 0) ? "MOST VOTED: " : "";
            sb.AppendLine($"{prefix}{p.Name} drew \"{item}\" ({votes} vote{(votes != 1 ? "s" : "")})");
        }
        return sb.ToString();
    }

    // ============================================================
    // JSON helpers (manual construction for complex nested structures)
    // ============================================================

    private string BuildVisionRequestJson(string imageUrl)
    {
        string escapedPrompt = EscapeJsonString(@"You analyze rough, low-quality doodles and describe what objects appear in them. Follow these rules: Interpret the doodle flexibly; assume it was drawn quickly or roughly. Abstract, imaginative, and exaggerated objects are allowed. Respond with a very short description (max 20 characters including spaces). Use no periods, punctuation, or filler words. Do not say drawing of, sketch of, doodle of, etc. State the object(s) directly. Try to describe the entire scene, not only one isolated part. Avoid vague terms like shapes or lines; be specific. Be as descriptive as possible within the character limit. If nothing is recognizable, respond with weird mess or unknown object. If more than one interpretation is possible, choose the single most likely one. Do not list alternatives. Never use or.");

        return $@"{{
  ""model"": ""{Model}"",
  ""max_tokens"": 200,
  ""messages"": [{{
    ""role"": ""user"",
    ""content"": [
      {{ ""type"": ""text"", ""text"": ""{escapedPrompt}"" }},
      {{ ""type"": ""image_url"", ""image_url"": {{ ""url"": ""{EscapeJsonString(imageUrl)}"", ""detail"": ""low"" }} }}
    ]
  }}]
}}";
    }

    private string BuildChatRequestJson(string systemPrompt, string userPrompt)
    {
        return $@"{{
  ""model"": ""{Model}"",
  ""temperature"": 1.0,
  ""messages"": [
    {{ ""role"": ""system"", ""content"": ""{EscapeJsonString(systemPrompt)}"" }},
    {{ ""role"": ""user"", ""content"": ""{EscapeJsonString(userPrompt)}"" }}
  ]
}}";
    }

    private string ExtractContentFromResponse(string responseJson)
    {
        // Simple JSON parsing to extract choices[0].message.content
        // Avoids needing a full JSON library
        try
        {
            int contentIdx = responseJson.IndexOf("\"content\"");
            if (contentIdx < 0) return "Unknown";

            // Find the value after "content":
            int colonIdx = responseJson.IndexOf(':', contentIdx);
            if (colonIdx < 0) return "Unknown";

            // Find the opening quote of the value
            int startQuote = responseJson.IndexOf('"', colonIdx + 1);
            if (startQuote < 0) return "Unknown";

            // Find the closing quote (accounting for escaped quotes)
            int endQuote = startQuote + 1;
            while (endQuote < responseJson.Length)
            {
                if (responseJson[endQuote] == '"' && responseJson[endQuote - 1] != '\\')
                    break;
                endQuote++;
            }

            string content = responseJson.Substring(startQuote + 1, endQuote - startQuote - 1);
            // Unescape basic JSON escapes
            content = content.Replace("\\n", "\n").Replace("\\\"", "\"").Replace("\\\\", "\\");
            return content;
        }
        catch
        {
            return "Unknown";
        }
    }

    private string EscapeJsonString(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return s.Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
    }
}

/// <summary>
/// Context passed to narrative generation.
/// </summary>
public class NarrativeContext
{
    public List<string> History;
    public List<PlayerData> Players;
    public List<List<PlayerPositionSnapshot>> Positions;
    public bool Finished;
    public int CurrentRound;
    public int TotalRounds;
}

// ============================================================
// OpenAI API request types (for reference, not used with JsonUtility)
// ============================================================

[Serializable]
public class OpenAIVisionRequest
{
    public string model;
    public int max_tokens;
    public OpenAIVisionMessage[] messages;
}

[Serializable]
public class OpenAIVisionMessage
{
    public string role;
    public OpenAIVisionContent[] content;
}

[Serializable]
public class OpenAIVisionContent
{
    public string type;
    public string text;
    public OpenAIImageUrl image_url;
}

[Serializable]
public class OpenAIImageUrl
{
    public string url;
    public string detail;
}
