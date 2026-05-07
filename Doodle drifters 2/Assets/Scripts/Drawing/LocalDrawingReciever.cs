using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using UnityEngine;

public class LocalDrawingReceiver : MonoBehaviour
{
    [SerializeField] private int port = 8085;

    [Serializable]
    private class DrawingPayload
    {
        public string championName;
        public string imageDataUrl;
        public string submittedAt;
    }

    private class PendingDrawing
    {
        public string championName;
        public byte[] pngBytes;
    }

    private HttpListener listener;
    private Thread listenerThread;
    private volatile bool running;

    private readonly object queueLock = new object();
    private readonly System.Collections.Generic.Queue<PendingDrawing> queue =
        new System.Collections.Generic.Queue<PendingDrawing>();

    public HostLobbyScript lobbyScript;

    private void Start()
    {
        StartServer();
    }

    private void OnDestroy()
    {
        StopServer();
    }

    private void Update()
    {
        lock (queueLock)
        {
            while (queue.Count > 0)
            {
                var item = queue.Dequeue();

                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (texture.LoadImage(item.pngBytes))
                {
                    // Send to your Unity-side game flow here
                    if (HostScreen.Instance != null)
                    {
                        HostScreen.Instance.AddDrawing(texture, item.championName);
                    }

                    var stateMachine = FindFirstObjectByType<StateMachine>();
                    if (stateMachine != null)
                    {
                        stateMachine.Context.latestDrawing = texture;
                        stateMachine.Context.latestTitle = item.championName;
                        Debug.Log($"[LocalDrawingReceiver] Set context: '{item.championName}'");
                    }

                    if (stateMachine.currentStateType == StateMachine.GameStateType.Lobby)
                    {
                        lobbyScript.AddPlayer(item.championName, texture);
                    }
                    // If you want the state machine to react:
                    // FindFirstObjectByType<StateMachine>().SwitchState(StateMachine.GameStateType.Voting);
                }
                else
                {
                    Destroy(texture);
                    Debug.LogWarning("Failed to decode submitted drawing image.");
                }
            }
        }
    }

    private void StartServer()
    {
        listener = new HttpListener();
        listener.Prefixes.Add($"http://localhost:{port}/drawing/");
        listener.Start();

        running = true;
        listenerThread = new Thread(ListenLoop);
        listenerThread.IsBackground = true;
        listenerThread.Start();

        Debug.Log($"Listening for drawings on http://localhost:{port}/drawing/");
    }

    private void StopServer()
    {
        running = false;

        try { listener?.Stop(); } catch { }
        try { listener?.Close(); } catch { }
    }

    private void ListenLoop()
    {
        while (running)
        {
            try
            {
                var context = listener.GetContext();
                HandleRequest(context);
            }
            catch (Exception ex)
            {
                if (running)
                    Debug.LogWarning(ex.Message);
            }
        }
    }

    private void HandleRequest(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;

        response.AddHeader("Access-Control-Allow-Origin", "*");
        response.AddHeader("Access-Control-Allow-Headers", "Content-Type");
        response.AddHeader("Access-Control-Allow-Methods", "POST, OPTIONS");

        if (request.HttpMethod == "OPTIONS")
        {
            response.StatusCode = 204;
            response.Close();
            return;
        }

        if (request.HttpMethod != "POST")
        {
            response.StatusCode = 405;
            WriteJson(response, "{\"error\":\"POST only\"}");
            return;
        }

        string body;
        using (var reader = new StreamReader(request.InputStream, request.ContentEncoding ?? Encoding.UTF8))
        {
            body = reader.ReadToEnd();
        }

        try
        {
            var payload = JsonUtility.FromJson<DrawingPayload>(body);
            if (payload == null || string.IsNullOrWhiteSpace(payload.imageDataUrl))
                throw new Exception("Invalid payload");

            string championName = string.IsNullOrWhiteSpace(payload.championName)
                ? "Unnamed Champion"
                : payload.championName.Trim();

            byte[] pngBytes = DecodeDataUrl(payload.imageDataUrl);

            lock (queueLock)
            {
                queue.Enqueue(new PendingDrawing
                {
                    championName = championName,
                    pngBytes = pngBytes
                });
            }

            response.StatusCode = 200;
            WriteJson(response, "{\"ok\":true}");
        }
        catch (Exception ex)
        {
            response.StatusCode = 400;
            WriteJson(response, "{\"ok\":false,\"error\":\"" + EscapeJson(ex.Message) + "\"}");
        }
    }

    private static byte[] DecodeDataUrl(string dataUrl)
    {
        int comma = dataUrl.IndexOf(',');
        string base64 = comma >= 0 ? dataUrl.Substring(comma + 1) : dataUrl;
        return Convert.FromBase64String(base64);
    }

    private static void WriteJson(HttpListenerResponse response, string json)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(json);
        response.ContentType = "application/json";
        response.ContentLength64 = bytes.Length;
        using var output = response.OutputStream;
        output.Write(bytes, 0, bytes.Length);
    }

    private static string EscapeJson(string value)
    {
        return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}