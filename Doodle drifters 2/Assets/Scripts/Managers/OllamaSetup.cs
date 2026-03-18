#if UNITY_EDITOR
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor-only utility that runs automatically when Unity loads (or via the menu).
/// It ensures Ollama is installed and the required model is available on Windows.
///
/// Flow:
///   1. Check if 'ollama' is on PATH (or in the known install location).
///   2. If not found → download OllamaSetup.exe silently and install it.
///   3. Start 'ollama serve' in the background if it isn't already running.
///   4. Pull the required model if it is not yet present.
///
/// The status window stays open until setup is complete so teammates can see progress.
/// </summary>
[InitializeOnLoad]
public static class OllamaSetup
{
    // ── Configuration ────────────────────────────────────────────────────────
    private const string ModelName          = "llama3.1:8b";
    private const string OllamaDownloadUrl  = "https://ollama.com/download/OllamaSetup.exe";
    private const string OllamaHost         = "http://localhost:11434";
    private const int    ServeStartupWaitMs = 8000;   // ms to wait after launching serve
    private const int    HttpTimeoutSeconds = 10;

    // Where Windows installs Ollama by default
    private static readonly string DefaultInstallPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Programs", "Ollama", "ollama.exe");

    // ── Auto-run on Unity load ───────────────────────────────────────────────
    static OllamaSetup()
    {
        // Delay slightly so the Unity Editor finishes loading before we show UI / do work.
        EditorApplication.delayCall += () => _ = RunSetupAsync(silent: true);
    }

    // ── Menu item ────────────────────────────────────────────────────────────
    [MenuItem("Tools/Ollama/Run Setup Now")]
    public static void RunSetupFromMenu()
    {
        _ = RunSetupAsync(silent: false);
    }

    [MenuItem("Tools/Ollama/Check Status")]
    public static void CheckStatus()
    {
        _ = CheckStatusAsync();
    }

    // ── Core setup flow ──────────────────────────────────────────────────────
    private static async Task RunSetupAsync(bool silent)
    {
        try
        {
            Log("=== Ollama Setup Starting ===");

            // 1. Find or install Ollama
            string ollamaExe = FindOllamaExecutable();
            if (string.IsNullOrEmpty(ollamaExe))
            {
                bool proceed = silent || EditorUtility.DisplayDialog(
                    "Ollama Not Found",
                    "Ollama is not installed on this machine.\n\n" +
                    "The setup will now download and install Ollama silently (~60 MB).\n" +
                    "This is required for the AI Dungeon Master feature.\n\n" +
                    "Click OK to continue.",
                    "Install Ollama", "Skip");

                if (!proceed)
                {
                    Log("Setup skipped by user.");
                    return;
                }

                bool installed = await DownloadAndInstallOllamaAsync();
                if (!installed)
                {
                    LogError("Ollama installation failed. See console for details.");
                    if (!silent)
                        EditorUtility.DisplayDialog("Setup Failed",
                            "Ollama installation failed.\nCheck the Unity Console for details.", "OK");
                    return;
                }

                ollamaExe = FindOllamaExecutable();
                if (string.IsNullOrEmpty(ollamaExe))
                {
                    LogError("Ollama was installed but the executable could not be located. Restart Unity.");
                    return;
                }
            }

            Log($"Ollama executable: {ollamaExe}");

            // 2. Ensure 'ollama serve' is running
            bool serving = await IsOllamaServingAsync();
            if (!serving)
            {
                Log("Starting ollama serve...");
                StartOllamaServe(ollamaExe);
                Log($"Waiting {ServeStartupWaitMs / 1000}s for service to come up...");
                await Task.Delay(ServeStartupWaitMs);
                serving = await IsOllamaServingAsync();
            }

            if (!serving)
            {
                LogError("Ollama serve did not start in time. Try running 'ollama serve' manually and re-run setup.");
                return;
            }

            Log("Ollama service is running.");

            // 3. Ensure the model is available
            bool modelPresent = await IsModelPresentAsync();
            if (!modelPresent)
            {
                Log($"Model '{ModelName}' not found locally. Pulling now (this can take a few minutes on first run)...");
                bool pulled = await PullModelAsync(ollamaExe);
                if (!pulled)
                {
                    LogError($"Failed to pull model '{ModelName}'. Check your internet connection and try again via Tools > Ollama > Run Setup Now.");
                    return;
                }
            }
            else
            {
                Log($"Model '{ModelName}' already present.");
            }

            Log("=== Ollama Setup Complete ===");

            if (!silent)
                EditorUtility.DisplayDialog("Ollama Ready",
                    $"Ollama is installed, running, and model '{ModelName}' is available.\n\nYou can now use the AI Dungeon Master feature.", "OK");
        }
        catch (Exception ex)
        {
            LogError($"Unexpected error during setup: {ex}");
        }
    }

    private static async Task CheckStatusAsync()
    {
        string exe    = FindOllamaExecutable();
        bool serving  = await IsOllamaServingAsync();
        bool hasModel = serving && await IsModelPresentAsync();

        string msg =
            $"Ollama executable : {(string.IsNullOrEmpty(exe) ? "NOT FOUND" : exe)}\n" +
            $"Service running   : {(serving  ? "YES" : "NO")}\n" +
            $"Model '{ModelName}' : {(hasModel ? "PRESENT" : "NOT FOUND")}";

        EditorUtility.DisplayDialog("Ollama Status", msg, "OK");
        Log(msg);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>Looks for the ollama executable on PATH and in the default install location.</summary>
    private static string FindOllamaExecutable()
    {
        // Check default install path first
        if (File.Exists(DefaultInstallPath))
            return DefaultInstallPath;

        // Walk PATH
        string pathVar = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        foreach (string dir in pathVar.Split(Path.PathSeparator))
        {
            try
            {
                string candidate = Path.Combine(dir.Trim(), "ollama.exe");
                if (File.Exists(candidate))
                    return candidate;
            }
            catch { /* ignore bad PATH entries */ }
        }

        return null;
    }

    /// <summary>Downloads OllamaSetup.exe and runs it silently (/SILENT).</summary>
    private static async Task<bool> DownloadAndInstallOllamaAsync()
    {
        string tempInstaller = Path.Combine(Path.GetTempPath(), "OllamaSetup.exe");
        try
        {
            Log($"Downloading Ollama installer from {OllamaDownloadUrl} ...");
            using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
            byte[] data = await http.GetByteArrayAsync(OllamaDownloadUrl);
            File.WriteAllBytes(tempInstaller, data);
            Log($"Downloaded to {tempInstaller}");
        }
        catch (Exception ex)
        {
            LogError($"Download failed: {ex.Message}");
            return false;
        }

        try
        {
            Log("Running installer silently...");
            var psi = new ProcessStartInfo
            {
                FileName  = tempInstaller,
                Arguments = "/SILENT",   // Inno Setup silent flag (Ollama uses Inno Setup on Windows)
                UseShellExecute  = true, // Required for elevated install on some machines
                CreateNoWindow   = false
            };

            using var process = Process.Start(psi);
            if (process == null)
            {
                LogError("Could not start installer process.");
                return false;
            }

            // Wait up to 3 minutes for installer to finish
            await Task.Run(() => process.WaitForExit(180_000));
            int exitCode = process.ExitCode;
            Log($"Installer finished with exit code {exitCode}.");

            // Refresh PATH in this process (so FindOllamaExecutable picks up the new install)
            RefreshEnvironmentPath();

            return exitCode == 0 || File.Exists(DefaultInstallPath);
        }
        catch (Exception ex)
        {
            LogError($"Installer execution failed: {ex.Message}");
            return false;
        }
        finally
        {
            try { File.Delete(tempInstaller); } catch { /* best effort */ }
        }
    }

    /// <summary>Checks whether 'ollama serve' is reachable.</summary>
    private static async Task<bool> IsOllamaServingAsync()
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(HttpTimeoutSeconds) };
            HttpResponseMessage response = await http.GetAsync($"{OllamaHost}/api/tags");
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    /// <summary>Checks whether the target model is already downloaded.</summary>
    private static async Task<bool> IsModelPresentAsync()
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(HttpTimeoutSeconds) };
            HttpResponseMessage response = await http.GetAsync($"{OllamaHost}/api/tags");
            if (!response.IsSuccessStatusCode) return false;

            string body = await response.Content.ReadAsStringAsync();
            // Simple string check — avoids a full JSON parse dependency
            return body.Contains(ModelName, StringComparison.OrdinalIgnoreCase);
        }
        catch { return false; }
    }

    /// <summary>Starts 'ollama serve' as a detached background process.</summary>
    private static void StartOllamaServe(string ollamaExe)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName         = ollamaExe,
                Arguments        = "serve",
                UseShellExecute  = false,
                CreateNoWindow   = true,
                WindowStyle      = ProcessWindowStyle.Hidden
            };
            Process.Start(psi);
        }
        catch (Exception ex)
        {
            LogError($"Could not start ollama serve: {ex.Message}");
        }
    }

    /// <summary>Runs 'ollama pull <model>' and streams output to the console.</summary>
    private static async Task<bool> PullModelAsync(string ollamaExe)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName               = ollamaExe,
                Arguments              = $"pull {ModelName}",
                UseShellExecute        = false,
                CreateNoWindow         = true,
                RedirectStandardOutput = true,
                RedirectStandardError  = true
            };

            using var process = Process.Start(psi);
            if (process == null) return false;

            // Stream output so the developer can see progress in the Console
            process.OutputDataReceived += (_, e) => { if (e.Data != null) Log($"[ollama pull] {e.Data}"); };
            process.ErrorDataReceived  += (_, e) => { if (e.Data != null) Log($"[ollama pull] {e.Data}"); };
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Wait up to 30 minutes (large model download)
            await Task.Run(() => process.WaitForExit(30 * 60 * 1000));
            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            LogError($"Model pull failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Re-reads PATH from the Machine and User environment so a fresh Ollama
    /// install is found without restarting Unity.
    /// </summary>
    private static void RefreshEnvironmentPath()
    {
        try
        {
            string machinePath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine) ?? string.Empty;
            string userPath    = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User)    ?? string.Empty;
            string combined    = string.Join(";", machinePath, userPath);
            Environment.SetEnvironmentVariable("PATH", combined, EnvironmentVariableTarget.Process);
        }
        catch { /* non-critical */ }
    }

    private static void Log(string msg)      => UnityEngine.Debug.Log($"[OllamaSetup] {msg}");
    private static void LogError(string msg) => UnityEngine.Debug.LogError($"[OllamaSetup] {msg}");
}
#endif