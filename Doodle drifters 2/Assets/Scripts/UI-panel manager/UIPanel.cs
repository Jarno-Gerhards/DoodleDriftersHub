using UnityEngine;

/// <summary>
/// Base class for every UI panel in the game.
///
/// Each panel is a MonoBehaviour that lives on a root GameObject under the main Canvas.
/// PanelManager calls Show() and Hide() — never SetActive() directly from outside.
///
/// Subclasses override OnShow() and OnHide() to run panel-specific setup/teardown
/// without needing to call base.Show() / base.Hide() themselves.
///
/// Usage:
///   - Attach a UIPanel subclass to a root panel GameObject.
///   - Assign that GameObject as a slot on PanelManager in the Inspector.
///   - PanelManager will call Show() / Hide() automatically based on game state.
/// </summary>
public abstract class UIPanel : MonoBehaviour
{
    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Makes this panel visible and fires OnShow().</summary>
    public void Show()
    {
        gameObject.SetActive(true);
        OnShow();
    }

    /// <summary>Hides this panel and fires OnHide().</summary>
    public void Hide()
    {
        OnHide();
        gameObject.SetActive(false);
    }

    /// <summary>Whether this panel is currently visible.</summary>
    public bool IsVisible => gameObject.activeSelf;

    // ── Overridable hooks ─────────────────────────────────────────────────────

    /// <summary>
    /// Called by Show() after the GameObject is activated.
    /// Override to populate data, start animations, reset scroll positions, etc.
    /// </summary>
    protected virtual void OnShow() { }

    /// <summary>
    /// Called by Hide() before the GameObject is deactivated.
    /// Override to stop coroutines, clean up state, etc.
    /// </summary>
    protected virtual void OnHide() { }

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    protected virtual void Awake()
    {
        // Panels start hidden — PanelManager controls visibility.
        gameObject.SetActive(false);
    }
}
