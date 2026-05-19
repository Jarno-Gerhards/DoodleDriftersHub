#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO.Compression;

[InitializeOnLoad]
public class LlamaLibInstaller
{
    // --- Config ---
    const string VERSION     = "v2.0.5";
    const string ZIP_NAME    = "LlamaLib-v2.0.5.zip"; // adjust if needed
    const string DOWNLOAD_URL = "https://github.com/undreamai/LlamaLib/releases/download/" 
                                + VERSION + "/" + ZIP_NAME;

    // Path relative to the project root (where Assets/ lives)
    static readonly string DestFolder = Path.Combine(
        Application.dataPath,
        "StreamingAssets", "LlamaLib-" + VERSION, "win-x64", "native"
    );

    // A sentinel file — if this exists, we consider LlamaLib installed
    static string SentinelFile => Path.Combine(DestFolder, "llamalib_win-x64_runtime.dll");

    // --- This runs automatically every time Unity loads the project ---
    static LlamaLibInstaller()
    {
        if (IsInstalled())
        {
            Debug.Log($"[LlamaLibInstaller] LlamaLib {VERSION} already installed. Skipping.");
            return;
        }

        Debug.Log($"[LlamaLibInstaller] LlamaLib {VERSION} not found. Starting download...");
        EditorApplication.delayCall += () => PromptAndInstall();
    }

    static bool IsInstalled() => File.Exists(SentinelFile);

    static void PromptAndInstall()
    {
        bool confirm = EditorUtility.DisplayDialog(
            "LlamaLib Missing",
            $"LlamaLib {VERSION} (~1.8 GB) is required for the in-game AI but wasn't found.\n\n" +
            "Download and install it now?",
            "Download",
            "Skip for now"
        );

        if (confirm)
            _ = DownloadAndInstall();
    }

    static async Task DownloadAndInstall()
    {
        // Find setup.ps1 relative to the project root
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string scriptPath = Path.Combine(projectRoot, "..", "setup.ps1"); // adjust if needed

        if (!File.Exists(scriptPath))
        {
            EditorUtility.DisplayDialog("LlamaLib Installer Failed",
                "setup.ps1 not found. Please run it manually from the repo root.",
                "OK");
            return;
        }

        EditorUtility.DisplayProgressBar("LlamaLib Installer", 
            "Running setup.ps1... This may take a while for 1.7 GB", 0.1f);

        try
        {
            var process = new System.Diagnostics.Process();
            process.StartInfo.FileName = "powershell.exe";
            process.StartInfo.Arguments = $"-ExecutionPolicy Bypass -File \"{scriptPath}\"";
            process.StartInfo.UseShellExecute = true; // shows a PowerShell window with progress
            process.StartInfo.CreateNoWindow = false;
            process.Start();

            // Wait for it to finish without blocking Unity's main thread
            await Task.Run(() => process.WaitForExit());

            EditorUtility.ClearProgressBar();

            if (IsInstalled())
            {
                EditorUtility.DisplayDialog("LlamaLib Installer",
                    $"LlamaLib {VERSION} installed successfully!", "OK");
                AssetDatabase.Refresh();
                Debug.Log($"[LlamaLibInstaller] LlamaLib {VERSION} installed at: {DestFolder}");
            }
            else
            {
                EditorUtility.DisplayDialog("LlamaLib Installer Failed",
                    "setup.ps1 ran but LlamaLib wasn't found afterwards.\n\nCheck the PowerShell window for errors.",
                    "OK");
            }
        }
        catch (System.Exception e)
        {
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("LlamaLib Installer Failed",
                $"Failed to launch setup.ps1:\n\n{e.Message}",
                "OK");
            Debug.LogError($"[LlamaLibInstaller] Failed to launch setup.ps1: {e}");
        }
    }   

    // --- Manual trigger via Unity menu ---
    [MenuItem("Tools/Install LlamaLib")]
    static void ManualInstall()
    {
        if (IsInstalled())
        {
            bool reinstall = EditorUtility.DisplayDialog("LlamaLib Installer",
                $"LlamaLib {VERSION} is already installed.\n\nReinstall?",
                "Reinstall", "Cancel");
            if (!reinstall) return;
            Directory.Delete(DestFolder, true);
        }
        _ = DownloadAndInstall();
    }
}
#endif
