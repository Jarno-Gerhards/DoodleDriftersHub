using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Central manager for the voting phase.
///
/// Responsibilities:
///   1. Collects solution submissions from all players (via SolutionSubmitBridge).
///   2. Fires OnAllSubmissionsReceived when everyone has submitted → triggers voting UI.
///   3. Collects votes from all players, enforcing the "no self-vote" rule.
///   4. Fires OnAllVotesReceived when everyone has voted → triggers resolution.
///   5. Determines the winning submission via GetWinner().
///
/// Multiplayer note:
///   Currently all data lives locally. When multiplayer is added:
///   - RegisterSubmission() becomes an RPC so the host collects all client submissions.
///   - RegisterVote() becomes an RPC so the host collects all votes.
///   - The host then broadcasts the winner back to all clients.
///   The events and public API stay identical — only the transport layer changes.
///
/// Hook-up:
///   Place on any persistent GameObject in the scene (e.g. the same one as GameLoopManager).
///   VotingHostPanel and VotingClientPanel subscribe to the events automatically.
/// </summary>
public class VotingManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────

    public static VotingManager Instance { get; private set; }

    // ── Inspector ─────────────────────────────────────────────────────────────

    [Tooltip("Expected number of players. Set this when the room is created.")]
    [SerializeField] private int expectedPlayerCount = 4;

    // ── Events ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Fired when all players have submitted their solution.
    /// Carry the full list so the UI panels can populate themselves.
    /// </summary>
    public event Action<IReadOnlyList<SolutionSubmission>> OnAllSubmissionsReceived;

    /// <summary>
    /// Fired when all players have cast their vote.
    /// Carries the winning submission.
    /// </summary>
    public event Action<SolutionSubmission> OnAllVotesReceived;

    /// <summary>
    /// Fired every time a single vote comes in.
    /// Useful for showing a live vote-count update on the host screen.
    /// </summary>
    public event Action<int, int> OnVoteProgress; // (votesIn, totalExpected)

    // ── Public read-only state ────────────────────────────────────────────────

    public IReadOnlyList<SolutionSubmission> Submissions => _submissions;
    public int SubmissionCount => _submissions.Count;
    public int VoteCount       => _votes.Count;

    // ── Internal state ────────────────────────────────────────────────────────

    private readonly List<SolutionSubmission>    _submissions = new();
    private readonly Dictionary<string, string>  _votes       = new(); // voterId → chosenPlayerId

    private bool _votingOpen = false;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Call this at the start of each drawing phase to reset all state.
    /// Should be called by the DrawingState or GameLoopManager when entering Drawing.
    /// </summary>
    public void ResetForNewRound()
    {
        _submissions.Clear();
        _votes.Clear();
        _votingOpen = false;
        Debug.Log("[VotingManager] Reset for new round.");
    }

    /// <summary>
    /// Set the number of players before the round starts.
    /// When multiplayer is implemented, pass the connected player count here.
    /// </summary>
    public void SetExpectedPlayerCount(int count)
    {
        expectedPlayerCount = count;
        Debug.Log($"[VotingManager] Expecting {count} players.");
    }

    /// <summary>
    /// Called by SolutionSubmitBridge when a player submits their drawing.
    /// Duplicate submissions from the same player are ignored.
    /// </summary>
    public void RegisterSubmission(SolutionSubmission submission)
    {
        if (submission == null) return;

        // Prevent duplicate submissions from the same player.
        if (_submissions.Any(s => s.PlayerId == submission.PlayerId))
        {
            Debug.LogWarning($"[VotingManager] Duplicate submission from '{submission.PlayerId}' — ignored.");
            return;
        }

        _submissions.Add(submission);
        Debug.Log($"[VotingManager] Submission from '{submission.PlayerName}' " +
                  $"({_submissions.Count}/{expectedPlayerCount}).");

        if (_submissions.Count >= expectedPlayerCount)
            OpenVoting();
    }

    /// <summary>
    /// Called by VotingClientPanel when a player selects a drawing to vote for.
    /// Enforces: one vote per player, no self-voting.
    /// </summary>
    public void RegisterVote(string voterPlayerId, string chosenPlayerId)
    {
        if (!_votingOpen)
        {
            Debug.LogWarning("[VotingManager] Vote received but voting is not open yet.");
            return;
        }

        if (voterPlayerId == chosenPlayerId)
        {
            Debug.LogWarning($"[VotingManager] '{voterPlayerId}' tried to vote for themselves — ignored.");
            return;
        }

        if (_votes.ContainsKey(voterPlayerId))
        {
            Debug.LogWarning($"[VotingManager] '{voterPlayerId}' already voted — ignored.");
            return;
        }

        _votes[voterPlayerId] = chosenPlayerId;

        Debug.Log($"[VotingManager] '{voterPlayerId}' voted for '{chosenPlayerId}'. " +
                  $"({_votes.Count}/{expectedPlayerCount}).");

        OnVoteProgress?.Invoke(_votes.Count, expectedPlayerCount);

        if (_votes.Count >= expectedPlayerCount)
            FinalizeVoting();
    }

    /// <summary>
    /// Returns the submission with the most votes.
    /// In case of a tie, the first submission in the list wins (submission order).
    /// Returns null if there are no submissions.
    /// </summary>
    public SolutionSubmission GetWinner()
    {
        if (_submissions.Count == 0) return null;

        // Count votes per player.
        Dictionary<string, int> voteCounts = new();
        foreach (var submission in _submissions)
            voteCounts[submission.PlayerId] = 0;

        foreach (var chosenId in _votes.Values)
            if (voteCounts.ContainsKey(chosenId))
                voteCounts[chosenId]++;

        // Apply vote counts to submissions.
        foreach (var submission in _submissions)
            submission.VoteCount = voteCounts[submission.PlayerId];

        // Return the one with the highest vote count (stable: first in list wins ties).
        return _submissions.OrderByDescending(s => s.VoteCount).First();
    }

    /// <summary>
    /// Returns all submissions except the one belonging to the given player.
    /// Used by VotingClientPanel to hide the local player's own drawing.
    /// </summary>
    public IReadOnlyList<SolutionSubmission> GetSubmissionsExcluding(string playerId)
    {
        return _submissions.Where(s => s.PlayerId != playerId).ToList();
    }

    // ── Internal ──────────────────────────────────────────────────────────────

    /// <summary>Called when all submissions are in. Opens voting and notifies UI.</summary>
    private void OpenVoting()
    {
        _votingOpen = true;
        Debug.Log("[VotingManager] All submissions received — voting is open.");
        OnAllSubmissionsReceived?.Invoke(_submissions);
    }

    /// <summary>Called when all votes are in. Resolves winner and notifies systems.</summary>
    private void FinalizeVoting()
    {
        _votingOpen = false;
        SolutionSubmission winner = GetWinner();
        Debug.Log($"[VotingManager] Voting complete. Winner: '{winner?.PlayerName}' " +
                  $"with {winner?.VoteCount} vote(s).");
        OnAllVotesReceived?.Invoke(winner);
    }
}
