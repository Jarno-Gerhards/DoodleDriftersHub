using UnityEngine;

/// <summary>
/// Zit op de client side. Koppelt de Submit actie aan de HostScreen.
///
/// Werking:
///   - Roept DoodleInference.Predict() aan (image recognition).
///   - Stuurt de tekening via HostScreen.AddDrawing() naar het host scherm.
///
/// Inspector hook-up:
///   - drawer      : DoodleDrawerPro op de client
///   - doodleInference : DoodleInference (optioneel — voor predict)
///   - playerName  : naam die onder het portrait komt (tijdelijk hardcoded)
///
/// Koppel de Submit knop OnClick aan: DrawingSubmitBridge.OnSubmit()
/// </summary>
public class DrawingSubmitBridge : MonoBehaviour
{
    [Header("Client References")]
    [SerializeField] private DoodleDrawerPro  drawer;
    [SerializeField] private DoodleInference  doodleInference;

    [Header("Player Info")]
    [SerializeField] private string playerName = "Player 1";

    // ── Public — gekoppeld aan Submit knop OnClick ────────────────────────────

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

        // Run image recognition if available
        if (doodleInference != null)
            doodleInference.Predict();

        // Send drawing to host screen
        Texture2D drawing = drawer.GetTextureWithTransparentBackground();
        HostScreen.Instance.AddDrawing(drawing, playerName);

        Debug.Log($"[DrawingSubmitBridge] Submitted drawing for '{playerName}'.");
    }
}