using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// DEBUG ONLY: Adds fake players and lets you cycle through game phases with keyboard.
/// Attach to the GameManager GameObject in HostScene.
/// 
/// Controls:
///   F1 = Add 3 bot players to lobby
///   F2 = Start game (goes to Drawing Phase)
///   F3 = Go to Voting Phase (with fake drawings)
///   F4 = Go to Narrative/Racing Phase
///   F5 = Go to Game Over
///   
/// Remove this script before building!
/// </summary>
public class HostDebugHelper : MonoBehaviour
{
    public HostManager HostManager;
    public HostUIManager HostUI;

    private RoomManager _roomManager;
    private RoomData _room;
    private List<PlayerData> _fakePlayers = new List<PlayerData>();
    private bool _playersAdded;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1)) AddFakePlayers();
        if (Input.GetKeyDown(KeyCode.F2)) StartFakeGame();
        if (Input.GetKeyDown(KeyCode.F3)) GoToVoting();
        if (Input.GetKeyDown(KeyCode.F4)) GoToNarrative();
        if (Input.GetKeyDown(KeyCode.F5)) GoToGameOver();
    }

    private void AddFakePlayers()
    {
        if (_playersAdded) return;
        _playersAdded = true;

        // Access the room via reflection or re-create for debug
        // We'll directly add to UI and create a local room for state
        _roomManager = new RoomManager();
        _room = _roomManager.CreateRoom();

        string[] names = { "EL", "KACHOW", "BEE" };
        for (int i = 0; i < names.Length; i++)
        {
            var player = new PlayerData($"bot_{i}", names[i], false);
            player.IsLeader = i == 0;
            player.CarId = i + 1;
            _room.Players[player.Id] = player;
            _fakePlayers.Add(player);

            HostUI.AddPlayerToLobby(player);
        }

        Debug.Log("[Debug] 3 fake players added. Press F2 to start game.");
    }

    private void StartFakeGame()
    {
        if (!_playersAdded || _room == null) return;

        _room.GameState.StartGame(_room.Players);
        HostUI.ShowDrawingPhase(_room.GameState.Round);

        Debug.Log("[Debug] Drawing Phase started. Press F3 for Voting.");
    }

    private void GoToVoting()
    {
        if (_room == null) return;

        // Submit fake drawings
        foreach (var p in _fakePlayers)
        {
            _room.GameState.SubmitDrawing(p.Id, CreatePlaceholderBase64(), $"{p.Name}'s Gadget");
        }

        _room.GameState.Status = GameState.Phase.Voting;
        var drawings = new List<DrawingData>(_room.GameState.CurrentDrawings.Values);
        HostUI.ShowVotingPhase(drawings);

        Debug.Log("[Debug] Voting Phase started. Press F4 for Narrative.");
    }

    private void GoToNarrative()
    {
        if (_room == null) return;

        // Fake vote results
        var results = new List<VotingResult>();
        int score = 3;
        foreach (var p in _fakePlayers)
        {
            _room.GameState.PlayerScores[p.Id] = score;
            results.Add(new VotingResult
            {
                PlayerId = p.Id,
                VotesReceived = score,
                PreviousScore = 0,
                NewScore = score
            });
            score--;
        }

        // Snapshot positions
        _room.GameState.SnapshotPositions(_room.Players);
        _room.GameState.Status = GameState.Phase.Racing;

        var positions = _room.GameState.Positions[_room.GameState.Positions.Count - 1];
        string narrative = "EL raced ahead with a rocket booster! KACHOW deployed a shield, blocking incoming attacks. BEE swerved through the obstacles with style!";

        HostUI.ShowNarrativePhase(narrative, positions);

        Debug.Log("[Debug] Narrative Phase shown. Press F5 for Game Over.");
    }

    private void GoToGameOver()
    {
        if (_room == null) return;

        _room.GameState.Status = GameState.Phase.Finished;

        // Make sure we have position data
        if (_room.GameState.Positions.Count == 0)
        {
            _room.GameState.SnapshotPositions(_room.Players);
        }

        HostUI.ShowGameOver(_room.GameState.Positions);

        Debug.Log("[Debug] Game Over screen shown.");
    }

    /// <summary>
    /// Creates a tiny 4x4 white PNG as base64 placeholder for fake drawings.
    /// </summary>
    private string CreatePlaceholderBase64()
    {
        var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        var colors = new Color[16];
        for (int i = 0; i < 16; i++) colors[i] = Color.gray;
        tex.SetPixels(colors);
        tex.Apply();
        byte[] bytes = tex.EncodeToPNG();
        Destroy(tex);
        return "data:image/png;base64," + System.Convert.ToBase64String(bytes);
    }
}
