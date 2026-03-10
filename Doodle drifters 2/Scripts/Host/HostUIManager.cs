using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages the Host's UI: lobby, drawing phase, voting phase, narrative, and podium.
/// Equivalent to the HTML/CSS UI in host.html + host.js UI logic.
/// Attach to a Canvas or UI root in the Host scene.
/// </summary>
public class HostUIManager : MonoBehaviour
{
    [Header("Screens")]
    public GameObject LobbyScreen;
    public GameObject GameScreen;
    public GameObject PasswordScreen;

    [Header("Lobby UI")]
    public TMP_Text RoomCodeText;
    public TMP_Text LobbyStatusText;
    public TMP_Text IPAddressText;
    public Transform PlayerListContainer;
    public GameObject PlayerCardPrefab; // Prefab with: Image (car), TMP_Text (name), Image (statusIcon)

    [Header("Drawing Phase UI")]
    public GameObject DrawingInstructionPanel;
    public TMP_Text DrawingInstructionText;
    public GameObject TitleImageContainer;
    public GameObject RoomCodeBox;
    public GameObject StatusTextContainer;

    [Header("Voting Phase UI")]
    public GameObject VotingInstructionPanel;
    public TMP_Text VotingInstructionText;
    public Transform VotingDrawingsContainer;
    public GameObject VotingDrawingPrefab; // Prefab with: TMP_Text (number), Image (drawing), TMP_Text (description)

    [Header("Narrative Phase UI")]
    public GameObject NarrativePhaseContainer;
    public Transform NarrativePlayersContainer;
    public TMP_Text NarrativeText;

    [Header("Narrative Overlay")]
    public GameObject NarrativeOverlay;
    public TMP_Text NarrativeOverlayText;

    [Header("Game Over UI")]
    public GameObject GameOverOverlay;
    public TMP_Text WinnerText;
    public Image[] PodiumCarImages; // [0] = 1st, [1] = 2nd, [2] = 3rd
    public TMP_Text[] PodiumNameTexts;
    public TMP_Text[] PodiumScoreTexts;

    [Header("Overlays")]
    public GameObject LoadingOverlay;
    public GameObject ErrorOverlay;
    public TMP_Text ErrorMessageText;

    [Header("Car Sprites")]
    public Sprite[] CarSprites; // Index 0 = car1, etc. Assign in Inspector.
    public Sprite DrawingIcon;
    public Sprite ReadyIcon;

    private GameConfig _config;
    private Dictionary<string, GameObject> _playerCards = new Dictionary<string, GameObject>();
    private Dictionary<string, int> _previousPositions = new Dictionary<string, int>();
    private Coroutine _typewriterCoroutine;
    private Coroutine _narrativeHideCoroutine;

    private void Awake()
    {
        _config = GameConfig.Instance;
    }

    // ============================================================
    // Screen Management
    // ============================================================

    public void ShowLobby(string roomId, string localIP)
    {
        HideAll();
        LobbyScreen.SetActive(true);
        RoomCodeText.text = roomId;
        if (IPAddressText != null)
            IPAddressText.text = $"{localIP}:{_config.ServerPort}";
        LobbyStatusText.text = "Waiting for players to join...";
    }

    public void ShowDrawingPhase(int round)
    {
        HideAll();
        LobbyScreen.SetActive(true);

        if (TitleImageContainer != null) TitleImageContainer.SetActive(false);
        if (RoomCodeBox != null) RoomCodeBox.SetActive(false);
        if (DrawingInstructionPanel != null)
        {
            DrawingInstructionPanel.SetActive(true);
            DrawingInstructionText.text = "Draw your gadgets!";
        }

        // Show player status icons
        ShowPlayerStatusIcons();
        ResetPlayerStatuses();

        ShowNarrativeOverlay($"Round {round} Start! Start Drawing!", _config.RoundStartNarrativeDuration);
    }

    public void ShowVotingPhase(List<DrawingData> drawings)
    {
        if (DrawingInstructionPanel != null) DrawingInstructionPanel.SetActive(false);
        if (VotingInstructionPanel != null)
        {
            VotingInstructionPanel.SetActive(true);
            VotingInstructionText.text = "Vote for your\nfavorite drawing";
        }

        HidePlayerStatusIcons();
        RenderVotingDrawings(drawings);
    }

    public void ShowVoteResults(List<VotingResult> results, Dictionary<string, string> playerNames)
    {
        LobbyStatusText.text = "Tallying the votes...";
        if (VotingInstructionText != null)
            VotingInstructionText.text = "And the votes are in!";

        // Update voting cards with player names and vote counts
        foreach (Transform child in VotingDrawingsContainer)
        {
            var card = child.GetComponent<VotingCardUI>();
            if (card == null) continue;

            string playerId = card.PlayerId;
            var result = results.Find(r => r.PlayerId == playerId);
            string playerName = playerNames.ContainsKey(playerId) ? playerNames[playerId] : "Unknown";
            int votes = result?.VotesReceived ?? 0;

            card.ShowResults(playerName, votes, _config.VoteCountAnimationInterval);
        }
    }

    public void ShowNarrativePhase(string narrative, List<PlayerPositionSnapshot> positions)
    {
        // Hide voting
        if (VotingInstructionPanel != null) VotingInstructionPanel.SetActive(false);
        ClearVotingDrawings();

        // Hide player list
        if (PlayerListContainer != null) PlayerListContainer.gameObject.SetActive(false);
        if (StatusTextContainer != null) StatusTextContainer.SetActive(false);

        // Render player positions
        RenderNarrativePlayers(positions);

        // Show narrative with typewriter effect
        if (NarrativePhaseContainer != null) NarrativePhaseContainer.SetActive(true);
        StartTypewriter(NarrativeText, narrative, _config.NarrativeTypingSpeed);

        // Update previous positions
        UpdatePreviousPositions(positions);
    }

    public void ShowGameOver(List<List<PlayerPositionSnapshot>> allPositions)
    {
        HideAll();
        GameScreen.SetActive(true);

        var finalPositions = allPositions[allPositions.Count - 1];
        if (finalPositions.Count == 0) return;

        var winner = finalPositions[0];
        WinnerText.text = $"{winner.Name} won the race!";

        for (int i = 0; i < 3 && i < finalPositions.Count; i++)
        {
            var player = finalPositions[i];
            if (i < PodiumCarImages.Length && PodiumCarImages[i] != null)
            {
                int carIdx = Mathf.Clamp(player.CarId - 1, 0, CarSprites.Length - 1);
                PodiumCarImages[i].sprite = CarSprites[carIdx];
            }
            if (i < PodiumNameTexts.Length && PodiumNameTexts[i] != null)
                PodiumNameTexts[i].text = player.Name;
            if (i < PodiumScoreTexts.Length && PodiumScoreTexts[i] != null)
                PodiumScoreTexts[i].text = player.Score.ToString();
        }

        GameOverOverlay.SetActive(true);
    }

    // ============================================================
    // Player List Management
    // ============================================================

    public void AddPlayerToLobby(PlayerData player)
    {
        if (_playerCards.ContainsKey(player.Id)) return;

        var card = Instantiate(PlayerCardPrefab, PlayerListContainer);
        card.name = $"Player_{player.Id}";

        // Set car image
        var carImage = card.transform.Find("CarImage")?.GetComponent<Image>();
        if (carImage != null && CarSprites != null && player.CarId > 0 && player.CarId <= CarSprites.Length)
        {
            carImage.sprite = CarSprites[player.CarId - 1];
        }

        // Set name
        var nameText = card.GetComponentInChildren<TMP_Text>();
        if (nameText != null)
            nameText.text = player.Name.ToUpper();

        // Set status icon (drawing by default)
        var statusIcon = card.transform.Find("StatusIcon")?.GetComponent<Image>();
        if (statusIcon != null && DrawingIcon != null)
        {
            statusIcon.sprite = DrawingIcon;
            statusIcon.gameObject.SetActive(false); // Hidden until game starts
        }

        _playerCards[player.Id] = card;
    }

    public void RemovePlayerFromLobby(string playerId)
    {
        if (_playerCards.TryGetValue(playerId, out var card))
        {
            Destroy(card);
            _playerCards.Remove(playerId);
        }
    }

    public void UpdatePlayerConnectionStatus(string playerId, bool connected)
    {
        if (_playerCards.TryGetValue(playerId, out var card))
        {
            var canvasGroup = card.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = card.AddComponent<CanvasGroup>();
            canvasGroup.alpha = connected ? 1f : 0.4f;
        }
    }

    public void UpdatePlayerStatus(string playerId, string status)
    {
        if (_playerCards.TryGetValue(playerId, out var card))
        {
            var statusIcon = card.transform.Find("StatusIcon")?.GetComponent<Image>();
            if (statusIcon != null)
            {
                statusIcon.sprite = status == "ready" ? ReadyIcon : DrawingIcon;
            }
        }
    }

    public void ResetPlayerStatuses()
    {
        foreach (var card in _playerCards.Values)
        {
            var statusIcon = card.transform.Find("StatusIcon")?.GetComponent<Image>();
            if (statusIcon != null)
                statusIcon.sprite = DrawingIcon;
        }
    }

    public void ShowPlayerStatusIcons()
    {
        foreach (var card in _playerCards.Values)
        {
            var statusIcon = card.transform.Find("StatusIcon");
            if (statusIcon != null) statusIcon.gameObject.SetActive(true);
        }
    }

    public void HidePlayerStatusIcons()
    {
        foreach (var card in _playerCards.Values)
        {
            var statusIcon = card.transform.Find("StatusIcon");
            if (statusIcon != null) statusIcon.gameObject.SetActive(false);
        }
    }

    // ============================================================
    // Lobby Status
    // ============================================================

    public void UpdateLobbyStatus(int playerCount, int minPlayers, int maxPlayers, string leaderName)
    {
        if (playerCount == 0)
            LobbyStatusText.text = "Waiting for players to join...";
        else if (playerCount < minPlayers)
        {
            int needed = minPlayers - playerCount;
            LobbyStatusText.text = $"Waiting for at least {needed} more player{(needed != 1 ? "s" : "")} to join...";
        }
        else if (playerCount >= maxPlayers)
            LobbyStatusText.text = !string.IsNullOrEmpty(leaderName)
                ? $"Game is full. {leaderName} can start the game."
                : "Game is full. VIP can start the game.";
        else
            LobbyStatusText.text = !string.IsNullOrEmpty(leaderName)
                ? $"{leaderName} can start the game."
                : "VIP can start the game.";
    }

    public void UpdateVotingStatus(int totalPlayers, int votedCount)
    {
        int remaining = totalPlayers - votedCount;
        LobbyStatusText.text = remaining > 0
            ? $"Waiting for {remaining} more player{(remaining != 1 ? "s" : "")} to vote"
            : "All votes received!";
    }

    // ============================================================
    // Voting Drawings
    // ============================================================

    private void RenderVotingDrawings(List<DrawingData> drawings)
    {
        ClearVotingDrawings();

        for (int i = 0; i < drawings.Count; i++)
        {
            var drawing = drawings[i];
            var card = Instantiate(VotingDrawingPrefab, VotingDrawingsContainer);
            
            var cardUI = card.GetComponent<VotingCardUI>();
            if (cardUI == null)
                cardUI = card.AddComponent<VotingCardUI>();

            cardUI.Setup(i + 1, drawing);
        }

        VotingDrawingsContainer.gameObject.SetActive(true);
    }

    private void ClearVotingDrawings()
    {
        foreach (Transform child in VotingDrawingsContainer)
            Destroy(child.gameObject);
    }

    // ============================================================
    // Narrative Phase Players
    // ============================================================

    private void RenderNarrativePlayers(List<PlayerPositionSnapshot> positions)
    {
        // Clear existing
        foreach (Transform child in NarrativePlayersContainer)
            Destroy(child.gameObject);

        // Reverse: first place on right, last on left
        var reversed = positions.ToList();
        reversed.Reverse();

        for (int i = 0; i < reversed.Count; i++)
        {
            var player = reversed[i];
            var card = Instantiate(PlayerCardPrefab, NarrativePlayersContainer);

            // Car image
            var carImage = card.transform.Find("CarImage")?.GetComponent<Image>();
            if (carImage != null && CarSprites != null && player.CarId > 0 && player.CarId <= CarSprites.Length)
                carImage.sprite = CarSprites[player.CarId - 1];

            // Name
            var nameText = card.GetComponentInChildren<TMP_Text>();
            if (nameText != null) nameText.text = player.Name.ToUpper();

            // Score (replace status icon)
            var statusIcon = card.transform.Find("StatusIcon");
            if (statusIcon != null) statusIcon.gameObject.SetActive(false);
            
            var scoreText = card.transform.Find("ScoreText")?.GetComponent<TMP_Text>();
            if (scoreText != null)
                scoreText.text = player.Score.ToString();

            // Animate position from previous
            int currentIndex = positions.FindIndex(p => p.Id == player.Id);
            int prevPos = _previousPositions.ContainsKey(player.Id)
                ? _previousPositions[player.Id] - 1
                : currentIndex;

            int totalPlayers = positions.Count;
            int newReversedIdx = i;
            int oldReversedIdx = totalPlayers - 1 - prevPos;

            if (newReversedIdx != oldReversedIdx)
            {
                float offsetX = (newReversedIdx - oldReversedIdx) * -250f; // Approximate spacing
                var rect = card.GetComponent<RectTransform>();
                if (rect != null)
                {
                    Vector2 startPos = rect.anchoredPosition;
                    startPos.x += offsetX;
                    rect.anchoredPosition = startPos;
                    StartCoroutine(AnimatePosition(rect, startPos,
                        new Vector2(startPos.x - offsetX, startPos.y),
                        _config.PositionAnimationDuration));
                }
            }
        }
    }

    private IEnumerator AnimatePosition(RectTransform rect, Vector2 from, Vector2 to, float duration)
    {
        float elapsed = 0;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / duration);
            rect.anchoredPosition = Vector2.Lerp(from, to, t);
            yield return null;
        }
        rect.anchoredPosition = to;
    }

    private void UpdatePreviousPositions(List<PlayerPositionSnapshot> positions)
    {
        _previousPositions.Clear();
        for (int i = 0; i < positions.Count; i++)
            _previousPositions[positions[i].Id] = i + 1;
    }

    // ============================================================
    // Narrative Overlay (short pop-up messages)
    // ============================================================

    public void ShowNarrativeOverlay(string text, float duration)
    {
        if (NarrativeOverlay == null) return;
        NarrativeOverlayText.text = text;
        NarrativeOverlay.SetActive(true);

        if (_narrativeHideCoroutine != null)
            StopCoroutine(_narrativeHideCoroutine);

        if (duration > 0)
            _narrativeHideCoroutine = StartCoroutine(HideAfterDelay(NarrativeOverlay, duration));
    }

    private IEnumerator HideAfterDelay(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        obj.SetActive(false);
    }

    // ============================================================
    // Typewriter Effect
    // ============================================================

    private void StartTypewriter(TMP_Text textComponent, string text, float charDelay)
    {
        if (_typewriterCoroutine != null)
            StopCoroutine(_typewriterCoroutine);
        _typewriterCoroutine = StartCoroutine(TypewriterCoroutine(textComponent, text, charDelay));
    }

    private IEnumerator TypewriterCoroutine(TMP_Text textComponent, string text, float charDelay)
    {
        textComponent.text = "";
        for (int i = 0; i < text.Length; i++)
        {
            textComponent.text += text[i];
            yield return new WaitForSeconds(charDelay);
        }
    }

    // ============================================================
    // Error/Loading
    // ============================================================

    public void ShowError(string message)
    {
        ErrorMessageText.text = message;
        ErrorOverlay.SetActive(true);
        if (LoadingOverlay != null) LoadingOverlay.SetActive(false);
    }

    public void HideError()
    {
        ErrorOverlay.SetActive(false);
    }

    private void HideAll()
    {
        LobbyScreen.SetActive(false);
        GameScreen.SetActive(false);
        if (NarrativePhaseContainer != null) NarrativePhaseContainer.SetActive(false);
        if (NarrativeOverlay != null) NarrativeOverlay.SetActive(false);
        if (GameOverOverlay != null) GameOverOverlay.SetActive(false);
        if (DrawingInstructionPanel != null) DrawingInstructionPanel.SetActive(false);
        if (VotingInstructionPanel != null) VotingInstructionPanel.SetActive(false);
        if (VotingDrawingsContainer != null) ClearVotingDrawings();
        if (PasswordScreen != null) PasswordScreen.SetActive(false);
    }
}
