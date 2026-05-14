// using System.Collections;
// using UnityEngine;
// using UnityEngine.UI;

// /// <summary>
// /// Central game loop for one room:
// ///   1. GenerateScenario  → spoken via TTS
// ///   2. Player draws      → presses existing Submit button
// ///   3. DoodleInference   → label + confidence via callback
// ///   4. GenerateSolution  → spoken via TTS
// ///   5. Room complete     → ready for next room
// ///
// /// Hook-up in Inspector:
// ///   - dmClient      : OllamaDungeonMasterTtsClient
// ///   - doodleInference : DoodleInference
// ///   - doodleDrawer  : DoodleDrawer  (to clear canvas between rooms)
// ///   - startButton   : shown at scene start to kick off the loop
// ///   - drawingPhaseUI: root panel shown while player draws (contains Submit button)
// ///
// /// The existing Submit button on TempDrawPlane already calls DoodleInference.Predict().
// /// The orchestrator registers OnPredictionComplete before the drawing phase and
// /// unregisters after — no changes to the prefab wiring needed.
// /// </summary>
// public class GameLoopOrchestrator : MonoBehaviour
// {
//     [Header("Core References")]
//     [SerializeField] private OllamaDungeonMasterTtsClient dmClient;
//     [SerializeField] private DoodleInference              doodleInference;
//     [SerializeField] private DoodleDrawer                 doodleDrawer;

//     [Header("UI")]
//     [Tooltip("Button the player clicks to start the first room.")]
//     [SerializeField] private Button startButton;

//     [Tooltip("Root panel shown while the player is drawing (should contain the Submit button).")]
//     [SerializeField] private GameObject drawingPhaseUI;

//     [Header("Room Settings")]
//     [SerializeField] private int totalRooms = 3;

//     // ── Runtime state ─────────────────────────────────────────────────────────

//     private RoomState currentRoom;
//     private bool      predictionReceived;
//     private bool      isRunning;

//     // ── Lifecycle ─────────────────────────────────────────────────────────────

//     private void Start()
//     {
//         SetDrawingPhaseActive(false);

//         if (startButton != null)
//             startButton.onClick.AddListener(StartGame);
//         else
//             Debug.LogWarning("[GameLoop] No Start button assigned — call StartGame() manually.");
//     }

//     // ── Public entry point ────────────────────────────────────────────────────

//     public void StartGame()
//     {
//         if (isRunning) return;
//         if (startButton != null) startButton.gameObject.SetActive(false);
//         StartCoroutine(RunAllRooms());
//     }

//     // ── Main loop ─────────────────────────────────────────────────────────────

//     private IEnumerator RunAllRooms()
//     {
//         isRunning = true;

//         for (int i = 0; i < totalRooms; i++)
//         {
//             currentRoom = new RoomState(i);
//             yield return RunRoom(currentRoom);
//         }

//         isRunning = false;
//         Debug.Log("[GameLoop] All rooms complete!");
//         // TODO: hook into victory screen / room transition here
//     }

//     private IEnumerator RunRoom(RoomState room)
//     {
//         Debug.Log($"[GameLoop] ── Room {room.roomIndex + 1} ──");

//         // ── Step 1: Generate & speak scenario ────────────────────────────────
//         yield return dmClient.GenerateScenario(
//             room.roomIndex,
//             text => room.scenarioText = text);

//         Debug.Log($"[GameLoop] Scenario: {room.scenarioText}");

//         // ── Step 2: Drawing phase ─────────────────────────────────────────────
//         doodleDrawer?.ClearCanvas();
//         SetDrawingPhaseActive(true);

//         predictionReceived = false;
//         doodleInference.OnPredictionComplete = OnPredictionReceived;

//         // Wait until the player submits their drawing
//         yield return new WaitUntil(() => predictionReceived);

//         doodleInference.OnPredictionComplete = null;
//         SetDrawingPhaseActive(false);

//         Debug.Log($"[GameLoop] Prediction: {room.predictedObject} ({room.confidence:P0})");

//         // ── Step 3: Generate & speak solution ────────────────────────────────
//         yield return dmClient.GenerateSolution(
//             room.scenarioText,
//             room.predictedObject,
//             room.confidence,
//             text => room.solutionText = text);

//         Debug.Log($"[GameLoop] Solution: {room.solutionText}");

//         // Brief pause between rooms
//         yield return new WaitForSeconds(2f);
//     }

//     // ── Prediction callback ───────────────────────────────────────────────────

//     private void OnPredictionReceived(string label, float confidence)
//     {
//         if (currentRoom == null) return;

//         currentRoom.predictedObject = label;
//         currentRoom.confidence      = confidence;
//         predictionReceived          = true;
//     }

//     // ── UI helpers ────────────────────────────────────────────────────────────

//     private void SetDrawingPhaseActive(bool active)
//     {
//         if (drawingPhaseUI != null)
//             drawingPhaseUI.SetActive(active);
//     }
// }
