using UnityEngine;
using NativeWebSocket;

public class WebSocketClient : MonoBehaviour
{
    WebSocket websocket;
    public StateMachine stateMachine;

    async void Start()
    {
        websocket = new WebSocket("wss://doodledrifterserver-production.up.railway.app");

        websocket.OnOpen += () =>
        {
            stateMachine.SetNetwork(this);
            Debug.Log("Connected to server");
            Debug.Log("WS CLIENT INSTANCE: " + GetInstanceID());
            Send("{\"type\":\"HOST_CONNECT\"}");
        };

        websocket.OnMessage += (bytes) =>
        {
            var msg = System.Text.Encoding.UTF8.GetString(bytes);
            Debug.Log("Received: " + msg);
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

    void Update()
    {
        websocket.DispatchMessageQueue();
        Debug.Log("WS STATE TICK → " + websocket.State);
    }
}