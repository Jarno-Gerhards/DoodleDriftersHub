using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Game state machine. Manages rounds, phases, drawings, voting, and scoring.
/// Equivalent to GameState.js.
/// </summary>
public class GameState
{
    public enum Phase
    {
        Waiting,
        Drawing,
        Voting,
        VoteResults,
        Racing,
        Finished
    }

    public int Round { get; private set; }
    public int MaxRounds { get; private set; }
    public Phase Status { get; set; }
    public List<string> History { get; private set; } // narrative history
    public Dictionary<string, DrawingData> CurrentDrawings { get; private set; } // playerId -> DrawingData
    public Dictionary<string, int> CurrentVotes { get; private set; } // playerId -> voteCount
    public HashSet<string> Voters { get; private set; } // playerIds who voted
    public Dictionary<string, int> PlayerScores { get; private set; } // playerId -> score
    public List<List<PlayerPositionSnapshot>> Positions { get; private set; } // per-round snapshots
    public Dictionary<string, string> PlayerItems { get; private set; } // playerId -> currentItemName

    private int _scoresCalculatedForRound;
    private int _positionsSnapshotForRound;

    public GameState()
    {
        Round = 0;
        MaxRounds = GameConfig.Instance.MaxRounds;
        Status = Phase.Waiting;
        History = new List<string>();
        CurrentDrawings = new Dictionary<string, DrawingData>();
        CurrentVotes = new Dictionary<string, int>();
        Voters = new HashSet<string>();
        PlayerScores = new Dictionary<string, int>();
        Positions = new List<List<PlayerPositionSnapshot>>();
        PlayerItems = new Dictionary<string, string>();
        _scoresCalculatedForRound = 0;
        _positionsSnapshotForRound = 0;
    }

    public bool StartGame(Dictionary<string, PlayerData> players)
    {
        if (Status != Phase.Waiting) return false;

        Status = Phase.Drawing;
        Round = 1;

        // Initialize scores
        foreach (var p in players.Values)
        {
            if (!p.IsHost)
                PlayerScores[p.Id] = 0;
        }

        // Initial positions snapshot
        SnapshotPositions(players, true);
        return true;
    }

    /// <summary>
    /// Advances to the next game phase. Returns voting results if transitioning from voting.
    /// </summary>
    public (Phase newPhase, List<VotingResult> votingResults) NextPhase(Dictionary<string, PlayerData> players)
    {
        List<VotingResult> votingResults = null;

        switch (Status)
        {
            case Phase.Drawing:
                Status = Phase.Voting;
                break;
            case Phase.Voting:
                votingResults = CalculateScores();
                SnapshotPositions(players);
                Status = Phase.Racing;
                break;
            case Phase.Racing:
                if (Round >= MaxRounds)
                {
                    Status = Phase.Finished;
                }
                else
                {
                    Round++;
                    Status = Phase.Drawing;
                    ResetRoundData();
                }
                break;
        }

        return (Status, votingResults);
    }

    public void ResetRoundData()
    {
        CurrentDrawings.Clear();
        CurrentVotes.Clear();
        Voters.Clear();
        PlayerItems.Clear();
    }

    public bool SubmitDrawing(string playerId, string drawingBase64, string itemLabel = "Unknown Object")
    {
        if (Status != Phase.Drawing) return false;
        CurrentDrawings[playerId] = new DrawingData(playerId, drawingBase64, itemLabel);
        PlayerItems[playerId] = itemLabel;
        return true;
    }

    public bool SubmitVote(string voterId, string targetPlayerId)
    {
        if (Status != Phase.Voting) return false;
        if (voterId == targetPlayerId) return false; // No self-voting
        if (Voters.Contains(voterId)) return false; // Already voted

        Voters.Add(voterId);
        if (!CurrentVotes.ContainsKey(targetPlayerId))
            CurrentVotes[targetPlayerId] = 0;
        CurrentVotes[targetPlayerId]++;
        return true;
    }

    public List<VotingResult> CalculateScores()
    {
        // Prevent double calculation
        if (_scoresCalculatedForRound >= Round)
        {
            var cached = new List<VotingResult>();
            foreach (var kv in PlayerScores)
            {
                int votes = CurrentVotes.ContainsKey(kv.Key) ? CurrentVotes[kv.Key] : 0;
                cached.Add(new VotingResult
                {
                    PlayerId = kv.Key,
                    VotesReceived = votes,
                    PreviousScore = kv.Value - votes,
                    NewScore = kv.Value
                });
            }
            return cached;
        }

        var results = new List<VotingResult>();

        foreach (var kv in CurrentVotes)
        {
            int currentScore = PlayerScores.ContainsKey(kv.Key) ? PlayerScores[kv.Key] : 0;
            int newScore = currentScore + kv.Value;
            PlayerScores[kv.Key] = newScore;

            results.Add(new VotingResult
            {
                PlayerId = kv.Key,
                VotesReceived = kv.Value,
                PreviousScore = currentScore,
                NewScore = newScore
            });
        }

        // Include players with 0 votes
        foreach (var kv in PlayerScores)
        {
            if (!CurrentVotes.ContainsKey(kv.Key))
            {
                results.Add(new VotingResult
                {
                    PlayerId = kv.Key,
                    VotesReceived = 0,
                    PreviousScore = kv.Value,
                    NewScore = kv.Value
                });
            }
        }

        _scoresCalculatedForRound = Round;
        return results;
    }

    public void SnapshotPositions(Dictionary<string, PlayerData> players, bool isInitial = false)
    {
        if (!isInitial && _positionsSnapshotForRound >= Round)
            return;

        var snapshot = new List<PlayerPositionSnapshot>();

        foreach (var p in players.Values)
        {
            if (p.IsHost) continue;

            int score = PlayerScores.ContainsKey(p.Id) ? PlayerScores[p.Id] : 0;
            string item = PlayerItems.ContainsKey(p.Id) ? PlayerItems[p.Id] : "nothing";
            int votes = CurrentVotes.ContainsKey(p.Id) ? CurrentVotes[p.Id] : 0;

            snapshot.Add(new PlayerPositionSnapshot(p, score, item, votes));
        }

        // Sort by score descending
        snapshot.Sort((a, b) => b.Score.CompareTo(a.Score));
        Positions.Add(snapshot);

        if (!isInitial)
            _positionsSnapshotForRound = Round;
    }

    /// <summary>
    /// Serializable state for sending to clients.
    /// </summary>
    public GameStateSnapshot ToSnapshot()
    {
        return new GameStateSnapshot
        {
            Round = this.Round,
            MaxRounds = this.MaxRounds,
            Status = this.Status.ToString().ToLower(),
            History = this.History,
            Positions = this.Positions,
            PlayerScores = new Dictionary<string, int>(this.PlayerScores)
        };
    }
}

/// <summary>
/// Serializable snapshot of game state for network transmission.
/// </summary>
[Serializable]
public class GameStateSnapshot
{
    public int Round;
    public int MaxRounds;
    public string Status;
    public List<string> History;
    public List<List<PlayerPositionSnapshot>> Positions;
    public Dictionary<string, int> PlayerScores;
}
