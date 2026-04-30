using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Client screen panel for the voting phase.
///
/// Shows all solution drawings EXCEPT the local player's own drawing.
/// The player taps a drawing to select it, then confirms with the vote button.
///
/// Rules enforced here:
///   - Player cannot see or vote for their own drawing.
///   - Player can only vote once (button is disabled after submitting).
///
/// Inspector hook-up:
///   - cardContainer    : layout group parent for the voting cards
///   - clientCardPrefab : prefab with RawImage + playerNameLabel + selectionHighlight
///   - voteButton       : confirm vote button (disabled until a card is selected)
///   - statusLabel      : "Choose a drawing" / "Vote submitted!" text
///
/// The local player ID must be set via SetLocalPlayer() before this panel is shown.
/// SolutionSubmitBridge calls SetLocalPlayer() when the player submits their drawing,
/// so the ID is always available by the time voting starts.
/// </summary>
public class VotingClientPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform       cardContainer;
    [SerializeField] private GameObject      clientCardPrefab;
    [SerializeField] private Button          voteButton;
    [SerializeField] private TextMeshProUGUI statusLabel;

    // Set by SolutionSubmitBridge before this panel opens.
    private string _localPlayerId;

    private string                    _selectedPlayerId = null;
    private readonly List<ClientDrawingCard> _cards    = new();
    private bool                      _hasVoted         = false;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        EnsureLocalPlayerId();

        if (voteButton != null)
        {
            voteButton.onClick.AddListener(OnVoteButtonPressed);
            voteButton.interactable = false;
        }
    }

    private void OnEnable()
    {
        if (VotingManager.Instance == null) return;
        VotingManager.Instance.OnAllSubmissionsReceived += PopulateCards;
        VotingManager.Instance.OnAllVotesReceived       += OnVotingComplete;
    }

    private void OnDisable()
    {
        if (VotingManager.Instance == null) return;
        VotingManager.Instance.OnAllSubmissionsReceived -= PopulateCards;
        VotingManager.Instance.OnAllVotesReceived       -= OnVotingComplete;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Must be called before this panel is shown so it knows whose drawing to hide.
    /// SolutionSubmitBridge sets this when the player submits their drawing.
    /// </summary>
    public void SetLocalPlayer(string playerId)
    {
        if (string.IsNullOrWhiteSpace(playerId))
        {
            Debug.LogWarning("[VotingClientPanel] Ignoring empty local player ID.");
            return;
        }

        _localPlayerId = playerId;
    }

    // ── Internal ──────────────────────────────────────────────────────────────

    /// <summary>Spawns one card per submission, excluding the local player's own.</summary>
    private void PopulateCards(IReadOnlyList<SolutionSubmission> submissions)
    {
        ClearCards();
        _selectedPlayerId = null;
        _hasVoted         = false;

        EnsureLocalPlayerId();

        if (clientCardPrefab == null || cardContainer == null)
        {
            Debug.LogError("[VotingClientPanel] clientCardPrefab or cardContainer is not assigned.");
            return;
        }

        // Filter out the local player's own drawing.
        IReadOnlyList<SolutionSubmission> votable =
            VotingManager.Instance.GetSubmissionsExcluding(_localPlayerId);

        foreach (var submission in votable)
        {
            GameObject cardGO = Instantiate(clientCardPrefab, cardContainer);

            // Each card gets a click callback that selects it.
            string capturedId = submission.PlayerId;
            var card = new ClientDrawingCard(cardGO, submission, () => OnCardSelected(capturedId));
            _cards.Add(card);
        }

        SetStatus("Choose a drawing to vote for.");

        if (votable.Count == 0)
            SetStatus("Waiting for more player submissions...");

        if (voteButton != null)
            voteButton.interactable = false;

        Debug.Log($"[VotingClientPanel] Showing {_cards.Count} votable drawings.");
    }

    /// <summary>Called when the player taps a drawing card.</summary>
    private void OnCardSelected(string chosenPlayerId)
    {
        if (_hasVoted) return;

        _selectedPlayerId = chosenPlayerId;

        // Update highlight on all cards.
        foreach (var card in _cards)
            card.SetSelected(card.PlayerId == chosenPlayerId);

        if (voteButton != null)
            voteButton.interactable = true;

        SetStatus("Tap 'Vote' to confirm your choice.");
    }

    /// <summary>Called when the player presses the confirm vote button.</summary>
    private void OnVoteButtonPressed()
    {
        if (_hasVoted || string.IsNullOrEmpty(_selectedPlayerId)) return;

        _hasVoted = true;

        if (voteButton != null)
            voteButton.interactable = false;

        VotingManager.Instance.RegisterVote(_localPlayerId, _selectedPlayerId);

        SetStatus("Vote submitted! Waiting for other players...");
        Debug.Log($"[VotingClientPanel] '{_localPlayerId}' voted for '{_selectedPlayerId}'.");
    }

    /// <summary>Called when all votes are in and a winner is determined.</summary>
    private void OnVotingComplete(SolutionSubmission winner)
    {
        SetStatus($"The party chose: {winner?.PlayerName}!");

        // Lock all cards.
        foreach (var card in _cards)
            card.SetLocked(true);

        Debug.Log("[VotingClientPanel] Voting complete.");
    }

    private void SetStatus(string message)
    {
        if (statusLabel != null)
            statusLabel.text = message;
    }

    private void ClearCards()
    {
        foreach (var card in _cards)
            if (card.Root != null) Destroy(card.Root);
        _cards.Clear();
    }

    private void EnsureLocalPlayerId()
    {
        if (!string.IsNullOrWhiteSpace(_localPlayerId))
            return;

        _localPlayerId = SystemInfo.deviceUniqueIdentifier;

        if (string.IsNullOrWhiteSpace(_localPlayerId))
            _localPlayerId = "local-player";

        Debug.LogWarning(
            $"[VotingClientPanel] Local player ID was not set. Falling back to '{_localPlayerId}'."
        );
    }

    // ── Inner helper ──────────────────────────────────────────────────────────

    /// <summary>
    /// Wraps a spawned voting card and handles selection highlight + click.
    /// Note: descriptions are NOT shown on client cards (only on the host screen).
    /// </summary>
    private class ClientDrawingCard
    {
        public GameObject Root     { get; }
        public string     PlayerId { get; }

        private readonly RawImage         _image;
        private readonly TextMeshProUGUI  _nameLabel;
        private readonly Image            _selectionHighlight;
        private readonly Button           _button;

        public ClientDrawingCard(GameObject root, SolutionSubmission submission,
                                  System.Action onClicked)
        {
            Root     = root;
            PlayerId = submission.PlayerId;

            _image              = root.GetComponentInChildren<RawImage>();
            _nameLabel          = root.GetComponentInChildren<TextMeshProUGUI>();
            _selectionHighlight = root.GetComponent<Image>();
            _button             = root.GetComponent<Button>();

            if (_image != null)
                _image.texture = submission.Texture;

            // Intentionally no description — that is only on the host screen.
            if (_nameLabel != null)
                _nameLabel.text = submission.PlayerName;

            if (_button != null)
                _button.onClick.AddListener(() => onClicked());

            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            if (_selectionHighlight != null)
                _selectionHighlight.color = selected
                    ? new Color(0.78f, 0.53f, 0.23f, 0.5f)  // torch flame tint
                    : Color.white;
        }

        public void SetLocked(bool locked)
        {
            if (_button != null)
                _button.interactable = !locked;
        }
    }
}
