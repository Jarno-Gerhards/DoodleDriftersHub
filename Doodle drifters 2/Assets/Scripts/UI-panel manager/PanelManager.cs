using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton that manages all UI panels.
///
/// Responsibilities:
///   - Subscribes to GameLoopManager.OnStateChanged.
///   - Maps each GameStateType to one PanelType.
///   - Ensures only one panel is visible at a time.
///   - Exposes ShowPanel() / HideAll() for manual control when needed
///     (e.g. showing a loading overlay on top of the current panel).
///
/// Hook-up in Inspector:
///   Assign each PanelEntry's type + panel reference under "Panel Slots".
///   Every panel in PanelType should have exactly one entry.
///
/// Scene setup:
///   - One Canvas in the scene.
///   - Each panel is a direct child of that Canvas (or a child of a shared container).
///   - Each panel root has the matching UIPanel subclass attached.
///   - All panels start inactive — UIPanel.Awake() handles this automatically.
/// </summary>
public class PanelManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────

    public static PanelManager Instance { get; private set; }

    // ── Inspector ─────────────────────────────────────────────────────────────

    [Tooltip("Assign every panel type and its corresponding UIPanel component here.")]
    [SerializeField] private List<PanelEntry> panelSlots = new();

    // ── Internal state ────────────────────────────────────────────────────────

    private readonly Dictionary<PanelType, UIPanel> _panels   = new();
    private          PanelType                       _current  = PanelType.None;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        RegisterPanels();
    }

    private void OnEnable()
    {
        if (GameLoopManager.Instance != null)
            GameLoopManager.Instance.OnStateChanged += OnGameStateChanged;
    }

    private void OnDisable()
    {
        if (GameLoopManager.Instance != null)
            GameLoopManager.Instance.OnStateChanged -= OnGameStateChanged;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Shows the given panel and hides every other panel.
    /// Safe to call with PanelType.None — will hide everything.
    /// </summary>
    public void ShowPanel(PanelType type)
    {
        if (_current == type) return;

        HideAll();

        if (type == PanelType.None) return;

        if (!_panels.TryGetValue(type, out UIPanel panel))
        {
            Debug.LogWarning($"[PanelManager] Panel '{type}' is not registered. " +
                             $"Add it to the panelSlots list in the Inspector.");
            return;
        }

        panel.Show();
        _current = type;

        Debug.Log($"[PanelManager] Showing panel: {type}");
    }

    /// <summary>Hides all panels without showing a replacement.</summary>
    public void HideAll()
    {
        foreach (var kvp in _panels)
            if (kvp.Value.IsVisible)
                kvp.Value.Hide();

        _current = PanelType.None;
    }

    /// <summary>Returns the UIPanel instance for the given type, or null if not found.</summary>
    public UIPanel GetPanel(PanelType type)
    {
        _panels.TryGetValue(type, out UIPanel panel);
        return panel;
    }

    /// <summary>
    /// Typed convenience getter — returns the panel cast to T, or null.
    /// Example: PanelManager.Instance.GetPanel<DrawingPanel>()
    /// </summary>
    public T GetPanel<T>() where T : UIPanel
    {
        foreach (var panel in _panels.Values)
            if (panel is T typed) return typed;
        return null;
    }

    /// <summary>The PanelType that is currently visible.</summary>
    public PanelType CurrentPanel => _current;

    // ── GameState → Panel mapping ─────────────────────────────────────────────

    /// <summary>
    /// Defines which panel to show for each game state.
    /// Called automatically when GameLoopManager fires OnStateChanged.
    ///
    /// To add a mapping: add a case here and make sure the panel is registered.
    /// Returning PanelType.None for a state means no panel change happens —
    /// useful for transient states that manage their own UI internally.
    /// </summary>
    private PanelType GetPanelForState(GameStateType state)
    {
        return state switch
        {
            GameStateType.MainMenu            => PanelType.MainMenu,
            GameStateType.CharacterCreation   => PanelType.CharacterCreation,
            GameStateType.EnterDungeon        => PanelType.EnterDungeon,
            GameStateType.RoomNarration       => PanelType.RoomNarration,
            GameStateType.Drawing             => PanelType.Drawing,
            GameStateType.Voting              => PanelType.Voting,
            GameStateType.ResolutionNarration => PanelType.ResolutionNarration,
            GameStateType.BossRoom            => PanelType.BossIntro,
            GameStateType.EndScreen           => PanelType.EndScreen,
            _                                 => PanelType.None
        };
    }

    // ── Internal ──────────────────────────────────────────────────────────────

    private void OnGameStateChanged(GameStateType newState)
    {
        PanelType targetPanel = GetPanelForState(newState);

        if (targetPanel != PanelType.None)
            ShowPanel(targetPanel);
    }

    private void RegisterPanels()
    {
        _panels.Clear();

        foreach (PanelEntry entry in panelSlots)
        {
            if (entry.panel == null)
            {
                Debug.LogWarning($"[PanelManager] Panel slot '{entry.type}' has no UIPanel assigned.");
                continue;
            }

            if (_panels.ContainsKey(entry.type))
            {
                Debug.LogWarning($"[PanelManager] Duplicate panel type '{entry.type}' — skipping.");
                continue;
            }

            _panels[entry.type] = entry.panel;
        }

        Debug.Log($"[PanelManager] Registered {_panels.Count} panels.");
    }
}

// =============================================================================
//  Inspector helper struct
// =============================================================================

/// <summary>
/// One entry in PanelManager's inspector list.
/// Maps a PanelType to its UIPanel component in the scene.
/// </summary>
[Serializable]
public class PanelEntry
{
    public PanelType type;
    public UIPanel   panel;
}
