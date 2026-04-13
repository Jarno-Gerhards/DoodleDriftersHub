// =============================================================================
//  UI Panel stubs
//
//  Each class below is a placeholder for a real panel.
//  When a teammate picks up a panel, they:
//    1. Add [SerializeField] fields for the UI elements on that panel.
//    2. Fill in OnShow() with setup logic (populate text, reset state, etc.).
//    3. Fill in OnHide() with teardown logic if needed.
//    4. Add public methods the GameStates can call (e.g. SetScenarioText()).
//
//  Every panel is already wired into PanelManager via the Inspector slot list —
//  no code changes needed in PanelManager when implementing a panel.
// =============================================================================

using UnityEngine;

// ── Main Menu ─────────────────────────────────────────────────────────────────

/// <summary>
/// Panel: Main Menu
/// Shows title, create-room button, and game settings.
///
/// TODO:
///   - Add room code display field.
///   - Add settings sliders (totalRooms, drawing timer, etc.).
///   - "Start Adventure" button → GameLoopManager.Instance.ChangeState(CharacterCreation)
///     (or let the host confirm settings first).
/// </summary>
public class MainMenuPanel : UIPanel
{
    protected override void OnShow()
    {
        Debug.Log("[MainMenuPanel] Shown");
        // TODO: Reset any leftover state from a previous session.
    }

    protected override void OnHide()
    {
        Debug.Log("[MainMenuPanel] Hidden");
    }
}

// ── Character Creation ────────────────────────────────────────────────────────

/// <summary>
/// Panel: Character Creation
/// Clients see the drawing canvas. Host sees the assembly screen.
///
/// TODO:
///   - Show DoodleDrawerPro canvas for clients.
///   - Show "Waiting for players..." overlay on host.
///   - Submit button calls DrawingSubmitBridge.OnSubmit().
///   - Name input field for the champion name.
///   - Progress indicator: "X / Y players ready".
/// </summary>
public class CharacterCreationPanel : UIPanel
{
    protected override void OnShow()
    {
        Debug.Log("[CharacterCreationPanel] Shown");
        // TODO: Clear the canvas (DoodleDrawerPro.ClearCanvas()).
        // TODO: Clear the name input field.
    }

    protected override void OnHide()
    {
        Debug.Log("[CharacterCreationPanel] Hidden");
    }
}

// ── Enter Dungeon ─────────────────────────────────────────────────────────────

/// <summary>
/// Panel: Enter Dungeon (optional cinematic)
/// Characters walk into the dungeon entrance.
///
/// TODO:
///   - Play character walk animation.
///   - Animation end callback → GameLoopManager.Instance.StartFirstRoom().
///   - Can be skipped entirely — EnterDungeonState auto-advances after a timer.
/// </summary>
public class EnterDungeonPanel : UIPanel
{
    protected override void OnShow()
    {
        Debug.Log("[EnterDungeonPanel] Shown");
        // TODO: Start walk animation.
    }

    protected override void OnHide()
    {
        Debug.Log("[EnterDungeonPanel] Hidden");
    }
}

// ── Room Narration ────────────────────────────────────────────────────────────

/// <summary>
/// Panel: Room Narration
/// Host screen shows the scenario text and AI-generated room images.
/// Clients see a "Get ready..." screen while the host narrates.
///
/// TODO:
///   - SetScenarioText(string text) — called by RoomNarrationState after AI responds.
///   - Display AI-generated images alongside the text.
///   - Show TTS progress indicator ("Dungeon Master is speaking...").
/// </summary>
public class RoomNarrationPanel : UIPanel
{
    protected override void OnShow()
    {
        Debug.Log("[RoomNarrationPanel] Shown");
        // TODO: Clear previous scenario text and images.
    }

    protected override void OnHide()
    {
        Debug.Log("[RoomNarrationPanel] Hidden");
    }

    /// <summary>Called by RoomNarrationState once the AI returns the scenario text.</summary>
    public void SetScenarioText(string text)
    {
        Debug.Log($"[RoomNarrationPanel] Scenario: {text}");
        // TODO: Assign text to TextMeshPro field.
    }
}

// ── Drawing ───────────────────────────────────────────────────────────────────

/// <summary>
/// Panel: Drawing Phase
/// Clients draw their solution. Timer counts down.
/// Host sees a "Waiting for drawings..." screen.
///
/// TODO:
///   - Show scenario text above the canvas so players remember what they're solving.
///   - Show countdown timer (updated every second by DrawingState).
///   - Submit button calls DrawingSubmitBridge.OnSubmit() + name input.
///   - After submit: swap canvas for "Waiting for other players..." message.
/// </summary>
public class DrawingPanel : UIPanel
{
    protected override void OnShow()
    {
        Debug.Log("[DrawingPanel] Shown");
        // TODO: Clear canvas.
        // TODO: Populate scenario text from GameLoopManager.Instance.CurrentRoom.ScenarioText.
    }

    protected override void OnHide()
    {
        Debug.Log("[DrawingPanel] Hidden");
    }

    /// <summary>Called every second by DrawingState to update the countdown display.</summary>
    public void SetTimerDisplay(int secondsRemaining)
    {
        // TODO: Update timer TextMeshPro field.
    }
}

// ── Voting ────────────────────────────────────────────────────────────────────

/// <summary>
/// Panel: Voting Phase
/// Host screen shows all submitted drawings in a grid.
/// Clients see voting options (all drawings except their own).
///
/// TODO:
///   - PopulateDrawings(List<PlayerDrawing>) — spawns a card prefab per drawing.
///   - Each card shows: drawing image + label below.
///   - Clients cannot see or vote for their own drawing.
///   - Selecting a card calls VotingState.RegisterVote(localPlayerId, chosenPlayerId).
///   - After voting: swap to "Waiting for other players..." message.
/// </summary>
public class VotingPanel : UIPanel
{
    protected override void OnShow()
    {
        Debug.Log("[VotingPanel] Shown");
        // TODO: Call PopulateDrawings() using GameLoopManager.Instance.CurrentRoom.Drawings.
    }

    protected override void OnHide()
    {
        Debug.Log("[VotingPanel] Hidden");
        // TODO: Destroy spawned drawing cards.
    }
}

// ── Resolution Narration ──────────────────────────────────────────────────────

/// <summary>
/// Panel: Resolution Narration
/// Host screen shows the winning drawing and the AI-generated outcome narrative.
/// Win and fail outcomes have different visual treatment (color, animation).
///
/// TODO:
///   - ShowWinningDrawing(PlayerDrawing) — display the winning drawing prominently.
///   - SetResolutionText(string text) — called after AI responds.
///   - SetOutcome(RoomOutcome) — visually distinguish win vs fail.
///   - Show TTS progress indicator.
/// </summary>
public class ResolutionNarrationPanel : UIPanel
{
    protected override void OnShow()
    {
        Debug.Log("[ResolutionNarrationPanel] Shown");
        // TODO: Clear previous resolution content.
    }

    protected override void OnHide()
    {
        Debug.Log("[ResolutionNarrationPanel] Hidden");
    }

    /// <summary>Called by ResolutionNarrationState with the winning drawing.</summary>
    public void ShowWinningDrawing(PlayerDrawing drawing)
    {
        Debug.Log($"[ResolutionNarrationPanel] Winning drawing: {drawing?.PlayerName}");
        // TODO: Assign drawing.Texture to a RawImage.
        // TODO: Show drawing.PlayerName as caption.
    }

    /// <summary>Called by ResolutionNarrationState once the AI returns the outcome text.</summary>
    public void SetResolutionText(string text)
    {
        Debug.Log($"[ResolutionNarrationPanel] Resolution: {text}");
        // TODO: Assign text to TextMeshPro field.
    }

    /// <summary>Called to visually reflect whether the room was won or lost.</summary>
    public void SetOutcome(RoomOutcome outcome)
    {
        Debug.Log($"[ResolutionNarrationPanel] Outcome: {outcome}");
        // TODO: Change background color / show win or fail banner.
    }
}

// ── Boss Intro ────────────────────────────────────────────────────────────────

/// <summary>
/// Panel: Boss Intro
/// Special intro screen shown before the boss room narration starts.
/// Makes the boss room feel distinct from normal rooms.
///
/// TODO:
///   - Show boss name / ominous intro animation.
///   - "BOSS ROOM" badge with dragon fire styling (see color palette).
///   - Auto-advance or button to continue to RoomNarration.
/// </summary>
public class BossIntroPanel : UIPanel
{
    protected override void OnShow()
    {
        Debug.Log("[BossIntroPanel] Shown");
        // TODO: Play boss intro animation.
        // TODO: Auto-advance after animation → GameLoopManager.Instance.ChangeState(RoomNarration).
    }

    protected override void OnHide()
    {
        Debug.Log("[BossIntroPanel] Hidden");
    }
}

// ── End Screen ────────────────────────────────────────────────────────────────

/// <summary>
/// Panel: End Screen
/// Shows the final results after the boss room is resolved.
///
/// TODO:
///   - Display all player characters with their names.
///   - Show overall win/loss result.
///   - "Play Again" button → GameLoopManager.Instance.ChangeState(MainMenu).
/// </summary>
public class EndScreenPanel : UIPanel
{
    protected override void OnShow()
    {
        Debug.Log("[EndScreenPanel] Shown");
        // TODO: Populate results from session data.
    }

    protected override void OnHide()
    {
        Debug.Log("[EndScreenPanel] Hidden");
    }
}
