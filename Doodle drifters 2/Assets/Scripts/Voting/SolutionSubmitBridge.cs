using UnityEngine;
using TMPro;

/// <summary>
/// Client-side bridge for the solution drawing phase.
/// Collects the drawing + description from the UI and passes it to VotingManager.
///
/// This is intentionally separate from DrawingSubmitBridge (champion submission)
/// because the destination and context are different:
///   - Champion submissions → HostScreen (assembly lobby)
///   - Solution submissions → VotingManager (voting phase)
///
/// Inspector hook-up:
///   - drawer             : DoodleDrawerPro on the client drawing canvas
///   - descriptionInput   : TMP_InputField where the player describes their drawing
///   - submitButton       : wire OnClick to OnSubmit()
///
/// The local player ID is set via SetLocalPlayerId() when the player joins.
/// For now it defaults to a device-unique ID as a placeholder until
/// multiplayer assigns real network IDs.
/// </summary>
public class SolutionSubmitBridge : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DoodleDrawerPro drawer;
    [SerializeField] private TMP_InputField  descriptionInput;

    [Header("Fallbacks")]
    [SerializeField] private string fallbackDescription = "Something useful";
    [SerializeField] private string fallbackPlayerName  = "Unknown Hero";

    // Set this when the player joins (multiplayer will override this).
    private string _localPlayerId;
    private string _localPlayerName;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        // Placeholder ID until multiplayer provides a real one.
        _localPlayerId   = SystemInfo.deviceUniqueIdentifier;
        _localPlayerName = fallbackPlayerName;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Call this when the player's identity is known (e.g. after joining a room).
    /// </summary>
    public void SetLocalPlayer(string playerId, string playerName)
    {
        _localPlayerId   = playerId;
        _localPlayerName = playerName;
    }

    /// <summary>
    /// Called by the Submit button's OnClick event.
    /// Validates input, builds a SolutionSubmission, and sends it to VotingManager.
    /// </summary>
    public void OnSubmit()
    {
        if (drawer == null)
        {
            Debug.LogError("[SolutionSubmitBridge] drawer is not assigned.");
            return;
        }

        if (VotingManager.Instance == null)
        {
            Debug.LogError("[SolutionSubmitBridge] No VotingManager found in scene.");
            return;
        }

        string description = descriptionInput != null &&
                             !string.IsNullOrWhiteSpace(descriptionInput.text)
            ? descriptionInput.text.Trim()
            : fallbackDescription;

        Texture2D texture = drawer.GetTextureWithTransparentBackground();

        var submission = new SolutionSubmission(
            _localPlayerId,
            _localPlayerName,
            texture,
            description
        );

        VotingManager.Instance.RegisterSubmission(submission);

        Debug.Log($"[SolutionSubmitBridge] Submitted solution for '{_localPlayerName}'.");
    }
}
