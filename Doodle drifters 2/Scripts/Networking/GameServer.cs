using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

/// <summary>
/// TCP-based game server that runs on the Host.
/// Replaces Express + Socket.IO server.
/// Clients connect via TCP and exchange JSON messages.
/// 
/// Message protocol: 4-byte little-endian length prefix + UTF-8 JSON string.
/// </summary>
public class GameServer
{
    public event Action<int> OnClientConnected;
    public event Action<int> OnClientDisconnected;
    public event Action<int, string, string> OnMessageReceived; // connectionId, type, dataJson

    private TcpListener _listener;
    private readonly Dictionary<int, TcpClient> _clients = new Dictionary<int, TcpClient>();
    private readonly Dictionary<int, NetworkStream> _streams = new Dictionary<int, NetworkStream>();
    private readonly ConcurrentQueue<Action> _mainThreadQueue = new ConcurrentQueue<Action>();
    private int _nextConnectionId = 1;
    private bool _running;
    private Thread _acceptThread;
    private readonly object _lock = new object();

    public int Port { get; private set; }
    public bool IsRunning => _running;

    public void Start(int port)
    {
        Port = port;
        _listener = new TcpListener(IPAddress.Any, port);
        _listener.Start();
        _running = true;

        _acceptThread = new Thread(AcceptClients) { IsBackground = true };
        _acceptThread.Start();

        Debug.Log($"[GameServer] Listening on port {port}");
    }

    public void Stop()
    {
        _running = false;

        lock (_lock)
        {
            foreach (var client in _clients.Values)
            {
                try { client.Close(); } catch { }
            }
            _clients.Clear();
            _streams.Clear();
        }

        try { _listener?.Stop(); } catch { }
        Debug.Log("[GameServer] Stopped");
    }

    /// <summary>
    /// Must be called from Unity's Update() to process queued events on the main thread.
    /// </summary>
    public void ProcessMainThreadQueue()
    {
        while (_mainThreadQueue.TryDequeue(out var action))
        {
            try { action(); }
            catch (Exception ex) { Debug.LogError($"[GameServer] Main thread error: {ex}"); }
        }
    }

    /// <summary>
    /// Send a message to a specific client.
    /// </summary>
    public void Send(int connectionId, string type, string dataJson = "{}")
    {
        string json = NetworkMessage.Serialize(type, dataJson);
        byte[] data = Encoding.UTF8.GetBytes(json);
        byte[] lengthPrefix = BitConverter.GetBytes(data.Length);

        lock (_lock)
        {
            if (_streams.TryGetValue(connectionId, out var stream))
            {
                try
                {
                    stream.Write(lengthPrefix, 0, 4);
                    stream.Write(data, 0, data.Length);
                    stream.Flush();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[GameServer] Failed to send to {connectionId}: {ex.Message}");
                    DisconnectClient(connectionId);
                }
            }
        }
    }

    /// <summary>
    /// Send a message to all connected clients.
    /// </summary>
    public void Broadcast(string type, string dataJson = "{}")
    {
        List<int> ids;
        lock (_lock)
        {
            ids = new List<int>(_clients.Keys);
        }
        foreach (var id in ids)
        {
            Send(id, type, dataJson);
        }
    }

    /// <summary>
    /// Send a message to all clients except the specified one.
    /// </summary>
    public void BroadcastExcept(int excludeId, string type, string dataJson = "{}")
    {
        List<int> ids;
        lock (_lock)
        {
            ids = new List<int>(_clients.Keys);
        }
        foreach (var id in ids)
        {
            if (id != excludeId)
                Send(id, type, dataJson);
        }
    }

    public void DisconnectClient(int connectionId)
    {
        lock (_lock)
        {
            if (_clients.TryGetValue(connectionId, out var client))
            {
                try { client.Close(); } catch { }
                _clients.Remove(connectionId);
                _streams.Remove(connectionId);
            }
        }
        _mainThreadQueue.Enqueue(() => OnClientDisconnected?.Invoke(connectionId));
    }

    public string GetClientIP(int connectionId)
    {
        lock (_lock)
        {
            if (_clients.TryGetValue(connectionId, out var client))
            {
                var endpoint = client.Client.RemoteEndPoint as IPEndPoint;
                return endpoint?.Address.ToString() ?? "unknown";
            }
        }
        return "unknown";
    }

    private void AcceptClients()
    {
        while (_running)
        {
            try
            {
                var client = _listener.AcceptTcpClient();
                int connId;
                lock (_lock)
                {
                    connId = _nextConnectionId++;
                    _clients[connId] = client;
                    _streams[connId] = client.GetStream();
                }

                _mainThreadQueue.Enqueue(() => OnClientConnected?.Invoke(connId));

                // Start reading thread for this client
                var readThread = new Thread(() => ReadClient(connId)) { IsBackground = true };
                readThread.Start();
            }
            catch (SocketException)
            {
                // Listener stopped
                break;
            }
            catch (Exception ex)
            {
                if (_running)
                    Debug.LogError($"[GameServer] Accept error: {ex}");
            }
        }
    }

    private void ReadClient(int connectionId)
    {
        try
        {
            NetworkStream stream;
            lock (_lock)
            {
                if (!_streams.TryGetValue(connectionId, out stream))
                    return;
            }

            byte[] lengthBuffer = new byte[4];

            while (_running)
            {
                // Read 4-byte length prefix
                int bytesRead = ReadExact(stream, lengthBuffer, 4);
                if (bytesRead < 4) break;

                int messageLength = BitConverter.ToInt32(lengthBuffer, 0);
                if (messageLength <= 0 || messageLength > 10 * 1024 * 1024) // 10MB max
                {
                    Debug.LogWarning($"[GameServer] Invalid message length from {connectionId}: {messageLength}");
                    break;
                }

                // Read message body
                byte[] messageBuffer = new byte[messageLength];
                bytesRead = ReadExact(stream, messageBuffer, messageLength);
                if (bytesRead < messageLength) break;

                string json = Encoding.UTF8.GetString(messageBuffer);

                // Parse message type
                try
                {
                    var msg = JsonUtility.FromJson<NetworkMessage>(json);
                    if (msg != null && !string.IsNullOrEmpty(msg.type))
                    {
                        _mainThreadQueue.Enqueue(() =>
                            OnMessageReceived?.Invoke(connectionId, msg.type, msg.data));
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[GameServer] Parse error from {connectionId}: {ex.Message}");
                }
            }
        }
        catch (Exception)
        {
            // Client disconnected
        }

        _mainThreadQueue.Enqueue(() =>
        {
            lock (_lock)
            {
                _clients.Remove(connectionId);
                _streams.Remove(connectionId);
            }
            OnClientDisconnected?.Invoke(connectionId);
        });
    }

    private int ReadExact(NetworkStream stream, byte[] buffer, int count)
    {
        int totalRead = 0;
        while (totalRead < count)
        {
            int read = stream.Read(buffer, totalRead, count - totalRead);
            if (read == 0) return totalRead; // Connection closed
            totalRead += read;
        }
        return totalRead;
    }
}
