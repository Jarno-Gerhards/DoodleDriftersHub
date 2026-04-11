using UnityEngine;
using TMPro;

/// <summary>
/// Koppelt de client Submit knop aan de HostScreen.
/// Stuurt de getekende afbeelding + de door de speler ingetypte naam door.
///
/// Inspector hook-up:
///   - drawer        : DoodleDrawerPro op de client
///   - championNameInput : TMP_InputField waarin de speler de naam typt
///
/// Koppel Submit knop OnClick aan: DrawingSubmitBridge.OnSubmit()
/// </summary>
public class DrawingSubmitBridge : MonoBehaviour
{
    [Header("Client References")]
    [SerializeField] private DoodleDrawerPro  drawer;
    [SerializeField] private TMP_InputField   championNameInput;

    [Header("Fallback naam (als speler niks intypt)")]
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

        // Gebruik ingetypte naam, of fallback als het veld leeg is
        string championName = championNameInput != null && !string.IsNullOrWhiteSpace(championNameInput.text)
            ? championNameInput.text.Trim()
            : fallbackName;

        Texture2D drawing = drawer.GetTextureWithTransparentBackground();
        HostScreen.Instance.AddDrawing(drawing, championName);

        Debug.Log($"[DrawingSubmitBridge] Submitted '{championName}'.");
    }
}