using UnityEngine;
using NativeWebSocket;
using System;
using System.Collections;

public class WebSocketClient : MonoBehaviour
{
    WebSocket websocket;
    public StateMachine stateMachine;
    [SerializeField] private HostLobbyScript hostLobby;
    [SerializeField] private ScenarioManager scenarioManager;
    [SerializeField] private string roomCodeOverride = "";

    async void Start()
    {
        websocket = new WebSocket("wss://doodledrifterserver-production.up.railway.app");

        websocket.OnOpen += () =>
        {
            if (stateMachine != null)
            {
                stateMachine.SetNetwork(this);
            }
            Debug.Log("Connected to server");
            Debug.Log("WS CLIENT INSTANCE: " + GetInstanceID());
            string desiredRoomCode = GetDesiredRoomCode();
            if (!string.IsNullOrWhiteSpace(desiredRoomCode))
            {
                Send("{\"type\":\"HOST_CONNECT\",\"roomCode\":\"" + desiredRoomCode + "\"}");
            }
            else
            {
                Send("{\"type\":\"HOST_CONNECT\"}");
            }
            StartCoroutine(KeepAliveLoop());
        };

        websocket.OnMessage += (bytes) =>
        {
            var msg = System.Text.Encoding.UTF8.GetString(bytes);
            Debug.Log("Received: " + msg);
            HandleMessage(msg);
        };

        websocket.OnClose += (e) =>
{
    Debug.LogError("WS CLOSED: " + e);
};

        websocket.OnError += (e) =>
        {
            Debug.LogError("WS ERROR: " + e);
        };

        await websocket.Connect();
    }

    public async void Send(string msg)
    {
        Debug.Log("RAW OUT → " + msg);

        var bytes = System.Text.Encoding.UTF8.GetBytes(msg);
        Debug.Log("BYTE LEN → " + bytes.Length);

        await websocket.SendText(msg);

        Debug.Log("SEND COMPLETE");
    }

    public void SendState(string state)
    {
        if (string.IsNullOrWhiteSpace(state)) return;
        Send("{\"type\":\"STATE\",\"state\":\"" + state + "\"}");
    }

    void Update()
    {
        websocket.DispatchMessageQueue();
        Debug.Log("WS STATE TICK → " + websocket.State);
    }

    private IEnumerator KeepAliveLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(10f);

            if (websocket != null && websocket.State == WebSocketState.Open)
            {
                Send("{\"type\":\"PING\"}");
                Debug.Log("PING sent");
            }
        }
    }


    private void HandleMessage(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return;

        MessageEnvelope envelope = null;
        try
        {
            envelope = JsonUtility.FromJson<MessageEnvelope>(raw);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[WebSocketClient] Failed to parse message envelope: " + ex.Message);
            return;
        }

        if (envelope == null || string.IsNullOrWhiteSpace(envelope.type)) return;

        switch (envelope.type)
        {
            case "HOST_READY":
                HandleHostReady(raw);
                break;
            case "CHAMPION_SUBMITTED":
                HandleChampionSubmitted(raw);
                break;
            case "VOTE_RESULT":
                HandleVoteResult(raw);
                break;
            case "PLAYER_JOINED":
                HandlePlayerJoined(raw);
                break;
        }
    }

    private void HandleChampionSubmitted(string raw)
    {
        ChampionSubmittedMessage msg = JsonUtility.FromJson<ChampionSubmittedMessage>(raw);
        if (msg == null) return;

        Texture2D avatar = TryDecodeImage(msg.imageDataUrl);

        if (hostLobby == null)
        {
            hostLobby = FindFirstObjectByType<HostLobbyScript>();
        }

        if (hostLobby != null && avatar != null)
        {
            hostLobby.AddPlayer(msg.playerName, avatar);
            return;
        }

        if (HostScreen.Instance != null && avatar != null)
        {
            HostScreen.Instance.AddDrawing(avatar, msg.playerName);
        }
    }

    private void HandleVoteResult(string raw)
    {
        VoteResultMessage msg = JsonUtility.FromJson<VoteResultMessage>(raw);
        if (msg == null || msg.winner == null) return;

        if (scenarioManager == null)
        {
            scenarioManager = FindFirstObjectByType<ScenarioManager>();
        }

        if (scenarioManager != null)
        {
            scenarioManager.GenerateSolution(msg.winner.description);
        }
    }

    private void HandlePlayerJoined(string raw)
    {
        PlayerJoinedMessage msg = JsonUtility.FromJson<PlayerJoinedMessage>(raw);
        if (msg == null) return;

        Debug.Log("[WebSocketClient] Player joined: " + msg.name + " (" + msg.playerId + ")");
    }

    private Texture2D TryDecodeImage(string dataUrl)
    {
        if (string.IsNullOrWhiteSpace(dataUrl)) return null;

        try
        {
            int commaIndex = dataUrl.IndexOf(',');
            string base64 = commaIndex >= 0 ? dataUrl.Substring(commaIndex + 1) : dataUrl;
            byte[] bytes = Convert.FromBase64String(base64);
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (tex.LoadImage(bytes))
            {
                return tex;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[WebSocketClient] Failed to decode image: " + ex.Message);
        }

        return null;
    }

    [Serializable]
    private class MessageEnvelope
    {
        public string type;
    }

    [Serializable]
    private class HostReadyMessage
    {
        public string type;
        public string roomCode;
    }

    [Serializable]
    private class PlayerJoinedMessage
    {
        public string type;
        public string playerId;
        public string name;
    }

    [Serializable]
    private class ChampionSubmittedMessage
    {
        public string type;
        public string playerId;
        public string playerName;
        public string imageDataUrl;
    }

    [Serializable]
    private class VoteResultMessage
    {
        public string type;
        public WinnerData winner;
    }

    [Serializable]
    private class WinnerData
    {
        public string playerId;
        public string playerName;
        public string description;
        public string imageDataUrl;
    }

    private void HandleHostReady(string raw)
    {
        HostReadyMessage msg = JsonUtility.FromJson<HostReadyMessage>(raw);
        if (msg == null || string.IsNullOrWhiteSpace(msg.roomCode)) return;

        if (hostLobby == null)
        {
            hostLobby = FindFirstObjectByType<HostLobbyScript>();
        }

        if (hostLobby != null)
        {
            hostLobby.SetRoomCode(msg.roomCode);
        }
    }

    private string GetDesiredRoomCode()
    {
        if (!string.IsNullOrWhiteSpace(roomCodeOverride))
        {
            return roomCodeOverride.Trim();
        }

        return "";
    }
}