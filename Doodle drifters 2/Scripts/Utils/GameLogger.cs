using UnityEngine;

/// <summary>
/// Simple logging utility wrapping Unity's Debug.Log.
/// Equivalent to Logger.js (Winston-based logger).
/// Usage: GameLogger.Info("message"), GameLogger.Warn("message"), etc.
/// </summary>
public static class GameLogger
{
    public enum LogLevel { Debug, Info, Warn, Error }

    public static LogLevel MinLevel = LogLevel.Info;

    public static void Debug(string message, string context = "")
    {
        if (MinLevel > LogLevel.Debug) return;
        string prefix = string.IsNullOrEmpty(context) ? "[DEBUG]" : $"[DEBUG][{context}]";
        UnityEngine.Debug.Log($"{prefix} {message}");
    }

    public static void Info(string message, string context = "")
    {
        if (MinLevel > LogLevel.Info) return;
        string prefix = string.IsNullOrEmpty(context) ? "[INFO]" : $"[INFO][{context}]";
        UnityEngine.Debug.Log($"{prefix} {message}");
    }

    public static void Warn(string message, string context = "")
    {
        if (MinLevel > LogLevel.Warn) return;
        string prefix = string.IsNullOrEmpty(context) ? "[WARN]" : $"[WARN][{context}]";
        UnityEngine.Debug.LogWarning($"{prefix} {message}");
    }

    public static void Error(string message, string context = "")
    {
        string prefix = string.IsNullOrEmpty(context) ? "[ERROR]" : $"[ERROR][{context}]";
        UnityEngine.Debug.LogError($"{prefix} {message}");
    }
}
