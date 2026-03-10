using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Main Host controller. Runs the game server, manages game flow,
/// and controls host UI. Equivalent to Server.js + host.js combined.
/// Attach to a GameObject in the Host scene.
/// </summary>
public class HostManager : MonoBehaviour
{
    [Header("References")]
    public HostUIManager UI;

    [Header("Config")]
    public GameConfig Config;

    // Server + Room
    private GameServer _server;
    private RoomManager _roomManager;
    private RoomData _currentRoom;

    // Connection tracking
    private Dictionary<int, string> _connectionToPlayer = new Dictionary<int, string>(); // connectionId -> playerId
    private Dictionary<string, int> _playerToConnection = new Dictionary<string, int>(); // playerId -> connectionId
    private int _hostConnectionId = -1;

    // Timers
    private Coroutine _activeTimer;
    private float _timerRemaining;

    // State
    private string _leaderName;

    private void Awake()
    {
        if (Config == null)
            Config = GameConfig.Instance;
    }

    private void Start()
    {
        _server = new GameServer();
        _roomManager = new RoomManager();

        _server.OnClientConnected += OnClientConnected;
        _server.OnClientDisconnected += OnClientDisconnected;
        _server.OnMessageReceived += OnMessageReceived;

        _server.Start(Config.ServerPort);

        // Create room automatically
        _currentRoom = _roomManager.CreateRoom();
        Debug.Log($"[Host] Room created: {_currentRoom.Id}");

        UI.ShowLobby(_currentRoom.Id, GetLocalIP());
    }

    private void Update()
    {
        _server?.ProcessMainThreadQueue();
    }

    private void OnDestroy()
    {
        _server?.Stop();
    }

    // ============================================================
    // Network Event Handlers
    // ============================================================

    private void OnClientConnected(int connectionId)
    {
        Debug.Log($"[Host] Client connected: {connectionId}");
    }

    private void OnClientDisconnected(int connectionId)
    {
        Debug.Log($"[Host] Client disconnected: {connectionId}");

        if (_connectionToPlayer.TryGetValue(connectionId, out string playerId))
        {
            _roomManager.SetPlayerConnected(_currentRoom.Id, playerId, false);
            _connectionToPlayer.Remove(connectionId);
            _playerToConnection.Remove(playerId);

            // Notify other players
            string json = JsonUtility.ToJson(new PlayerConnectionChangedMessage
            {
                playerId = playerId,
                connected = false
            });
            _server.Broadcast("playerConnectionChanged", json);

            // Update host UI
            UI.UpdatePlayerConnectionStatus(playerId, false);

            var player = _currentRoom.Players.ContainsKey(playerId) ? _currentRoom.Players[playerId] : null;
            if (player != null)
                GameLogger.Log($"Player {player.Name} disconnected");
        }
    }

    private void OnMessageReceived(int connectionId, string type, string dataJson)
    {
        switch (type)
        {
            case "joinRoom": HandleJoinRoom(connectionId, dataJson); break;
            case "startGame": HandleStartGame(connectionId, dataJson); break;
            case "submitDrawing": HandleSubmitDrawing(connectionId, dataJson); break;
            case "submitCarDrawing": HandleSubmitCarDrawing(connectionId, dataJson); break;
            case "submitVote": HandleSubmitVote(connectionId, dataJson); break;
            case "leaveRoom": HandleLeaveRoom(connectionId, dataJson); break;
            default:
                Debug.LogWarning($"[Host] Unknown message type: {type}");
                break;
        }
    }

    // ============================================================
    // Message Handlers (equivalent to Server.js socket handlers)
    // ============================================================

    private void HandleJoinRoom(int connectionId, string dataJson)
    {
        try
        {
            var request = JsonUtility.FromJson<JoinRoomRequest>(dataJson);

            if (string.IsNullOrEmpty(request.roomId) || string.IsNullOrEmpty(request.playerName))
                throw new Exception("Invalid join data");

            if (request.roomId != _currentRoom.Id)
                throw new Exception("Room not found");

            // Normalize name
            string normalizedName = NormalizeName(request.playerName);

            var playerData = new PlayerData(request.playerId, normalizedName, request.isHost);

            // Check max players
            if (!request.isHost)
            {
                int currentCount = _currentRoom.GetPlayerCount();
                if (currentCount >= Config.MaxPlayers && !_currentRoom.Players.ContainsKey(request.playerId))
                    throw new Exception("Room is full");
            }

            var (room, isReconnect) = _roomManager.JoinRoom(request.roomId, playerData);

            // Track connection
            _connectionToPlayer[connectionId] = request.playerId;
            _playerToConnection[request.playerId] = connectionId;

            if (request.isHost)
            {
                _hostConnectionId = connectionId;
            }

            // Send response
            var response = new JoinRoomResponse
            {
                success = true,
                isReconnect = isReconnect,
                roomStateJson = JsonUtility.ToJson(room.ToSnapshot())
            };
            _server.Send(connectionId, "joinRoomResponse", JsonUtility.ToJson(response));

            if (!request.isHost)
            {
                if (isReconnect)
                {
                    string reconnectJson = JsonUtility.ToJson(new PlayerConnectionChangedMessage
                    {
                        playerId = request.playerId,
                        connected = true
                    });
                    _server.Broadcast("playerConnectionChanged", reconnectJson);
                    UI.UpdatePlayerConnectionStatus(request.playerId, true);
                    GameLogger.Log($"Player {normalizedName} reconnected");
                }
                else if (Config.DrawOwnCar)
                {
                    _server.Send(connectionId, "drawCar",
                        JsonUtility.ToJson(new DrawCarMessage { message = "Draw your own car!" }));
                }
                else
                {
                    int playerCount = room.GetPlayerCount();
                    var joinedMsg = new PlayerJoinedMessage
                    {
                        id = request.playerId,
                        name = normalizedName,
                        carId = room.Players[request.playerId].CarId,
                        playerCount = playerCount,
                        minPlayers = Config.MinPlayers,
                        maxPlayers = Config.MaxPlayers
                    };
                    string joinedJson = JsonUtility.ToJson(joinedMsg);
                    _server.Broadcast("playerJoined", joinedJson);

                    // Update host UI
                    UI.AddPlayerToLobby(room.Players[request.playerId]);
                    UpdateLobbyStatus();

                    // Track leader name
                    if (playerCount == 1 || string.IsNullOrEmpty(_leaderName))
                        _leaderName = normalizedName;

                    GameLogger.Log($"Player {normalizedName} joined");
                }
            }
        }
        catch (Exception ex)
        {
            var response = new JoinRoomResponse { success = false, error = ex.Message };
            _server.Send(connectionId, "joinRoomResponse", JsonUtility.ToJson(response));
        }
    }

    private void HandleStartGame(int connectionId, string dataJson)
    {
        try
        {
            var request = JsonUtility.FromJson<StartGameRequest>(dataJson);

            // Verify leader
            string playerId = _connectionToPlayer.ContainsKey(connectionId)
                ? _connectionToPlayer[connectionId] : null;

            if (playerId == null) throw new Exception("Unknown player");

            var player = _currentRoom.Players.ContainsKey(playerId) ? _currentRoom.Players[playerId] : null;
            if (player == null || !player.IsLeader)
                throw new Exception("Only the VIP player can start the game");

            int playerCount = _currentRoom.GetPlayerCount();
            if (playerCount < Config.MinPlayers)
                throw new Exception($"Need at least {Config.MinPlayers} players (currently {playerCount})");

            if (_currentRoom.GameState.StartGame(_currentRoom.Players))
            {
                // Send gameStarted
                var startedMsg = new GameStartedMessage
                {
                    round = _currentRoom.GameState.Round,
                    status = _currentRoom.GameState.Status.ToString().ToLower(),
                    positionsJson = SerializePositions(_currentRoom.GameState.Positions)
                };
                _server.Broadcast("gameStarted", JsonUtility.ToJson(startedMsg));

                // Send roundStarted
                var roundMsg = new RoundStartedMessage
                {
                    round = _currentRoom.GameState.Round,
                    positionsJson = SerializePositions(_currentRoom.GameState.Positions)
                };
                _server.Broadcast("roundStarted", JsonUtility.ToJson(roundMsg));

                // Start drawing timer
                StartDrawingTimer();

                // Update host UI
                UI.ShowDrawingPhase(_currentRoom.GameState.Round);

                GameLogger.Log("Game started");
                SendResponse(connectionId, true);
            }
            else
            {
                throw new Exception("Could not start game");
            }
        }
        catch (Exception ex)
        {
            SendResponse(connectionId, false, ex.Message);
        }
    }

    private void HandleSubmitDrawing(int connectionId, string dataJson)
    {
        try
        {
            var request = JsonUtility.FromJson<SubmitDrawingRequest>(dataJson);
            
            if (!_currentRoom.Players.TryGetValue(request.playerId, out var player))
                throw new Exception("Player not found");

            // Security check
            if (!_connectionToPlayer.TryGetValue(connectionId, out var connPlayerId) || connPlayerId != request.playerId)
                throw new Exception("Unauthorized submission");

            SendResponse(connectionId, true);

            // Analyze image with AI
            OpenAIService.Instance.AnalyzeDrawing(request.drawingBase64, (itemLabel) =>
            {
                GameLogger.Log($"Analyzed image for {player.Name}: {itemLabel}");

                if (_currentRoom.GameState.SubmitDrawing(request.playerId, request.drawingBase64, itemLabel))
                {
                    // Notify host UI
                    UI.UpdatePlayerStatus(request.playerId, "ready");

                    // Check if all submitted
                    int activePlayers = _currentRoom.Players.Values.Count(p => !p.IsHost);
                    if (_currentRoom.GameState.CurrentDrawings.Count >= activePlayers)
                    {
                        StopTimer();
                        _server.Broadcast("timerStop", "{}");
                        _server.Broadcast("allDrawingsSubmitted", "{}");
                        // Auto-advance to voting
                        AdvanceToVoting();
                    }
                }
            });
        }
        catch (Exception ex)
        {
            SendResponse(connectionId, false, ex.Message);
        }
    }

    private void HandleSubmitCarDrawing(int connectionId, string dataJson)
    {
        try
        {
            var request = JsonUtility.FromJson<SubmitCarDrawingRequest>(dataJson);

            if (!_currentRoom.Players.TryGetValue(request.playerId, out var player))
                throw new Exception("Player not found");

            if (!_connectionToPlayer.TryGetValue(connectionId, out var connPlayerId) || connPlayerId != request.playerId)
                throw new Exception("Unauthorized submission");

            player.CarDrawingBase64 = request.drawingBase64;

            int playerCount = _currentRoom.GetPlayerCount();
            var joinedMsg = new PlayerJoinedMessage
            {
                id = request.playerId,
                name = player.Name,
                carId = player.CarId,
                playerCount = playerCount,
                minPlayers = Config.MinPlayers,
                maxPlayers = Config.MaxPlayers,
                carDrawingBase64 = request.drawingBase64
            };
            _server.Broadcast("playerJoined", JsonUtility.ToJson(joinedMsg));
            _server.Send(connectionId, "carReceived", JsonUtility.ToJson(new GenericResponse { success = true }));

            UI.AddPlayerToLobby(player);
            UpdateLobbyStatus();
        }
        catch (Exception ex)
        {
            SendResponse(connectionId, false, ex.Message);
        }
    }

    private void HandleSubmitVote(int connectionId, string dataJson)
    {
        try
        {
            var request = JsonUtility.FromJson<SubmitVoteRequest>(dataJson);

            // Security check
            if (!_connectionToPlayer.TryGetValue(connectionId, out var connPlayerId) || connPlayerId != request.voterId)
                throw new Exception("Unauthorized vote");

            if (_currentRoom.GameState.SubmitVote(request.voterId, request.targetPlayerId))
            {
                SendResponse(connectionId, true);

                int activeVoters = _currentRoom.Players.Values.Count(p => !p.IsHost);
                int voteCount = _currentRoom.GameState.Voters.Count;

                // Broadcast vote update
                var votedMsg = new PlayerVotedMessage
                {
                    votedCount = voteCount,
                    totalVoters = activeVoters
                };
                _server.Broadcast("playerVoted", JsonUtility.ToJson(votedMsg));
                UI.UpdateVotingStatus(activeVoters, voteCount);

                if (voteCount >= activeVoters)
                {
                    StopTimer();
                    _server.Broadcast("timerStop", "{}");
                    _server.Broadcast("allVotesSubmitted", "{}");

                    StartCoroutine(ProcessVotingResults());
                }
            }
            else
            {
                SendResponse(connectionId, false, "Failed to submit vote");
            }
        }
        catch (Exception ex)
        {
            SendResponse(connectionId, false, ex.Message);
        }
    }

    private void HandleLeaveRoom(int connectionId, string dataJson)
    {
        try
        {
            var request = JsonUtility.FromJson<LeaveRoomRequest>(dataJson);

            if (!_connectionToPlayer.TryGetValue(connectionId, out var connPlayerId) || connPlayerId != request.playerId)
                throw new Exception("Unauthorized");

            var result = _roomManager.LeaveRoom(request.roomId, request.playerId);

            if (result.success)
            {
                _connectionToPlayer.Remove(connectionId);
                _playerToConnection.Remove(request.playerId);

                int playerCount = _currentRoom.GetPlayerCount();
                var leftMsg = new PlayerLeftMessage
                {
                    playerId = request.playerId,
                    playerName = result.playerName,
                    newLeaderId = result.newLeaderId,
                    playerCount = playerCount,
                    minPlayers = Config.MinPlayers,
                    maxPlayers = Config.MaxPlayers
                };
                _server.Broadcast("playerLeft", JsonUtility.ToJson(leftMsg));

                UI.RemovePlayerFromLobby(request.playerId);
                UpdateLobbyStatus();

                GameLogger.Log($"Player {result.playerName} left");
                SendResponse(connectionId, true);
            }
            else
            {
                SendResponse(connectionId, false, result.error);
            }
        }
        catch (Exception ex)
        {
            SendResponse(connectionId, false, ex.Message);
        }
    }

    // ============================================================
    // Game Flow
    // ============================================================

    private void AdvanceToVoting()
    {
        var (newPhase, _) = _currentRoom.GameState.NextPhase(_currentRoom.Players);

        // Randomize drawings order
        var drawings = _currentRoom.GameState.CurrentDrawings.Values.ToList();
        ShuffleList(drawings);

        string drawingsJson = SerializeDrawingsList(drawings);

        var updateMsg = new UpdateStateMessage
        {
            status = "voting",
            round = _currentRoom.GameState.Round,
            drawingsJson = drawingsJson,
            votedCount = 0
        };
        _server.Broadcast("updateState", JsonUtility.ToJson(updateMsg));

        // Start voting timer
        StartVotingTimer();

        // Update host UI
        UI.ShowVotingPhase(drawings);
    }

    /// <summary>
    /// Called when nextPhase is triggered by the host (auto-advance from racing or manual).
    /// </summary>
    public void AdvancePhase()
    {
        var (newPhase, votingResults) = _currentRoom.GameState.NextPhase(_currentRoom.Players);

        if (votingResults != null)
        {
            LogVotingResults(votingResults);
        }

        var updateMsg = new UpdateStateMessage
        {
            status = newPhase.ToString().ToLower(),
            round = _currentRoom.GameState.Round
        };

        switch (newPhase)
        {
            case GameState.Phase.Voting:
                StopTimer();
                var drawings = _currentRoom.GameState.CurrentDrawings.Values.ToList();
                ShuffleList(drawings);
                updateMsg.drawingsJson = SerializeDrawingsList(drawings);
                updateMsg.votedCount = _currentRoom.GameState.Voters.Count;

                _server.Broadcast("updateState", JsonUtility.ToJson(updateMsg));
                StartVotingTimer();
                UI.ShowVotingPhase(drawings);
                break;

            case GameState.Phase.Drawing:
                _server.Broadcast("roundStarted", JsonUtility.ToJson(new RoundStartedMessage
                {
                    round = _currentRoom.GameState.Round
                }));
                StartDrawingTimer();
                UI.ShowDrawingPhase(_currentRoom.GameState.Round);
                break;

            case GameState.Phase.Racing:
                updateMsg.positionsJson = SerializePositions(_currentRoom.GameState.Positions);
                StartCoroutine(GenerateNarrativeAndBroadcast(updateMsg));
                break;

            case GameState.Phase.Finished:
                updateMsg.positionsJson = SerializePositions(_currentRoom.GameState.Positions);
                _server.Broadcast("updateState", JsonUtility.ToJson(updateMsg));
                UI.ShowGameOver(_currentRoom.GameState.Positions);
                GameLogger.Log("Game finished");
                break;

            default:
                _server.Broadcast("updateState", JsonUtility.ToJson(updateMsg));
                break;
        }
    }

    private IEnumerator ProcessVotingResults()
    {
        // Calculate scores
        var votingResults = _currentRoom.GameState.CalculateScores();
        _currentRoom.GameState.SnapshotPositions(_currentRoom.Players);
        LogVotingResults(votingResults);

        // Show vote results
        var playerNames = new Dictionary<string, string>();
        foreach (var p in _currentRoom.Players.Values.Where(p => !p.IsHost))
            playerNames[p.Id] = p.Name;

        UI.ShowVoteResults(votingResults, playerNames);

        // Broadcast vote results
        var voteResultsMsg = new UpdateStateMessage
        {
            status = "vote_results",
            votingResultsJson = SerializeVotingResults(votingResults),
            playerNamesJson = SerializePlayerNames(playerNames)
        };
        _server.Broadcast("updateState", JsonUtility.ToJson(voteResultsMsg));

        // Wait for display time
        yield return new WaitForSeconds(Config.VoteResultsDisplayTime);

        // Generate narrative
        bool narrativeDone = false;
        var context = BuildNarrativeContext();
        OpenAIService.Instance.GenerateNarrative(context, (narrative) =>
        {
            narrativeDone = true;
        });

        // Wait for narrative
        while (!narrativeDone)
            yield return null;

        // Transition to racing
        _currentRoom.GameState.Status = GameState.Phase.Racing;

        var updateMsg = new UpdateStateMessage
        {
            status = "racing",
            round = _currentRoom.GameState.Round,
            positionsJson = SerializePositions(_currentRoom.GameState.Positions),
            historyJson = SerializeHistory(_currentRoom.GameState.History)
        };
        _server.Broadcast("updateState", JsonUtility.ToJson(updateMsg));

        // Update host UI
        string narrative = _currentRoom.GameState.History.Count > 0
            ? _currentRoom.GameState.History[_currentRoom.GameState.History.Count - 1]
            : "The race continues!";
        var positions = _currentRoom.GameState.Positions.Count > 0
            ? _currentRoom.GameState.Positions[_currentRoom.GameState.Positions.Count - 1]
            : new List<PlayerPositionSnapshot>();

        UI.ShowNarrativePhase(narrative, positions);

        // Auto-advance after delay
        yield return new WaitForSeconds(Config.RacingAutoAdvanceDelay);

        if (_currentRoom.GameState.Status != GameState.Phase.Finished)
        {
            AdvancePhase();
        }
    }

    private IEnumerator GenerateNarrativeAndBroadcast(UpdateStateMessage updateMsg)
    {
        bool done = false;
        var context = BuildNarrativeContext();
        OpenAIService.Instance.GenerateNarrative(context, (narrative) =>
        {
            done = true;
        });

        while (!done) yield return null;

        updateMsg.historyJson = SerializeHistory(_currentRoom.GameState.History);
        _server.Broadcast("updateState", JsonUtility.ToJson(updateMsg));

        string narrativeText = _currentRoom.GameState.History.Count > 0
            ? _currentRoom.GameState.History[_currentRoom.GameState.History.Count - 1]
            : "The race continues!";
        var positions = _currentRoom.GameState.Positions.Count > 0
            ? _currentRoom.GameState.Positions[_currentRoom.GameState.Positions.Count - 1]
            : new List<PlayerPositionSnapshot>();

        UI.ShowNarrativePhase(narrativeText, positions);

        yield return new WaitForSeconds(Config.RacingAutoAdvanceDelay);

        if (_currentRoom.GameState.Status != GameState.Phase.Finished)
            AdvancePhase();
    }

    // ============================================================
    // Timers
    // ============================================================

    private void StartDrawingTimer()
    {
        StopTimer();
        var timerMsg = new TimerStartMessage { phase = "drawing", seconds = Config.DrawingTimerSeconds };
        _server.Broadcast("timerStart", JsonUtility.ToJson(timerMsg));
        _activeTimer = StartCoroutine(RunTimer(Config.DrawingTimerSeconds, OnDrawingTimerExpired));
        GameLogger.Log($"Drawing timer started: {Config.DrawingTimerSeconds}s");
    }

    private void StartVotingTimer()
    {
        StopTimer();
        var timerMsg = new TimerStartMessage { phase = "voting", seconds = Config.VotingTimerSeconds };
        _server.Broadcast("timerStart", JsonUtility.ToJson(timerMsg));
        _activeTimer = StartCoroutine(RunTimer(Config.VotingTimerSeconds, OnVotingTimerExpired));
        GameLogger.Log($"Voting timer started: {Config.VotingTimerSeconds}s");
    }

    private void StopTimer()
    {
        if (_activeTimer != null)
        {
            StopCoroutine(_activeTimer);
            _activeTimer = null;
        }
    }

    private IEnumerator RunTimer(float seconds, Action onExpired)
    {
        _timerRemaining = seconds;
        while (_timerRemaining > 0)
        {
            yield return new WaitForSeconds(1f);
            _timerRemaining--;
        }
        _activeTimer = null;
        onExpired?.Invoke();
    }

    private void OnDrawingTimerExpired()
    {
        GameLogger.Log("Drawing timer expired");
        if (_currentRoom.GameState.Status == GameState.Phase.Drawing)
        {
            AdvanceToVoting();
        }
    }

    private void OnVotingTimerExpired()
    {
        GameLogger.Log("Voting timer expired");
        if (_currentRoom.GameState.Status == GameState.Phase.Voting)
        {
            _server.Broadcast("timerStop", "{}");
            StartCoroutine(ProcessVotingResults());
        }
    }

    // ============================================================
    // Helpers
    // ============================================================

    private NarrativeContext BuildNarrativeContext()
    {
        return new NarrativeContext
        {
            History = _currentRoom.GameState.History,
            Players = _currentRoom.Players.Values.ToList(),
            Positions = _currentRoom.GameState.Positions,
            Finished = _currentRoom.GameState.Round >= _currentRoom.GameState.MaxRounds,
            CurrentRound = _currentRoom.GameState.Round,
            TotalRounds = _currentRoom.GameState.MaxRounds
        };
    }

    private void UpdateLobbyStatus()
    {
        int playerCount = _currentRoom.GetPlayerCount();
        UI.UpdateLobbyStatus(playerCount, Config.MinPlayers, Config.MaxPlayers, _leaderName);
    }

    private void LogVotingResults(List<VotingResult> results)
    {
        GameLogger.Log("=== VOTING RESULTS ===");
        foreach (var r in results)
        {
            string name = _currentRoom.Players.ContainsKey(r.PlayerId)
                ? _currentRoom.Players[r.PlayerId].Name : r.PlayerId;
            GameLogger.Log($"{name}: {r.VotesReceived} votes, score {r.PreviousScore} -> {r.NewScore}");
        }
    }

    private void SendResponse(int connectionId, bool success, string error = null)
    {
        var response = new GenericResponse { success = success, error = error };
        _server.Send(connectionId, "response", JsonUtility.ToJson(response));
    }

    private string NormalizeName(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        string trimmed = name.Trim();
        if (trimmed.Length == 0) return trimmed;
        return char.ToUpper(trimmed[0]) + trimmed.Substring(1).ToLower();
    }

    private static void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private string GetLocalIP()
    {
        try
        {
            var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    return ip.ToString();
            }
        }
        catch { }
        return "localhost";
    }

    // ============================================================
    // Serialization helpers (simple JSON arrays/objects)
    // ============================================================

    private string SerializePositions(List<List<PlayerPositionSnapshot>> positions)
    {
        // Serialize as JSON array of arrays
        var parts = new List<string>();
        foreach (var round in positions)
        {
            var playerParts = new List<string>();
            foreach (var p in round)
                playerParts.Add(JsonUtility.ToJson(p));
            parts.Add("[" + string.Join(",", playerParts) + "]");
        }
        return "[" + string.Join(",", parts) + "]";
    }

    private string SerializeDrawingsList(List<DrawingData> drawings)
    {
        var parts = new List<string>();
        foreach (var d in drawings)
            parts.Add(JsonUtility.ToJson(d));
        return "[" + string.Join(",", parts) + "]";
    }

    private string SerializeVotingResults(List<VotingResult> results)
    {
        var parts = new List<string>();
        foreach (var r in results)
            parts.Add(JsonUtility.ToJson(r));
        return "[" + string.Join(",", parts) + "]";
    }

    private string SerializeHistory(List<string> history)
    {
        var escaped = history.Select(h => $"\"{EscapeJson(h)}\"");
        return "[" + string.Join(",", escaped) + "]";
    }

    private string SerializePlayerNames(Dictionary<string, string> names)
    {
        var parts = names.Select(kv => $"\"{EscapeJson(kv.Key)}\":\"{EscapeJson(kv.Value)}\"");
        return "{" + string.Join(",", parts) + "}";
    }

    private string EscapeJson(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
    }
}
