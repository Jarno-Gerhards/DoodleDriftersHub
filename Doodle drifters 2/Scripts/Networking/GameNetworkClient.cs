using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

/// <summary>
/// TCP client that runs on the mobile/player build.
/// Connects to the Host's GameServer.
/// 
/// Message protocol: 4-byte little-endian length prefix + UTF-8 JSON string.
/// </summary>
public class GameNetworkClient
{
    public event Action OnConnected;
    public event Action OnDisconnected;
    public event Action<string, string> OnMessageReceived; // type, dataJson

    private TcpClient _client;
    private NetworkStream _stream;
    private Thread _readThread;
    private readonly ConcurrentQueue<Action> _mainThreadQueue = new ConcurrentQueue<Action>();
    private bool _connected;

    public bool IsConnected => _connected;

    public void Connect(string host, int port)
    {
        try
        {
            _client = new TcpClient();
            _client.Connect(host, port);
            _stream = _client.GetStream();
            _connected = true;

            _readThread = new Thread(ReadLoop) { IsBackground = true };
            _readThread.Start();

            _mainThreadQueue.Enqueue(() => OnConnected?.Invoke());
            Debug.Log($"[GameClient] Connected to {host}:{port}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GameClient] Connection failed: {ex.Message}");
            _mainThreadQueue.Enqueue(() => OnDisconnected?.Invoke());
        }
    }

    public void Disconnect()
    {
        _connected = false;
        try { _stream?.Close(); } catch { }
        try { _client?.Close(); } catch { }
        Debug.Log("[GameClient] Disconnected");
    }

    /// <summary>
    /// Must be called from Unity's Update() to process queued events on the main thread.
    /// </summary>
    public void ProcessMainThreadQueue()
    {
        while (_mainThreadQueue.TryDequeue(out var action))
        {
            try { action(); }
            catch (Exception ex) { Debug.LogError($"[GameClient] Main thread error: {ex}"); }
        }
    }

    /// <summary>
    /// Send a message to the server (Host).
    /// </summary>
    public void Send(string type, string dataJson = "{}")
    {
        if (!_connected || _stream == null) return;

        string json = NetworkMessage.Serialize(type, dataJson);
        byte[] data = Encoding.UTF8.GetBytes(json);
        byte[] lengthPrefix = BitConverter.GetBytes(data.Length);

        try
        {
            _stream.Write(lengthPrefix, 0, 4);
            _stream.Write(data, 0, data.Length);
            _stream.Flush();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[GameClient] Send failed: {ex.Message}");
            HandleDisconnect();
        }
    }

    private void ReadLoop()
    {
        try
        {
            byte[] lengthBuffer = new byte[4];

            while (_connected)
            {
                int bytesRead = ReadExact(lengthBuffer, 4);
                if (bytesRead < 4) break;

                int messageLength = BitConverter.ToInt32(lengthBuffer, 0);
                if (messageLength <= 0 || messageLength > 10 * 1024 * 1024)
                {
                    Debug.LogWarning($"[GameClient] Invalid message length: {messageLength}");
                    break;
                }

                byte[] messageBuffer = new byte[messageLength];
                bytesRead = ReadExact(messageBuffer, messageLength);
                if (bytesRead < messageLength) break;

                string json = Encoding.UTF8.GetString(messageBuffer);

                try
                {
                    var msg = JsonUtility.FromJson<NetworkMessage>(json);
                    if (msg != null && !string.IsNullOrEmpty(msg.type))
                    {
                        _mainThreadQueue.Enqueue(() =>
                            OnMessageReceived?.Invoke(msg.type, msg.data));
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[GameClient] Parse error: {ex.Message}");
                }
            }
        }
        catch (Exception)
        {
            // Disconnected
        }

        HandleDisconnect();
    }

    private void HandleDisconnect()
    {
        if (!_connected) return;
        _connected = false;
        _mainThreadQueue.Enqueue(() => OnDisconnected?.Invoke());
    }

    private int ReadExact(byte[] buffer, int count)
    {
        int totalRead = 0;
        while (totalRead < count && _connected)
        {
            int read = _stream.Read(buffer, totalRead, count - totalRead);
            if (read == 0) return totalRead;
            totalRead += read;
        }
        return totalRead;
    }
}
