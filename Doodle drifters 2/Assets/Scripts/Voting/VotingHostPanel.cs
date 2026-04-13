using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Host screen panel for the voting phase.
///
/// Shows all submitted solution drawings in a grid, each with:
///   - The drawing image
///   - The player's name
///   - The description the player gave their drawing
///   - A live vote counter that updates as votes come in
///
/// This panel is read-only — the host does not vote.
/// It subscribes to VotingManager events directly.
///
/// Inspector hook-up:
///   - cardContainer    : HorizontalLayoutGroup or GridLayoutGroup parent
///   - drawingCardPrefab: prefab with RawImage + nameLabel + descriptionLabel + voteLabel
///   - statusLabel      : "Waiting for votes... (X/Y)" text
/// </summary>
public class VotingHostPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform          cardContainer;
    [SerializeField] private GameObject         drawingCardPrefab;
    [SerializeField] private TextMeshProUGUI    statusLabel;

    // Runtime list of spawned cards so we can update vote counts live.
    private readonly List<HostDrawingCard> _cards = new();

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void OnEnable()
    {
        if (VotingManager.Instance == null) return;
        VotingManager.Instance.OnAllSubmissionsReceived += PopulateCards;
        VotingManager.Instance.OnVoteProgress           += UpdateVoteProgress;
        VotingManager.Instance.OnAllVotesReceived       += OnVotingComplete;
    }

    private void OnDisable()
    {
        if (VotingManager.Instance == null) return;
        VotingManager.Instance.OnAllSubmissionsReceived -= PopulateCards;
        VotingManager.Instance.OnVoteProgress           -= UpdateVoteProgress;
        VotingManager.Instance.OnAllVotesReceived       -= OnVotingComplete;
    }

    // ── Internal ──────────────────────────────────────────────────────────────

    /// <summary>Spawns one card per submission when all drawings are in.</summary>
    private void PopulateCards(IReadOnlyList<SolutionSubmission> submissions)
    {
        ClearCards();

        if (drawingCardPrefab == null || cardContainer == null)
        {
            Debug.LogError("[VotingHostPanel] drawingCardPrefab or cardContainer is not assigned.");
            return;
        }

        foreach (var submission in submissions)
        {
            GameObject cardGO = Instantiate(drawingCardPrefab, cardContainer);
            var card = new HostDrawingCard(cardGO, submission);
            _cards.Add(card);
        }

        SetStatus($"Players are voting... (0/{submissions.Count})");
        Debug.Log($"[VotingHostPanel] Populated {submissions.Count} cards.");
    }

    /// <summary>Updates the status label as votes come in.</summary>
    private void UpdateVoteProgress(int votesIn, int totalExpected)
    {
        SetStatus($"Players are voting... ({votesIn}/{totalExpected})");

        // Refresh vote count labels on each card.
        foreach (var card in _cards)
            card.RefreshVoteCount();
    }

    /// <summary>Called when all votes are counted and a winner is determined.</summary>
    private void OnVotingComplete(SolutionSubmission winner)
    {
        SetStatus($"Voting complete! Winner: {winner?.PlayerName}");

        // Highlight the winning card.
        foreach (var card in _cards)
            card.SetHighlight(card.PlayerId == winner?.PlayerId);

        Debug.Log("[VotingHostPanel] Voting complete, winner highlighted.");
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

    // ── Inner helper ──────────────────────────────────────────────────────────

    /// <summary>
    /// Wraps a spawned card GameObject and holds references to its UI elements.
    /// Looks up child components by name — match these names in your prefab.
    /// </summary>
    private class HostDrawingCard
    {
        public GameObject         Root        { get; }
        public string             PlayerId    { get; }

        private readonly SolutionSubmission  _submission;
        private readonly RawImage            _image;
        private readonly TextMeshProUGUI     _nameLabel;
        private readonly TextMeshProUGUI     _descriptionLabel;
        private readonly TextMeshProUGUI     _voteLabel;
        private readonly Image               _highlight;

        public HostDrawingCard(GameObject root, SolutionSubmission submission)
        {
            Root        = root;
            PlayerId    = submission.PlayerId;
            _submission = submission;

            // Look up child components.
            // Name your prefab children to match these strings.
            _image            = root.GetComponentInChildren<RawImage>();
            _nameLabel        = FindLabel(root, "NameLabel");
            _descriptionLabel = FindLabel(root, "DescriptionLabel");
            _voteLabel        = FindLabel(root, "VoteLabel");
            _highlight        = root.GetComponent<Image>();

            Populate();
        }

        private void Populate()
        {
            if (_image != null)
                _image.texture = _submission.Texture;

            if (_nameLabel != null)
                _nameLabel.text = _submission.PlayerName;

            if (_descriptionLabel != null)
                _descriptionLabel.text = _submission.Description;

            RefreshVoteCount();
        }

        public void RefreshVoteCount()
        {
            if (_voteLabel != null)
                _voteLabel.text = _submission.VoteCount == 1
                    ? "1 vote"
                    : $"{_submission.VoteCount} votes";
        }

        public void SetHighlight(bool isWinner)
        {
            if (_highlight != null)
                _highlight.color = isWinner
                    ? new Color(0.78f, 0.53f, 0.23f, 0.4f)  // torch flame tint
                    : Color.white;
        }

        private static TextMeshProUGUI FindLabel(GameObject root, string childName)
        {
            Transform t = root.transform.Find(childName);
            return t != null ? t.GetComponent<TextMeshProUGUI>() : null;
        }
    }
}
