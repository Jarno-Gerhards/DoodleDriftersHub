using UnityEngine;
using NativeWebSocket;

public class WebSocketClient : MonoBehaviour
{
    WebSocket websocket;

    async void Start()
    {
        websocket = new WebSocket("wss://doodledrifterserver-production.up.railway.app");

        websocket.OnOpen += () =>
        {
            Debug.Log("Connected to server");

            Send("{\"type\":\"HOST_CONNECT\"}");
        };

        websocket.OnMessage += (bytes) =>
        {
            var msg = System.Text.Encoding.UTF8.GetString(bytes);
            Debug.Log("Received: " + msg);
        };

        await websocket.Connect();
    }

    public async void Send(string msg)
    {
        if (websocket.State == WebSocketState.Open)
        {
            await websocket.SendText(msg);
        }
    }

    void Update()
    {
        websocket.DispatchMessageQueue();
    }
}