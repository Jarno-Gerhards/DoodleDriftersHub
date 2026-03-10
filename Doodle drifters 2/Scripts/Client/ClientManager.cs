using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Main client controller for the mobile/player build.
/// Connects to the Host's GameServer and handles all game phases.
/// Equivalent to client.js.
/// Attach to a GameObject in the Client scene.
/// </summary>
public class ClientManager : MonoBehaviour
{
    [Header("References")]
    public ClientUIManager UI;
    public DrawingCanvas DrawingCanvas;

    [Header("Connection")]
    public string ServerHost = "localhost";
    public int ServerPort = 7777;

    // Network
    private GameNetworkClient _client;

    // State
    private string _roomId;
    private string _playerName;
    private string _playerId;
    private bool _gameStarted;
    private bool _isLeader;
    private int _playerCount;
    private int _minPlayers;
    private int _maxPlayers;
    private string _currentPhase = "waiting";
    private string _drawingEndpoint = "submitCarDrawing";
    private bool _useWhiteBackground;

    // Timer
    private float _timerRemaining;
    private bool _timerRunning;
    private string _timerPhase;

    // Voting
    private List<DrawingData> _currentVotingDrawings;

    private void Start()
    {
        _playerId = LoadOrGeneratePlayerId();

        _client = new GameNetworkClient();
        _client.OnConnected += OnConnected;
        _client.OnDisconnected += OnDisconnected;
        _client.OnMessageReceived += OnMessageReceived;

        UI.ShowView("login");
        UI.SetupEvents(this);
    }

    private void Update()
    {
        _client?.ProcessMainThreadQueue();

        // Update timer display
        if (_timerRunning)
        {
            _timerRemaining -= Time.deltaTime;
            if (_timerRemaining <= 0)
            {
                _timerRunning = false;
                _timerRemaining = 0;
            }
            UI.UpdateTimerDisplay(_timerRemaining, _timerPhase);
        }
    }

    private void OnDestroy()
    {
        _client?.Disconnect();
    }

    // ============================================================
    // Public API (called by UI)
    // ============================================================

    public void ConnectAndJoin(string serverAddress, string roomId, string playerName)
    {
        // Parse host:port from serverAddress
        string host = serverAddress;
        int port = ServerPort;

        if (serverAddress.Contains(":"))
        {
            var parts = serverAddress.Split(':');
            host = parts[0];
            if (parts.Length > 1 && int.TryParse(parts[1], out int parsedPort))
                port = parsedPort;
        }

        _roomId = roomId;
        _playerName = playerName;
        ServerHost = host;
        ServerPort = port;

        UI.ShowLoading();
        _client.Connect(host, port);
    }

    public void JoinRoom()
    {
        var request = new JoinRoomRequest
        {
            roomId = _roomId,
            playerId = _playerId,
            playerName = _playerName,
            isHost = false
        };
        _client.Send("joinRoom", JsonUtility.ToJson(request));
    }

    public void StartGame()
    {
        var request = new StartGameRequest { roomId = _roomId };
        _client.Send("startGame", JsonUtility.ToJson(request));
    }

    public void SubmitDrawing()
    {
        string imageBase64 = DrawingCanvas.ExportImage(_useWhiteBackground);

        UI.SetSubmitButtonState(false, "Sending...");
        DrawingCanvas.SetInteractable(false);

        var request = new SubmitDrawingRequest
        {
            roomId = _roomId,
            playerId = _playerId,
            drawingBase64 = imageBase64
        };
        _client.Send(_drawingEndpoint, JsonUtility.ToJson(request));

        // Transition to waiting
        UI.ShowView("waiting");
        UI.SetWaitingMessage("Drawing submitted! Waiting for others...");
    }

    public void SubmitVote(string targetPlayerId)
    {
        var request = new SubmitVoteRequest
        {
            roomId = _roomId,
            voterId = _playerId,
            targetPlayerId = targetPlayerId
        };
        _client.Send("submitVote", JsonUtility.ToJson(request));

        UI.ShowView("waiting");
        UI.SetWaitingMessage("Vote cast! Waiting for results...");
    }

    public void LeaveRoom(bool skipConfirm = false)
    {
        if (!_gameStarted && !string.IsNullOrEmpty(_roomId))
        {
            var request = new LeaveRoomRequest
            {
                roomId = _roomId,
                playerId = _playerId
            };
            _client.Send("leaveRoom", JsonUtility.ToJson(request));
        }

        ClearSession();
        _client.Disconnect();
        UI.ShowView("login");
    }

    public void UndoDrawing()
    {
        DrawingCanvas.Undo();
    }

    public void ClearDrawing()
    {
        DrawingCanvas.Clear();
    }

    // ============================================================
    // Network Handlers
    // ============================================================

    private void OnConnected()
    {
        Debug.Log("[Client] Connected to host");
        UI.HideLoading();
        UI.HideError();
        JoinRoom();
    }

    private void OnDisconnected()
    {
        Debug.Log("[Client] Disconnected from host");
        UI.ShowError("Connection lost. Please reconnect.");
    }

    private void OnMessageReceived(string type, string dataJson)
    {
        switch (type)
        {
            case "joinRoomResponse": HandleJoinRoomResponse(dataJson); break;
            case "response": HandleGenericResponse(dataJson); break;
            case "drawCar": HandleDrawCar(dataJson); break;
            case "carReceived": HandleCarReceived(dataJson); break;
            case "gameStarted": HandleGameStarted(dataJson); break;
            case "roundStarted": HandleRoundStarted(dataJson); break;
            case "updateState": HandleUpdateState(dataJson); break;
            case "playerJoined": HandlePlayerJoined(dataJson); break;
            case "playerLeft": HandlePlayerLeft(dataJson); break;
            case "timerStart": HandleTimerStart(dataJson); break;
            case "timerStop": HandleTimerStop(); break;
            case "allDrawingsSubmitted": break; // Host handles this
            case "allVotesSubmitted": break;
            case "playerSubmitted": break; // Host UI only
            case "playerVoted": break;
            case "playerConnectionChanged": break;
            default:
                Debug.Log($"[Client] Unhandled message: {type}");
                break;
        }
    }

    private void HandleJoinRoomResponse(string dataJson)
    {
        var response = JsonUtility.FromJson<JoinRoomResponse>(dataJson);
        UI.HideLoading();

        if (response.success)
        {
            SaveSession();
            Debug.Log("[Client] Joined room successfully");

            // Parse room state to determine current phase
            // For simplicity, show waiting view
            _gameStarted = false;
            UI.ShowView("waiting");
            UI.SetWaitingMessage("Waiting for players to join...");

            // Show start button if leader
            // (leader detection from room state would need parsing roomStateJson)
        }
        else
        {
            ClearSession();
            UI.ShowError(response.error ?? "Failed to join room");
            UI.ShowView("login");
        }
    }

    private void HandleGenericResponse(string dataJson)
    {
        var response = JsonUtility.FromJson<GenericResponse>(dataJson);
        if (!response.success)
        {
            Debug.LogWarning($"[Client] Request failed: {response.error}");
        }
    }

    private void HandleDrawCar(string dataJson)
    {
        _drawingEndpoint = "submitCarDrawing";
        _useWhiteBackground = false;
        HandlePhaseChange("drawing");
        UI.SetDrawingPrompt("Draw your car!");
    }

    private void HandleCarReceived(string dataJson)
    {
        _drawingEndpoint = "submitDrawing";
        _useWhiteBackground = true;
        UI.ShowView("waiting");
        UI.SetWaitingMessage("Car received! Waiting for others...");
    }

    private void HandleGameStarted(string dataJson)
    {
        var msg = JsonUtility.FromJson<GameStartedMessage>(dataJson);
        _drawingEndpoint = "submitDrawing";
        _useWhiteBackground = true;
        _currentPhase = msg.status;
        _gameStarted = true;
        UI.HideStartButton();
        HandlePhaseChange(msg.status);
    }

    private void HandleRoundStarted(string dataJson)
    {
        var msg = JsonUtility.FromJson<RoundStartedMessage>(dataJson);
        _currentPhase = "drawing";
        _gameStarted = true;
        UI.HideStartButton();
        HandlePhaseChange("drawing");
        UI.SetDrawingPrompt("Draw your gadget!");
    }

    private void HandleUpdateState(string dataJson)
    {
        var msg = JsonUtility.FromJson<UpdateStateMessage>(dataJson);
        _currentPhase = msg.status;
        if (msg.status != "waiting") _gameStarted = true;
        if (msg.status != "waiting") UI.HideStartButton();

        HandlePhaseChange(msg.status, msg);
    }

    private void HandlePlayerJoined(string dataJson)
    {
        var msg = JsonUtility.FromJson<PlayerJoinedMessage>(dataJson);
        _playerCount = msg.playerCount;
        _minPlayers = msg.minPlayers;
        _maxPlayers = msg.maxPlayers;

        if (_isLeader)
            UpdateStartButtonState();
        else
            UpdateNonLeaderWaitingMessage();
    }

    private void HandlePlayerLeft(string dataJson)
    {
        var msg = JsonUtility.FromJson<PlayerLeftMessage>(dataJson);
        _playerCount = msg.playerCount;
        _minPlayers = msg.minPlayers;
        _maxPlayers = msg.maxPlayers;

        if (msg.newLeaderId == _playerId)
        {
            _isLeader = true;
            if (!_gameStarted)
            {
                UI.ShowStartButton();
                UpdateStartButtonState();
            }
        }
        else if (_isLeader)
        {
            UpdateStartButtonState();
        }
        else
        {
            UpdateNonLeaderWaitingMessage();
        }
    }

    private void HandleTimerStart(string dataJson)
    {
        var msg = JsonUtility.FromJson<TimerStartMessage>(dataJson);
        _timerRemaining = msg.seconds;
        _timerPhase = msg.phase;
        _timerRunning = true;
        UI.ShowTimer(msg.phase);
    }

    private void HandleTimerStop()
    {
        _timerRunning = false;
        UI.HideTimer();
    }

    // ============================================================
    // Phase Management
    // ============================================================

    private void HandlePhaseChange(string phase, UpdateStateMessage data = null)
    {
        if (phase != "waiting" || _gameStarted) UI.HideStartButton();

        switch (phase)
        {
            case "waiting":
                UI.ShowView("waiting");
                StopTimer();
                if (_gameStarted)
                    UI.SetWaitingMessage("Waiting for next round...");
                break;

            case "drawing":
                UI.ShowView("drawing");
                DrawingCanvas.Clear();
                DrawingCanvas.SetInteractable(true);
                UI.SetSubmitButtonState(true, "Submit");
                break;

            case "voting":
                UI.ShowView("voting");
                // Parse drawings from data if available
                if (data != null && !string.IsNullOrEmpty(data.drawingsJson))
                {
                    _currentVotingDrawings = ParseDrawingsJson(data.drawingsJson);
                    UI.RenderVotingGrid(_currentVotingDrawings, _playerId, this);
                }
                break;

            case "vote_results":
                // Stay on current view, host shows results
                break;

            case "racing":
                UI.ShowView("waiting");
                StopTimer();
                UI.SetWaitingMessage("Race in progress! Look at the host screen.");
                break;

            case "finished":
                UI.ShowView("finished");
                StopTimer();
                break;
        }
    }

    private void StopTimer()
    {
        _timerRunning = false;
        UI.HideTimer();
    }

    // ============================================================
    // Start Button Logic
    // ============================================================

    private void UpdateStartButtonState()
    {
        if (!_isLeader || _gameStarted) return;

        bool canStart = _playerCount >= _minPlayers;
        UI.SetStartButtonInteractable(canStart);

        if (!canStart)
        {
            int needed = _minPlayers - _playerCount;
            UI.SetStartButtonText($"Need {needed} more player{(needed != 1 ? "s" : "")}");
            UI.SetWaitingMessage($"Waiting for at least {needed} more player{(needed != 1 ? "s" : "")} to join...");
        }
        else
        {
            UI.SetStartButtonText("Start Game");
            UI.SetWaitingMessage(_playerCount >= _maxPlayers
                ? "Game is full! You can start the game now."
                : "You can start the game when everyone has joined.");
        }
    }

    private void UpdateNonLeaderWaitingMessage()
    {
        if (_isLeader || _gameStarted || _currentPhase != "waiting") return;

        int needed = _minPlayers - _playerCount;
        if (needed > 0)
            UI.SetWaitingMessage($"Waiting for at least {needed} more player{(needed != 1 ? "s" : "")} to join...");
        else
            UI.SetWaitingMessage("Waiting for the VIP to start the game...");
    }

    // ============================================================
    // Session Management
    // ============================================================

    private string LoadOrGeneratePlayerId()
    {
        string id = PlayerPrefs.GetString("dd_playerId", "");
        if (string.IsNullOrEmpty(id))
        {
            id = "player_" + Guid.NewGuid().ToString("N").Substring(0, 9) + "_" +
                 DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            PlayerPrefs.SetString("dd_playerId", id);
            PlayerPrefs.Save();
        }
        return id;
    }

    private void SaveSession()
    {
        PlayerPrefs.SetString("dd_roomId", _roomId);
        PlayerPrefs.SetString("dd_playerName", _playerName);
        PlayerPrefs.Save();
    }

    private void ClearSession()
    {
        PlayerPrefs.DeleteKey("dd_roomId");
        PlayerPrefs.DeleteKey("dd_playerName");
        PlayerPrefs.Save();
        _roomId = null;
        _playerName = null;
        _gameStarted = false;
        _isLeader = false;
    }

    // ============================================================
    // JSON Parsing Helpers
    // ============================================================

    private List<DrawingData> ParseDrawingsJson(string json)
    {
        // Simple JSON array parser for DrawingData list
        var result = new List<DrawingData>();
        if (string.IsNullOrEmpty(json) || json == "[]") return result;

        // Split by objects - find each {...}
        int depth = 0;
        int start = -1;
        for (int i = 0; i < json.Length; i++)
        {
            if (json[i] == '{')
            {
                if (depth == 0) start = i;
                depth++;
            }
            else if (json[i] == '}')
            {
                depth--;
                if (depth == 0 && start >= 0)
                {
                    string obj = json.Substring(start, i - start + 1);
                    try
                    {
                        var drawing = JsonUtility.FromJson<DrawingData>(obj);
                        if (drawing != null) result.Add(drawing);
                    }
                    catch { }
                    start = -1;
                }
            }
        }
        return result;
    }
}
