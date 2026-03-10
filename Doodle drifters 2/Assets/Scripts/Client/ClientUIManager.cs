using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Client-side UI manager handling all mobile player screens.
/// Equivalent to the HTML views in client.html + UI logic in client.js.
/// Attach to the Canvas in the Client scene.
/// </summary>
public class ClientUIManager : MonoBehaviour
{
    [Header("Views")]
    public GameObject LoginView;
    public GameObject WaitingView;
    public GameObject DrawingView;
    public GameObject VotingView;
    public GameObject FinishedView;

    [Header("Login View")]
    public TMP_InputField ServerAddressInput;
    public TMP_InputField RoomCodeInput;
    public TMP_InputField PlayerNameInput;
    public Button JoinButton;

    [Header("Waiting View")]
    public TextMeshProUGUI WaitingMessage;
    public Button StartGameButton;
    public TextMeshProUGUI StartButtonLabel;

    [Header("Drawing View")]
    public TextMeshProUGUI DrawingPrompt;
    public Button SubmitDrawingButton;
    public TextMeshProUGUI SubmitButtonLabel;
    public Button UndoButton;
    public Button ClearButton;
    public TextMeshProUGUI DrawingTimerText;

    [Header("Voting View")]
    public Transform VotingGridParent;
    public GameObject VotingCardPrefab;
    public TextMeshProUGUI VotingTimerText;

    [Header("Finished View")]
    public TextMeshProUGUI FinishedMessage;

    [Header("Overlays")]
    public GameObject LoadingOverlay;
    public GameObject ErrorOverlay;
    public TextMeshProUGUI ErrorMessage;
    public Button ErrorDismissButton;

    [Header("Timer")]
    public GameObject TimerContainer;
    public TextMeshProUGUI TimerText;

    private ClientManager _manager;
    private string _currentView = "";

    public void SetupEvents(ClientManager manager)
    {
        _manager = manager;

        JoinButton.onClick.AddListener(OnJoinClicked);
        if (StartGameButton != null) StartGameButton.onClick.AddListener(() => _manager.StartGame());
        if (SubmitDrawingButton != null) SubmitDrawingButton.onClick.AddListener(() => _manager.SubmitDrawing());
        if (UndoButton != null) UndoButton.onClick.AddListener(() => _manager.UndoDrawing());
        if (ClearButton != null) ClearButton.onClick.AddListener(() => _manager.ClearDrawing());
        if (ErrorDismissButton != null) ErrorDismissButton.onClick.AddListener(HideError);
    }

    private void OnJoinClicked()
    {
        string serverAddr = ServerAddressInput != null ? ServerAddressInput.text.Trim() : "localhost:7777";
        string roomCode = RoomCodeInput != null ? RoomCodeInput.text.Trim().ToUpper() : "";
        string playerName = PlayerNameInput != null ? PlayerNameInput.text.Trim() : "";

        if (string.IsNullOrEmpty(roomCode))
        {
            ShowError("Please enter a room code.");
            return;
        }
        if (string.IsNullOrEmpty(playerName))
        {
            ShowError("Please enter your name.");
            return;
        }
        if (playerName.Length > 16)
        {
            ShowError("Name must be 16 characters or less.");
            return;
        }

        _manager.ConnectAndJoin(serverAddr, roomCode, playerName);
    }

    // ============================================================
    // View Management
    // ============================================================

    public void ShowView(string viewName)
    {
        _currentView = viewName;

        if (LoginView != null) LoginView.SetActive(viewName == "login");
        if (WaitingView != null) WaitingView.SetActive(viewName == "waiting");
        if (DrawingView != null) DrawingView.SetActive(viewName == "drawing");
        if (VotingView != null) VotingView.SetActive(viewName == "voting");
        if (FinishedView != null) FinishedView.SetActive(viewName == "finished");
    }

    // ============================================================
    // Waiting View
    // ============================================================

    public void SetWaitingMessage(string message)
    {
        if (WaitingMessage != null) WaitingMessage.text = message;
    }

    public void ShowStartButton()
    {
        if (StartGameButton != null) StartGameButton.gameObject.SetActive(true);
    }

    public void HideStartButton()
    {
        if (StartGameButton != null) StartGameButton.gameObject.SetActive(false);
    }

    public void SetStartButtonInteractable(bool interactable)
    {
        if (StartGameButton != null) StartGameButton.interactable = interactable;
    }

    public void SetStartButtonText(string text)
    {
        if (StartButtonLabel != null) StartButtonLabel.text = text;
    }

    // ============================================================
    // Drawing View
    // ============================================================

    public void SetDrawingPrompt(string prompt)
    {
        if (DrawingPrompt != null) DrawingPrompt.text = prompt;
    }

    public void SetSubmitButtonState(bool interactable, string text = null)
    {
        if (SubmitDrawingButton != null) SubmitDrawingButton.interactable = interactable;
        if (SubmitButtonLabel != null && text != null) SubmitButtonLabel.text = text;
    }

    // ============================================================
    // Voting View
    // ============================================================

    public void RenderVotingGrid(List<DrawingData> drawings, string myPlayerId, ClientManager manager)
    {
        // Clear existing cards
        if (VotingGridParent != null)
        {
            foreach (Transform child in VotingGridParent)
                Destroy(child.gameObject);
        }

        if (drawings == null || VotingCardPrefab == null || VotingGridParent == null) return;

        int cardNumber = 1;
        foreach (var drawing in drawings)
        {
            // Don't show own drawing
            if (drawing.PlayerId == myPlayerId) continue;

            GameObject card = Instantiate(VotingCardPrefab, VotingGridParent);
            var cardUI = card.GetComponent<VotingCardUI>();
            if (cardUI != null)
            {
                cardUI.Setup(cardNumber, drawing);
            }

            // Add vote button listener
            Button btn = card.GetComponent<Button>();
            if (btn == null) btn = card.GetComponentInChildren<Button>();
            if (btn != null)
            {
                string targetId = drawing.PlayerId;
                btn.onClick.AddListener(() => manager.SubmitVote(targetId));
            }
            cardNumber++;
        }
    }

    // ============================================================
    // Timer
    // ============================================================

    public void ShowTimer(string phase)
    {
        if (TimerContainer != null) TimerContainer.SetActive(true);
    }

    public void HideTimer()
    {
        if (TimerContainer != null) TimerContainer.SetActive(false);
    }

    public void UpdateTimerDisplay(float seconds, string phase)
    {
        int s = Mathf.CeilToInt(seconds);
        string display = $"{s / 60}:{(s % 60):D2}";

        if (TimerText != null) TimerText.text = display;

        // Also update phase-specific timer
        if (phase == "drawing" && DrawingTimerText != null)
            DrawingTimerText.text = display;
        else if (phase == "voting" && VotingTimerText != null)
            VotingTimerText.text = display;
    }

    // ============================================================
    // Loading & Errors
    // ============================================================

    public void ShowLoading()
    {
        if (LoadingOverlay != null) LoadingOverlay.SetActive(true);
    }

    public void HideLoading()
    {
        if (LoadingOverlay != null) LoadingOverlay.SetActive(false);
    }

    public void ShowError(string message)
    {
        if (ErrorOverlay != null) ErrorOverlay.SetActive(true);
        if (ErrorMessage != null) ErrorMessage.text = message;
    }

    public void HideError()
    {
        if (ErrorOverlay != null) ErrorOverlay.SetActive(false);
    }
}
