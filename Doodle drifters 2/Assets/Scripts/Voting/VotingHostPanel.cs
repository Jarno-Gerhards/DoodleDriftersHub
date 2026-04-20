using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Host screen panel for the voting phase (UI Toolkit version).
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
/// UIDocument requirement:
///   - Root must contain either:
///       * left + right page containers (preferred book layout)
///       * or one fallback card container
///   - Root must contain a status Label named by statusLabelName
///
/// Card template requirement (VisualTreeAsset):
///   - VisualElement named "drawing-image"
///   - Label named "NameLabel"
///   - Label named "DescriptionLabel"
///   - Label named "VoteLabel"
/// </summary>
public class VotingHostPanel : MonoBehaviour
{
    [Header("UI Toolkit")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private VisualTreeAsset drawingCardTemplate;

    [SerializeField] private string leftPageContainerName = "left-page-grid";
    [SerializeField] private string rightPageContainerName = "right-page-grid";
    [SerializeField] private string fallbackCardContainerName = "card-container";
    [SerializeField] private string statusLabelName = "status-label";
    [SerializeField, Min(1)] private int maxCards = 8;
    [SerializeField, Min(1)] private int maxCardsPerPage = 4;

    private VisualElement _leftPageContainer;
    private VisualElement _rightPageContainer;
    private VisualElement _fallbackCardContainer;
    private Label _statusLabel;

    // Runtime list of spawned cards so we can update vote counts live.
    private readonly List<HostDrawingCard> _cards = new();

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (uiDocument == null)
        {
            uiDocument = GetComponent<UIDocument>();
        }

        if (uiDocument == null)
        {
            Debug.LogError("[VotingHostPanel] UIDocument is not assigned.");
            return;
        }

        VisualElement root = uiDocument.rootVisualElement;
        _leftPageContainer = root.Q<VisualElement>(leftPageContainerName);
        _rightPageContainer = root.Q<VisualElement>(rightPageContainerName);
        _fallbackCardContainer = root.Q<VisualElement>(fallbackCardContainerName);
        _statusLabel = root.Q<Label>(statusLabelName);

        if (_leftPageContainer == null || _rightPageContainer == null)
        {
            if (_fallbackCardContainer == null)
            {
                Debug.LogError(
                    $"[VotingHostPanel] Could not find two-page containers " +
                    $"('{leftPageContainerName}', '{rightPageContainerName}') " +
                    $"or fallback container '{fallbackCardContainerName}'."
                );
            }
            else
            {
                Debug.LogWarning(
                    $"[VotingHostPanel] Two-page containers not found. " +
                    $"Using fallback container '{fallbackCardContainerName}'."
                );
            }
        }

        if (_statusLabel == null)
        {
            Debug.LogWarning($"[VotingHostPanel] Could not find status label '{statusLabelName}'.");
        }
    }

    private void OnEnable()
    {
        if (VotingManager.Instance == null) return;
        VotingManager.Instance.OnAllSubmissionsReceived += PopulateCards;
        VotingManager.Instance.OnVoteProgress += UpdateVoteProgress;
        VotingManager.Instance.OnAllVotesReceived += OnVotingComplete;
    }

    private void OnDisable()
    {
        if (VotingManager.Instance == null) return;
        VotingManager.Instance.OnAllSubmissionsReceived -= PopulateCards;
        VotingManager.Instance.OnVoteProgress -= UpdateVoteProgress;
        VotingManager.Instance.OnAllVotesReceived -= OnVotingComplete;
    }

    // ── Internal ──────────────────────────────────────────────────────────────

    /// <summary>Spawns one card per submission when all drawings are in.</summary>
    private void PopulateCards(IReadOnlyList<SolutionSubmission> submissions)
    {
        ClearCards();

        if (!HasAnyContainer())
        {
            Debug.LogError("[VotingHostPanel] Card container is not available.");
            return;
        }

        if (drawingCardTemplate == null)
        {
            Debug.LogError("[VotingHostPanel] drawingCardTemplate is not assigned.");
            return;
        }

        int drawCount = Mathf.Min(submissions.Count, maxCards);

        for (int i = 0; i < drawCount; i++)
        {
            SolutionSubmission submission = submissions[i];
            TemplateContainer cardRoot = drawingCardTemplate.CloneTree();

            VisualElement targetContainer = ResolveContainerForIndex(i);
            if (targetContainer == null)
            {
                Debug.LogError("[VotingHostPanel] Could not resolve target page container.");
                break;
            }

            targetContainer.Add(cardRoot);

            var card = new HostDrawingCard(cardRoot, submission);
            _cards.Add(card);
        }

        if (submissions.Count > maxCards)
        {
            Debug.LogWarning($"[VotingHostPanel] Received {submissions.Count} submissions, " +
                             $"showing first {maxCards}.");
        }

        SetStatus($"Players are voting... (0/{drawCount})");
        Debug.Log($"[VotingHostPanel] Populated {drawCount} cards.");
    }

    /// <summary>Updates the status label as votes come in.</summary>
    private void UpdateVoteProgress(int votesIn, int totalExpected)
    {
        SetStatus($"Players are voting... ({votesIn}/{totalExpected})");

        foreach (var card in _cards)
            card.RefreshVoteCount();
    }

    /// <summary>Called when all votes are counted and a winner is determined.</summary>
    private void OnVotingComplete(SolutionSubmission winner)
    {
        SetStatus($"Voting complete! Winner: {winner?.PlayerName}");

        foreach (var card in _cards)
            card.SetHighlight(card.PlayerId == winner?.PlayerId);

        Debug.Log("[VotingHostPanel] Voting complete, winner highlighted.");
    }

    private void SetStatus(string message)
    {
        if (_statusLabel != null)
            _statusLabel.text = message;
    }

    private void ClearCards()
    {
        _leftPageContainer?.Clear();
        _rightPageContainer?.Clear();
        _fallbackCardContainer?.Clear();
        _cards.Clear();
    }

    private bool HasAnyContainer()
    {
        return (_leftPageContainer != null && _rightPageContainer != null) ||
               _fallbackCardContainer != null;
    }

    private VisualElement ResolveContainerForIndex(int index)
    {
        if (_leftPageContainer != null && _rightPageContainer != null)
        {
            return index < maxCardsPerPage ? _leftPageContainer : _rightPageContainer;
        }

        return _fallbackCardContainer;
    }

    // ── Inner helper ──────────────────────────────────────────────────────────

    private class HostDrawingCard
    {
        public VisualElement Root { get; }
        public string PlayerId { get; }

        private readonly SolutionSubmission _submission;
        private readonly VisualElement _image;
        private readonly Label _nameLabel;
        private readonly Label _descriptionLabel;
        private readonly Label _voteLabel;

        public HostDrawingCard(VisualElement root, SolutionSubmission submission)
        {
            Root = root;
            PlayerId = submission.PlayerId;
            _submission = submission;

            _image = root.Q<VisualElement>("drawing-image");
            _nameLabel = root.Q<Label>("NameLabel");
            _descriptionLabel = root.Q<Label>("DescriptionLabel");
            _voteLabel = root.Q<Label>("VoteLabel");

            Populate();
        }

        private void Populate()
        {
            if (_image != null && _submission.Texture != null)
                _image.style.backgroundImage = new StyleBackground(_submission.Texture);

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
            Root.style.backgroundColor = isWinner
                ? new StyleColor(new Color(0.78f, 0.53f, 0.23f, 0.4f))
                : StyleKeyword.Null;
        }
    }
}
