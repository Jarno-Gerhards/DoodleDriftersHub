using UnityEngine;
using TMPro;

/// <summary>
/// Connects the client Submit button to the HostScreen.
/// Sends the drawn image together with the player-entered name.
///
/// Inspector hook-up:
///   - drawer        : DoodleDrawerPro on the client
///   - championNameInput : TMP_InputField where the player enters the name
///
/// Connect Submit button OnClick to: DrawingSubmitBridge.OnSubmit()
/// </summary>
public class DrawingSubmitBridge : MonoBehaviour
{
    [Header("Client References")]
    [SerializeField] private DoodleDrawerPro  drawer;
    [SerializeField] private TMP_InputField   championNameInput;

    [Header("Fallback name (if the player enters nothing)")]
    [SerializeField] private string fallbackName = "Unnamed Champion";

    public void OnSubmit()
    {
        if (drawer == null)
        {
            Debug.LogError("[DrawingSubmitBridge] drawer is not assigned!");
            return;
        }

        if (HostScreen.Instance == null)
        {
            Debug.LogError("[DrawingSubmitBridge] No HostScreen found in scene!");
            return;
        }

        string championName = championNameInput != null && !string.IsNullOrWhiteSpace(championNameInput.text)
            ? championNameInput.text.Trim()
            : fallbackName;

        Texture2D drawing = drawer.GetTextureWithTransparentBackground();
        HostScreen.Instance.AddDrawing(drawing, championName);

        Debug.Log($"[DrawingSubmitBridge] Submitted '{championName}'.");
    }
}