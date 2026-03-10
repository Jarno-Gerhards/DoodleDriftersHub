using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Manages game rooms. Equivalent to RoomManager.js.
/// Used by the Host to track players, assign cars, and manage room state.
/// </summary>
public class RoomManager
{
    private Dictionary<string, RoomData> _rooms = new Dictionary<string, RoomData>();

    public RoomData CreateRoom()
    {
        string roomId = GenerateRoomId();
        var room = new RoomData(roomId);
        _rooms[roomId] = room;
        return room;
    }

    public (RoomData room, bool isReconnect) JoinRoom(string roomId, PlayerData player)
    {
        if (!_rooms.TryGetValue(roomId, out var room))
            throw new Exception("Room not found");

        if (player.IsHost)
        {
            bool isReconnect = room.HostId != null && room.HostId != player.Id;
            room.HostId = player.Id;
            return (room, isReconnect);
        }

        // Check room capacity for new players
        int currentCount = room.GetPlayerCount();
        if (currentCount >= GameConfig.Instance.MaxPlayers && !room.Players.ContainsKey(player.Id))
            throw new Exception("Room is full");

        // Reconnect logic
        if (GameConfig.Instance.AllowReconnect && room.Players.ContainsKey(player.Id))
        {
            var existing = room.Players[player.Id];
            existing.Connected = true;
            return (room, true);
        }

        // Prevent joining active game
        if (room.GameState.Status != GameState.Phase.Waiting)
            throw new Exception("Game already started");

        // Enforce unique names
        if (IsNameTaken(room, player.Name))
            throw new Exception("Name already taken");

        // Check duplicate ID
        if (room.Players.ContainsKey(player.Id))
            throw new Exception("You are already in this room");

        // Determine if leader (first player)
        player.IsLeader = room.Players.Count == 0;

        // Assign a random car
        player.CarId = room.AssignCar();

        // Add player
        room.Players[player.Id] = player;

        return (room, false);
    }

    public RoomData GetRoom(string roomId)
    {
        _rooms.TryGetValue(roomId, out var room);
        return room;
    }

    public void SetPlayerConnected(string roomId, string playerId, bool connected)
    {
        var room = GetRoom(roomId);
        if (room == null) return;
        if (room.Players.TryGetValue(playerId, out var player))
            player.Connected = connected;
    }

    public (string roomId, string playerId, PlayerData player)? FindPlayerByConnectionId(int connectionId, Dictionary<int, string> connectionPlayerMap)
    {
        if (!connectionPlayerMap.TryGetValue(connectionId, out var playerId))
            return null;

        foreach (var kv in _rooms)
        {
            if (kv.Value.Players.TryGetValue(playerId, out var player))
                return (kv.Key, playerId, player);
        }
        return null;
    }

    public (bool success, string playerName, string newLeaderId, string error) LeaveRoom(string roomId, string playerId)
    {
        var room = GetRoom(roomId);
        if (room == null)
            return (false, null, null, "Room not found");

        if (room.GameState.Status != GameState.Phase.Waiting)
            return (false, null, null, "Cannot leave after game has started");

        if (!room.Players.TryGetValue(playerId, out var player))
            return (false, null, null, "Player not found");

        // Return car to pool
        if (player.CarId > 0)
            room.AvailableCars.Add(player.CarId);

        bool wasLeader = player.IsLeader;
        room.Players.Remove(playerId);

        string newLeaderId = null;

        // Assign new leader
        if (wasLeader && room.Players.Count > 0)
        {
            var newLeader = room.Players.Values.First();
            newLeader.IsLeader = true;
            newLeaderId = newLeader.Id;
        }

        return (true, player.Name, newLeaderId, null);
    }

    public void DeleteRoom(string roomId)
    {
        _rooms.Remove(roomId);
    }

    private bool IsNameTaken(RoomData room, string name)
    {
        return room.Players.Values.Any(p =>
            string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    private string GenerateRoomId()
    {
        int attempts = 0;
        string code;
        do
        {
            if (attempts++ > 100) throw new Exception("Failed to generate room ID");
            code = UnityEngine.Random.Range(10000, 99999).ToString();
        } while (_rooms.ContainsKey(code));
        return code;
    }
}

/// <summary>
/// Data for a single game room.
/// </summary>
public class RoomData
{
    public string Id;
    public Dictionary<string, PlayerData> Players;
    public string HostId;
    public GameState GameState;
    public long CreatedAt;
    public List<int> AvailableCars;
    public bool DrawOwnCar;

    public RoomData(string id)
    {
        Id = id;
        Players = new Dictionary<string, PlayerData>();
        HostId = null;
        GameState = new GameState();
        CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        AvailableCars = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8 };
        DrawOwnCar = GameConfig.Instance.DrawOwnCar;
    }

    public int GetPlayerCount()
    {
        return Players.Values.Count(p => !p.IsHost);
    }

    public int AssignCar()
    {
        if (AvailableCars.Count > 0)
        {
            int index = UnityEngine.Random.Range(0, AvailableCars.Count);
            int carId = AvailableCars[index];
            AvailableCars.RemoveAt(index);
            return carId;
        }
        return UnityEngine.Random.Range(1, 9);
    }

    /// <summary>
    /// Get serializable room state for sending to clients.
    /// </summary>
    public RoomStateSnapshot ToSnapshot()
    {
        return new RoomStateSnapshot
        {
            Id = Id,
            HostId = HostId,
            DrawOwnCar = DrawOwnCar,
            Players = Players.Values.ToList(),
            GameState = GameState.ToSnapshot(),
            PlayerCount = GetPlayerCount(),
            MinPlayers = GameConfig.Instance.MinPlayers,
            MaxPlayers = GameConfig.Instance.MaxPlayers
        };
    }
}

/// <summary>
/// Serializable room state for network transmission.
/// </summary>
[Serializable]
public class RoomStateSnapshot
{
    public string Id;
    public string HostId;
    public bool DrawOwnCar;
    public List<PlayerData> Players;
    public GameStateSnapshot GameState;
    public int PlayerCount;
    public int MinPlayers;
    public int MaxPlayers;
}
