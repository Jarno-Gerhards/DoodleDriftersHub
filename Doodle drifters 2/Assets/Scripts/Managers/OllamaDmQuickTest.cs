// using UnityEngine;
// using UnityEngine.InputSystem;

// public class OllamaDmQuickTest : MonoBehaviour
// {
//     [Header("References")]
//     [SerializeField] private OllamaDungeonMasterTtsClient dmClient;

//     [Header("Test Prompt")]
//     [SerializeField] private string testPrompt = "The party enters a cursed forest. Describe what they sense.";

//     private bool isRequestInProgress;

//     private void Reset()
//     {
//         // Auto-assign if both components are on the same GameObject.
//         if (dmClient == null)
//         {
//             dmClient = GetComponent<OllamaDungeonMasterTtsClient>();
//         }
//     }

//     private void Update()
//     {
//         if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
//         {
//             if (dmClient == null)
//             {
//                 Debug.LogError("OllamaDmQuickTest: Assign an OllamaDungeonMasterTtsClient in the Inspector.");
//                 return;
//             }

//             if (isRequestInProgress)
//             {
//                 Debug.LogWarning("OllamaDmQuickTest: Request already in progress. Wait for completion.");
//                 return;
//             }

//             StartCoroutine(RunQuickTest());
//         }
//     }

//     private System.Collections.IEnumerator RunQuickTest()
//     {
//         isRequestInProgress = true;
//         Debug.Log("OllamaDmQuickTest: Sending prompt to Ollama + Piper...");

//         yield return StartCoroutine(dmClient.GenerateAndSpeak(testPrompt, OnAiResponseReceived));

//         isRequestInProgress = false;
//         Debug.Log("OllamaDmQuickTest: Flow finished.");
//     }

//     private void OnAiResponseReceived(string aiText)
//     {
//         if (string.IsNullOrWhiteSpace(aiText))
//         {
//             Debug.LogWarning("OllamaDmQuickTest: Received empty AI response.");
//             return;
//         }

//         Debug.Log($"OllamaDmQuickTest AI Response: {aiText}");
//     }
// }
